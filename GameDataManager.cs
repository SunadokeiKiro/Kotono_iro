// Scripts/GameDataManager.cs (新規作成)
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    public int SlotToLoadFromGallery { get; set; } = -1; // -1 は未選択を示す

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンをまたいで破棄されないようにする
        }
        else
        {
            Destroy(gameObject);
        }
    }
}