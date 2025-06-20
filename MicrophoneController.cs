// Scripts/MicrophoneController.cs
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Linq;

/// <summary>
/// マイク入力、録音、音声/無音検出を管理します。
/// </summary>
public class MicrophoneController : MonoBehaviour
{
    [Header("Api Handler Reference")]
    [SerializeField] private ApiHandler apiHandler;

    // --- 設定値 (SettingsManagerからロード) ---
    private int recordLengthSec = 20;
    private float voiceDetectionThreshold = 0.02f;
    private float silenceDetectionTime = 2.0f;
    private const int SAMPLE_RATE = 44100; // APIの推奨に応じて変更

    // --- 内部状態 ---
    private string selectedMicrophoneDevice;
    private AudioClip microphoneInput;
    private string audioFilePath;
    private bool isRecording = false;
    private bool isListening = false;
    private float lastVoiceDetectedTime = 0f;
    private float[] samples = new float[128]; // 音声レベル確認用のサンプルバッファ

    public bool IsAutoRecordEnabled { get; private set; } = false;

    void Start()
    {
        if (apiHandler == null)
        {
            Debug.LogError("MicrophoneController: ApiHandler is not set in the inspector.");
            // 必須コンポーネントなので、なければ無効化
            enabled = false;
            return;
        }

        // 設定をファイルから読み込み
        LoadSettings();

        // 使用するマイクデバイスを決定
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone found. Recording features will be disabled.");
            // MainUIManager側でボタンを無効化するなどの対応が望ましい
            return;
        }

        // 設定ファイルで指定されたマイクが存在するか確認
        if (!string.IsNullOrEmpty(selectedMicrophoneDevice) && Microphone.devices.Contains(selectedMicrophoneDevice))
        {
             // OK, 設定されたマイクを使用
        }
        else
        {
            // 存在しない、または指定がない場合は最初のマイクをデフォルトとして使用
            selectedMicrophoneDevice = Microphone.devices[0];
            Debug.LogWarning($"Specified microphone not found. Using default microphone: '{selectedMicrophoneDevice}'");
        }
        Debug.Log("Using Microphone: " + selectedMicrophoneDevice);

