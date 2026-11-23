using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class SensorUIManager : MonoBehaviour
{
    public static SensorUIManager Instance;

    private static Dictionary<string, bool> acStateMap = new Dictionary<string, bool>();
    private static Dictionary<string, bool> ventilatorStateMap = new Dictionary<string, bool>();
    private static Dictionary<string, bool> dehumidifierStateMap = new Dictionary<string, bool>();

    [Header("UI 面板")]
    public GameObject infoPanel;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtDirection;

    [Header("数值文本")]
    public TextMeshProUGUI txtTemp;
    public TextMeshProUGUI txtHum;
    public TextMeshProUGUI txtCO2;

    [Header("状态文本")]
    public TextMeshProUGUI txtStatusTemp;
    public TextMeshProUGUI txtStatusHum;
    public TextMeshProUGUI txtStatusCO2;

    [Header("设备控制按钮")]
    public Button btnAC;
    public TextMeshProUGUI txtACBtn;

    public Button btnVentilator;
    public TextMeshProUGUI txtVentilatorBtn;

    public Button btnDehumidifier;
    public TextMeshProUGUI txtDehumidifierBtn;

    private SensorController currentSensor;

    public static event System.Action<SensorController> OnSensorDataUpdated;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        OnSensorDataUpdated += HandleSensorDataUpdated;
    }

    void OnDestroy()
    {
        OnSensorDataUpdated -= HandleSensorDataUpdated;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        if (btnAC != null)
        {
            btnAC.onClick.AddListener(OnACButtonClicked);
        }

        if (btnVentilator != null)
        {
            btnVentilator.onClick.AddListener(OnVentilatorButtonClicked);
        }

        if (btnDehumidifier != null)
        {
            btnDehumidifier.onClick.AddListener(OnDehumidifierButtonClicked);
        }

        ResetButtonText();
    }

    void ResetButtonText()
    {
        if (txtACBtn != null)
        {
            txtACBtn.text = "开启";
        }

        if (txtVentilatorBtn != null)
        {
            txtVentilatorBtn.text = "开启";
        }

        if (txtDehumidifierBtn != null)
        {
            txtDehumidifierBtn.text = "开启";
        }
    }

    void UpdateButtonTextForSensor(string sensorId)
    {
        bool acOn = false;
        bool ventilatorOn = false;
        bool dehumidifierOn = false;

        if (!string.IsNullOrEmpty(sensorId))
        {
            acStateMap.TryGetValue(sensorId, out acOn);
            ventilatorStateMap.TryGetValue(sensorId, out ventilatorOn);
            dehumidifierStateMap.TryGetValue(sensorId, out dehumidifierOn);
        }

        if (txtACBtn != null)
        {
            txtACBtn.text = acOn ? "关闭" : "开启";
        }

        if (txtVentilatorBtn != null)
        {
            txtVentilatorBtn.text = ventilatorOn ? "关闭" : "开启";
        }

        if (txtDehumidifierBtn != null)
        {
            txtDehumidifierBtn.text = dehumidifierOn ? "关闭" : "开启";
        }
    }

    public void ShowSensor(SensorController sensor)
    {
        if (sensor == null)
        {
            return;
        }

        currentSensor = sensor;

        if (infoPanel == null)
        {
            return;
        }

        infoPanel.SetActive(true);
        EnsurePanelBgActive();
        UpdateButtonVisibility(sensor.warehouse);
        UpdateButtonTextForSensor(sensor.sensorId);
        RefreshPanel();
    }

    void EnsurePanelBgActive()
    {
        if (infoPanel == null)
        {
            return;
        }

        Transform panelBg = infoPanel.transform.Find("PanelBg");
        if (panelBg != null && !panelBg.gameObject.activeSelf)
        {
            panelBg.gameObject.SetActive(true);
            return;
        }

        if (txtTitle != null && !txtTitle.gameObject.activeInHierarchy)
        {
            Transform parent = txtTitle.transform.parent;
            while (parent != null && parent != infoPanel.transform)
            {
                if (!parent.gameObject.activeSelf)
                {
                    parent.gameObject.SetActive(true);
                    break;
                }
                parent = parent.parent;
            }
        }
    }

    void UpdateButtonVisibility(string warehouse)
    {
        bool hasCO2 = (warehouse == "apple");

        if (btnVentilator != null)
        {
            btnVentilator.gameObject.SetActive(hasCO2);
        }

        if (txtCO2 != null && txtCO2.transform.parent != null)
        {
            GameObject co2Row = txtCO2.transform.parent.gameObject;
            co2Row.SetActive(hasCO2);
        }
    }

    public static void NotifySensorDataUpdated(SensorController sensor)
    {
        if (OnSensorDataUpdated != null)
        {
            OnSensorDataUpdated(sensor);
        }
    }

    void HandleSensorDataUpdated(SensorController sensor)
    {
        if (sensor == null)
        {
            return;
        }

        if (currentSensor != sensor)
        {
            return;
        }

        if (infoPanel == null || !infoPanel.activeSelf)
        {
            return;
        }

        RefreshPanel();
    }

    void RefreshPanel()
    {
        if (currentSensor == null)
        {
            return;
        }

        SensorState state = currentSensor.GetState();

        if (state == null)
        {
            return;
        }

        if (txtTitle != null)
        {
            txtTitle.text = state.Warehouse;
        }

        if (txtDirection != null)
        {
            txtDirection.text = DirectionOf(state);
        }

        if (txtTemp != null)
        {
            if (state.Temperature != null && state.Temperature.Value.HasValue)
            {
                txtTemp.text = $"{state.Temperature.Value.Value:F1} ℃";
            }
            else
            {
                txtTemp.text = "-- ℃";
            }
        }

        if (txtHum != null)
        {
            if (state.Humidity != null && state.Humidity.Value.HasValue)
            {
                txtHum.text = $"{state.Humidity.Value.Value:F1} %";
            }
            else
            {
                txtHum.text = "-- %";
            }
        }

        if (txtCO2 != null)
        {
            bool hasCO2 = state.CO2 != null && state.CO2.Value.HasValue;

            txtCO2.gameObject.SetActive(hasCO2);

            if (hasCO2)
            {
                txtCO2.text = $"{state.CO2.Value.Value:F2}";
            }
        }

        ApplyStatus(txtStatusTemp, state.Temperature);
        ApplyStatus(txtStatusHum, state.Humidity);

        if (txtStatusCO2 != null)
        {
            bool hasCO2 = state.CO2 != null && state.CO2.Value.HasValue;

            txtStatusCO2.gameObject.SetActive(hasCO2);

            if (hasCO2)
            {
                ApplyStatus(txtStatusCO2, state.CO2);
            }
        }
    }

    string DirectionOf(SensorState state)
    {
        char letter = 'M';

        if (!string.IsNullOrEmpty(state.SensorId))
        {
            char c = char.ToUpper(state.SensorId[0]);

            if (c == 'L' || c == 'M' || c == 'R')
            {
                letter = c;
            }
        }

        string num;

        switch (state.Warehouse)
        {
            case "frozen_meat":
                num = "01";
                break;

            case "apple":
                num = "02";
                break;

            case "chocolate":
                num = "03";
                break;

            default:
                num = "00";
                break;
        }

        return $"{letter}{num}";
    }

    void ApplyStatus(
        TextMeshProUGUI text,
        ParameterState parameter)
    {
        if (text == null || parameter == null)
        {
            return;
        }

        switch (parameter.Status)
        {
            case SensorStatus.Normal:
                text.text = "正常";
                text.color = Color.green;
                break;

            case SensorStatus.Warning:
                text.text = "预警";
                text.color = Color.yellow;
                break;

            case SensorStatus.Abnormal:
                text.text = "异常";
                text.color = Color.red;
                break;
        }
    }

    public void HidePanel()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        currentSensor = null;
        ResetButtonText();
    }

    void OnACButtonClicked()
    {
        if (currentSensor == null)
        {
            return;
        }

        SensorState state = currentSensor.GetState();

        if (state == null)
        {
            return;
        }

        string sensorId = currentSensor.sensorId;
        bool currentOn;
        acStateMap.TryGetValue(sensorId, out currentOn);
        bool newOn = !currentOn;
        acStateMap[sensorId] = newOn;

        if (txtACBtn != null)
        {
            txtACBtn.text = newOn ? "关闭" : "开启";
        }

        SendDeviceCommand(state.Warehouse, sensorId, "ac", newOn);
    }

    void OnVentilatorButtonClicked()
    {
        if (currentSensor == null)
        {
            return;
        }

        SensorState state = currentSensor.GetState();

        if (state == null)
        {
            return;
        }

        string sensorId = currentSensor.sensorId;
        bool currentOn;
        ventilatorStateMap.TryGetValue(sensorId, out currentOn);
        bool newOn = !currentOn;
        ventilatorStateMap[sensorId] = newOn;

        if (txtVentilatorBtn != null)
        {
            txtVentilatorBtn.text = newOn ? "关闭" : "开启";
        }

        SendDeviceCommand(state.Warehouse, sensorId, "ventilator", newOn);
    }

    void OnDehumidifierButtonClicked()
    {
        if (currentSensor == null)
        {
            return;
        }

        SensorState state = currentSensor.GetState();

        if (state == null)
        {
            return;
        }

        string sensorId = currentSensor.sensorId;
        bool currentOn;
        dehumidifierStateMap.TryGetValue(sensorId, out currentOn);
        bool newOn = !currentOn;
        dehumidifierStateMap[sensorId] = newOn;

        if (txtDehumidifierBtn != null)
        {
            txtDehumidifierBtn.text = newOn ? "关闭" : "开启";
        }

        SendDeviceCommand(state.Warehouse, sensorId, "dehumidifier", newOn);
    }

    void SendDeviceCommand(string warehouse, string sensorId, string device, bool isOn)
    {
        if (MQTTManager.Instance == null ||
            MQTTManager.Instance.DataProcessor == null)
        {
            return;
        }

        DeviceControlCommand cmd =
            MQTTManager.Instance.DataProcessor.CreateControlCommand(
                warehouse,
                sensorId,
                device,
                isOn
            );

        if (cmd == null)
        {
            return;
        }

        MQTTManager.Instance.SendControlCommand(cmd);

        Debug.Log(
            $"[UI] 设备控制: " +
            $"{warehouse}/{sensorId}/{device}/IsOn={isOn}"
        );
    }
}
