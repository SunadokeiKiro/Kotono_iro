using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;

public class ButtonScript : MonoBehaviour
{
    [Header("GameController Reference")]
    public GameController gameController;

    [Header("UI Elements (Assign in Inspector)")]
    public Button sendButton;
    public Button sampleButton;
    public Button recButton;
    public Button autoRecButton;
    public GameObject recSec;
    public InputField json;
    public InputField jsontext;
    public Button settingsButton;
    public Toggle vadToggle;
    public Slider vadThresholdSlider;
    public Text statusText;

    private string apiUrl = "https://acp-api-async.amivoice.com/v1/recognitions";
    private string sampleUrl = "https://acp-dsrpp.amivoice.com/v1/sentiment-analysis/ja/result-parameters.json";
    private string apiKeyFilePath;
    private string audioFilePath;
    private string sessionID;
    private string microphone;
    private AudioClip microphoneInput;
    private int RECORD_LENGTH_SEC = 20;
    private const int SAMPLE_RATE = 41100;

    // WebRTC VAD
    private bool isVadEnabled = false;
    private bool isListening = false;
    private bool isSpeaking = false;
    private float vadThreshold = 0.3f;
    private float silenceThreshold = 1.0f;
    private float currentSilenceTime = 0f;
    private float speakingTimeoutDuration = 30f;
    private float speakingStartTime = 0f;
    private AudioClip listeningClip;
    private List<float> audioBuffer = new List<float>();
    private int bufferSize = 1024;
    private WebRTCVad vad;

    private const int VAD_SAMPLE_RATE = 16000;
    private const int VAD_FRAME_SIZE = 480;
    private float[] vadBuffer;
    private int vadBufferPosition = 0;
    private int aggressiveness = 2;
    private List<float> recordedSamples = new List<float>();

