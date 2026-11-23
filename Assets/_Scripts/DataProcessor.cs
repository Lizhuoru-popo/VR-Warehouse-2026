public class DataProcessor
{
    /// <summary>
    /// ����һ�����������ݡ�
    /// ��ɲ���״̬�жϣ����������յĴ�����״̬��
    /// </summary>
    public SensorState Process(SensorData data)
    {
        if (data == null)
        {
            return null;
        }

        ParameterState temperature = ProcessTemperature(data);
        ParameterState humidity = ProcessHumidity(data);
        ParameterState co2 = ProcessCO2(data);

        // �����޷��������ҵ���ж�
        if (temperature == null || humidity == null || co2 == null)
        {
            return null;
        }

        SensorState state = new SensorState
        {
            SensorId = data.sensor_id,
            Warehouse = data.warehouse,
            Temperature = temperature,
            Humidity = humidity,
            CO2 = co2,
            Timestamp = data.timestamp
        };

        state.OverallStatus = CalculateOverallStatus(state);

        return state;
    }


    private ParameterState ProcessTemperature(SensorData data)
    {
        SensorStatus status;

        switch (data.warehouse)
        {
            case "frozen_meat":
                status = JudgeFrozenMeatTemperature(data.temperature);
                break;

            case "apple":
                status = JudgeAppleTemperature(data.temperature);
                break;

            case "chocolate":
                status = JudgeChocolateTemperature(data.temperature);
                break;

            default:
                return null;
        }

        return new ParameterState(data.temperature, status);
    }


    private ParameterState ProcessHumidity(SensorData data)
    {
        SensorStatus status;

        switch (data.warehouse)
        {
            case "frozen_meat":
                status = JudgeFrozenMeatHumidity(data.humidity);
                break;

            case "apple":
                status = JudgeAppleHumidity(data.humidity);
                break;

            case "chocolate":
                status = JudgeChocolateHumidity(data.humidity);
                break;

            default:
                return null;
        }

        return new ParameterState(data.humidity, status);
    }


    private ParameterState ProcessCO2(SensorData data)
    {
        // ��ƻ���ֲ�ʹ�� CO2
        if (data.warehouse != "apple")
        {
            return new ParameterState(null, SensorStatus.Normal);
        }

        // ƻ����ȱ�� CO2 ����
        if (!data.co2.HasValue)
        {
            return null;
        }

        float value = data.co2.Value;

        SensorStatus status;

        if (value <= 1.0f)
        {
            status = SensorStatus.Normal;
        }
        else if (value <= 2.0f)
        {
            status = SensorStatus.Warning;
        }
        else
        {
            status = SensorStatus.Abnormal;
        }

        return new ParameterState(value, status);
    }


    // ============================================================
    // �䶳���
    // ============================================================

    private SensorStatus JudgeFrozenMeatTemperature(float value)
    {
        if (value <= -18.0f)
        {
            return SensorStatus.Normal;
        }

        if (value <= -15.0f)
        {
            return SensorStatus.Warning;
        }

        return SensorStatus.Abnormal;
    }


    private SensorStatus JudgeFrozenMeatHumidity(float value)
    {
        if (value >= 90.0f && value <= 95.0f)
        {
            return SensorStatus.Normal;
        }

        if ((value >= 85.0f && value < 90.0f) ||
            (value > 95.0f && value <= 98.0f))
        {
            return SensorStatus.Warning;
        }

        return SensorStatus.Abnormal;
    }


    // ============================================================
    // ƻ����
    // ============================================================

    private SensorStatus JudgeAppleTemperature(float value)
    {
        if (value >= -1.0f && value <= 1.0f)
        {
            return SensorStatus.Normal;
        }

        if ((value >= -2.0f && value < -1.0f) ||
            (value > 1.0f && value <= 3.0f))
        {
            return SensorStatus.Warning;
        }

        return SensorStatus.Abnormal;
    }


    private SensorStatus JudgeAppleHumidity(float value)
    {
        if (value >= 85.0f && value <= 95.0f)
        {
            return SensorStatus.Normal;
        }

        if ((value >= 80.0f && value < 85.0f) ||
            (value > 95.0f && value <= 97.0f))
        {
            return SensorStatus.Warning;
        }

        return SensorStatus.Abnormal;
    }


    // ============================================================
    // �ɿ�����
    // ============================================================

    private SensorStatus JudgeChocolateTemperature(float value)
    {
        if (value >= 18.0f && value <= 20.0f)
        {
            return SensorStatus.Normal;
        }

        if ((value >= 15.0f && value < 18.0f) ||
            (value > 20.0f && value <= 23.0f))
        {
            return SensorStatus.Warning;
        }

        return SensorStatus.Abnormal;
    }


    private SensorStatus JudgeChocolateHumidity(float value)
    {
        if (value >= 45.0f && value <= 55.0f)
        {
            return SensorStatus.Normal;
        }

        if (value > 55.0f && value <= 65.0f)
        {
            return SensorStatus.Warning;
        }

        return SensorStatus.Abnormal;
    }


    // ============================================================
    // ����������״̬
    // ============================================================

    private SensorStatus CalculateOverallStatus(SensorState state)
    {
        if (state.Temperature.Status == SensorStatus.Abnormal ||
            state.Humidity.Status == SensorStatus.Abnormal ||
            state.CO2.Status == SensorStatus.Abnormal)
        {
            return SensorStatus.Abnormal;
        }

        if (state.Temperature.Status == SensorStatus.Warning ||
            state.Humidity.Status == SensorStatus.Warning ||
            state.CO2.Status == SensorStatus.Warning)
        {
            return SensorStatus.Warning;
        }

        return SensorStatus.Normal;
    }
    public DeviceControlCommand CreateControlCommand(string warehouse, string sensorId, string device, bool isOn, float? targetTemperature = null)
    {
        if (string.IsNullOrEmpty(warehouse) ||
            string.IsNullOrEmpty(device))
        {
            return null;
        }

        return new DeviceControlCommand
        {
            Warehouse = warehouse,
            SensorId = sensorId,
            Device = device,
            IsOn = isOn,
            TargetTemperature = targetTemperature
        };
    }
}
