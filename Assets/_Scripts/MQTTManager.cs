using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;
using System.Text;
using System.Collections.Generic;

public class MQTTManager : MonoBehaviour
{
    [Header("MQTT 配置")]
    public string brokerAddress = "broker.emqx.io";
    public int brokerPort = 1883;
    public string subscribeTopic = "23VRteamA/#";

    [Header("调试")]
    public string lastReceivedMessage = "";
    public bool showDebugLog = true;

    [Header("重连设置")]
    public float reconnectInterval = 5f;
    public bool autoReconnect = true;

    private MqttClient client;
    private float reconnectTimer;
    private bool isReconnecting;

    private Queue<string> messageQueue = new Queue<string>();
    private object queueLock = new object();

    private DataProcessor dataProcessor;

    public DataProcessor DataProcessor
    {
        get { return dataProcessor; }
    }

    public static MQTTManager Instance;

    public delegate void SensorStateHandler(SensorState state);
    public event SensorStateHandler OnSensorStateReceived;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        dataProcessor = new DataProcessor();
    }

    void Start()
    {
        Connect();
    }

    void Connect()
    {
        try
        {
            client = new MqttClient(
                brokerAddress,
                brokerPort,
                false,
                null,
                null,
                MqttSslProtocols.None
            );

            string clientId = Guid.NewGuid().ToString();

            client.Connect(clientId);

            client.MqttMsgPublishReceived += OnMessageReceived;

            client.Subscribe(
                new string[] { subscribeTopic },
                new byte[]
                {
                    MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE
                }
            );

            Debug.Log(
                $"[MQTT] 已连接 " +
                $"{brokerAddress}:{brokerPort}"
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[MQTT] 连接失败: {e.Message}"
            );
        }
    }

    void OnMessageReceived(
        object sender,
        MqttMsgPublishEventArgs e)
    {
        string msg = Encoding.UTF8.GetString(e.Message);

        lock (queueLock)
        {
            messageQueue.Enqueue(msg);
        }
    }

    void Update()
    {
        while (true)
        {
            string msg = null;

            lock (queueLock)
            {
                if (messageQueue.Count == 0)
                {
                    break;
                }

                msg = messageQueue.Dequeue();
            }

            ProcessMessage(msg);
        }

        if (autoReconnect && !isReconnecting)
        {
            if (client == null || !client.IsConnected)
            {
                reconnectTimer += Time.deltaTime;

                if (reconnectTimer >= reconnectInterval)
                {
                    reconnectTimer = 0f;
                    TryReconnect();
                }
            }
            else
            {
                reconnectTimer = 0f;
            }
        }
    }

    void TryReconnect()
    {
        if (isReconnecting)
        {
            return;
        }

        isReconnecting = true;

        Debug.Log(
            $"[MQTT] 尝试重连 " +
            $"{brokerAddress}:{brokerPort}"
        );

        try
        {
            if (client != null)
            {
                try
                {
                    client.MqttMsgPublishReceived -= OnMessageReceived;
                }
                catch
                {
                }

                client = null;
            }

            client = new MqttClient(
                brokerAddress,
                brokerPort,
                false,
                null,
                null,
                MqttSslProtocols.None
            );

            string clientId = Guid.NewGuid().ToString();

            client.Connect(clientId);

            client.MqttMsgPublishReceived += OnMessageReceived;

            client.Subscribe(
                new string[] { subscribeTopic },
                new byte[]
                {
                    MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE
                }
            );

            Debug.Log(
                $"[MQTT] 重连成功 " +
                $"{brokerAddress}:{brokerPort}"
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                $"[MQTT] 重连失败: {e.Message}"
            );
        }
        finally
        {
            isReconnecting = false;
        }
    }

    void ProcessMessage(string json)
    {
        lastReceivedMessage = json;

        try
        {
            SensorDataJson raw =
                JsonUtility.FromJson<SensorDataJson>(json);

            if (raw == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(raw.sensor_id))
            {
                return;
            }

            if (string.IsNullOrEmpty(raw.warehouse))
            {
                return;
            }

            SensorData data = new SensorData();

            data.sensor_id = raw.sensor_id;
            data.warehouse = raw.warehouse;
            data.temperature = raw.temperature;
            data.humidity = raw.humidity;
            data.timestamp = raw.timestamp;

            if (raw.warehouse == "apple")
            {
                data.co2 = raw.co2;
            }
            else
            {
                data.co2 = null;
            }

            if (showDebugLog)
            {
                Debug.Log(
                    $"[MQTT] 收到 {data.sensor_id}: " +
                    $"T={data.temperature}, " +
                    $"H={data.humidity}"
                );
            }

            /*
             * C 的职责到这里完成：
             *
             * MQTT JSON
             *     ↓
             * SensorData
             *
             * 下面交给 D。
             */
            SensorState state =
                dataProcessor.Process(data);

            if (state == null)
            {
                if (showDebugLog)
                {
                    Debug.LogWarning(
                        $"[MQTT] 数据未通过业务处理: " +
                        $"{data.sensor_id}"
                    );
                }

                return;
            }

            /*
             * D 输出 SensorState
             *
             * A / B 只接收这个结果，
             * 不再自己进行阈值判定。
             */
            OnSensorStateReceived?.Invoke(state);
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                $"[MQTT] 解析失败: " +
                $"{e.Message}\n{json}"
            );
        }
    }

    public void SendControlCommand(
        DeviceControlCommand cmd)
    {
        if (cmd == null)
        {
            return;
        }

        if (client == null || !client.IsConnected)
        {
            Debug.LogError(
                "[MQTT] 未连接，无法发送控制指令"
            );

            return;
        }

        if (string.IsNullOrEmpty(cmd.Warehouse) ||
            string.IsNullOrEmpty(cmd.Device))
        {
            return;
        }

        string topic =
            $"23VRteamA/control/" +
            $"{cmd.Warehouse}/" +
            $"{cmd.Device}";

        /*
         * DeviceControlCommand 是 D → C 的内部数据模型。
         *
         * C 负责把它转换成 MQTT 协议格式。
         */
        string action =
            cmd.IsOn ? "on" : "off";

        string json;

        if (cmd.TargetTemperature.HasValue)
        {
            json =
                "{"
                + $"\"Warehouse\":\"{cmd.Warehouse}\","
                + $"\"SensorId\":\"{cmd.SensorId}\","
                + $"\"Device\":\"{cmd.Device}\","
                + $"\"IsOn\":{cmd.IsOn.ToString().ToLower()},"
                + $"\"TargetTemperature\":{cmd.TargetTemperature.Value}"
                + "}";
        }
        else
        {
            json =
                "{"
                + $"\"Warehouse\":\"{cmd.Warehouse}\","
                + $"\"SensorId\":\"{cmd.SensorId}\","
                + $"\"Device\":\"{cmd.Device}\","
                + $"\"IsOn\":{cmd.IsOn.ToString().ToLower()}"
                + "}";
        }

        client.Publish(
            topic,
            Encoding.UTF8.GetBytes(json),
            MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE,
            false
        );

        Debug.Log(
            $"[MQTT] 发送控制 [{topic}]: " +
            $"{json}"
        );
    }

    void OnDestroy()
    {
        if (client != null && client.IsConnected)
        {
            client.Disconnect();
        }
    }
}
