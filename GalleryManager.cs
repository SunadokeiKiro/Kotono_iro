// Scripts/GalleryManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;

public class GalleryManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject[] slotElements = new GameObject[3]; // 各スロットの親UI要素 (Panelなど)
    public Image[] slotThumbnailImages = new Image[3];    // 各スロットのサムネイル表示用Image
    public TextMeshProUGUI[] slotInfoTexts = new TextMeshProUGUI[3]; // 各スロットの情報表示用Text
    public Button loadSelectedButton;
    public Button backToMainButton;
    public TextMeshProUGUI statusText; // 状態表示用 (例: "スロットXを選択中")

    private const string saveFileBaseName = "glass_art_slot_";
    private const string saveFileExtension = ".json";
    private const string thumbnailFileBaseName = "slot_";
    private const string thumbnailFileExtension = "_thumbnail.png";
    private int selectedSlot = -1;

    void Start()
    {
        if (loadSelectedButton != null)
        {
            loadSelectedButton.onClick.AddListener(LoadSelectedArtwork);
            loadSelectedButton.interactable = false;
        }
        if (backToMainButton != null)
        {
            backToMainButton.onClick.AddListener(() => SceneManager.LoadScene("KotonoGem")); // メインシーン名
        }
        if (statusText != null) statusText.text = "ロードする作品を選んでください";

        DisplayGalleryPreviews();
    }

    void DisplayGalleryPreviews()
    {
        for (int i = 0; i < 3; i++)
        {
            if (slotElements[i] == null) continue;

            string jsonFilePath = Path.Combine(Application.persistentDataPath, $"{saveFileBaseName}{i}{saveFileExtension}");
            string thumbnailPath = Path.Combine(Application.persistentDataPath, $"{thumbnailFileBaseName}{i}{thumbnailFileExtension}");

            Button slotButton = slotElements[i].GetComponent<Button>();
            if (slotButton == null) slotButton = slotElements[i].AddComponent<Button>();
            
            int slotIndex = i; // クロージャ用
            slotButton.onClick.RemoveAllListeners(); // 既存リスナーをクリア
            slotButton.onClick.AddListener(() => SelectSlot(slotIndex));

            if (File.Exists(jsonFilePath))
            {
                slotElements[i].SetActive(true);
                // サムネイル表示
                if (slotThumbnailImages[i] != null)
                {
                    if (File.Exists(thumbnailPath))
                    {
                        byte[] fileData = File.ReadAllBytes(thumbnailPath);
                        Texture2D tex = new Texture2D(2, 2); // サイズはLoadImageで自動調整される
                        if (tex.LoadImage(fileData)) // 必ずtrueかチェック
                        {
                            slotThumbnailImages[i].sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                            slotThumbnailImages[i].color = Color.white; // 画像があるので不透明に
                            slotThumbnailImages[i].gameObject.SetActive(true);
                        }
                        else
                        {
                            Debug.LogError($"サムネイルのロードに失敗: {thumbnailPath}");
                            slotThumbnailImages[i].sprite = null; // またはデフォルト画像
                            slotThumbnailImages[i].color = new Color(0.5f,0.5f,0.5f,0.5f); // 半透明グレーなど
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"サムネイルファイルが見つかりません: {thumbnailPath}");
                        slotThumbnailImages[i].sprite = null; // または「No Image」のようなSprite
                        slotThumbnailImages[i].color = new Color(0.8f,0.8f,0.8f,0.5f); // 薄い色
                    }
                }

                // 情報テキスト表示 (任意)
                if (slotInfoTexts[i] != null)
                {
                     try {
                        string jsonData = File.ReadAllText(jsonFilePath);
                        SceneData sceneData = JsonUtility.FromJson<SceneData>(jsonData);
                        if (sceneData != null && sceneData.objects != null && sceneData.objects.Count > 0) {
                            string mainObjectName = sceneData.objects[0].prefabName ?? "名無し";
                            if(mainObjectName.EndsWith("(Clone)")) mainObjectName = mainObjectName.Replace("(Clone)", "");
                            slotInfoTexts[i].text = $"スロット {i + 1}\n<size=20>{mainObjectName}</size>"; // 少し小さめに
                        } else {
                             slotInfoTexts[i].text = $"スロット {i + 1}\n(データ破損)";
                        }
                    } catch {
                        slotInfoTexts[i].text = $"スロット {i + 1}\n(読込エラー)";
                    }
                }
            }
            else
            {
                // 保存データがないスロット
                if (slotThumbnailImages[i] != null) {
                    slotThumbnailImages[i].sprite = null; // または「空」を示すSprite
                    slotThumbnailImages[i].color = Color.gray; // グレーアウトなど
                }
                if (slotInfoTexts[i] != null) slotInfoTexts[i].text = $"スロット {i + 1}\n(空)";
                slotButton.interactable = false; // 空のスロットは選択不可
            }
        }
    }

    void SelectSlot(int slotIndex)
    {
        selectedSlot = slotIndex;
        if (statusText != null) statusText.text = $"スロット {selectedSlot + 1} を選択中";
        if (loadSelectedButton != null) loadSelectedButton.interactable = true;

        for (int i = 0; i < slotElements.Length; i++)
        {
            if (slotElements[i] != null)
            {
                Image bgImage = slotElements[i].GetComponent<Image>(); // PanelのImageコンポーネントを想定
                if (bgImage != null)
                {
                    // 選択状態に応じて背景色や枠線などを変更する（例）
                    bgImage.color = (i == selectedSlot) ? new Color(0.8f, 1f, 0.8f, 1f) : Color.white;
                }
            }
        }
    }

    void LoadSelectedArtwork()
    {
        if (selectedSlot != -1)
        {
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.SlotToLoadFromGallery = selectedSlot;
                SceneManager.LoadScene("KotonoGem");
            }
            else Debug.LogError("GameDataManager が見つかりません！");
        }
    }
}