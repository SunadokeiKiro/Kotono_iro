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
/// 接続情報はApiConfigアセットから読み込みます。
/// </summary>
public class ApiHandler : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ApiConfig apiConfig; // ★★★ 修正点: 設定アセットへの参照を追加 ★★★

    [Header("Controller References")]
    [SerializeField] private GameController gameController;

    private string apiKey;
    private string sessionID;

    void Start()
    {
        // 参照チェック
        if (apiConfig == null)
        {
            Debug.LogError("ApiHandler: ApiConfig is not set in the inspector!");
            enabled = false;
            return;
        }
        if (gameController == null)
        {
            Debug.LogError("ApiHandler: GameController is not set in the inspector.");
            enabled = false;
            return;
        }

        apiKey = LoadApiKey();
    }

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

        // ★★★ 修正点: ApiConfigから設定値を取得 ★★★
        List<IMultipartFormSection> form = new List<IMultipartFormSection>
        {
            new MultipartFormDataSection("u", apiKey),
            new MultipartFormDataSection("d", apiConfig.DParameter),
            new MultipartFormFileSection("a", audioData, Path.GetFileName(audioFilePath), "audio/wav")
        };

        // ★★★ 修正点: ApiConfigからURLを取得 ★★★
        using (UnityWebRequest request = UnityWebRequest.Post(apiConfig.ApiUrlBase, form))
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

    private IEnumerator PollJobStatus()
    {
        if (string.IsNullOrEmpty(sessionID))
        {
            Debug.LogError("Cannot poll job status: SessionID is empty.");
            yield break;
        }

        Debug.Log("Starting to poll for analysis results...");
        
        // ★★★ 修正点: ApiConfigからURLを取得 ★★★
        string pollUrl = $"{apiConfig.ApiUrlBase}/{sessionID}";
        const int maxPolls = 100;
        const float pollInterval = 4f;

        for (int i = 0; i < maxPolls; i++)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(pollUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    bool isJobFinished = HandlePollResponse(request.downloadHandler.text);
                    if (isJobFinished)
                    {
                        yield break;
                    }
                }
                else
                {
                    Debug.LogError($"Polling failed: {request.error}, Code: {request.responseCode}, Body: {request.downloadHandler?.text}");
                    yield break;
                }
            }

            Debug.Log($"Analysis in progress... (Polling attempt {i + 1}/{maxPolls})");
            yield return new WaitForSeconds(pollInterval);
        }

        Debug.LogWarning($"Reached maximum polling attempts ({maxPolls}). Assuming timeout.");
    }

    private bool HandlePollResponse(string jsonResponse)
    {
        try
        {
            SentimentAnalysisResponse sentimentResponse = JsonUtility.FromJson<SentimentAnalysisResponse>(jsonResponse);

            if (sentimentResponse == null)
            {
                Debug.LogError("Polling response is null. Parsing may have failed.");
                return true;
            }

            switch (sentimentResponse.status)
            {
                case "completed":
                    Debug.Log("Analysis completed successfully!");
                    SaveLog(jsonResponse);
                    gameController.SetParametersFromJson(jsonResponse);
                    Debug.Log("Sentiment parameters sent to GameController.");
                    PrintResultsSummary(sentimentResponse);
                    return true;

                case "error":
                    Debug.LogError($"Job error from API: {sentimentResponse.message} (Code: {sentimentResponse.code})");
                    return true;

                default:
                    return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse polling response JSON: {e.Message}\nResponse: {jsonResponse}");
            return true;
        }
    }
    
    private void PrintResultsSummary(SentimentAnalysisResponse response)
    {
        StringBuilder sb = new StringBuilder("Recognized Text:\n");
        if (response.segments != null && response.segments.Count > 0)
        {
            foreach (var segment in response.segments)
            {
                if (segment.results != null && segment.results.Count > 0)
                {
                    sb.AppendLine(segment.results[0].text);
                }
                else
                {
                    sb.AppendLine(segment.text);
                }
            }
        }
        else if (!string.IsNullOrEmpty(response.text))
        {
            sb.AppendLine(response.text);
        }
        Debug.Log(sb.ToString().Trim());
    }

    private void SaveLog(string data)
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, "api_response_log.txt");
            File.WriteAllText(filePath, data);
            Debug.Log("API response log saved to: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save API log file: " + e.Message);
        }
    }
}