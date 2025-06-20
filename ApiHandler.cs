// Scripts/ApiHandler.cs
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text;

/// <summary>
/// AmiVoice APIとの通信（音声認識・感情分析）を管理します。
/// </summary>
public class ApiHandler : MonoBehaviour
{
    [Header("GameController Reference")]
    [SerializeField] private GameController gameController;

    private const string ApiUrlBase = "https://acp-api-async.amivoice.com/v1/recognitions";
    private string apiKey;
    private string sessionID;

    void Start()
    {
        if (gameController == null)
        {
            Debug.LogError("ApiHandler: GameController is not set in the inspector.");
            enabled = false;
            return;
        }
        apiKey = LoadApiKey();
    }

    /// <summary>
    /// apikey.txtからAPIキーを読み込みます。
    /// </summary>
    private string LoadApiKey()
    {
        string keyPath = Path.Combine(Application.persistentDataPath, "apikey.txt");
        if (File.Exists(keyPath))
        {
            string key = File.ReadAllText(keyPath).Trim();
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("API key file is empty. Please set your API key in the settings scene.");
                return null;
            }
            Debug.Log("API key loaded successfully.");
            return key;
        }
        else
        {
            Debug.LogError($"API key file not found at: {keyPath}. Please set your API key in the settings scene.");
            return null;
        }
    }

    /// <summary>
    /// 指定された音声ファイルの解析を開始します。
    /// </summary>
    /// <param name="audioFilePath">WAVファイルのパス</param>
    public void StartAnalysis(string audioFilePath)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API key is not set. Aborting analysis request.");
            return;
        }
        if (!File.Exists(audioFilePath))
        {
            Debug.LogError($"Audio file not found for analysis: {audioFilePath}");
            return;
        }

        StartCoroutine(PostRequest(audioFilePath));
    }

    /// <summary>
    /// 音声ファイルをAPIにPOSTして、解析ジョブを開始するコルーチン。
    /// </summary>
    private IEnumerator PostRequest(string audioFilePath)
    {
        Debug.Log("Sending audio recognition request to the API...");

        byte[] audioData;
        try
        {
            audioData = File.ReadAllBytes(audioFilePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to read audio file: {audioFilePath}, Error: {e.Message}");
            yield break;
        }

        // マルチパートフォームを作成
        List<IMultipartFormSection> form = new List<IMultipartFormSection>
        {
            new MultipartFormDataSection("u", apiKey),
            new MultipartFormDataSection("d", "grammarFileNames=-a-general loggingOptOut=True sentimentAnalysis=True"),
            // API仕様に応じて "c" (コーデック) を指定。マイク録音は非圧縮PCMなので不要な場合が多い
            // new MultipartFormDataSection("c", "MSB44K") 
            new MultipartFormFileSection("a", audioData, Path.GetFileName(audioFilePath), "audio/wav")
        };

        using (UnityWebRequest request = UnityWebRequest.Post(ApiUrlBase, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("API request successful. Received session ID.");
                HandlePostResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"API request failed: {request.error}, Code: {request.responseCode}, Body: {request.downloadHandler?.text}");
            }
        }
    }

    /// <summary>
    /// POSTリクエストのレスポンスを処理し、SessionIDを抽出してポーリングを開始します。
    /// </summary>
    private void HandlePostResponse(string jsonResponse)
    {
        try
        {
            RecognitionResponse recogResponse = JsonUtility.FromJson<RecognitionResponse>(jsonResponse);
            if (recogResponse != null && !string.IsNullOrEmpty(recogResponse.sessionid))
            {
                sessionID = recogResponse.sessionid;
                Debug.Log("SessionID obtained: " + sessionID);
                StartCoroutine(PollJobStatus());
            }
            else
            {
                Debug.LogError("Failed to obtain SessionID from response: " + jsonResponse);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to parse initial response JSON: " + e.Message);
        }
    }

    /// <summary>
    /// SessionIDを使って、解析ジョブのステータスを定期的に問い合わせるコルーチン。
    /// </summary>
    private IEnumerator PollJobStatus()
    {
        if (string.IsNullOrEmpty(sessionID))
        {
            Debug.LogError("Cannot poll job status: SessionID is empty.");
            yield break;
        }

        Debug.Log("Starting to poll for analysis results...");
        string pollUrl = $"{ApiUrlBase}/{sessionID}";
        const int maxPolls = 100; // ポーリング回数の上限
        const float pollInterval = 4f; // ポーリング間隔（秒）

        for (int i = 0; i < maxPolls; i++)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(pollUrl))
            {
                // API仕様によっては認証ヘッダが必要な場合がある
                // request.SetRequestHeader("Authorization", "Bearer " + apiKey);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    bool isJobFinished = HandlePollResponse(request.downloadHandler.text);
                    if (isJobFinished)
                    {
                        yield break; // ジョブが完了またはエラーになったらループを抜ける
                    }
                }
                else
                {
                    Debug.LogError($"Polling failed: {request.error}, Code: {request.responseCode}, Body: {request.downloadHandler?.text}");
                    yield break; // ポーリングに失敗したら終了
                }
            }

            Debug.Log($"Analysis in progress... (Polling attempt {i + 1}/{maxPolls})");
            yield return new WaitForSeconds(pollInterval);
        }

        Debug.LogWarning($"Reached maximum polling attempts ({maxPolls}). Assuming timeout.");
    }

    /// <summary>
    /// ポーリングのレスポンスを処理し、ジョブが完了したかどうかを返します。
    /// </summary>
    /// <returns>ジョブが完了またはエラーの場合はtrue、継続中の場合はfalse</returns>
    private bool HandlePollResponse(string jsonResponse)
    {
        try
        {
            // まずstatusだけを簡易的にパースして確認することも可能
            SentimentAnalysisResponse sentimentResponse = JsonUtility.FromJson<SentimentAnalysisResponse>(jsonResponse);

            if (sentimentResponse == null)
            {
                Debug.LogError("Polling response is null. Parsing may have failed.");
                return true; // エラーとして処理を終了
            }

            switch (sentimentResponse.status)
            {
                case "completed":
                    Debug.Log("Analysis completed successfully!");
                    SaveLog(jsonResponse); // デバッグ用にレスポンス全体をログに保存
                    gameController.SetParametersFromJson(jsonResponse);
                    Debug.Log("Sentiment parameters sent to GameController.");
                    PrintResultsSummary(sentimentResponse);
                    return true; // 完了

                case "error":
                    Debug.LogError($"Job error from API: {sentimentResponse.message} (Code: {sentimentResponse.code})");
                    return true; // エラーで完了

                default: // "queued", "processing"など
                    return false; // ジョブはまだ継続中
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse polling response JSON: {e.Message}\nResponse: {jsonResponse}");
            return true; // パースエラーとして処理を終了
        }
    }
    
    /// <summary>
    /// 解析結果の概要をコンソールに出力します。
    /// </summary>
    private void PrintResultsSummary(SentimentAnalysisResponse response)
    {
        // 認識テキストの表示
        StringBuilder sb = new StringBuilder("Recognized Text:\n");
        if (response.segments != null && response.segments.Count > 0)
        {
            foreach (var segment in response.segments)
            {
                // APIのレスポンス構造に応じて、より詳細なテキストを取得
                if (segment.results != null && segment.results.Count > 0)
                {
                    sb.AppendLine(segment.results[0].text);
                }
                else
                {
                    sb.AppendLine(segment.text); // resultsがない場合のフォールバック
                }
            }
        }
        else if (!string.IsNullOrEmpty(response.text))
        {
            sb.AppendLine(response.text); // segmentsがない場合のフォールバック
        }
        Debug.Log(sb.ToString().Trim());

        // 感情分析結果のサマリー表示 (オプション)
        if (response.sentiment_analysis?.segments != null && response.sentiment_analysis.segments.Count > 0)
        {
             // ここで感情値の平均などを計算してログに出力することも可能
             // 例: float avgEnergy = response.sentiment_analysis.segments.Average(s => s.energy);
             // Debug.Log($"Average Energy: {avgEnergy}");
        }
    }

    /// <summary>
    /// デバッグ用にAPIのレスポンスをファイルに保存します。
    /// </summary>
    private void SaveLog(string data)
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, "api_response_log.txt");
            // 追記ではなく、常に最新のレスポンスで上書きする
            File.WriteAllText(filePath, data);
            Debug.Log("API response log saved to: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save API log file: " + e.Message);
        }
    }
}