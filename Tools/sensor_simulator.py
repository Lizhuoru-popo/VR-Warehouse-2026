import paho.mqtt.client as mqtt
import json
import time
import random
import threading

BROKER = "broker.emqx.io"
PORT = 1883

SENSORS = {
    "frozen_meat": ["L01", "M01", "R01"],
    "apple": ["L02", "M02", "R02"],
    "chocolate": ["L03", "M03", "R03"]
}

SIMULATION_MODE = {
    "frozen_meat": {sid: {"temperature": "normal_data", "humidity": "normal_data"}
                    for sid in ["L01", "M01", "R01"]},
    "apple": {sid: {"temperature": "normal_data", "humidity": "normal_data", "co2": "normal_data"}
              for sid in ["L02", "M02", "R02"]},
    "chocolate": {sid: {"temperature": "normal_data", "humidity": "normal_data"}
                  for sid in ["L03", "M03", "R03"]}
}

VALID_MODES = {"normal_data", "warning_data", "abnormal_data"}

DEVICE_PARAM = {
    "ac": "temperature",
    "ventilator": "co2",
    "dehumidifier": "humidity",
}

PARAM_DEVICE = {
    "temperature": "ac",
    "co2": "ventilator",
    "humidity": "dehumidifier",
}

DEVICE_STATE = {
    warehouse: {
        sid: {"ac": False, "ventilator": False, "dehumidifier": False}
        for sid in SENSORS[warehouse]
    }
    for warehouse in SENSORS
}

TRANSITION_ALPHA = 0.3

CURRENT_VALUES = {}

ABNORMAL_DIRECTION = {}

CALLBACK_TARGETS = {}

NORMAL_RANGES = {
    "frozen_meat": {"temperature": (-25, -18.1), "humidity": (90, 95)},
    "apple": {"temperature": (-0.9, 0.9), "humidity": (85.1, 94.9), "co2": (0.2, 0.9)},
    "chocolate": {"temperature": (18, 20), "humidity": (45.1, 54.9)},
}


def _create_client():
    try:
        return mqtt.Client(mqtt.CallbackAPIVersion.VERSION2)
    except (AttributeError, TypeError):
        return mqtt.Client()


client = _create_client()


def on_connect(client, userdata, flags, reason_code, properties=None):
    if reason_code == 0 or (hasattr(reason_code, 'is_failure') and not reason_code.is_failure):
        print("[MQTT] 已连接：", BROKER)
        client.subscribe("23VRteamA/control/#")
        print("[MQTT] 已订阅：23VRteamA/control/#")
    else:
        print("[MQTT] 连接失败，代码：", reason_code)


def on_message(client, userdata, msg):
    try:
        data = json.loads(msg.payload.decode("utf-8"))
        is_on = data.get("IsOn", False)
        warehouse = data.get("Warehouse", "")
        sensor_id = data.get("SensorId", "")
        device = data.get("Device", "")
        target_temp = data.get("TargetTemperature", None)

        print(f"\n[控制] {msg.topic} -> {data}")

        if warehouse not in DEVICE_STATE:
            print(f"[控制] 未知仓库：{warehouse}")
            return

        param = DEVICE_PARAM.get(device)

        # 确定受影响的传感器列表
        if sensor_id and sensor_id in DEVICE_STATE[warehouse]:
            affected_sids = [sensor_id]
            scope = f"{warehouse}/{sensor_id}"
        else:
            affected_sids = list(DEVICE_STATE[warehouse].keys())
            scope = warehouse

        if is_on is True:
            # 设备开启：标记每个受影响传感器的设备状态 + 对应参数恢复正常
            for sid in affected_sids:
                DEVICE_STATE[warehouse][sid][device] = True

            if param is not None:
                for sid in affected_sids:
                    if param in SIMULATION_MODE[warehouse][sid]:
                        SIMULATION_MODE[warehouse][sid][param] = "normal_data"
                        key = f"{warehouse}/{sid}/{param}"
                        if key in ABNORMAL_DIRECTION:
                            del ABNORMAL_DIRECTION[key]
                print(f"[恢复] {scope}/{device} 开启，{param} 回调正常")
            else:
                for sid in affected_sids:
                    for parameter in SIMULATION_MODE[warehouse][sid]:
                        SIMULATION_MODE[warehouse][sid][parameter] = "normal_data"
                        key = f"{warehouse}/{sid}/{parameter}"
                        if key in ABNORMAL_DIRECTION:
                            del ABNORMAL_DIRECTION[key]
                print(f"[恢复] {scope}/{device} 开启，全部传感器恢复正常")
        elif is_on is False:
            # 设备关闭：标记状态，参数维持正常（不回退）
            for sid in affected_sids:
                DEVICE_STATE[warehouse][sid][device] = False

            if param is not None:
                print(f"[设备] {scope}/{device} 已关闭，{param} 维持正常状态")
            else:
                print(f"[设备] {scope}/{device} 已关闭")

        if target_temp is not None:
            print(f"[控制] TargetTemperature={target_temp}")
    except Exception as e:
        print("[控制] 解析失败：", e)


