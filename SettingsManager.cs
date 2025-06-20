// Scripts/SettingsManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// 設定シーンにおけるAPIキーやマイク関連の設定を管理し、ファイルに保存・読込します。
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("API設定")]
    [SerializeField] private InputField apiKeyInput;

    [Header("マイク設定")]
    [SerializeField] private Dropdown microphoneDropdown;
    [SerializeField] private Slider recordLengthSlider;
    [SerializeField] private Text recordLengthValueText;
    [SerializeField] private Slider thresholdSlider;
    [SerializeField] private Text thresholdValueText;
    [SerializeField] private Slider silenceTimeSlider;
    [SerializeField] private Text silenceTimeText;

    [Header("音声モニター")]
    [SerializeField] private Image voiceLevelBar;
    [SerializeField] private Image thresholdLineMarker;
    [SerializeField] private Text currentLevelText;

    [Header("UI要素")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Text statusText;

    private string apiKeyFilePath;
    private string micSettingsFilePath;

    private AudioClip microphoneClip;
    private string currentMonitoringDevice;
    private float[] samplesData;
    private float currentVoiceLevel = 0f;
    private bool isMonitoring = false;

    /// <summary>
    /// マイク設定をシリアライズするためのデータクラス。
    /// </summary>
    [Serializable]
    public class MicSettings
    {
        public string selectedMicrophone = "";
        public int recordLengthSec = 20;
        public float voiceDetectionThreshold = 0.02f;
        public float silenceDetectionTime = 2.0f;
    }

    private MicSettings micSettings = new MicSettings();

    void Start()
    {
        // ファイルパスを設定
        apiKeyFilePath = Path.Combine(Application.persistentDataPath, "apikey.txt");
        micSettingsFilePath = Path.Combine(Application.persistentDataPath, "micsettings.json");
        
        InitializeSliderRanges();

        // UIイベントのリスナーを設定
        saveButton.onClick.AddListener(SaveAllSettings);
        backButton.onClick.AddListener(GoBackToMainScene);

        recordLengthSlider.onValueChanged.AddListener(OnRecordLengthChanged);
        thresholdSlider.onValueChanged.AddListener(OnThresholdChanged);
        silenceTimeSlider.onValueChanged.AddListener(OnSilenceTimeChanged);

        // 保存されている設定を読み込んでUIに反映
        LoadApiKey();
        LoadMicSettings();

        // マイク関連の初期化
        InitializeMicrophoneDropdown();

        // サンプルデータ用の配列を初期化
        samplesData = new float[1024];

        // マイクモニタリングを開始
        StartMicrophoneMonitoring();
    }

    void Update()
    {
        if (isMonitoring && microphoneClip != null)
        {
            UpdateVoiceLevel();
        }
    }

    /// <summary>
    /// 各スライダーの最小値、最大値、刻み方を設定します。
    /// </summary>
    private void InitializeSliderRanges()
    {
        if (recordLengthSlider != null)
        {
            recordLengthSlider.minValue = 1f;
            recordLengthSlider.maxValue = 60f;
            recordLengthSlider.wholeNumbers = true;
        }
        if (thresholdSlider != null)
        {
            thresholdSlider.minValue = 0.001f;
            thresholdSlider.maxValue = 0.2f;
            thresholdSlider.wholeNumbers = false;
        }
        if (silenceTimeSlider != null)
        {
            silenceTimeSlider.minValue = 0.5f;
            silenceTimeSlider.maxValue = 5.0f;
            silenceTimeSlider.wholeNumbers = false;
        }
    }

    /// <summary>
    /// 利用可能なマイクデバイスをドロップダウンに設定します。
    /// </summary>
    void InitializeMicrophoneDropdown()
    {
        if (microphoneDropdown == null) return;

        microphoneDropdown.ClearOptions();
        string[] devices = Microphone.devices;

        if (devices.Length > 0)
        {
            microphoneDropdown.AddOptions(devices.ToList());
            microphoneDropdown.onValueChanged.AddListener(OnMicrophoneSelectionChanged);

            int savedIndex = Array.IndexOf(devices, micSettings.selectedMicrophone);
            if (savedIndex >= 0)
            {
                microphoneDropdown.value = savedIndex;
            }
            else if(!string.IsNullOrEmpty(micSettings.selectedMicrophone))
            {
                Debug.LogWarning($"Saved microphone '{micSettings.selectedMicrophone}' not found.");
            }
        }
        else
        {
            microphoneDropdown.options.Add(new Dropdown.OptionData("マイクが見つかりません"));
            microphoneDropdown.interactable = false;
            if (statusText != null) statusText.text = "警告: マイクデバイスが見つかりません";
        }
    }

    /// <summary>
    /// マイクの選択が変更されたときにモニタリングを再開します。
    /// </summary>
    void OnMicrophoneSelectionChanged(int index)
    {
        StopMicrophoneMonitoring();
        StartMicrophoneMonitoring();
    }

    #region 設定の保存と読込
    /// <summary>
    /// APIキーとマイク設定の両方を保存します。
    /// </summary>
    void SaveAllSettings()
    {
        SaveApiKey();
        SaveMicSettings();
        if (statusText != null)
        {
            statusText.text = "すべての設定を保存しました";
            statusText.color = Color.green;
        }
    }
    
    void LoadApiKey()
    {
        try
        {
            if (File.Exists(apiKeyFilePath))
            {
                apiKeyInput.text = File.ReadAllText(apiKeyFilePath).Trim();
                if (statusText != null) statusText.text = "保存されたAPIキーを読み込みました";
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load API key: " + e.Message);
            if (statusText != null) statusText.text = "APIキーの読み込みに失敗";
        }
    }

    void LoadMicSettings()
    {
        try
        {
            if (File.Exists(micSettingsFilePath))
            {
                string json = File.ReadAllText(micSettingsFilePath);
                micSettings = JsonUtility.FromJson<MicSettings>(json);
                Debug.Log("Microphone settings loaded.");
            }
            
            recordLengthSlider.value = Mathf.Clamp(micSettings.recordLengthSec, recordLengthSlider.minValue, recordLengthSlider.maxValue);
            thresholdSlider.value = Mathf.Clamp(micSettings.voiceDetectionThreshold, thresholdSlider.minValue, thresholdSlider.maxValue);
            silenceTimeSlider.value = Mathf.Clamp(micSettings.silenceDetectionTime, silenceTimeSlider.minValue, silenceTimeSlider.maxValue);
            
            UpdateRecordLengthText();
            UpdateThresholdValueText();
            UpdateSilenceTimeText();
            UpdateThresholdMarkerPosition();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load microphone settings: " + e.Message);
        }
    }
    
    void SaveApiKey()
    {
        try
        {
            if (!string.IsNullOrEmpty(apiKeyInput.text))
            {
                File.WriteAllText(apiKeyFilePath, apiKeyInput.text.Trim());
                Debug.Log("API key saved to: " + apiKeyFilePath);
            }
            else
            {
                Debug.LogWarning("API key input is empty.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save API key: " + e.Message);
        }
    }

    void SaveMicSettings()
    {
        try
        {
            if (microphoneDropdown.options.Count > 0 && microphoneDropdown.interactable)
            {
                micSettings.selectedMicrophone = microphoneDropdown.options[microphoneDropdown.value].text;
            }

            micSettings.recordLengthSec = (int)recordLengthSlider.value;
            micSettings.voiceDetectionThreshold = thresholdSlider.value;
            micSettings.silenceDetectionTime = silenceTimeSlider.value;

            string json = JsonUtility.ToJson(micSettings, true);
            File.WriteAllText(micSettingsFilePath, json);
            Debug.Log("Microphone settings saved to: " + micSettingsFilePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save microphone settings: " + e.Message);
        }
    }
    #endregion

    #region UIスライダーのイベントハンドラ
    void OnRecordLengthChanged(float value)
    {
        UpdateRecordLengthText();
    }

    void UpdateRecordLengthText()
    {
        if (recordLengthValueText != null)
        {
            recordLengthValueText.text = recordLengthSlider.value.ToString("F0") + "秒";
        }
    }

    void OnThresholdChanged(float value)
    {
        UpdateThresholdValueText();
        UpdateThresholdMarkerPosition();
    }

    void UpdateThresholdValueText()
    {
        if (thresholdValueText != null)
        {
            thresholdValueText.text = thresholdSlider.value.ToString("F3");
        }
    }
    
    void OnSilenceTimeChanged(float value)
    {
        UpdateSilenceTimeText();
    }

    void UpdateSilenceTimeText()
    {
        if (silenceTimeText != null)
        {
            silenceTimeText.text = silenceTimeSlider.value.ToString("F1") + "秒";
        }
    }
    #endregion
    
    #region マイクモニタリング
    void StartMicrophoneMonitoring()
    {
        if (microphoneDropdown.options.Count == 0 || !microphoneDropdown.interactable)
        {
            Debug.LogWarning("Cannot start monitoring: No microphone available.");
            return;
        }

        currentMonitoringDevice = microphoneDropdown.options[microphoneDropdown.value].text;
        microphoneClip = Microphone.Start(currentMonitoringDevice, true, 1, 44100);

        if (microphoneClip == null)
        {
            Debug.LogError("Microphone.Start failed for monitoring.");
            return;
        }

        isMonitoring = true;
        if (statusText != null) statusText.text = "マイクモニタリング中...";
    }

    void StopMicrophoneMonitoring()
    {
        if (isMonitoring && !string.IsNullOrEmpty(currentMonitoringDevice))
        {
            Microphone.End(currentMonitoringDevice);
            isMonitoring = false;
            currentMonitoringDevice = null;
            microphoneClip = null;

            if (voiceLevelBar != null) voiceLevelBar.fillAmount = 0f;
            if (currentLevelText != null) currentLevelText.text = "0.000";
        }
    }

    void UpdateVoiceLevel()
    {
        if (microphoneClip == null || !Microphone.IsRecording(currentMonitoringDevice))
        {
            isMonitoring = false;
            return;
        }

        int micPosition = Microphone.GetPosition(currentMonitoringDevice);
        if (micPosition < samplesData.Length) return;

        microphoneClip.GetData(samplesData, micPosition - samplesData.Length);

        float sum = 0f;
        foreach (float sample in samplesData)
        {
            sum += Mathf.Abs(sample);
        }
        currentVoiceLevel = sum / samplesData.Length;

        UpdateVoiceLevelUI();
    }

    void UpdateVoiceLevelUI()
    {
        if (voiceLevelBar != null)
        {
            float maxDisplayValue = thresholdSlider.maxValue * 2.0f;
            voiceLevelBar.fillAmount = Mathf.Clamp01(currentVoiceLevel / maxDisplayValue);

            voiceLevelBar.color = (currentVoiceLevel > thresholdSlider.value) ? Color.green : Color.gray;
        }

        if (currentLevelText != null)
        {
            currentLevelText.text = currentVoiceLevel.ToString("F3");
        }
    }
    
    void UpdateThresholdMarkerPosition()
    {
        if (thresholdLineMarker != null && voiceLevelBar != null)
        {
            RectTransform barRect = voiceLevelBar.rectTransform;
            RectTransform markerRect = thresholdLineMarker.rectTransform;

            float barWidth = barRect.rect.width;
            float normalizedThreshold = Mathf.InverseLerp(thresholdSlider.minValue, thresholdSlider.maxValue, thresholdSlider.value);
            
            Vector2 anchoredPosition = markerRect.anchoredPosition;
            anchoredPosition.x = normalizedThreshold * barWidth;
            markerRect.anchoredPosition = anchoredPosition;
        }
    }
    #endregion

    void GoBackToMainScene()
    {
        SceneManager.LoadScene("KotonoGem");
    }

    void OnDestroy()
    {
        StopMicrophoneMonitoring();
    }
}