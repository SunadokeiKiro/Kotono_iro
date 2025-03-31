using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;

public class ButtonScript : MonoBehaviour
{
    // Inspectorで必ずアタッチする
    [Header("UI Elements (Assign in Inspector)")]
    public Button sendButton;
    public Button sampleButton;  // 必要であれば利用
    public Button recButton;
    public GameObject recSec;    // 録音秒数を表示・入力するTextが含まれるオブジェクト
    public InputField json;      // APIの進捗などを表示
    public InputField jsontext;  // 結果データを表示

    private string apiUrl = "https://acp-api-async.amivoice.com/v1/recognitions";
    private string sampleUrl = "https://acp-dsrpp.amivoice.com/v1/sentiment-analysis/ja/result-parameters.json";
    private string apiKeyFilePath;
    private string audioFilePath;
    private string sessionID;
    private string microphone;
    private AudioClip microphoneInput;
    private int RECORD_LENGTH_SEC = 20;
    private const int SAMPLE_RATE = 41100;

    void Start()
    {
        // 手動アタッチが正しく行われているか確認
        if (sendButton == null || recButton == null || json == null || jsontext == null || recSec == null)
        {
            Debug.LogError("InspectorにUI要素が正しく割り当てられていません。各フィールドを確認してください。");
            return;
        }

        json.text = "Start";

        // マイクの設定
        microphone = Microphone.devices.FirstOrDefault();
        Debug.Log("microphone: " + microphone);
        if (microphone == null)
        {
            Debug.LogError("No microphone found");
            return;
        }

        // StreamingAssetsフォルダ内のファイルパスを取得
        apiKeyFilePath = Path.Combine(Application.streamingAssetsPath, "apikey.txt");

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
    }

    IEnumerator ReadApiKeyAndPostRequest()
    {
        json.text = "Post Requesting...";
        string apiKey = "";

        // StreamingAssets から API キーを読み込む（Android でも対応）
        if (apiKeyFilePath.StartsWith("http") || Application.platform == RuntimePlatform.Android)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(apiKeyFilePath))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    apiKey = request.downloadHandler.text.Trim();
                }
                else
                {
                    Debug.LogError("APIキーの取得に失敗: " + request.error);
                    yield break;
                }
            }
        }
        else
        {
            if (File.Exists(apiKeyFilePath))
            {
                apiKey = File.ReadAllText(apiKeyFilePath).Trim();
            }
            else
            {
                Debug.LogError("APIキーのファイルが見つかりません: " + apiKeyFilePath);
                yield break;
            }
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

        if (RECORD_LENGTH_SEC == null)
        {
            Debug.Log("ERROR: null record sec");
            json.text = "ERROR: null record sec";
            yield break;
        }

        try
        {
            RECORD_LENGTH_SEC = int.Parse(tmp);

            if (RECORD_LENGTH_SEC > 0)
            {
                microphoneInput = Microphone.Start(microphone, false, RECORD_LENGTH_SEC, SAMPLE_RATE);
                Debug.Log("録音を開始します。何か話してください。");
                json.text = "START Recording for " + RECORD_LENGTH_SEC + " sec!";
                StartCoroutine(WaitAndExecute());
            }
            else
            {
                Debug.Log("error: RECORD_LENGTH_SEC");
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
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
        // WavUtilityのスクリプトが必要です（https://github.com/deadlyfingers/UnityWav）
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
}