client.on_connect = on_connect
client.on_message = on_message


def generate_parameter(warehouse, parameter, mode, direction=None):
    if warehouse == "frozen_meat":
        if parameter == "temperature":
            if mode == "normal_data": return random.uniform(-25, -18.1)
            if mode == "warning_data": return random.uniform(-17.9, -15.1)
            if mode == "abnormal_data": return random.uniform(-14.9, -5)
        if parameter == "humidity":
            if mode == "normal_data": return random.uniform(90, 95)
            if mode == "warning_data":
                if direction == "below":
                    return random.uniform(85, 89.9)
                elif direction == "above":
                    return random.uniform(95.1, 98)
                else:
                    return random.choice([random.uniform(85, 89.9), random.uniform(95.1, 98)])
            if mode == "abnormal_data":
                if direction == "below":
                    return random.uniform(70, 84.9)
                elif direction == "above":
                    return random.uniform(98.1, 100)
                else:
                    return random.choice([random.uniform(70, 84.9), random.uniform(98.1, 100)])

    if warehouse == "apple":
        if parameter == "temperature":
            if mode == "normal_data": return random.uniform(-0.9, 0.9)
            if mode == "warning_data":
                if direction == "below":
                    return random.uniform(-1.9, -1.01)
                elif direction == "above":
                    return random.uniform(1.01, 2.9)
                else:
                    return random.choice([random.uniform(-1.9, -1.01), random.uniform(1.01, 2.9)])
            if mode == "abnormal_data":
                if direction == "below":
                    return random.uniform(-8, -2.1)
                elif direction == "above":
                    return random.uniform(3.1, 8)
                else:
                    return random.choice([random.uniform(-8, -2.1), random.uniform(3.1, 8)])
        if parameter == "humidity":
            if mode == "normal_data": return random.uniform(85.1, 94.9)
            if mode == "warning_data":
                if direction == "below":
                    return random.uniform(80, 84.9)
                elif direction == "above":
                    return random.uniform(95.1, 97)
                else:
                    return random.choice([random.uniform(80, 84.9), random.uniform(95.1, 97)])
            if mode == "abnormal_data":
                if direction == "below":
                    return random.uniform(60, 79.9)
                elif direction == "above":
                    return random.uniform(97.1, 100)
                else:
                    return random.choice([random.uniform(60, 79.9), random.uniform(97.1, 100)])
        if parameter == "co2":
            if mode == "normal_data": return random.uniform(0.2, 0.9)
            if mode == "warning_data": return random.uniform(1.1, 1.9)
            if mode == "abnormal_data": return random.uniform(2.1, 4)

    if warehouse == "chocolate":
        if parameter == "temperature":
            if mode == "normal_data": return random.uniform(18, 20)
            if mode == "warning_data":
                if direction == "below":
                    return random.uniform(15.1, 17.9)
                elif direction == "above":
                    return random.uniform(20.1, 22.9)
                else:
                    return random.choice([random.uniform(15.1, 17.9), random.uniform(20.1, 22.9)])
            if mode == "abnormal_data":
                if direction == "below":
                    return random.uniform(5, 14.9)
                elif direction == "above":
                    return random.uniform(23.1, 28)
                else:
                    return random.choice([random.uniform(5, 14.9), random.uniform(23.1, 28)])
        if parameter == "humidity":
            if mode == "normal_data": return random.uniform(45.1, 54.9)
            if mode == "warning_data": return random.uniform(55.1, 64.9)
            if mode == "abnormal_data": return random.uniform(65.1, 85)
    return None


