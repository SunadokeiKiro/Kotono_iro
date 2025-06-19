// Scripts/GalleryManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;

public class GalleryManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject[] slotElements = new GameObject[3]; 
    public Image[] slotThumbnailImages = new Image[3];    
    public TextMeshProUGUI[] slotInfoTexts = new TextMeshProUGUI[3]; 
    public Button loadSelectedButton;
    public Button backToMainButton;
    public TextMeshProUGUI statusText; 

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
            backToMainButton.onClick.AddListener(() => SceneManager.LoadScene("KotonoGem")); 
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
            
            int slotIndex = i; 
            slotButton.onClick.RemoveAllListeners(); 
            slotButton.onClick.AddListener(() => SelectSlot(slotIndex));

            if (File.Exists(jsonFilePath))
            {
                slotElements[i].SetActive(true);
                if (slotThumbnailImages[i] != null)
                {
                    if (File.Exists(thumbnailPath))
                    {
                        byte[] fileData = File.ReadAllBytes(thumbnailPath);
                        Texture2D tex = new Texture2D(2, 2); 
                        if (tex.LoadImage(fileData)) 
                        {
                            slotThumbnailImages[i].sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                            slotThumbnailImages[i].color = Color.white; 
                            slotThumbnailImages[i].gameObject.SetActive(true);
                        }
                        else
                        {
                            Debug.LogError($"サムネイルのロードに失敗: {thumbnailPath}");
                            slotThumbnailImages[i].sprite = null; 
                            slotThumbnailImages[i].color = new Color(0.5f,0.5f,0.5f,0.5f); 
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"サムネイルファイルが見つかりません: {thumbnailPath}");
                        slotThumbnailImages[i].sprite = null; 
                        slotThumbnailImages[i].color = new Color(0.8f,0.8f,0.8f,0.5f); 
                    }
                }

                if (slotInfoTexts[i] != null)
                {
                    // メインオブジェクト名を表示せず、スロット番号のみ表示するように変更
                    slotInfoTexts[i].text = $"スロット {i + 1}";
                }
            }
            else
            {
                if (slotThumbnailImages[i] != null) {
                    slotThumbnailImages[i].sprite = null; 
                    slotThumbnailImages[i].color = Color.gray; 
                }
                if (slotInfoTexts[i] != null) slotInfoTexts[i].text = $"スロット {i + 1}\n(空)";
                slotButton.interactable = false; 
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
                Image bgImage = slotElements[i].GetComponent<Image>(); 
                if (bgImage != null)
                {
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
                GameDataManager.Instance.SlotToLoadFromGallery = selectedSlot; //
                SceneManager.LoadScene("KotonoGem");
            }
            else Debug.LogError("GameDataManager が見つかりません！");
        }
    }
}