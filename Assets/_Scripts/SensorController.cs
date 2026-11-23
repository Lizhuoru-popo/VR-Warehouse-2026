using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SensorController : MonoBehaviour
{
    [Header("传感器身份")]
    public string sensorId;
    public string warehouse;

    [Header("外观材质")]
    public MeshRenderer meshRenderer;
    public Material matNormal;
    public Material matWarn;
    public Material matError;

    [Header("运行时状态（只读）")]
    private SensorState latestState;

    public SensorState LatestState
    {
        get { return latestState; }
    }

    void Start()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (MQTTManager.Instance != null)
        {
            MQTTManager.Instance.OnSensorStateReceived += OnStateReceived;
        }
    }

    void OnDestroy()
    {
        if (MQTTManager.Instance != null)
        {
            MQTTManager.Instance.OnSensorStateReceived -= OnStateReceived;
        }
    }

    void OnStateReceived(SensorState state)
    {
        if (state == null)
        {
            return;
        }

        if (state.SensorId != sensorId)
        {
            return;
        }

        if (state.Warehouse != warehouse)
        {
            return;
        }

        latestState = state;

        UpdateMaterial();

        SensorUIManager.NotifySensorDataUpdated(this);
    }

    void UpdateMaterial()
    {
        if (meshRenderer == null || latestState == null)
        {
            return;
        }

        switch (latestState.OverallStatus)
        {
            case SensorStatus.Normal:
                if (matNormal != null)
                {
                    meshRenderer.material = matNormal;
                }
                break;

            case SensorStatus.Warning:
                if (matWarn != null)
                {
                    meshRenderer.material = matWarn;
                }
                break;

            case SensorStatus.Abnormal:
                if (matError != null)
                {
                    meshRenderer.material = matError;
                }
                break;
        }
    }

    public void OnSelect()
    {
        if (SensorUIManager.Instance != null)
        {
            SensorUIManager.Instance.ShowSensor(this);
        }
    }

    public SensorState GetState()
    {
        return latestState;
    }
}