    void Start()
    {
        if (sendButton == null || recButton == null || json == null || jsontext == null || recSec == null)
        {
            Debug.LogError("InspectorにUI要素が正しく割り当てられていません。各フィールドを確認してください。");
            return;
        }

        json.text = "Start";

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OpenSettingsScene);
        }

        microphone = Microphone.devices.FirstOrDefault();
        Debug.Log("microphone: " + microphone);
        if (microphone == null)
        {
            Debug.LogError("No microphone found");
            return;
        }

        apiKeyFilePath = Path.Combine(Application.streamingAssetsPath, "apikey.txt");

        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            audioFilePath = Path.Combine(Application.persistentDataPath, "test.wav");
        }
        else
        {
            audioFilePath = Path.Combine(Application.streamingAssetsPath, "test.wav");
        }

        sendButton.onClick.AddListener(() => StartCoroutine(ReadApiKeyAndPostRequest()));
        recButton.onClick.AddListener(() => StartCoroutine(StartRec()));

        if (autoRecButton != null)
            autoRecButton.onClick.AddListener(ToggleVoiceActivatedRecording);

        if (vadToggle != null)
            vadToggle.onValueChanged.AddListener(OnVadToggleChanged);

        if (vadThresholdSlider != null)
        {
            vadThresholdSlider.onValueChanged.AddListener(OnVadThresholdChanged);
            vadThresholdSlider.value = vadThreshold;
        }

        InitializeVad();
    }

    private void InitializeVad()
    {
        try
        {
            vad = new WebRTCVad();
            vad.Init();
            vad.SetMode(aggressiveness);

            vadBuffer = new float[VAD_FRAME_SIZE];

            Debug.Log("WebRTC VAD initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to initialize WebRTC VAD: " + e.Message);
            if (statusText != null)
                statusText.text = "VAD初期化エラー";
        }
    }

    private void OnDestroy()
    {
        if (vad != null)
            vad.Free();
    }

    private void OnVadToggleChanged(bool isOn)
    {
        isVadEnabled = isOn;
        if (statusText != null)
            statusText.text = isOn ? "VAD: オン" : "VAD: オフ";
    }

    private void OnVadThresholdChanged(float value)
    {
        vadThreshold = value;
        if (statusText != null)
            statusText.text = $"VAD感度: {vadThreshold:F2}";
    }

    private void ToggleVoiceActivatedRecording()
    {
        if (!isListening)
            StartVoiceActivatedListening();
        else
            StopVoiceActivatedListening();
    }

    private void StartVoiceActivatedListening()
    {
        if (isListening) return;

        isListening = true;
        isSpeaking = false;
        recordedSamples.Clear();
        currentSilenceTime = 0f;

        listeningClip = Microphone.Start(microphone, true, 1, VAD_SAMPLE_RATE);

        if (autoRecButton != null)
            autoRecButton.GetComponentInChildren<Text>().text = "音声検出中...";

        if (statusText != null)
            statusText.text = "音声を検出しています...";

        json.text = "音声検出待機中...";

        StartCoroutine(ProcessAudioForVad());
    }

    private void StopVoiceActivatedListening()
    {
        if (!isListening) return;

        isListening = false;
        isSpeaking = false;
        Microphone.End(microphone);

        if (autoRecButton != null)
            autoRecButton.GetComponentInChildren<Text>().text = "音声検出開始";

        if (statusText != null)
            statusText.text = "音声検出停止";

        json.text = "音声検出を停止しました";

        StopAllCoroutines();
    }

    IEnumerator ProcessAudioForVad()
    {
        int lastPos = 0;

        while (isListening)
        {
            int pos = Microphone.GetPosition(microphone);
            if (pos < lastPos)
                lastPos = 0;

            if (pos != lastPos)
            {
                int lengthToRead = pos - lastPos;
                if (lengthToRead > 0)
                {
                    float[] samples = new float[lengthToRead];
                    listeningClip.GetData(samples, lastPos);
                    ProcessAudioSamples(samples);
                }
                lastPos = pos;
            }

            if (isSpeaking && Time.time - speakingStartTime > speakingTimeoutDuration)
            {
                Debug.Log("最大録音時間に達しました");
                StopRecording();
            }

            yield return null;
        }
    }

    private void ProcessAudioSamples(float[] samples)
    {
        foreach (float sample in samples)
        {
            if (isSpeaking)
                recordedSamples.Add(sample);

            vadBuffer[vadBufferPosition++] = sample;

            if (vadBufferPosition >= VAD_FRAME_SIZE)
            {
                ProcessVadFrame();
                vadBufferPosition = 0;
            }
        }
    }

    private void ProcessVadFrame()
    {
        if (!isVadEnabled || vad == null) return;

        try
        {
            byte[] audioBytes = new byte[VAD_FRAME_SIZE * 2];
            for (int i = 0; i < VAD_FRAME_SIZE; i++)
            {
                short sampleValue = (short)(vadBuffer[i] * short.MaxValue);
                audioBytes[i * 2] = (byte)(sampleValue & 0xFF);
                audioBytes[i * 2 + 1] = (byte)((sampleValue >> 8) & 0xFF);
            }

            bool speechDetected = vad.Process(VAD_SAMPLE_RATE, audioBytes, audioBytes.Length);
            UpdateSpeechState(speechDetected);
        }
        catch (Exception e)
        {
            Debug.LogError("VAD処理エラー: " + e.Message);
        }
    }

    private void UpdateSpeechState(bool speechDetected)
    {
        if (speechDetected)
        {
            currentSilenceTime = 0f;
            if (!isSpeaking)
                StartRecording();
        }
        else if (isSpeaking)
        {
            currentSilenceTime += (float)VAD_FRAME_SIZE / VAD_SAMPLE_RATE;
            if (currentSilenceTime >= silenceThreshold)
                StopRecording();
        }
    }

    private void StartRecording()
    {
        isSpeaking = true;
        speakingStartTime = Time.time;
        recordedSamples.Clear();

        Debug.Log("音声検出: 録音開始");
        if (statusText != null)
            statusText.text = "録音中...";
        json.text = "音声検出: 録音中...";
    }

    private void StopRecording()
    {
        if (!isSpeaking) return;

        isSpeaking = false;
        Debug.Log("録音停止: サンプル数 " + recordedSamples.Count);

        if (statusText != null)
            statusText.text = "録音完了 - 処理中";
        json.text = "録音完了 - 処理中";

        if (recordedSamples.Count > 0)
        {
            float[] audioData = recordedSamples.ToArray();
            microphoneInput = AudioClip.Create("RecordedAudio", audioData.Length, 1, VAD_SAMPLE_RATE, false);
            microphoneInput.SetData(audioData, 0);

            SaveWavFile(audioFilePath, microphoneInput);
            StartCoroutine(ReadApiKeyAndPostRequest());
        }
        else
        {
            Debug.LogWarning("録音データがありません");
            if (statusText != null)
                statusText.text = "録音データなし";
            json.text = "録音データがありません";
        }

        currentSilenceTime = 0f;
    }

    void OpenSettingsScene()
    {
        SceneManager.LoadScene("SettingsScene");
    }

    private string LoadApiKey()
    {
        string apiKey = string.Empty;
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "apikey.txt");
            if (File.Exists(path))
                apiKey = File.ReadAllText(path).Trim();
            else
                Debug.LogError("APIキーファイルが見つかりません: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("APIキーの読み込みに失敗: " + e.Message);
        }
        return apiKey;
    }

    IEnumerator ReadApiKeyAndPostRequest()
    {
        json.text = "Post Requesting...";
        string apiKey = string.Empty;

        if (apiKeyFilePath.StartsWith("http") || Application.platform == RuntimePlatform.Android)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(apiKeyFilePath))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                    apiKey = request.downloadHandler.text.Trim();
                else
                    yield break;
            }
        }
        else if (File.Exists(apiKeyFilePath))
        {
            apiKey = File.ReadAllText(apiKeyFilePath).Trim();
        }

        yield return StartCoroutine(PostRequest(apiKey));
    }

    IEnumerator PostRequest(string apiKey)
    {
        var form = new List<IMultipartFormSection>
        {
            new MultipartFormDataSection("u", apiKey),
            new MultipartFormDataSection("d", "grammarFileNames=-a-general loggingOptOut=True sentimentAnalysis=True"),
            new MultipartFormDataSection("c", "LSB44K")
        };

        byte[] audioData = File.ReadAllBytes(audioFilePath);
        form.Add(new MultipartFormFileSection("a", audioData, "test.wav", "audio/wav"));

        using (UnityWebRequest request = UnityWebRequest.Post(apiUrl, form))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Save(request.downloadHandler.text);
                var response = JsonUtility.FromJson<RecognitionResponse>(request.downloadHandler.text);
                sessionID = response.sessionid;
                StartCoroutine(PollJobStatus(apiKey));
            }
            else
            {
                json.text = "ERROR: Post Request failed";
            }
        }
    }

    IEnumerator PollJobStatus(string apiKey)
    {
        json.text = "Poll Job";
        string pollUrl = $"https://acp-api-async.amivoice.com/v1/recognitions/{sessionID}";
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(pollUrl))
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<SentimentAnalysisResponse>(request.downloadHandler.text);
                    if (response.status == "completed")
                    {
                        if (gameController != null)
                            gameController.SetParametersFromJson(request.downloadHandler.text);

                        var sb = new StringBuilder();
                        foreach (var seg in response.segments)
                            foreach (var res in seg.results)
                                foreach (var tok in res.tokens)
                                    sb.Append(tok.written == "。" ? tok.written + "\n" : tok.written + " ");

                        if (jsontext != null)
                            jsontext.text = sb.ToString();

                        var sumSegments = new SentimentSegment();
                        int cnt = 0;
                        foreach (var seg in response.sentiment_analysis.segments)
                        {
                            sumSegments.energy += seg.energy;
                            sumSegments.content += seg.content;
                            sumSegments.upset += seg.upset;
                            sumSegments.aggression += seg.aggression;
                            sumSegments.stress += seg.stress;
                            sumSegments.uncertainty += seg.uncertainty;
                            sumSegments.excitement += seg.excitement;
                            sumSegments.concentration += seg.concentration;
                            sumSegments.emo_cog += seg.emo_cog;
                            sumSegments.hesitation += seg.hesitation;
                            sumSegments.brain_power += seg.brain_power;
                            sumSegments.embarrassment += seg.embarrassment;
                            sumSegments.intensive_thinking += seg.intensive_thinking;
                            sumSegments.imagination_activity += seg.imagination_activity;
                            sumSegments.extreme_emotion += seg.extreme_emotion;
                            sumSegments.passionate += seg.passionate;
                            sumSegments.atmosphere += seg.atmosphere;
                            sumSegments.anticipation += seg.anticipation;
                            sumSegments.dissatisfaction += seg.dissatisfaction;
                            sumSegments.confidence += seg.confidence;
                            cnt++;
                        }
                        Ave(sumSegments, cnt);
                        Save(request.downloadHandler.text);

                        if (isListening && isVadEnabled)
                            json.text = "音声検出待機中...";

                        break;
                    }
                    else
                    {
                        json.text = response.status switch
                        {
                            "started" => "Poll Job waiting... 1/3",
                            "processing" => "Poll Job waiting... 2/3",
                            "error" => "ERROR: Poll Job failed",
                            _ => "Poll Job waiting..."
                        };
                        if (response.status == "error") break;
                    }
                }
                else
                {
                    json.text = "ERROR: Poll Job failed";
                    break;
                }
            }
            yield return new WaitForSeconds(5f);
        }
    }

    IEnumerator StartRec()
    {
        yield return null;
        string tmp = recSec.GetComponent<Text>().text;
        if (!int.TryParse(tmp, out RECORD_LENGTH_SEC) || RECORD_LENGTH_SEC <= 0)
        {
            json.text = "ERROR: invalid record sec";
            yield break;
        }

        microphoneInput = Microphone.Start(microphone, false, RECORD_LENGTH_SEC, SAMPLE_RATE);
        json.text = $"START Recording for {RECORD_LENGTH_SEC} sec!";
        StartCoroutine(WaitAndExecute());
    }

    IEnumerator WaitAndExecute()
    {
        yield return new WaitForSeconds(RECORD_LENGTH_SEC);
        SaveWavFile(audioFilePath, microphoneInput);
        json.text = "RECORDING END";
    }

    private void SaveWavFile(string filepath, AudioClip clip)
    {
        WavUtility.FromAudioClip(clip, filepath, true);
    }

    void Ave(SentimentSegment res, int cnt)
    {
        json.text =
            $"エネルギー: {Calc(res.energy, cnt)}\n" +
            $"よろこび: {Calc(res.content, cnt)}\n" +
            $"動揺: {Calc(res.upset, cnt)}\n" +
            $"攻撃性 憤り: {Calc(res.aggression, cnt)}\n" +
            $"ストレス: {Calc(res.stress, cnt)}\n" +
            $"不確実性: {Calc(res.uncertainty, cnt)}\n" +
            $"興奮: {Calc(res.excitement, cnt)}\n" +
            $"集中: {Calc(res.concentration, cnt)}\n" +
            $"感情バランス論理: {Calc(res.emo_cog, cnt)}\n" +
            $"ためらい(躊躇): {Calc(res.hesitation, cnt)}\n" +
            $"脳活動: {Calc(res.brain_power, cnt)}\n" +
            $"困惑: {Calc(res.embarrassment, cnt)}\n" +
            $"思考: {Calc(res.intensive_thinking, cnt)}\n" +
            $"想像力: {Calc(res.imagination_activity, cnt)}\n" +
            $"極端な起伏(感情): {Calc(res.extreme_emotion, cnt)}\n" +
            $"情熱: {Calc(res.passionate, cnt)}\n" +
            $"雰囲気: {Calc(res.atmosphere, cnt)}\n" +
            $"期待: {Calc(res.anticipation, cnt)}\n" +
            $"不満: {Calc(res.dissatisfaction, cnt)}\n" +
            $"自信: {Calc(res.confidence, cnt)}";
    }

    void Save(string data)
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, "log.txt");
            File.WriteAllText(filePath, data);
        }
        catch (Exception e)
        {
            Debug.LogError("ログデータの保存に失敗: " + e.Message);
        }
    }

    float Calc(int num, int div)
    {
        return (float)num / div;
    }
}

