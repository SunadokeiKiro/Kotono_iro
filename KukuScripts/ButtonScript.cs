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

    // Inspectorで必ずアタッチする
    [Header("UI Elements (Assign in Inspector)")]
    public Button sendButton;
    public Button sampleButton;  // 必要であれば利用
    public Button recButton;
    public GameObject recSec;    // 録音秒数を表示・入力するTextが含まれるオブジェクト
    public InputField json;      // APIの進捗などを表示
    public InputField jsontext;  // 結果データを表示
    public Button settingsButton;
    
    // 自動録音用のUI要素
    [Header("Auto Recording Settings")]
    public Toggle autoRecordToggle;
    public Text autoRecordStatusText;
    
    private string apiUrl = "https://acp-api-async.amivoice.com/v1/recognitions";
    private string sampleUrl = "https://acp-dsrpp.amivoice.com/v1/sentiment-analysis/ja/result-parameters.json";
    private string apiKeyFilePath;
    private string audioFilePath;
    private string sessionID;
    private string microphone;
    private AudioClip microphoneInput;
    private int RECORD_LENGTH_SEC = 20;
    private const int SAMPLE_RATE = 41100;
    
    // 自動録音用の変数
    private bool isAutoRecordEnabled = false;
    private bool isListeningForVoice = false;
    private bool isRecording = false;
    private float[] samples = new float[128];
    private float lastVoiceDetectedTime = 0f;
    
    // システム設定の参照（SettingsManagerから読み込む）
    private float voiceDetectionThreshold = 0.02f;
    private float silenceDetectionTime = 2.0f;
    private string selectedMicrophone = "";

    void Start()
    {
        // 手動アタッチが正しく行われているか確認
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

        // 設定を読み込む
        LoadSettings();

        // マイクの設定
        // 設定で選択されたマイクを優先的に使用
        if (!string.IsNullOrEmpty(selectedMicrophone) && Array.IndexOf(Microphone.devices, selectedMicrophone) >= 0)
        {
            microphone = selectedMicrophone;
        }
        else
        {
            microphone = Microphone.devices.FirstOrDefault();
        }
        
        Debug.Log("microphone: " + microphone);
        if (microphone == null)
        {
            Debug.LogError("No microphone found");
            return;
        }

        // プラットフォーム別にファイル保存先を設定
        if (Application.platform == RuntimePlatform.Android)
        {
            audioFilePath = Path.Combine(Application.persistentDataPath, "test.wav");
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            audioFilePath = Path.Combine(Application.persistentDataPath, "test.wav");
        }
        else
        {
            audioFilePath = Path.Combine(Application.streamingAssetsPath, "test.wav");
        }

        // ボタンのクリックイベントにリスナーを設定（手動アタッチ済みのため直接利用）
        sendButton.onClick.AddListener(() => StartCoroutine(ReadApiKeyAndPostRequest()));
        recButton.onClick.AddListener(() => StartCoroutine(StartRec()));
        
        // 自動録音のトグルイベントを設定
        if (autoRecordToggle != null)
        {
            autoRecordToggle.onValueChanged.AddListener(ToggleAutoRecording);
            UpdateAutoRecordStatus();
        }
        else
        {
            Debug.LogWarning("自動録音トグルがInspectorにアタッチされていません");
        }
    }
    
    // 設定ファイルから設定を読み込む
    void LoadSettings()
    {
        try
        {
            // SettingsManagerと同じファイル名を使用
            string settingsPath = Path.Combine(Application.persistentDataPath, "micsettings.json");
            if (File.Exists(settingsPath))
            {
                string jsonData = File.ReadAllText(settingsPath);
                SettingsManager.MicSettings settings = JsonUtility.FromJson<SettingsManager.MicSettings>(jsonData);
                
                // 設定を読み込む
                voiceDetectionThreshold = settings.voiceDetectionThreshold;
                silenceDetectionTime = settings.silenceDetectionTime;
                RECORD_LENGTH_SEC = settings.recordLengthSec;
                selectedMicrophone = settings.selectedMicrophone;
                
                // recSecに録音時間を設定
                if (recSec != null && recSec.GetComponent<Text>() != null)
                {
                    recSec.GetComponent<Text>().text = RECORD_LENGTH_SEC.ToString();
                }
                
                Debug.Log($"マイク設定を読み込みました: 閾値={voiceDetectionThreshold}, 無音検出時間={silenceDetectionTime}, 録音時間={RECORD_LENGTH_SEC}秒, マイク={selectedMicrophone}");
            }
            else
            {
                Debug.Log("設定ファイルが見つからないため、デフォルト値を使用します");
                // recSecにデフォルト値を設定
                if (recSec != null && recSec.GetComponent<Text>() != null)
                {
                    recSec.GetComponent<Text>().text = RECORD_LENGTH_SEC.ToString();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("設定の読み込みに失敗: " + e.Message);
        }
    }
    
    // 自動録音トグルの状態変更時に呼ばれる
    public void ToggleAutoRecording(bool isOn)
    {
        isAutoRecordEnabled = isOn;
        UpdateAutoRecordStatus();
        
        if (isAutoRecordEnabled)
        {
            StartListeningForVoice();
        }
        else
        {
            StopListeningForVoice();
            if (isRecording)
            {
                StopRecording();
            }
        }
    }
    
    // 自動録音状態テキストを更新
    void UpdateAutoRecordStatus()
    {
        if (autoRecordStatusText != null)
        {
            autoRecordStatusText.text = isAutoRecordEnabled ? "自動録音: オン" : "自動録音: オフ";
            autoRecordStatusText.color = isAutoRecordEnabled ? Color.green : Color.red;
        }
    }
    
    // 音声検出のためのリスニング開始
    void StartListeningForVoice()
    {
        if (!isListeningForVoice && !isRecording)
        {
            isListeningForVoice = true;
            Debug.Log("音声検出リスニングを開始します");
            
            // 音声検出用マイク入力の開始
            AudioClip listeningClip = Microphone.Start(microphone, true, 1, SAMPLE_RATE);
            StartCoroutine(MonitorAudioLevel(listeningClip));
        }
    }
    
    // 音声検出のためのリスニング停止
    void StopListeningForVoice()
    {
        if (isListeningForVoice)
        {
            isListeningForVoice = false;
            Microphone.End(microphone);
            Debug.Log("音声検出リスニングを停止しました");
        }
    }
    
    // 音声レベルのモニタリング
    IEnumerator MonitorAudioLevel(AudioClip clip)
    {
        while (isListeningForVoice && isAutoRecordEnabled && !isRecording)
        {
            // サンプルデータを取得
            clip.GetData(samples, 0);
            
            // 音量レベルを計算
            float rmsValue = 0f;
            foreach (float sample in samples)
            {
                rmsValue += sample * sample;
            }
            rmsValue = Mathf.Sqrt(rmsValue / samples.Length);
            
            // 閾値を超えた場合、録音開始
            if (rmsValue > voiceDetectionThreshold)
            {
                Debug.Log("音声を検出: レベル " + rmsValue);
                StopListeningForVoice();
                StartCoroutine(StartAutomaticRecording());
                yield break;
            }
            
            yield return null;
        }
    }
    
    // 自動録音の開始
    IEnumerator StartAutomaticRecording()
    {
        if (!isRecording && isAutoRecordEnabled)
        {
            isRecording = true;
            Debug.Log("自動録音を開始します");
            json.text = "自動録音中...";
            
            // 録音開始
            microphoneInput = Microphone.Start(microphone, true, RECORD_LENGTH_SEC, SAMPLE_RATE);
            
            lastVoiceDetectedTime = Time.time;
            
            // 録音中の音声レベルをモニタリング
            StartCoroutine(MonitorSilence());
        }
        yield return null;
    }
    
    // 無音区間の検出
    IEnumerator MonitorSilence()
    {
        float[] silenceSamples = new float[128];
        bool hasDetectedSilence = false;
        
        while (isRecording && isAutoRecordEnabled)
        {
            // 音量レベルを確認
            microphoneInput.GetData(silenceSamples, microphoneInput.samples - 128);
            
            float rmsValue = 0f;
            foreach (float sample in silenceSamples)
            {
                rmsValue += sample * sample;
            }
            rmsValue = Mathf.Sqrt(rmsValue / silenceSamples.Length);
            
            // 音声がある場合は最終検出時間を更新
            if (rmsValue > voiceDetectionThreshold)
            {
                lastVoiceDetectedTime = Time.time;
                hasDetectedSilence = false;
            }
            // 一定時間音声がない場合は録音停止
            else if (!hasDetectedSilence && Time.time - lastVoiceDetectedTime > silenceDetectionTime)
            {
                Debug.Log("無音を検出: 録音を停止します");
                hasDetectedSilence = true;
                StopRecording();
                break;
            }
            
            yield return null;
        }
    }
    
    // 録音停止処理
    void StopRecording()
    {
        if (isRecording)
        {
            isRecording = false;
            Debug.Log("録音を停止し、WAVファイルに保存します。");
            
            // 録音停止
            Microphone.End(microphone);
            
            // WAVファイルとして保存
            SaveWavFile(audioFilePath, microphoneInput);
            json.text = "自動録音完了 - 解析開始";
            
            // APIリクエスト開始
            StartCoroutine(ReadApiKeyAndPostRequest());
            
            // 自動録音モードが有効なら、また音声検出を開始
            if (isAutoRecordEnabled)
            {
                StartListeningForVoice();
            }
        }
    }

    void OpenSettingsScene()
    {
        SceneManager.LoadScene("SettingsScene");
    }

    // APIキーを読み込む
    private string LoadApiKey()
    {
        string apiKey = "";
        try
        {
            string apiKeyFilePath = Path.Combine(Application.persistentDataPath, "apikey.txt");
            if (File.Exists(apiKeyFilePath))
            {
                apiKey = File.ReadAllText(apiKeyFilePath).Trim();
                Debug.Log("保存されたAPIキーを読み込みました");
            }
            else
            {
                Debug.LogError("APIキーファイルが見つかりません: " + apiKeyFilePath);
                json.text = "ERROR: APIキーが設定されていません。設定画面で設定してください。";
            }
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
        
        // APIキーをファイルから読み込む
        string apiKey = LoadApiKey();
        
        // APIキーが読み込めなかった場合
        if (string.IsNullOrEmpty(apiKey))
        {
            json.text = "ERROR: APIキーが設定されていません。設定画面で設定してください。";
            Debug.LogError("APIキーが設定されていません");
            yield break;
        }
        
        // リクエスト送信
        yield return StartCoroutine(PostRequest(apiKey));
    }

    IEnumerator PostRequest(string apiKey)
    {
        // マルチパートフォームデータの作成
        List<IMultipartFormSection> form = new List<IMultipartFormSection>
        {
            new MultipartFormDataSection("u", apiKey),
            new MultipartFormDataSection("d", "grammarFileNames=-a-general loggingOptOut=True sentimentAnalysis=True"),
            new MultipartFormDataSection("c", "LSB44K")
        };

        // 音声ファイルの読み込み
        byte[] audioData = File.ReadAllBytes(audioFilePath);
        form.Add(new MultipartFormFileSection("a", audioData, "test.wav", "audio/wav"));

        using (UnityWebRequest request = UnityWebRequest.Post(apiUrl, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("リクエスト成功: " + request.downloadHandler.text);
                Save(request.downloadHandler.text);

                // sessionID取得（レスポンスからパース）
                var response = JsonUtility.FromJson<RecognitionResponse>(request.downloadHandler.text);
                sessionID = response.sessionid;
                Debug.Log("sessionID: " + sessionID);

                // セッションの結果をポーリング
                StartCoroutine(PollJobStatus(apiKey));
            }
            else
            {
                Debug.LogError("リクエスト失敗: " + request.error);
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
                    SentimentAnalysisResponse response = JsonUtility.FromJson<SentimentAnalysisResponse>(request.downloadHandler.text);
                    Debug.Log("ジョブ状態: " + response.status);
                    if (response.status == "completed")
                    {
                        json.text = "Poll Job waiting... 3/3";
                        Debug.Log("音声認識完了！");

                        if (gameController != null)
                        {
                            gameController.SetParametersFromJson(request.downloadHandler.text);
                            Debug.Log("感情パラメータをGameControllerに送信しました");
                        }
                        else
                        {
                            Debug.LogError("GameControllerが見つかりません！");
                        }

                        StringBuilder sb = new StringBuilder();
                        if (response != null && response.segments != null)
                        {
                            foreach (Segment seg in response.segments)
                            {
                                foreach (Result res in seg.results)
                                {
                                    foreach (Token tok in res.tokens)
                                    {
                                        if (tok.written.Equals("。"))
                                        {
                                            sb.Append(tok.written).Append("\n");
                                        }
                                        else
                                        {
                                            sb.Append(tok.written).Append(" ");
                                        }
                                    }
                                }
                            }
                        }

                        if (jsontext != null)
                        {
                            jsontext.text = sb.ToString();
                        }
                        else
                        {
                            Debug.LogError("jsontext が null です！");
                            json.text = "ERROR: jsontext is null!";
                        }

                        SentimentSegment sumSegments = new SentimentSegment();
                        int cnt = 0;
                        foreach (SentimentSegment seg in response.sentiment_analysis.segments)
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
                        break;
                    }
                    else
                    {
                        if (response.status == "started")
                        {
                            json.text = "Poll Job waiting... 1/3";
                        }
                        else if (response.status == "processing")
                        {
                            json.text = "Poll Job waiting... 2/3";
                        }
                        else if (response.status == "error")
                        {
                            json.text = "ERROR: Poll Job failed";
                            break;
                        }
                        else
                        {
                            json.text = "Poll Job waiting...";
                        }
                    }
                }
                else
                {
                    Debug.LogError("ポーリング失敗: " + request.error);
                    json.text = "ERROR: Poll Job failed";
                    break;
                }
            }

            yield return new WaitForSeconds(4f);
            json.text = "Poll Job waiting";
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator StartRec()
    {
        yield return null;

        string tmp = recSec.GetComponent<Text>().text;
        Debug.Log(tmp);

        if (RECORD_LENGTH_SEC == 0)
        {
            Debug.Log("ERROR: null record sec");
            json.text = "ERROR: null record sec";
            yield break;
        }

        try
        {
            int parsedLength = int.Parse(tmp);
            if (parsedLength > 0)
            {
                RECORD_LENGTH_SEC = parsedLength;
                microphoneInput = Microphone.Start(microphone, false, RECORD_LENGTH_SEC, SAMPLE_RATE);
                Debug.Log("録音を開始します。何か話してください。");
                json.text = "START Recording for " + RECORD_LENGTH_SEC + " sec!";
                StartCoroutine(WaitAndExecute());
            }
            else
            {
                Debug.Log("error: RECORD_LENGTH_SEC");
                json.text = "ERROR: 録音時間は1秒以上に設定してください";
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
            json.text = "ERROR: 録音秒数の解析に失敗しました";
            yield break;
        }
    }

    IEnumerator WaitAndExecute()
    {
        yield return new WaitForSeconds(RECORD_LENGTH_SEC);
        Debug.Log("録音を終了し、WAVファイルに保存します。");

        var filePath = string.Format("{0}", audioFilePath);
        Debug.Log("filePath: " + filePath);

        SaveWavFile(filePath, microphoneInput);
        json.text = "RECORDING END";
    }

    private void SaveWavFile(string filepath, AudioClip clip)
    {
        // WavUtilityのスクリプトが必要です
        byte[] wavBytes = WavUtility.FromAudioClip(clip, filepath, true);
    }

    void Ave(SentimentSegment res, int cnt)
    {
        Debug.Log("Ave values below");
        Debug.Log("エネルギー: " + (double)res.energy / cnt);
        Debug.Log("よろこび: " + (double)res.content / cnt);
        Debug.Log("動揺: " + (double)res.upset / cnt);
        Debug.Log("攻撃性 憤り: " + (double)res.aggression / cnt);
        Debug.Log("ストレス: " + (double)res.stress / cnt);
        Debug.Log("不確実性: " + (double)res.uncertainty / cnt);
        Debug.Log("興奮: " + (double)res.excitement / cnt);
        Debug.Log("集中: " + (double)res.concentration / cnt);
        Debug.Log("感情バランス論理: " + (double)res.emo_cog / cnt);
        Debug.Log("ためらい(躊躇): " + (double)res.hesitation / cnt);
        Debug.Log("脳活動: " + (double)res.brain_power / cnt);
        Debug.Log("困惑: " + (double)res.embarrassment / cnt);
        Debug.Log("思考: " + (double)res.intensive_thinking / cnt);
        Debug.Log("想像力: " + (double)res.imagination_activity / cnt);
        Debug.Log("極端な起伏(感情): " + (double)res.extreme_emotion / cnt);
        Debug.Log("情熱: " + (double)res.passionate / cnt);
        Debug.Log("雰囲気: " + (double)res.atmosphere / cnt);
        Debug.Log("期待: " + (double)res.anticipation / cnt);
        Debug.Log("不満: " + (double)res.dissatisfaction / cnt);
        Debug.Log("自信: " + (double)res.confidence / cnt);

        json.text = "エネルギー: " + Calc(res.energy, cnt) + "\n" +
                    "よろこび: " + Calc(res.content, cnt) + "\n" +
                    "動揺: " + Calc(res.upset, cnt) + "\n" +
                    "攻撃性 憤り: " + Calc(res.aggression, cnt) + "\n" +
                    "ストレス: " + Calc(res.stress, cnt) + "\n" +
                    "不確実性: " + Calc(res.uncertainty, cnt) + "\n" +
                    "興奮: " + Calc(res.excitement, cnt) + "\n" +
                    "集中: " + Calc(res.concentration, cnt) + "\n" +
                    "感情バランス論理: " + Calc(res.emo_cog, cnt) + "\n" +
                    "ためらい(躊躇): " + Calc(res.hesitation, cnt) + "\n" +
                    "脳活動: " + Calc(res.brain_power, cnt) + "\n" +
                    "困惑: " + Calc(res.embarrassment, cnt) + "\n" +
                    "思考: " + Calc(res.intensive_thinking, cnt) + "\n" +
                    "想像力: " + Calc(res.imagination_activity, cnt) + "\n" +
                    "極端な起伏(感情): " + Calc(res.extreme_emotion, cnt) + "\n" +
                    "情熱: " + Calc(res.passionate, cnt) + "\n" +
                    "雰囲気: " + Calc(res.atmosphere, cnt) + "\n" +
                    "期待: " + Calc(res.anticipation, cnt) + "\n" +
                    "不満: " + Calc(res.dissatisfaction, cnt) + "\n" +
                    "自信: " + Calc(res.confidence, cnt);
    }

    void Save(string data)
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, "log.txt");
            File.WriteAllText(filePath, data);
            Debug.Log("ログデータを保存しました: " + filePath);
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
    
    // アプリケーション終了時にリソースを解放
    void OnDestroy()
    {
        // 全てのコルーチンを停止
        StopAllCoroutines();
        
        // マイクリソースの解放
        StopListeningForVoice();
        if (isRecording)
        {
            Microphone.End(microphone);
        }
    }

    // アプリケーションの一時停止時にも対応
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // アプリが一時停止したらマイクを停止
            if (Microphone.IsRecording(microphone))
            {
                Microphone.End(microphone);
            }
        }
        else
        {
            // アプリが再開したら必要に応じてマイクを再開
            if (isAutoRecordEnabled && !isRecording && !isListeningForVoice)
            {
                StartListeningForVoice();
            }
        }
    }
}