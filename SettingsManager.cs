using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;

public class SettingsManager : MonoBehaviour
{
    [Header("API設定")]
    public InputField apiKeyInput;

    [Header("マイク設定")]
    public Dropdown microphoneDropdown;      // マイク選択用ドロップダウン
    
    // ----- InputFieldからSliderに変更した箇所 -----
    public Slider recordLengthSlider;        // 録音時間設定スライダー
    public Text recordLengthValueText;       // 録音時間の値を表示するテキスト
    // public InputField recordLengthInput;  // ← 元のこの行は削除してください
    // -----------------------------------------
    
    public Slider thresholdSlider;           // 音声検出閾値スライダー
    public Text thresholdValueText;          // 音声検出閾値の表示
    public Slider silenceTimeSlider;         // 無音検出時間スライダー
    public Text silenceTimeText;             // 無音検出時間の表示

    [Header("音声モニター")]
    public Image voiceLevelBar;              // 音声レベルを表示するバー
    public Image thresholdLineMarker;        // 閾値を示すライン
    public Text currentLevelText;            // 現在の音量レベルを表示するテキスト

    [Header("UI要素")]
    public Button saveButton;
    public Button backButton;
    public Text statusText;                  // 操作結果を表示するためのテキスト

    private string apiKeyFilePath;
    private string micSettingsFilePath;

#if !UNITY_WEBGL
    private AudioClip microphoneClip;        // マイク入力用AudioClip
#endif
    private float[] samplesData;             // 音声データのサンプル
    private float currentVoiceLevel = 0f;    // 現在の音声レベル
    private bool isMonitoring = false;       // モニタリング中かどうか

    // 設定データを保存するためのクラス
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
        // ファイルパスの設定
        apiKeyFilePath = Path.Combine(Application.persistentDataPath, "apikey.txt");
        micSettingsFilePath = Path.Combine(Application.persistentDataPath, "micsettings.json");

        // マイクデバイスをドロップダウンに追加
        InitializeMicrophoneDropdown();

        // ボタンにリスナーを設定
        saveButton.onClick.AddListener(SaveSettings);
        backButton.onClick.AddListener(GoBackToMainScene);

        // スライダーのイベント設定
        if (recordLengthSlider != null) // 追加
        {
            recordLengthSlider.onValueChanged.AddListener(OnRecordLengthChanged);
        }
        if (thresholdSlider != null)
        {
            thresholdSlider.onValueChanged.AddListener(OnThresholdChanged);
        }
        if (silenceTimeSlider != null)
        {
            silenceTimeSlider.onValueChanged.AddListener(OnSilenceTimeChanged);
        }

        // 保存されている設定を読み込む
        LoadApiKey();
        LoadMicSettings();

        // サンプルデータ用の配列を初期化
        samplesData = new float[1024];

        // 閾値マーカーの初期位置を設定
        UpdateThresholdMarkerPosition();

