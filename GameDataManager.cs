// Scripts/GameDataManager.cs
using UnityEngine;

/// <summary>
/// シーン間で永続的にデータを保持するためのシングルトンマネージャー。
/// 主に、ギャラリーで選択されたスロット番号をメインシーンに渡すために使用します。
/// </summary>
public class GameDataManager : MonoBehaviour
{
    /// <summary>
    /// GameDataManagerの静的なインスタンス。どこからでもアクセスできます。
    /// </summary>
    public static GameDataManager Instance { get; private set; }

    /// <summary>
    /// ギャラリーシーンで選択され、メインシーンでロードすべきスロットの番号。
    /// -1は未選択を示します。
    /// </summary>
    public int SlotToLoadFromGallery { get; set; } = -1;

    private void Awake()
    {
        // シングルトンパターンの実装
        if (Instance == null)
        {
            // このインスタンスが唯一のものであれば、それを保持
            Instance = this;
            // シーンを切り替えてもこのオブジェクトが破棄されないようにする
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 既にインスタンスが存在する場合は、このオブジェクトを破棄して重複を防ぐ
            Destroy(gameObject);
        }
    }
}