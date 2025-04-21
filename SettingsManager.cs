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
    
    [Header("UI要素")]
    public Button saveButton;
    public Button backButton;
    public Text statusText;                  // 操作結果を表示するためのテキスト

    private string apiKeyFilePath;
    private string micSettingsFilePath;

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
                    thresholdSlider.minValue = 0.001f;
                    thresholdSlider.maxValue = 0.1f;
                    thresholdSlider.value = micSettings.voiceDetectionThreshold;
                    UpdateThresholdValueText();
                }
                
                if (silenceTimeSlider != null)
                {
                    silenceTimeSlider.minValue = 0.5f;
                    silenceTimeSlider.maxValue = 5.0f;
                    silenceTimeSlider.value = micSettings.silenceDetectionTime;
                    UpdateSilenceTimeText();
                }
            }
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
    }
    
    // 閾値の表示テキストを更新
    void UpdateThresholdValueText()
    {
        if (thresholdValueText != null)
        {
            thresholdValueText.text = thresholdSlider.value.ToString("F3");
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

    void GoBackToMainScene()
    {
        // メインシーンに戻る
        SceneManager.LoadScene("KotonoGem");
    }
}