        // Settingsシーンでは自動的にマイクモニタリングを開始
#if !UNITY_WEBGL
        StartMicrophoneMonitoring();
#else
        if (statusText != null)
            statusText.text = "WebGLではマイクモニタリングは利用不可";
        if (voiceLevelBar != null)
            voiceLevelBar.gameObject.SetActive(false);
        if (currentLevelText != null)
            currentLevelText.gameObject.SetActive(false);
        if (thresholdLineMarker != null)
            thresholdLineMarker.gameObject.SetActive(false);
#endif
    }

    void Update()
    {
#if !UNITY_WEBGL
        // モニタリング中は音声レベルを更新
        if (isMonitoring && microphoneClip != null)
        {
            UpdateVoiceLevel();
        }
#endif
    }

    // マイクデバイスの選択肢を初期化
    void InitializeMicrophoneDropdown()
    {
#if !UNITY_WEBGL
        if (microphoneDropdown != null)
        {
            microphoneDropdown.ClearOptions();

            // マイクデバイスのリストを取得
            string[] devices = Microphone.devices;

            if (devices.Length > 0)
            {
                // ドロップダウンにデバイスを追加
                microphoneDropdown.AddOptions(devices.ToList());

                // 保存されている設定があれば選択
                if (!string.IsNullOrEmpty(micSettings.selectedMicrophone))
                {
                    int savedIndex = Array.IndexOf(devices, micSettings.selectedMicrophone);
                    if (savedIndex >= 0)
                    {
                        microphoneDropdown.value = savedIndex;
                    }
                }

                // マイクデバイスが変更された時のイベントを追加
                microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
            }
            else
            {
                // マイクがない場合の処理
                microphoneDropdown.options.Add(new Dropdown.OptionData("マイクが見つかりません"));
                microphoneDropdown.interactable = false;

                if (statusText != null)
                    statusText.text = "警告: マイクデバイスが見つかりません";
            }
        }
#else
        if (microphoneDropdown != null)
        {
            microphoneDropdown.ClearOptions();
            microphoneDropdown.options.Add(new Dropdown.OptionData("WebGLではマイク選択不可"));
            microphoneDropdown.interactable = false;
        }
#endif
    }

    // マイクが変更された時の処理
    void OnMicrophoneChanged(int index)
    {
#if !UNITY_WEBGL
        // 現在のモニタリングを停止して再開
        StopMicrophoneMonitoring();
        StartMicrophoneMonitoring();
#endif
    }

    void LoadApiKey()
    {
        try
        {
            if (File.Exists(apiKeyFilePath))
            {
                string savedApiKey = File.ReadAllText(apiKeyFilePath).Trim();
                apiKeyInput.text = savedApiKey;

                if (statusText != null && !statusText.text.Contains("WebGL")) // WebGLメッセージを上書きしない
                    statusText.text = "保存されたAPIキーを読み込みました";
            }
        }
        catch (Exception e)
        {
            Debug.LogError("APIキーの読み込みに失敗: " + e.Message);
            if (statusText != null && !statusText.text.Contains("WebGL"))
                statusText.text = "APIキーの読み込みに失敗しました";
        }
    }

    void LoadMicSettings()
    {
        try
        {
            // --- 各スライダーの範囲を設定 ---
            if (recordLengthSlider != null)
            {
                recordLengthSlider.minValue = 1f;
                recordLengthSlider.maxValue = 60f;
                recordLengthSlider.wholeNumbers = true; // 値を整数に
            }
            if (thresholdSlider != null)
            {
                thresholdSlider.minValue = 0.001f;
                thresholdSlider.maxValue = 0.2f;
            }
            if (silenceTimeSlider != null)
            {
                silenceTimeSlider.minValue = 0.5f;
                silenceTimeSlider.maxValue = 5.0f;
            }
            
            // --- 設定ファイルの読み込み ---
            if (File.Exists(micSettingsFilePath))
            {
                string json = File.ReadAllText(micSettingsFilePath);
                micSettings = JsonUtility.FromJson<MicSettings>(json);
                Debug.Log("マイク設定を読み込みました");
            }
            
            // --- 読み込んだ値（またはデフォルト値）をUIに反映 ---
            if (recordLengthSlider != null)
            {
                // 保存された値が範囲外の場合、範囲内に収める
                micSettings.recordLengthSec = Mathf.Clamp(micSettings.recordLengthSec, (int)recordLengthSlider.minValue, (int)recordLengthSlider.maxValue);
                recordLengthSlider.value = micSettings.recordLengthSec;
                UpdateRecordLengthText(); // テキストも更新
            }
            if (thresholdSlider != null)
            {
                thresholdSlider.value = micSettings.voiceDetectionThreshold;
                UpdateThresholdValueText();
            }
            if (silenceTimeSlider != null)
            {
                silenceTimeSlider.value = micSettings.silenceDetectionTime;
                UpdateSilenceTimeText();
            }

            // 閾値マーカーの位置を更新
            UpdateThresholdMarkerPosition();
        }
        catch (Exception e)
        {
            Debug.LogError("マイク設定の読み込みに失敗: " + e.Message);
        }
    }

    void SaveSettings()
    {
        // APIキーの保存
        SaveApiKey();

        // マイク設定の保存
        SaveMicSettings();
    }

    void SaveApiKey()
    {
        try
        {
            if (!string.IsNullOrEmpty(apiKeyInput.text))
            {
                File.WriteAllText(apiKeyFilePath, apiKeyInput.text.Trim());
                Debug.Log("APIキーを保存しました: " + apiKeyFilePath);

                if (statusText != null && !statusText.text.Contains("WebGL"))
                    statusText.text = "APIキーを保存しました";
            }
            else
            {
                Debug.LogError("保存するAPIキーが入力されていません");
                if (statusText != null && !statusText.text.Contains("WebGL"))
                    statusText.text = "APIキーが入力されていません";
            }
        }
        catch (Exception e)
        {
            Debug.LogError("APIキーの保存に失敗: " + e.Message);
            if (statusText != null && !statusText.text.Contains("WebGL"))
                statusText.text = "APIキーの保存に失敗しました";
        }
    }

    void SaveMicSettings()
    {
        try
        {
#if !UNITY_WEBGL
            // 選択されたマイクを取得
            if (microphoneDropdown != null && microphoneDropdown.options.Count > 0 && microphoneDropdown.interactable)
            {
                micSettings.selectedMicrophone = microphoneDropdown.options[microphoneDropdown.value].text;
            }
#else
            micSettings.selectedMicrophone = ""; // WebGLではマイク名は空
#endif

            // 録音時間をスライダーから取得
            if (recordLengthSlider != null)
            {
                micSettings.recordLengthSec = (int)recordLengthSlider.value;
            }

            // 閾値を取得
            if (thresholdSlider != null)
            {
                micSettings.voiceDetectionThreshold = thresholdSlider.value;
            }

            // 無音検出時間を取得
            if (silenceTimeSlider != null)
            {
                micSettings.silenceDetectionTime = silenceTimeSlider.value;
            }

            // JSONに変換して保存
            string json = JsonUtility.ToJson(micSettings, true);
            File.WriteAllText(micSettingsFilePath, json);

            Debug.Log("マイク設定を保存しました: " + micSettingsFilePath);
            if (statusText != null && !statusText.text.Contains("WebGL"))
                statusText.text = "すべての設定を保存しました";
        }
        catch (Exception e)
        {
            Debug.LogError("マイク設定の保存に失敗: " + e.Message);
            if (statusText != null && !statusText.text.Contains("WebGL"))
                statusText.text = "マイク設定の保存に失敗しました";
        }
    }
    
    // --- 録音時間スライダー用のメソッド ---
    void OnRecordLengthChanged(float value)
    {
        UpdateRecordLengthText();
    }

    void UpdateRecordLengthText()
    {
        if (recordLengthValueText != null && recordLengthSlider != null)
        {
            recordLengthValueText.text = recordLengthSlider.value.ToString("F0") + "秒";
        }
    }
    // ------------------------------------

    // 閾値スライダーの値変更時
    void OnThresholdChanged(float value)
    {
        UpdateThresholdValueText();
        UpdateThresholdMarkerPosition();
    }

    // 閾値の表示テキストを更新
    void UpdateThresholdValueText()
    {
        if (thresholdValueText != null)
        {
            thresholdValueText.text = thresholdSlider.value.ToString("F3");
        }
    }

    // 閾値マーカーの位置を更新
    void UpdateThresholdMarkerPosition()
    {
#if !UNITY_WEBGL
        if (thresholdLineMarker != null && voiceLevelBar != null)
        {
            RectTransform barRect = voiceLevelBar.rectTransform.parent as RectTransform;
            RectTransform markerRect = thresholdLineMarker.rectTransform;

            if (barRect != null && markerRect != null)
            {
                float normalizedThreshold = Mathf.InverseLerp(thresholdSlider.minValue, thresholdSlider.maxValue, thresholdSlider.value);
                float barWidth = barRect.rect.width;
                Vector2 anchoredPosition = markerRect.anchoredPosition;
                anchoredPosition.x = normalizedThreshold * barWidth;
                markerRect.anchoredPosition = anchoredPosition;
            }
        }
#endif
    }

    // 無音検出時間スライダーの値変更時
    void OnSilenceTimeChanged(float value)
    {
        UpdateSilenceTimeText();
    }

    // 無音検出時間の表示テキストを更新
    void UpdateSilenceTimeText()
    {
        if (silenceTimeText != null)
        {
            silenceTimeText.text = silenceTimeSlider.value.ToString("F1") + "秒";
        }
    }

    // マイクモニタリングを開始
    void StartMicrophoneMonitoring()
    {
#if !UNITY_WEBGL
        if (microphoneDropdown != null && microphoneDropdown.options.Count > 0 && microphoneDropdown.interactable)
        {
            string deviceName = microphoneDropdown.options[microphoneDropdown.value].text;
            microphoneClip = Microphone.Start(deviceName, true, 1, 44100);

            if (Microphone.IsRecording(deviceName))
            {
                while (!(Microphone.GetPosition(deviceName) > 0)) { }
            }
            else
            {
                Debug.LogError("マイクの起動に失敗しました: " + deviceName);
                if (statusText != null)
                    statusText.text = "マイクの起動に失敗しました。";
                isMonitoring = false;
                return;
            }

            isMonitoring = true;
            Debug.Log("マイクモニタリングを開始: " + deviceName);

            if (statusText != null)
                statusText.text = "マイクモニタリング中...";
        }
        else
        {
            Debug.LogError("マイクデバイスが選択されていません");

            if (statusText != null)
                statusText.text = "マイクデバイスが選択されていません";
        }
#endif
    }

    // マイクモニタリングを停止
    void StopMicrophoneMonitoring()
    {
#if !UNITY_WEBGL
        if (microphoneClip != null)
        {
            string deviceName = "";

            if (microphoneDropdown != null && microphoneDropdown.options.Count > 0 && microphoneDropdown.interactable)
            {
                deviceName = microphoneDropdown.options[microphoneDropdown.value].text;
            }

            if (Microphone.IsRecording(deviceName))
            {
                Microphone.End(deviceName);
            }
            microphoneClip = null;

            if (voiceLevelBar != null)
                voiceLevelBar.fillAmount = 0f;
            if (currentLevelText != null)
                currentLevelText.text = "0.000";

            isMonitoring = false;
            Debug.Log("マイクモニタリングを停止");
        }
#endif
    }

    // 音声レベルを更新
    void UpdateVoiceLevel()
    {
#if !UNITY_WEBGL
        if (microphoneClip == null) return;

        string deviceName = "";
        if (microphoneDropdown != null && microphoneDropdown.options.Count > 0 && microphoneDropdown.interactable)
        {
            deviceName = microphoneDropdown.options[microphoneDropdown.value].text;
        }

        if (string.IsNullOrEmpty(deviceName) || !Microphone.IsRecording(deviceName)) return;

        int micPosition = Microphone.GetPosition(deviceName);

        if (micPosition > 0 && samplesData.Length > 0)
        {
            int startReadPosition = (micPosition - samplesData.Length + microphoneClip.samples) % microphoneClip.samples;
            microphoneClip.GetData(samplesData, startReadPosition);

            float sum = 0f;
            for (int i = 0; i < samplesData.Length; i++)
            {
                sum += Mathf.Abs(samplesData[i]);
            }
            currentVoiceLevel = sum / samplesData.Length;

            UpdateVoiceLevelUI();

            bool isAboveThreshold = currentVoiceLevel > thresholdSlider.value;
            if (voiceLevelBar != null)
            {
                voiceLevelBar.color = isAboveThreshold ? Color.green : Color.gray;
            }
        }
#endif
    }

    // 音声レベルUIを更新
    void UpdateVoiceLevelUI()
    {
#if !UNITY_WEBGL
        if (voiceLevelBar != null)
        {
            float maxDisplayValue = thresholdSlider.maxValue * 2.0f;
            float normalizedLevel = Mathf.Clamp01(currentVoiceLevel / maxDisplayValue);
            voiceLevelBar.fillAmount = normalizedLevel;
        }
        if (currentLevelText != null)
        {
            currentLevelText.text = currentVoiceLevel.ToString("F3");
        }
#endif
    }

    void GoBackToMainScene()
    {
#if !UNITY_WEBGL
        StopMicrophoneMonitoring();
#endif
        SceneManager.LoadScene("KotonoGem");
    }

    void OnDestroy()
    {
#if !UNITY_WEBGL
        StopMicrophoneMonitoring();
#endif
    }
}