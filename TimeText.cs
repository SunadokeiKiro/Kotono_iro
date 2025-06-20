// Scripts/TimeText.cs
using UnityEngine;
using TMPro; // TextMeshProを使用するために必要
using System; // DateTimeを使用するために必要

/// <summary>
/// TextMeshProのUIに現在の時刻（時:分:秒）をリアルタイムで表示します。
/// </summary>
public class TimeText : MonoBehaviour // クラス名をファイル名と一致させるため "Timetext" -> "TimeText" に変更
{
    [Tooltip("時刻を表示するTextMeshProコンポーネント")]
    [SerializeField] 
    private TextMeshProUGUI dateTimeText;

    void Start()
    {
        // 参照が設定されていない場合のエラーチェック
        if (dateTimeText == null)
        {
            Debug.LogError("DateTimeText is not assigned in the inspector.", this.gameObject);
            // 参照がないと動作しないため、このコンポーネントを無効化
            this.enabled = false;
        }
    }

    void Update()
    {
        // テキストUIに現在の時刻を長い形式（例: "14:25:30"）で表示
        dateTimeText.text = DateTime.Now.ToLongTimeString();
    }
}