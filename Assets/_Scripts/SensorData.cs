using System;

public enum SensorStatus
{
    Normal,
    Warning,
    Abnormal
}

[Serializable]
public class SensorData
{
    public string sensor_id;
    public string warehouse;

    public float temperature;
    public float humidity;

    // ֻ��ƻ���ֿ�ʹ�� CO2
    public float? co2;

    public long timestamp;
}

[Serializable]
public class ParameterState
{
    public float? Value;
    public SensorStatus Status;

    public ParameterState(float? value, SensorStatus status)
    {
        Value = value;
        Status = status;
    }
}

public class SensorState
{
    public string SensorId;
    public string Warehouse;

    public ParameterState Temperature;
    public ParameterState Humidity;
    public ParameterState CO2;

    public SensorStatus OverallStatus;
    public long Timestamp;
}

[Serializable]
public class DeviceControlCommand
{
    public string Warehouse;
    public string SensorId;
    public string Device; // vs ac dh
    public bool IsOn;
    public float? TargetTemperature;
}

// ==================== C ���ã�JSON ��ת�� ====================
// Unity �� JsonUtility ��֧�� float?�����÷ǿɿ����ͽ� JSON
[Serializable]
public class SensorDataJson
{
    public string sensor_id;
    public string warehouse;
    public float temperature;
    public float humidity;
    public float co2;
    public long timestamp;
}