        // 保存ファイルパスを設定
        audioFilePath = Path.Combine(Application.persistentDataPath, "recorded_audio.wav");
    }

    /// <summary>
    /// micsettings.jsonからマイク関連の設定を読み込みます。
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            string settingsPath = Path.Combine(Application.persistentDataPath, "micsettings.json");
            if (File.Exists(settingsPath))
            {
                string jsonData = File.ReadAllText(settingsPath);
                // SettingsManagerで定義されているクラスを流用
                SettingsManager.MicSettings settings = JsonUtility.FromJson<SettingsManager.MicSettings>(jsonData);

                voiceDetectionThreshold = settings.voiceDetectionThreshold;
                silenceDetectionTime = settings.silenceDetectionTime;
                recordLengthSec = settings.recordLengthSec;
                selectedMicrophoneDevice = settings.selectedMicrophone;

                Debug.Log($"Microphone settings loaded: Threshold={voiceDetectionThreshold}, SilenceTime={silenceDetectionTime}s, RecordLength={recordLengthSec}s, MicDevice='{selectedMicrophoneDevice}'");
            }
            else
            {
                Debug.Log("Microphone settings file not found. Using default values.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load microphone settings: " + e.Message);
        }
    }

    #region 自動録音
    /// <summary>
    /// 自動録音モードを開始します。
    /// </summary>
    public void StartAutoRecording()
    {
        if (string.IsNullOrEmpty(selectedMicrophoneDevice)) return;
        IsAutoRecordEnabled = true;
        Debug.Log("Auto-recording enabled.");
        StartListening();
    }

    /// <summary>
    /// 自動録音モードを停止します。
    /// </summary>
    public void StopAutoRecording()
    {
        IsAutoRecordEnabled = false;
        StopListening();
        if (isRecording)
        {
            StopRecordingAndProcess();
        }
        Debug.Log("Auto-recording disabled.");
    }

    private void StartListening()
    {
        // 既にリスニング中、録音中、または自動録音が無効なら何もしない
        if (string.IsNullOrEmpty(selectedMicrophoneDevice) || !IsAutoRecordEnabled || isListening || isRecording) return;

        isListening = true;
        Debug.Log("Starting to listen for voice activity...");
        // ループ再生(true), 期間1秒, サンプルレートでマイクを起動
        microphoneInput = Microphone.Start(selectedMicrophoneDevice, true, 1, SAMPLE_RATE);
        if (microphoneInput == null)
        {
            Debug.LogError("Microphone.Start failed (for listening).");
            isListening = false;
            return;
        }
        StartCoroutine(MonitorAudioLevel());
    }

    private void StopListening()
    {
        isListening = false;
        StopAllCoroutines(); // モニタリングコルーチンを確実に停止
        if (Microphone.IsRecording(selectedMicrophoneDevice))
        {
            Microphone.End(selectedMicrophoneDevice);
        }
    }

    /// <summary>
    /// マイクからの入力音量を監視し、閾値を超えたら録音を開始するコルーチン。
    /// </summary>
    private IEnumerator MonitorAudioLevel()
    {
        yield return new WaitForSeconds(0.1f); // マイク起動直後は不安定な場合があるため少し待つ

        while (isListening && IsAutoRecordEnabled && !isRecording)
        {
            float rmsValue = GetCurrentRmsValue(microphoneInput);
            if (rmsValue > voiceDetectionThreshold)
            {
                Debug.Log($"Voice detected (Level: {rmsValue:F4}). Starting automatic recording.");
                StopListening(); // モニタリングを停止
                yield return StartCoroutine(StartAutomaticRecordingSequence()); // 録音シーケンスへ
                yield break; // このコルーチンは終了
            }
            yield return null;
        }
        isListening = false;
    }

    /// <summary>
    /// 自動録音のシーケンス（録音開始から無音検出による停止まで）を管理するコルーチン。
    /// </summary>
    private IEnumerator StartAutomaticRecordingSequence()
    {
        if (isRecording || !IsAutoRecordEnabled || string.IsNullOrEmpty(selectedMicrophoneDevice)) yield break;

        isRecording = true;
        Debug.Log($"Automatic recording in progress... (Max {recordLengthSec} seconds)");

        microphoneInput = Microphone.Start(selectedMicrophoneDevice, false, recordLengthSec, SAMPLE_RATE);
        if (microphoneInput == null)
        {
            Debug.LogError("Microphone.Start failed (for automatic recording).");
            isRecording = false;
            if (IsAutoRecordEnabled) StartListening(); // リスニングに復帰
            yield break;
        }

        lastVoiceDetectedTime = Time.time;
        float recordingStartTime = Time.time;
        bool hasVoiceBeenDetected = false;

        while (isRecording && IsAutoRecordEnabled)
        {
            // 最大録音時間に達したら強制終了
            if (Time.time - recordingStartTime >= recordLengthSec)
            {
                Debug.Log("Maximum recording time reached.");
                StopRecordingAndProcess();
                yield break;
            }

            float rmsValue = GetCurrentRmsValue(microphoneInput);
            if (rmsValue > voiceDetectionThreshold)
            {
                lastVoiceDetectedTime = Time.time;
                if (!hasVoiceBeenDetected)
                {
                    hasVoiceBeenDetected = true;
                    Debug.Log("Initial voice detected during recording.");
                }
            }
            else if (hasVoiceBeenDetected && (Time.time - lastVoiceDetectedTime > silenceDetectionTime))
            {
                // 音声が一度検出された後、一定時間無音になったら終了
                Debug.Log("Silence detected for a period. Stopping recording.");
                StopRecordingAndProcess();
                yield break;
            }
            yield return null;
        }
    }
    #endregion

    #region 手動録音
    /// <summary>
    /// 手動での録音を開始するコルーチン。
    /// </summary>
    public IEnumerator StartManualRecording()
    {
        if (string.IsNullOrEmpty(selectedMicrophoneDevice) || isRecording) yield break;

        isRecording = true;
        Debug.Log($"Manual recording started for {recordLengthSec} seconds.");
        microphoneInput = Microphone.Start(selectedMicrophoneDevice, false, recordLengthSec, SAMPLE_RATE);
        if (microphoneInput == null)
        {
            Debug.LogError("Microphone.Start failed (for manual recording).");
            isRecording = false;
            yield break;
        }

        yield return new WaitForSeconds(recordLengthSec);

        Debug.Log("Manual recording time finished.");
        StopRecordingAndProcess();
    }
    #endregion

    /// <summary>
    /// 録音を停止し、ファイル保存とAPIへの解析依頼を行います。
    /// </summary>
    private void StopRecordingAndProcess()
    {
        if (!isRecording && !Microphone.IsRecording(selectedMicrophoneDevice)) return;

        if (Microphone.IsRecording(selectedMicrophoneDevice))
        {
            Microphone.End(selectedMicrophoneDevice);
        }

        if (isRecording)
        {
            isRecording = false;
            if (microphoneInput != null)
            {
                Debug.Log("Stopping recording, saving WAV file, and requesting analysis.");
                SaveWavFile(audioFilePath, microphoneInput);
                apiHandler.StartAnalysis(audioFilePath);
            }
            else
            {
                Debug.LogError("microphoneInput is null. Skipping save and analysis.");
            }
        }
        
        // 自動録音が有効な場合は、再度音声のリスニングを開始する
        if (IsAutoRecordEnabled)
        {
            StartListening();
        }
    }

    /// <summary>
    /// 現在のマイク入力のRMS（二乗平均平方根）値を計算して返します。音量の目安になります。
    /// </summary>
    private float GetCurrentRmsValue(AudioClip clip)
    {
        if (clip == null || !Microphone.IsRecording(selectedMicrophoneDevice))
        {
            if (isListening)
            {
                Debug.LogWarning("Microphone stopped while listening. Attempting to restart.");
                StopListening();
                if (IsAutoRecordEnabled) StartListening();
            }
            return 0f;
        }

        int micPosition = Microphone.GetPosition(selectedMicrophoneDevice);
        // バッファサイズより手前にポジションがある場合はスキップ
        if (micPosition < samples.Length)
        {
            return 0f;
        }

        // 最新のサンプルデータを取得
        clip.GetData(samples, micPosition - samples.Length);

        // RMSを計算
        float sum = 0f;
        foreach (float sample in samples)
        {
            sum += sample * sample;
        }
        return Mathf.Sqrt(sum / samples.Length);
    }

    /// <summary>
    /// AudioClipをWAVファイルとして保存します。
    /// </summary>
    private void SaveWavFile(string filepath, AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("SaveWavFile failed: AudioClip is null.");
            return;
        }
        try
        {
            WavUtility.FromAudioClip(clip, filepath, true);
            Debug.Log($"WAV file saved successfully: {filepath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"An exception occurred in WavUtility.FromAudioClip: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// このオブジェクトが破棄されるときに、実行中の処理を停止します。
    /// </summary>
    void OnDestroy()
    {
        // アプリケーション終了時などに、マイクが動作し続けないようにする
        StopAutoRecording();
    }
}