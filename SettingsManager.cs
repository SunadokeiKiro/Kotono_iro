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
    public InputField recordLengthInput;     // 録音時間入力フィールド
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
    
    private AudioClip microphoneClip;        // マイク入力用AudioClip
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
        StartMicrophoneMonitoring();
    }
    
    void Update()
    {
        // モニタリング中は音声レベルを更新
        if (isMonitoring && microphoneClip != null)
        {
            UpdateVoiceLevel();
        }
    }
    
    // マイクデバイスの選択肢を初期化
    void InitializeMicrophoneDropdown()
    {
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
    }

    // マイクが変更された時の処理
    void OnMicrophoneChanged(int index)
    {
        // 現在のモニタリングを停止して再開
        StopMicrophoneMonitoring();
        StartMicrophoneMonitoring();
    }

    void LoadApiKey()
    {
        try
        {
            if (File.Exists(apiKeyFilePath))
            {
                string savedApiKey = File.ReadAllText(apiKeyFilePath).Trim();
                apiKeyInput.text = savedApiKey;
                
                if (statusText != null)
                    statusText.text = "保存されたAPIキーを読み込みました";
            }
        }
        catch (Exception e)
        {
            Debug.LogError("APIキーの読み込みに失敗: " + e.Message);
            if (statusText != null)
                statusText.text = "APIキーの読み込みに失敗しました";
        }
    }
    
    void LoadMicSettings()
    {
        try
        {
            // スライダーの範囲を常に設定する（バグ修正）
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
            
            if (File.Exists(micSettingsFilePath))
            {
                string json = File.ReadAllText(micSettingsFilePath);
                micSettings = JsonUtility.FromJson<MicSettings>(json);
                
                // UI要素に値を設定
                if (recordLengthInput != null)
                    recordLengthInput.text = micSettings.recordLengthSec.ToString();
                
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
                
                Debug.Log("マイク設定を読み込みました");
            }
            else
            {
                // 初期値を設定
                if (recordLengthInput != null)
                    recordLengthInput.text = micSettings.recordLengthSec.ToString();
                
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
                
                if (statusText != null)
                    statusText.text = "APIキーを保存しました";
            }
            else
            {
                Debug.LogError("保存するAPIキーが入力されていません");
                if (statusText != null)
                    statusText.text = "APIキーが入力されていません";
            }
        }
        catch (Exception e)
        {
            Debug.LogError("APIキーの保存に失敗: " + e.Message);
            if (statusText != null)
                statusText.text = "APIキーの保存に失敗しました";
        }
    }
    
    void SaveMicSettings()
    {
        try
        {
            // 選択されたマイクを取得
            if (microphoneDropdown != null && microphoneDropdown.options.Count > 0 && microphoneDropdown.interactable)
            {
                micSettings.selectedMicrophone = microphoneDropdown.options[microphoneDropdown.value].text;
            }
            
            // 録音時間を取得
            if (recordLengthInput != null && !string.IsNullOrEmpty(recordLengthInput.text))
            {
                if (int.TryParse(recordLengthInput.text, out int recordLength) && recordLength > 0)
                {
                    micSettings.recordLengthSec = recordLength;
                }
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
            string json = JsonUtility.ToJson(micSettings);
            File.WriteAllText(micSettingsFilePath, json);
            
            Debug.Log("マイク設定を保存しました: " + micSettingsFilePath);
            if (statusText != null)
                statusText.text = "すべての設定を保存しました";
        }
        catch (Exception e)
        {
            Debug.LogError("マイク設定の保存に失敗: " + e.Message);
            if (statusText != null)
                statusText.text = "マイク設定の保存に失敗しました";
        }
    }
    
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
        if (thresholdLineMarker != null && voiceLevelBar != null)
        {
            // スライダーの値に基づいて閾値マーカーの位置を設定
            // voiceLevelBarの親要素（通常はRectTransform）を基準に位置を設定
            RectTransform barRect = voiceLevelBar.rectTransform.parent as RectTransform;
            RectTransform markerRect = thresholdLineMarker.rectTransform;
            
            if (barRect != null && markerRect != null)
            {
                // 閾値の値を0～1の範囲に正規化（スライダーの最小値から最大値の範囲で）
                float normalizedThreshold = Mathf.InverseLerp(thresholdSlider.minValue, thresholdSlider.maxValue, thresholdSlider.value);
                
                // バーの幅に応じてマーカーの位置を設定
                float barWidth = barRect.rect.width;
                Vector2 anchoredPosition = markerRect.anchoredPosition;
                anchoredPosition.x = normalizedThreshold * barWidth;
                markerRect.anchoredPosition = anchoredPosition;
            }
        }
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
        if (microphoneDropdown != null && microphoneDropdown.options.Count > 0 && microphoneDropdown.interactable)
        {
            string deviceName = microphoneDropdown.options[microphoneDropdown.value].text;
            
            // マイクからの録音を開始
            microphoneClip = Microphone.Start(deviceName, true, 1, 44100);
            
            // 録音が始まるまで少し待つ
            while (!(Microphone.GetPosition(deviceName) > 0)) { }
            
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
    }
    
    // マイクモニタリングを停止
    void StopMicrophoneMonitoring()
    {
        if (microphoneClip != null)
        {
            string deviceName = "";
            
            if (microphoneDropdown != null && microphoneDropdown.options.Count > 0 && microphoneDropdown.interactable)
            {
                deviceName = microphoneDropdown.options[microphoneDropdown.value].text;
            }
            
            // マイクの録音を停止
            Microphone.End(deviceName);
            microphoneClip = null;
            
            // UI表示をリセット
            if (voiceLevelBar != null)
                voiceLevelBar.fillAmount = 0f;
            
            if (currentLevelText != null)
                currentLevelText.text = "0.000";
            
            isMonitoring = false;
            Debug.Log("マイクモニタリングを停止");
        }
    }
    
    // 音声レベルを更新
    void UpdateVoiceLevel()
    {
        if (microphoneClip == null) return;
        
        // 現在のマイク位置を取得
        string deviceName = "";
        if (microphoneDropdown != null && microphoneDropdown.options.Count > 0 && microphoneDropdown.interactable)
        {
            deviceName = microphoneDropdown.options[microphoneDropdown.value].text;
        }
        
        int micPosition = Microphone.GetPosition(deviceName);
        
        // サンプルデータの取得
        if (micPosition > 0 && samplesData.Length > 0)
        {
            int startReadPosition = (micPosition - samplesData.Length + microphoneClip.samples) % microphoneClip.samples;
            microphoneClip.GetData(samplesData, startReadPosition);
            
            // 音声レベル（音量）の計算
            float sum = 0f;
            for (int i = 0; i < samplesData.Length; i++)
            {
                sum += Mathf.Abs(samplesData[i]);
            }
            
            // 平均音量
            currentVoiceLevel = sum / samplesData.Length;
            
            // UI更新
            UpdateVoiceLevelUI();
            
            // 閾値との関係を視覚的に表示
            bool isAboveThreshold = currentVoiceLevel > thresholdSlider.value;
            
            // 閾値を超えた時と下回った時で色を変える
            if (voiceLevelBar != null)
            {
                voiceLevelBar.color = isAboveThreshold ? Color.green : Color.gray;
            }
        }
    }
    
    // 音声レベルUIを更新
    void UpdateVoiceLevelUI()
    {
        if (voiceLevelBar != null)
        {
            // 音量レベルを0～1の範囲にマッピング
            // 最大値はスライダーの最大値の2倍くらいまで表示できるようにする
            float maxDisplayValue = thresholdSlider.maxValue * 2.0f;
            float normalizedLevel = Mathf.Clamp01(currentVoiceLevel / maxDisplayValue);
            
            // バーの塗りつぶし率を更新
            voiceLevelBar.fillAmount = normalizedLevel;
        }
        
        if (currentLevelText != null)
        {
            // 現在の音量レベルをテキストで表示
            currentLevelText.text = currentVoiceLevel.ToString("F3");
        }
    }

    void GoBackToMainScene()
    {
        // モニタリングを停止
        StopMicrophoneMonitoring();
        
        // メインシーンに戻る
        SceneManager.LoadScene("KotonoGem");
    }
    
    void OnDestroy()
    {
        // シーン遷移時やアプリケーション終了時にマイクモニタリングを停止
        StopMicrophoneMonitoring();
    }
}