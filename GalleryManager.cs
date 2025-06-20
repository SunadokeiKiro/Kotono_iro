// Scripts/GalleryManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;

/// <summary>
/// ギャラリーシーンのUIと機能を管理します。
/// 保存された作品のスロットを一覧表示し、選択された作品をロードする機能を提供します。
/// </summary>
public class GalleryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject[] slotElements = new GameObject[3]; 
    [SerializeField] private Image[] slotThumbnailImages = new Image[3];    
    [SerializeField] private TextMeshProUGUI[] slotInfoTexts = new TextMeshProUGUI[3]; 
    [SerializeField] private Button loadSelectedButton;
    [SerializeField] private Button backToMainButton;
    [SerializeField] private TextMeshProUGUI statusText; 

    // ファイル名の定数
    private const string SAVE_FILE_BASE_NAME = "glass_art_slot_";
    private const string SAVE_FILE_EXTENSION = ".json";
    private const string THUMBNAIL_FILE_BASE_NAME = "slot_";
    private const string THUMBNAIL_FILE_EXTENSION = "_thumbnail.png";
    
    private int selectedSlot = -1;

    void Start()
    {
        // ボタンのリスナーを設定
        if (loadSelectedButton != null)
        {
            loadSelectedButton.onClick.AddListener(LoadSelectedArtwork);
            loadSelectedButton.interactable = false; // 最初は非アクティブ
        }
        if (backToMainButton != null)
        {
            backToMainButton.onClick.AddListener(() => SceneManager.LoadScene("KotonoGem")); 
        }

        if (statusText != null)
        {
            statusText.text = "ロードする作品を選んでください";
        }

        // ギャラリーのプレビューを表示
        DisplayGalleryPreviews();
    }

    /// <summary>
    /// 保存されているスロットの情報を読み込み、UIにサムネイルとテキストを表示します。
    /// </summary>
    void DisplayGalleryPreviews()
    {
        for (int i = 0; i < slotElements.Length; i++)
        {
            if (slotElements[i] == null) continue;

            string jsonFilePath = Path.Combine(Application.persistentDataPath, $"{SAVE_FILE_BASE_NAME}{i}{SAVE_FILE_EXTENSION}");
            string thumbnailPath = Path.Combine(Application.persistentDataPath, $"{THUMBNAIL_FILE_BASE_NAME}{i}{THUMBNAIL_FILE_EXTENSION}");

            // スロットのボタンコンポーネントを取得または追加
            Button slotButton = slotElements[i].GetComponent<Button>();
            if (slotButton == null)
            {
                slotButton = slotElements[i].AddComponent<Button>();
                // ImageコンポーネントをTarget Graphicに設定
                Image targetImage = slotElements[i].GetComponent<Image>();
                if(targetImage != null)
                {
                    slotButton.targetGraphic = targetImage;
                }
            }
            
            int slotIndex = i; // クロージャ用のローカルコピー
            slotButton.onClick.RemoveAllListeners(); 
            slotButton.onClick.AddListener(() => SelectSlot(slotIndex));

            if (File.Exists(jsonFilePath))
            {
                // データが存在する場合
                slotButton.interactable = true;
                
                // サムネイル画像の表示
                if (slotThumbnailImages[i] != null)
                {
                    if (File.Exists(thumbnailPath))
                    {
                        byte[] fileData = File.ReadAllBytes(thumbnailPath);
                        // Texture2Dに読み込み、Spriteを作成
                        Texture2D tex = new Texture2D(2, 2); 
                        if (tex.LoadImage(fileData)) 
                        {
                            slotThumbnailImages[i].sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                            slotThumbnailImages[i].color = Color.white; 
                        }
                        else
                        {
                            Debug.LogError($"Failed to load thumbnail image: {thumbnailPath}");
                            slotThumbnailImages[i].sprite = null; 
                            slotThumbnailImages[i].color = new Color(0.5f, 0.5f, 0.5f, 0.5f); 
                        }
                    }
                    else
                    {
                        // サムネイルファイルがない場合
                        Debug.LogWarning($"Thumbnail file not found: {thumbnailPath}");
                        slotThumbnailImages[i].sprite = null; 
                        slotThumbnailImages[i].color = new Color(0.8f, 0.8f, 0.8f, 0.5f); 
                    }
                }

                // スロット情報のテキスト表示
                if (slotInfoTexts[i] != null)
                {
                    slotInfoTexts[i].text = $"スロット {i + 1}";
                }
            }
            else
            {
                // データが存在しない場合
                slotButton.interactable = false;
                if (slotThumbnailImages[i] != null)
                {
                    slotThumbnailImages[i].sprite = null; 
                    slotThumbnailImages[i].color = Color.gray;
                }
                if (slotInfoTexts[i] != null)
                {
                    slotInfoTexts[i].text = $"スロット {i + 1}\n(空)";
                }
            }
        }
    }

    /// <summary>
    /// スロットが選択されたときの処理。選択状態を更新し、UIに反映します。
    /// </summary>
    void SelectSlot(int slotIndex)
    {
        selectedSlot = slotIndex;
        if (statusText != null)
        {
            statusText.text = $"スロット {selectedSlot + 1} を選択中";
        }
        if (loadSelectedButton != null)
        {
            loadSelectedButton.interactable = true;
        }

        // 選択されたスロットの背景色などを変更して、視覚的に分かりやすくする
        for (int i = 0; i < slotElements.Length; i++)
        {
            if (slotElements[i] != null)
            {
                Image bgImage = slotElements[i].GetComponent<Image>(); 
                if (bgImage != null)
                {
                    bgImage.color = (i == selectedSlot) ? new Color(0.8f, 1f, 0.8f, 1f) : Color.white; // 選択中は緑っぽく
                }
            }
        }
    }

    /// <summary>
    /// 「ロード」ボタンが押されたときの処理。
    /// GameDataManagerにロードしたいスロット番号を記録し、メインシーンに遷移します。
    /// </summary>
    void LoadSelectedArtwork()
    {
        if (selectedSlot != -1)
        {
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.SlotToLoadFromGallery = selectedSlot;
                SceneManager.LoadScene("KotonoGem");
            }
            else
            {
                Debug.LogError("GameDataManager instance not found! Cannot pass selected slot info.");
            }
        }
        else
        {
            Debug.LogWarning("No slot selected to load.");
        }
    }
}