using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class RecognitionResponse
{
    public string sessionid; // JSON内の "sessionid" と一致させる
    public string text;      // 完了時に認識されたテキスト
}

[Serializable]
public class SentimentAnalysisResponse {
    public string status;
    public string session_id;
    public string service_id;
    public int audio_size;
    public string audio_md5;
    public List<Segment> segments;
    public string utteranceid;
    public string text;
    public string code;
    public string message;
    public SentimentAnalysis sentiment_analysis;
}

[Serializable]
public class Segment {
    public List<Result> results;
    public string text;
}

[Serializable]
public class Result {
    public List<Token> tokens;
    public float confidence; // この confidence は float のままが適切と思われます
    public int starttime;
    public int endtime;
    public List<string> tags;
    public string rulename;
    public string text;
}

[Serializable]
public class Token {
    public string written;
    public float confidence; // この confidence も float のままが適切と思われます
    public int starttime;
    public int endtime;
    public string spoken;
}

[Serializable]
public class SentimentAnalysis {
    public List<SentimentSegment> segments;
}

[Serializable]
public class SentimentSegment {
    public int starttime;
    public int endtime;
    public int energy; // float から int に変更
    public int content; // float から int に変更
    public int upset; // float から int に変更
    public int aggression; // float から int に変更
    public int stress; // float から int に変更
    public int uncertainty; // float から int に変更
    public int excitement; // float から int に変更
    public int concentration; // float から int に変更
    public int emo_cog; // float から int に変更
    public int hesitation; // float から int に変更
    public int brain_power; // float から int に変更
    public int embarrassment; // float から int に変更
    public int intensive_thinking; // float から int に変更
    public int imagination_activity; // float から int に変更
    public int extreme_emotion; // float から int に変更
    public int passionate; // float から int に変更
    public int atmosphere; // float から int に変更
    public int anticipation; // float から int に変更
    public int dissatisfaction; // float から int に変更
    public int confidence; // float から int に変更
}

public class SerializableExample : MonoBehaviour
{
    // このクラスはサンプルとして作成しています。必要に応じて他のスクリプトと連携してください。

    void Start()
    {
        // 例: JsonUtility.FromJson を使ったデシリアライズ
        // string jsonResponse = ...; // APIからのJSON文字列
        // RecognitionResponse response = JsonUtility.FromJson<RecognitionResponse>(jsonResponse);
        // Debug.Log("Session ID: " + response.sessionid);
    }

    void Update()
    {
        // フレームごとの処理が必要な場合に記述
    }
}