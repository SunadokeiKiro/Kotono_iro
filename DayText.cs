// Scripts/DayText.cs
using UnityEngine;
using TMPro; // TextMeshProを使用するために必要
using System; // DateTimeを使用するために必要

/// <summary>
/// TextMeshProのUIに現在の日付（年 / 月 / 日）をリアルタイムで表示します。
/// </summary>
public class DayText : MonoBehaviour // クラス名をファイル名と一致させるため "Daytext" -> "DayText" に変更
{
    [Tooltip("日付を表示するTextMeshProコンポーネント")]
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
        DateTime today = DateTime.Now;

        // テキストUIに年・月・日を表示
        dateTimeText.text = $"{today.Year} / {today.Month} / {today.Day}";
    }
}