def generate_value(warehouse, sensor_id, param):
    key = f"{warehouse}/{sensor_id}/{param}"
    mode = SIMULATION_MODE[warehouse][sensor_id][param]
    device = PARAM_DEVICE.get(param)
    device_on = bool(device and DEVICE_STATE.get(warehouse, {}).get(sensor_id, {}).get(device, False))
    normal_range = NORMAL_RANGES.get(warehouse, {}).get(param)

    if device_on and normal_range is not None:
        if key not in CALLBACK_TARGETS:
            CALLBACK_TARGETS[key] = random.uniform(normal_range[0], normal_range[1])

        target = CALLBACK_TARGETS[key]
        current = CURRENT_VALUES.get(key)

        if current is None:
            current = generate_parameter(warehouse, param, mode, ABNORMAL_DIRECTION.get(key))

        new_value = current + (target - current) * TRANSITION_ALPHA

        range_span = normal_range[1] - normal_range[0]
        noise = random.gauss(0, range_span * 0.01)
        new_value += noise

        new_value = max(normal_range[0] - range_span * 0.05,
                        min(normal_range[1] + range_span * 0.05, new_value))

        CURRENT_VALUES[key] = new_value
        return new_value
    else:
        direction = None
        if mode in ("warning_data", "abnormal_data"):
            if key not in ABNORMAL_DIRECTION:
                ABNORMAL_DIRECTION[key] = random.choice(["above", "below"])
            direction = ABNORMAL_DIRECTION[key]

        value = generate_parameter(warehouse, param, mode, direction)
        CURRENT_VALUES[key] = value

        if mode == "normal_data" and key in CALLBACK_TARGETS:
            del CALLBACK_TARGETS[key]

        return value


def generate_data(warehouse, sensor_id):
    data = {
        "sensor_id": sensor_id,
        "warehouse": warehouse,
        "temperature": round(generate_value(warehouse, sensor_id, "temperature"), 2),
        "humidity": round(generate_value(warehouse, sensor_id, "humidity"), 2),
        "timestamp": int(time.time())
    }
    if warehouse == "apple":
        data["co2"] = round(generate_value(warehouse, sensor_id, "co2"), 2)
    return data


def print_modes():
    print("\n当前模拟状态：")
    for warehouse, sensors in SIMULATION_MODE.items():
        print(f"  [{warehouse}]")
        for sid, params in sensors.items():
            mode_str = ", ".join(f"{p}={m}" for p, m in params.items())
            dev = DEVICE_STATE[warehouse][sid]
            dev_str = ", ".join(f"{d}={'开启' if s else '关闭'}" for d, s in dev.items())
            print(f"    {sid}: {mode_str} | 设备: {dev_str}")


def set_parameter_mode(warehouse, sensor_id, parameter, mode):
    if warehouse not in SIMULATION_MODE or sensor_id not in SIMULATION_MODE[warehouse]:
        print("[错误] 仓库或传感器 ID 不存在")
        return
    if parameter not in SIMULATION_MODE[warehouse][sensor_id]:
        print(f"[错误] {sensor_id} 不支持参数 {parameter}")
        return
    if mode not in VALID_MODES:
        print("[错误] 状态只能是 normal_data / warning_data / abnormal_data")
        return
    SIMULATION_MODE[warehouse][sensor_id][parameter] = mode
    key = f"{warehouse}/{sensor_id}/{parameter}"
    if mode in ("warning_data", "abnormal_data"):
        if key not in ABNORMAL_DIRECTION:
            ABNORMAL_DIRECTION[key] = random.choice(["above", "below"])
        print(f"[设置] {warehouse}/{sensor_id}/{parameter} -> {mode} (方向: {ABNORMAL_DIRECTION[key]})")
    else:
        if key in ABNORMAL_DIRECTION:
            del ABNORMAL_DIRECTION[key]
        if key in CALLBACK_TARGETS:
            del CALLBACK_TARGETS[key]
        print(f"[设置] {warehouse}/{sensor_id}/{parameter} -> {mode}")