public class WebRTCVad
{
    private IntPtr vadInst;

    [System.Runtime.InteropServices.DllImport("webrtc_vad")]
    private static extern IntPtr WebRtcVad_Create();

    [System.Runtime.InteropServices.DllImport("webrtc_vad")]
    private static extern void WebRtcVad_Free(IntPtr vadInst);

    [System.Runtime.InteropServices.DllImport("webrtc_vad")]
    private static extern int WebRtcVad_Init(IntPtr vadInst);

    [System.Runtime.InteropServices.DllImport("webrtc_vad")]
    private static extern int WebRtcVad_set_mode(IntPtr vadInst, int mode);

    [System.Runtime.InteropServices.DllImport("webrtc_vad")]
    private static extern int WebRtcVad_Process(IntPtr vadInst, int fs, byte[] audioFrame, int frameLength);

    public WebRTCVad()
    {
        vadInst = IntPtr.Zero;
    }

    public void Init()
    {
        vadInst = WebRtcVad_Create();
        if (vadInst == IntPtr.Zero)
            throw new Exception("Failed to create WebRTC VAD instance");

        int result = WebRtcVad_Init(vadInst);
        if (result != 0)
            throw new Exception("Failed to initialize WebRTC VAD: " + result);
    }

    public void SetMode(int mode)
    {
        if (vadInst == IntPtr.Zero)
            throw new Exception("WebRTC VAD not initialized");

        int result = WebRtcVad_set_mode(vadInst, mode);
        if (result != 0)
            throw new Exception("Failed to set WebRTC VAD mode: " + result);
    }

    public bool Process(int sampleRate, byte[] audioFrame, int frameLength)
    {
        if (vadInst == IntPtr.Zero)
            throw new Exception("WebRTC VAD not initialized");

        if (sampleRate != 8000 && sampleRate != 16000 && sampleRate != 32000)
            throw new Exception("Unsupported sample rate: " + sampleRate);

        int samplesIn10ms = sampleRate / 100;
        bool validFrame = false;
        for (int i = 1; i <= 3; i++)
        {
            if (frameLength == i * samplesIn10ms * 2)
            {
                validFrame = true;
                break;
            }
        }
        if (!validFrame)
            throw new Exception("Invalid frame length: " + frameLength);

        int result = WebRtcVad_Process(vadInst, sampleRate, audioFrame, frameLength);
        if (result < 0)
            throw new Exception("WebRTC VAD processing error: " + result);

        return result == 1;
    }

    public void Free()
    {
        if (vadInst != IntPtr.Zero)
        {
            WebRtcVad_Free(vadInst);
            vadInst = IntPtr.Zero;
        }
    }
}
