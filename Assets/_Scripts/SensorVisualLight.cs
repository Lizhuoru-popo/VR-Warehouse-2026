using UnityEngine;

public class SensorVisualLight : MonoBehaviour
{
    [Header("报警光源")]
    public Light alarmPointLight;

    [Header("灯光颜色")]
    public Color warnColor = Color.yellow;
    public Color errorColor = Color.red;

    void Start()
    {
        if (alarmPointLight != null)
        {
            alarmPointLight.enabled = false;
        }
    }

    public void RefreshStatus(SensorStatus status)
    {
        if (alarmPointLight == null)
        {
            return;
        }

        switch (status)
        {
            case SensorStatus.Normal:
                alarmPointLight.enabled = false;
                break;
            case SensorStatus.Warning:
                alarmPointLight.enabled = true;
                alarmPointLight.color = warnColor;
                break;
            case SensorStatus.Abnormal:
                alarmPointLight.enabled = true;
                alarmPointLight.color = errorColor;
                break;
        }
    }
}