def batch_set(warehouse, parameter, mode):
    if warehouse not in SIMULATION_MODE or mode not in VALID_MODES:
        print("[错误] 仓库或状态无效")
        return
    for sensor in SIMULATION_MODE[warehouse].values():
        if parameter not in sensor:
            print(f"[错误] {warehouse} 不支持参数 {parameter}")
            return
    for sid, sensor in SIMULATION_MODE[warehouse].items():
        sensor[parameter] = mode
        key = f"{warehouse}/{sid}/{parameter}"
        if mode in ("warning_data", "abnormal_data"):
            if key not in ABNORMAL_DIRECTION:
                ABNORMAL_DIRECTION[key] = random.choice(["above", "below"])
        else:
            if key in ABNORMAL_DIRECTION:
                del ABNORMAL_DIRECTION[key]
            if key in CALLBACK_TARGETS:
                del CALLBACK_TARGETS[key]
    print(f"[联动] {warehouse} 全部传感器的 {parameter} -> {mode}")


def reset_sensor(warehouse, sensor_id):
    if warehouse not in SIMULATION_MODE or sensor_id not in SIMULATION_MODE[warehouse]:
        print("[错误] 仓库或传感器 ID 不存在")
        return
    for parameter in SIMULATION_MODE[warehouse][sensor_id]:
        SIMULATION_MODE[warehouse][sensor_id][parameter] = "normal_data"
        key = f"{warehouse}/{sensor_id}/{parameter}"
        if key in ABNORMAL_DIRECTION:
            del ABNORMAL_DIRECTION[key]
        if key in CALLBACK_TARGETS:
            del CALLBACK_TARGETS[key]
    print(f"[恢复] {warehouse}/{sensor_id}")


def reset_warehouse(warehouse):
    if warehouse not in SIMULATION_MODE:
        print("[错误] 仓库不存在")
        return
    for sid, sensor in SIMULATION_MODE[warehouse].items():
        for parameter in sensor:
            sensor[parameter] = "normal_data"
            key = f"{warehouse}/{sid}/{parameter}"
            if key in ABNORMAL_DIRECTION:
                del ABNORMAL_DIRECTION[key]
            if key in CALLBACK_TARGETS:
                del CALLBACK_TARGETS[key]
    print(f"[恢复] {warehouse} 全部传感器")


def command_thread():
    print("\n命令：")
    print("  模式 仓库 传感器 参数 状态")
    print("    例：模式 apple L02 temperature abnormal_data")
    print("  联动 仓库 参数 状态")
    print("    例：联动 apple temperature abnormal_data")
    print("  恢复传感器 仓库 传感器")
    print("  恢复仓库 仓库")
    print("  状态")
    print("  退出")
    print("")
    print("  闭环说明：")
    print("    空调(ac) → 温度 | 除湿(dehumidifier) → 湿度 | 通风(ventilator) → CO2")
    print("    设备开启时：指数衰减过渡到正常（先快后慢）")
    print("    设备关闭后：对应参数维持正常（不回退）")

    while True:
        try:
            parts = input("> ").strip().split()
            if not parts:
                continue
            if parts[0] in ("模式", "mode") and len(parts) == 5:
                set_parameter_mode(*parts[1:])
            elif parts[0] == "联动" and len(parts) == 4:
                batch_set(*parts[1:])
            elif parts[0] == "恢复传感器" and len(parts) == 3:
                reset_sensor(parts[1], parts[2])
            elif parts[0] == "恢复仓库" and len(parts) == 2:
                reset_warehouse(parts[1])
            elif parts[0] in ("状态", "list"):
                print_modes()
            elif parts[0] in ("退出", "exit"):
                break
            else:
                print("[错误] 命令格式不正确")
        except (EOFError, KeyboardInterrupt):
            break
        except Exception as e:
            print("[错误]", e)


def publish_loop():
    while True:
        for warehouse, sensors in SENSORS.items():
            for sid in sensors:
                topic = f"23VRteamA/sensor/{warehouse}/{sid}"
                data = generate_data(warehouse, sid)
                client.publish(topic, json.dumps(data, ensure_ascii=False), qos=0)
                print(f"[{sid}] {data}")
        print("-" * 70)
        time.sleep(3)


if __name__ == "__main__":
    try:
        client.connect(BROKER, PORT, 60)
        client.loop_start()
        threading.Thread(target=command_thread, daemon=True).start()
        publish_loop()
    except KeyboardInterrupt:
        print("\n模拟器已停止")
    finally:
        client.loop_stop()
        client.disconnect()
