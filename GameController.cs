using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;

// SavedObjectData と SceneData クラスの定義 (変更なし)
[System.Serializable]
public class SavedObjectData {
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public string type;
    public int prefabIndex;
}
[System.Serializable]
public class SceneData {
    public List<SavedObjectData> objects = new List<SavedObjectData>();
    public float parameterA, parameterB, parameterC, parameterD, parameterE, parameterF, parameterG;
    public float enlargeFactorA, enlargeFactorB, enlargeFactorC, enlargeFactorD, enlargeFactorE, enlargeFactorF, enlargeFactorG;
    public float generationThreshold;
    public int mainObjectIndex = -1;
    public int totalEnergy, totalContent, totalUpset, totalAggression, totalStress, totalUncertainty,
               totalExcitement, totalConcentration, totalEmoCog, totalHesitation, totalBrainPower,
               totalEmbarrassment, totalIntensiveThinking, totalImaginationActivity, totalExtremeEmotion,
               totalPassionate, totalAtmosphere, totalAnticipation, totalDissatisfaction, totalConfidence;
}

public class GameController : MonoBehaviour
{
    [Header("感情データ表示用 UI")]
    public TextMeshProUGUI textEnergy;
    public TextMeshProUGUI textContent;
    public TextMeshProUGUI textUpset;
    public TextMeshProUGUI textAggression;
    public TextMeshProUGUI textStress;
    public TextMeshProUGUI textUncertainty;
    public TextMeshProUGUI textExcitement;
    public TextMeshProUGUI textConcentration;
    public TextMeshProUGUI textEmoCog;
    public TextMeshProUGUI textHesitation;
    public TextMeshProUGUI textBrainPower;
    public TextMeshProUGUI textEmbarrassment;
    public TextMeshProUGUI textIntensiveThinking;
    public TextMeshProUGUI textImaginationActivity;
    public TextMeshProUGUI textExtremeEmotion;
    public TextMeshProUGUI textPassionate;
    public TextMeshProUGUI textAtmosphere;
    public TextMeshProUGUI textAnticipation;
    public TextMeshProUGUI textDissatisfaction;
    public TextMeshProUGUI textConfidence;

    [Header("感情データの現在の作業値")]
    private int currentWorkEnergy = 0;
    private int currentWorkContent = 0;
    private int currentWorkUpset = 0;
    private int currentWorkAggression = 0;
    private int currentWorkStress = 0;
    private int currentWorkUncertainty = 0;
    private int currentWorkExcitement = 0;
    private int currentWorkConcentration = 0;
    private int currentWorkEmoCog = 0;
    private int currentWorkHesitation = 0;
    private int currentWorkBrainPower = 0;
    private int currentWorkEmbarrassment = 0;
    private int currentWorkIntensiveThinking = 0;
    private int currentWorkImaginationActivity = 0;
    private int currentWorkExtremeEmotion = 0;
    private int currentWorkPassionate = 0;
    private int currentWorkAtmosphere = 0;
    private int currentWorkAnticipation = 0;
    private int currentWorkDissatisfaction = 0;
    private int currentWorkConfidence = 0;

    private const string SLOT_FILE_BASE_NAME = "glass_art_slot_";
    private const string CURRENT_WORK_FILE_NAME = "current_work_session";
    private const string SAVE_FILE_EXTENSION = ".json";
    private const string THUMBNAIL_SLOT_BASE_NAME = "slot_";
    private const string THUMBNAIL_CURRENT_WORK_NAME = "current_work_";
    private const string THUMBNAIL_FILE_EXTENSION = "_thumbnail.png";
    
    private int lastManuallyAccessedSlot = 0;

    [Header("UI Buttons")]
    public Button buttonSaveToSlot;
    public Button buttonGallery;
    public Button buttonResetAndNew;
    public Button buttonSettings;
    public Button button1; 

    [Header("Prefabs")]
    public GameObject[] glassPrefabs;
    public GameObject[] modelAPrefabs;
    public GameObject[] modelBPrefabs;
    public GameObject[] modelCPrefabs;
    public GameObject[] modelDPrefabs;
    public GameObject[] modelEPrefabs;
    public GameObject[] modelFPrefabs;
    public GameObject[] modelGPrefabs;

    [Header("現在の作業モデル生成パラメーター")]
    public float currentWorkParameterA = 0.0f; public float currentWorkParameterB = 0.0f; public float currentWorkParameterC = 0.0f;
    public float currentWorkParameterD = 0.0f; public float currentWorkParameterE = 0.0f; public float currentWorkParameterF = 0.0f;
    public float currentWorkParameterG = 0.0f;

    [Header("生成半径")]
    public float generationThreshold = 0.3f;

    [Header("現在の作業モデルの拡大倍率")]
    public float currentWorkEnlargeFactorA = 1.0f; public float currentWorkEnlargeFactorB = 1.0f; public float currentWorkEnlargeFactorC = 1.0f;
    public float currentWorkEnlargeFactorD = 1.0f; public float currentWorkEnlargeFactorE = 1.0f; public float currentWorkEnlargeFactorF = 1.0f;
    public float currentWorkEnlargeFactorG = 1.0f;

    private List<GameObject> attachedModelsA = new List<GameObject>();
    private List<GameObject> attachedModelsB = new List<GameObject>();
    private List<GameObject> attachedModelsC = new List<GameObject>();
    private List<GameObject> attachedModelsD = new List<GameObject>();
    private List<GameObject> attachedModelsE = new List<GameObject>();
    private List<GameObject> attachedModelsF = new List<GameObject>();
    private List<GameObject> attachedModelsG = new List<GameObject>();

    public Camera mainCamera;
    private GameObject currentMainObject;

    [Header("テキスト切り替えUI")]
    public CanvasGroup[] textObjectGroups;
    public float textDisplayDuration = 5f;
    public float textFadeDuration = 0.5f;
    private int currentTextGroupIndex = -1;

    [Header("現在の作業派生感情 (デバッグ用)")]
    public float currentWorkDerivedJoy = 0f; public float currentWorkDerivedAnger = 0f; public float currentWorkDerivedSadness = 0f;
    public float currentWorkDerivedEnjoyment = 0f; public float currentWorkDerivedFocus = 0f; public float currentWorkDerivedAnxiety = 0f;
    public float currentWorkDerivedConfidence = 0f;

    [Header("スロット保存UI")]
    public GameObject saveSlotSelectionPanel;
    public Button[] saveSlotButtons = new Button[3];
    public Button closeSaveSlotPanelButton;

    GameObject RandomModel(GameObject[] modelPrefabs)
    {
        if (modelPrefabs == null || modelPrefabs.Length == 0) return null;
        List<GameObject> validPrefabs = new List<GameObject>();
        foreach(var p in modelPrefabs) if(p != null) validPrefabs.Add(p);
        if(validPrefabs.Count == 0) return null;
        return validPrefabs[Random.Range(0, validPrefabs.Count)];
    }

    Vector3 GetRandomPointInTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        float u = Random.value; float v = Random.value;
        if (u + v > 1f) { u = 1f - u; v = 1f - v; }
        return v0 + u * (v1 - v0) + v * (v2 - v0);
    }

    Quaternion GetRandomRotation(float minAngle, float maxAngle)
    {
        return Quaternion.Euler(Random.Range(minAngle, maxAngle), Random.Range(minAngle, maxAngle), Random.Range(minAngle, maxAngle));
    }

    GameObject[] GetPrefabArrayByType(string type)
    {
        switch (type) {
            case "A": return modelAPrefabs; case "B": return modelBPrefabs; case "C": return modelCPrefabs;
            case "D": return modelDPrefabs; case "E": return modelEPrefabs; case "F": return modelFPrefabs;
            case "G": return modelGPrefabs; default: return null;
        }
    }

    List<GameObject> GetModelListByType(string type)
    {
        switch (type) {
            case "A": return attachedModelsA; case "B": return attachedModelsB; case "C": return attachedModelsC;
            case "D": return attachedModelsD; case "E": return attachedModelsE; case "F": return attachedModelsF;
            case "G": return attachedModelsG; default: return null;
        }
    }

    void AttachModelA(GameObject[] prefabArray, ref float parameter, List<GameObject> modelList)
    {
        float currentParameterValue = parameter;
        if (currentMainObject == null || prefabArray == null || prefabArray.Length == 0) return;
        MeshFilter meshFilter = currentMainObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;
        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices; int[] triangles = mesh.triangles;
        if (triangles.Length == 0 || vertices.Length == 0) return;

        int iterations = Mathf.Max(1, Mathf.CeilToInt(currentParameterValue * 10));
        for(int k=0; k<iterations; k++) {
            if (Random.value > currentParameterValue && k > 0 && iterations > 1) continue;
            int triIndex = Random.Range(0, triangles.Length / 3) * 3;
            Vector3 v0 = vertices[triangles[triIndex]]; Vector3 v1 = vertices[triangles[triIndex + 1]]; Vector3 v2 = vertices[triangles[triIndex + 2]];
            Vector3 randomPointOnTriangle = GetRandomPointInTriangle(v0, v1, v2);
            randomPointOnTriangle = currentMainObject.transform.TransformPoint(randomPointOnTriangle);
            Vector3 directionFromCenter = (randomPointOnTriangle - currentMainObject.transform.position).normalized;
            if(directionFromCenter == Vector3.zero) directionFromCenter = Random.onUnitSphere;
            Quaternion baseRotation = Quaternion.LookRotation(directionFromCenter) * Quaternion.Euler(90, 0, 0);
            float randomSize = Random.Range(0.1f, 0.5f) * currentParameterValue;
            GameObject modelPrefab = RandomModel(prefabArray);
            if(modelPrefab == null) continue;
            GameObject newModel = Instantiate(modelPrefab, randomPointOnTriangle, baseRotation * GetRandomRotation(-10f, 10f));
            newModel.transform.localScale = Vector3.one * Mathf.Max(0.01f, randomSize);
            newModel.transform.SetParent(currentMainObject.transform);
            modelList.Add(newModel);
        }
    }

    void AttachModel(GameObject[] prefabArray, ref float parameter, List<GameObject> modelList, bool generateExtraA)
    {
        float currentParameterValue = parameter;
        if (currentMainObject == null || prefabArray == null || prefabArray.Length == 0) return;
        MeshFilter meshFilter = currentMainObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;
        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices; int[] triangles = mesh.triangles;
        if (triangles.Length == 0 || vertices.Length == 0) return;

        int iterations = Mathf.Max(1, Mathf.CeilToInt(currentParameterValue * 10));
        for(int k=0; k<iterations; k++) {
            if (Random.value > currentParameterValue && k > 0 && iterations > 1) continue;
            int triIndex = Random.Range(0, triangles.Length / 3) * 3;
            Vector3 v0 = vertices[triangles[triIndex]]; Vector3 v1 = vertices[triangles[triIndex + 1]]; Vector3 v2 = vertices[triangles[triIndex + 2]];
            Vector3 randomPointOnTriangle = GetRandomPointInTriangle(v0, v1, v2);
            randomPointOnTriangle = currentMainObject.transform.TransformPoint(randomPointOnTriangle);
            Vector3 directionFromCenter = (randomPointOnTriangle - currentMainObject.transform.position).normalized;
            if(directionFromCenter == Vector3.zero) directionFromCenter = Random.onUnitSphere;
            Quaternion baseRotation = Quaternion.LookRotation(directionFromCenter) * Quaternion.Euler(90, 0, 0);
            float randomSize = Random.Range(0.1f, 1.0f) * currentParameterValue;
            GameObject modelPrefab = RandomModel(prefabArray);
            if(modelPrefab == null) continue;
            GameObject newModel = Instantiate(modelPrefab, randomPointOnTriangle, baseRotation * GetRandomRotation(-15f, 15f));
            newModel.transform.localScale = Vector3.one * Mathf.Max(0.01f, randomSize);
            newModel.transform.SetParent(currentMainObject.transform);
            modelList.Add(newModel);

            if (generateExtraA && modelAPrefabs != null && modelAPrefabs.Length > 0) {
                GameObject extraAPrefab = RandomModel(modelAPrefabs);
                if(extraAPrefab == null) continue;
                GameObject extraA = Instantiate(extraAPrefab, randomPointOnTriangle, baseRotation * GetRandomRotation(-15f, 15f));
                extraA.transform.localScale = Vector3.one * Mathf.Max(0.01f, randomSize);
                extraA.transform.SetParent(currentMainObject.transform);
                if (attachedModelsA == null) attachedModelsA = new List<GameObject>();
                attachedModelsA.Add(extraA);
            }
        }
    }
    
    void GenerateAllModels() // モデルを追加生成するメソッド（既存モデルは削除しない）
    {
        if (currentMainObject == null) { Debug.LogError("メインオブジェクトが存在しません！モデルを生成できません。"); return; }
        // DeleteAllCurrentModels(); // ★この行を削除する
        AttachModelA(modelAPrefabs, ref currentWorkParameterA, attachedModelsA);
        AttachModel(modelBPrefabs, ref currentWorkParameterB, attachedModelsB, true);
        AttachModel(modelCPrefabs, ref currentWorkParameterC, attachedModelsC, true);
        AttachModel(modelDPrefabs, ref currentWorkParameterD, attachedModelsD, true);
        AttachModel(modelEPrefabs, ref currentWorkParameterE, attachedModelsE, true);
        AttachModel(modelFPrefabs, ref currentWorkParameterF, attachedModelsF, true);
        AttachModel(modelGPrefabs, ref currentWorkParameterG, attachedModelsG, true);
        Debug.Log("現在の作業状態に基づいてモデルを「追加」生成しました。");
    }

    void ScaleUpModelsBasedOnCurrentWorkParams()
    {
        ScaleModelList(attachedModelsA, currentWorkEnlargeFactorA);
        ScaleModelList(attachedModelsB, currentWorkEnlargeFactorB);
        ScaleModelList(attachedModelsC, currentWorkEnlargeFactorC);
        ScaleModelList(attachedModelsD, currentWorkEnlargeFactorD);
        ScaleModelList(attachedModelsE, currentWorkEnlargeFactorE);
        ScaleModelList(attachedModelsF, currentWorkEnlargeFactorF);
        ScaleModelList(attachedModelsG, currentWorkEnlargeFactorG);
        Debug.Log("現在の作業パラメータに基づいて既存のモデルを拡大しました！");
    }

    void Start()
    {
        Debug.Log($"PersistentDataPath: {Application.persistentDataPath}");
        lastManuallyAccessedSlot = PlayerPrefs.GetInt("LastAccessedSlot", 0);

        bool loadedState = false;

        if (GameDataManager.Instance != null && GameDataManager.Instance.SlotToLoadFromGallery != -1)
        {
            int slotFromGallery = GameDataManager.Instance.SlotToLoadFromGallery;
            GameDataManager.Instance.SlotToLoadFromGallery = -1;
            Debug.Log($"ギャラリーからスロット {slotFromGallery} のロード指示。");
            if (LoadWorkStateFromSlotFile(slotFromGallery))
            {
                lastManuallyAccessedSlot = slotFromGallery;
                PlayerPrefs.SetInt("LastAccessedSlot", lastManuallyAccessedSlot);
                loadedState = true;
            }
            else { Debug.LogWarning($"ギャラリー指定のスロット {slotFromGallery} のロードに失敗。作業ファイルのロードを試みます。"); }
        }
        
        if (!loadedState)
        {
            if (LoadCurrentWorkStateFromFile()) { Debug.Log("前回終了時の作業状態をロードしました。"); loadedState = true; }
            else { Debug.Log("前回終了時の作業状態ファイルが見つからないかロード失敗。"); }
        }

        if (!loadedState)
        {
            Debug.Log("有効な保存データが見つからないため、完全に新規のオブジェクトを生成します。");
            ResetAllCurrentWorkData(); GenerateRandomMainObject(); SaveCurrentWorkStateToFile();
        }

        if (buttonResetAndNew != null) buttonResetAndNew.onClick.AddListener(HandleResetAndNewClick);
        if (buttonSaveToSlot != null) buttonSaveToSlot.onClick.AddListener(ShowSaveSlotSelection);
        if (buttonGallery != null) buttonGallery.onClick.AddListener(() => SceneManager.LoadScene("GalleryScene"));
        if (buttonSettings != null) buttonSettings.onClick.AddListener(MoveToSettings);
        if (button1 != null) button1.onClick.AddListener(GenerateRandomMainObjectAndSaveToCurrentWork);
        
        if (closeSaveSlotPanelButton != null && saveSlotSelectionPanel != null) {
            closeSaveSlotPanelButton.onClick.AddListener(() => saveSlotSelectionPanel.SetActive(false));
        }
        if (saveSlotSelectionPanel != null) saveSlotSelectionPanel.SetActive(false);

        InitializeTextCycling();
    }

    void Update() { RotateMainObject(); }
    void RotateMainObject() { if (currentMainObject != null) currentMainObject.transform.Rotate(Vector3.up, 6f * Time.deltaTime, Space.World); }
    void MoveToSettings() { Debug.Log("設定画面へ遷移します。"); SceneManager.LoadScene("SettingsScene"); } //
    void InitializeTextCycling() {
        currentTextGroupIndex = -1;
        if (textObjectGroups == null || textObjectGroups.Length == 0) return;
        for (int i = 0; i < textObjectGroups.Length; i++) {
            if (textObjectGroups[i] != null) {
                if (currentTextGroupIndex == -1) {
                    currentTextGroupIndex = i; textObjectGroups[i].alpha = 1f; textObjectGroups[i].gameObject.SetActive(true);
                } else {
                    textObjectGroups[i].alpha = 0f; textObjectGroups[i].gameObject.SetActive(false);
                }
            }
        }
        if (currentTextGroupIndex != -1) {
            int validGroupCount = 0; foreach(var group in textObjectGroups) if (group != null) validGroupCount++;
            if (validGroupCount > 1) StartCoroutine(CycleTextGroupsCoroutine());
        }
       }
    IEnumerator CycleTextGroupsCoroutine() {
        while (true) {
            yield return new WaitForSeconds(textDisplayDuration);
            if (textObjectGroups == null || textObjectGroups.Length <= 1 || currentTextGroupIndex < 0 || currentTextGroupIndex >= textObjectGroups.Length || textObjectGroups[currentTextGroupIndex] == null) yield break;
            CanvasGroup currentGroup = textObjectGroups[currentTextGroupIndex];
            int nextValidIndex = currentTextGroupIndex; int attempts = 0;
            do {
                nextValidIndex = (nextValidIndex + 1) % textObjectGroups.Length;
                if (++attempts > textObjectGroups.Length) { Debug.LogError("次の有効なテキストグループが見つかりませんでした。"); yield break; }
            } while (textObjectGroups[nextValidIndex] == null);
            CanvasGroup nextGroup = textObjectGroups[nextValidIndex];
            if (currentGroup != null) {
                yield return StartCoroutine(FadeCanvasGroup(currentGroup, 1f, 0f, textFadeDuration));
                currentGroup.gameObject.SetActive(false);
            }
            if (nextGroup != null) {
                nextGroup.gameObject.SetActive(true);
                yield return StartCoroutine(FadeCanvasGroup(nextGroup, 0f, 1f, textFadeDuration));
            }
            currentTextGroupIndex = nextValidIndex;
        }
    }
    IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration) {
        if (cg == null) yield break;
        float currentTime = 0f; cg.alpha = startAlpha;
        while (currentTime < duration) {
            currentTime += Time.deltaTime; cg.alpha = Mathf.Lerp(startAlpha, endAlpha, currentTime / duration);
            yield return null;
        }
        cg.alpha = endAlpha;
    }

    void HandleResetAndNewClick()
    {
        Debug.Log($"オブジェクト初期化ボタン押下。現在の作業状態をリセット＆新規生成します。");
        ResetAllCurrentWorkData();
        GenerateRandomMainObject();
        SaveCurrentWorkStateToFile();
    }

    void GenerateRandomMainObjectAndSaveToCurrentWork()
    {
        Debug.Log($"button1押下。現在の作業状態に新規ランダムオブジェクトを生成＆保存します。");
        if (currentMainObject != null) { Destroy(currentMainObject); currentMainObject = null; DeleteAllCurrentModels(); }
        GenerateRandomMainObject();
        SaveCurrentWorkStateToFile();
    }

    void ShowSaveSlotSelection()
    {
        if (saveSlotSelectionPanel != null) {
            saveSlotSelectionPanel.SetActive(true);
            for (int i = 0; i < saveSlotButtons.Length; i++) {
                if (saveSlotButtons[i] != null) {
                    int slotIndex = i;
                    saveSlotButtons[i].onClick.RemoveAllListeners();
                    saveSlotButtons[i].onClick.AddListener(() => {
                        SaveCurrentWorkToDesignatedSlot(slotIndex);
                        saveSlotSelectionPanel.SetActive(false);
                    });
                }
            }
        } else { Debug.LogWarning("保存スロット選択UIが設定されていません。"); }
    }

    void SaveCurrentWorkToDesignatedSlot(int slotIndex)
    {
        if (currentMainObject == null) { Debug.LogWarning("スロットに保存する現在の作業オブジェクトがありません！"); return; }
        string filePath = Path.Combine(Application.persistentDataPath, $"{SLOT_FILE_BASE_NAME}{slotIndex}{SAVE_FILE_EXTENSION}");
        SceneData sceneDataToSave = CreateSceneDataFromCurrentWork();
        string jsonData = JsonUtility.ToJson(sceneDataToSave, true);
        try {
            File.WriteAllText(filePath, jsonData);
            Debug.Log($"現在の作業内容をスロット {slotIndex} にコピー保存しました: {filePath}");
        } catch (System.Exception e) {
            Debug.LogError($"スロット {slotIndex} へのファイル保存エラー: {e.Message}");
        }
        string thumbnailPath = Path.Combine(Application.persistentDataPath, $"{THUMBNAIL_SLOT_BASE_NAME}{slotIndex}{THUMBNAIL_FILE_EXTENSION}");
        ThumbnailGenerator.CaptureAndSaveThumbnail(currentMainObject, thumbnailPath); //
        lastManuallyAccessedSlot = slotIndex;
        PlayerPrefs.SetInt("LastAccessedSlot", lastManuallyAccessedSlot);
    }
    
    void SaveCurrentWorkStateToFile()
    {
        if (currentMainObject == null && !(Application.isEditor && !Application.isPlaying)) {
             if (Application.isPlaying) Debug.LogWarning("現在の作業状態を保存するメインオブジェクトがありません！(SaveCurrentWorkStateToFile)");
             return;
        }
        if (currentMainObject == null && Application.isEditor && !Application.isPlaying) return;

        string filePath = Path.Combine(Application.persistentDataPath, $"{CURRENT_WORK_FILE_NAME}{SAVE_FILE_EXTENSION}");
        SceneData sceneDataToSave = CreateSceneDataFromCurrentWork();
        string jsonData = JsonUtility.ToJson(sceneDataToSave, true);
        try {
            File.WriteAllText(filePath, jsonData);
        } catch (System.Exception e) {
            Debug.LogError($"現在の作業状態の自動保存エラー: {e.Message}");
        }
        
        if(currentMainObject != null) {
            string thumbnailPath = Path.Combine(Application.persistentDataPath, $"{THUMBNAIL_CURRENT_WORK_NAME}session{THUMBNAIL_FILE_EXTENSION}");
            ThumbnailGenerator.CaptureAndSaveThumbnail(currentMainObject, thumbnailPath); //
        }
    }
    
    SceneData CreateSceneDataFromCurrentWork() {
        SceneData sceneData = new SceneData {
            objects = new List<SavedObjectData>(),
            parameterA = this.currentWorkParameterA, parameterB = this.currentWorkParameterB, parameterC = this.currentWorkParameterC,
            parameterD = this.currentWorkParameterD, parameterE = this.currentWorkParameterE, parameterF = this.currentWorkParameterF,
            parameterG = this.currentWorkParameterG,
            enlargeFactorA = this.currentWorkEnlargeFactorA, enlargeFactorB = this.currentWorkEnlargeFactorB, enlargeFactorC = this.currentWorkEnlargeFactorC,
            enlargeFactorD = this.currentWorkEnlargeFactorD, enlargeFactorE = this.currentWorkEnlargeFactorE, enlargeFactorF = this.currentWorkEnlargeFactorF,
            enlargeFactorG = this.currentWorkEnlargeFactorG,
            generationThreshold = this.generationThreshold,
            totalEnergy = this.currentWorkEnergy, totalContent = this.currentWorkContent, totalUpset = this.currentWorkUpset,
            totalAggression = this.currentWorkAggression, totalStress = this.currentWorkStress, totalUncertainty = this.currentWorkUncertainty,
            totalExcitement = this.currentWorkExcitement, totalConcentration = this.currentWorkConcentration, totalEmoCog = this.currentWorkEmoCog,
            totalHesitation = this.currentWorkHesitation, totalBrainPower = this.currentWorkBrainPower, totalEmbarrassment = this.currentWorkEmbarrassment,
            totalIntensiveThinking = this.currentWorkIntensiveThinking, totalImaginationActivity = this.currentWorkImaginationActivity,
            totalExtremeEmotion = this.currentWorkExtremeEmotion, totalPassionate = this.currentWorkPassionate,
            totalAtmosphere = this.currentWorkAtmosphere, totalAnticipation = this.currentWorkAnticipation,
            totalDissatisfaction = this.currentWorkDissatisfaction, totalConfidence = this.currentWorkConfidence
        };

        if (currentMainObject != null) {
            SavedObjectData mainObjectData = new SavedObjectData {
                position = currentMainObject.transform.position, rotation = currentMainObject.transform.rotation,
                scale = currentMainObject.transform.localScale, type = "main", prefabIndex = -1
            };
            for (int i = 0; i < glassPrefabs.Length; i++) {
                if (glassPrefabs[i] != null && currentMainObject.name.StartsWith(glassPrefabs[i].name)) {
                    mainObjectData.prefabName = glassPrefabs[i].name; mainObjectData.prefabIndex = i;
                    sceneData.mainObjectIndex = i; break;
                }
            }
            if (mainObjectData.prefabIndex == -1 && glassPrefabs.Length > 0) Debug.LogWarning("メインオブジェクトのPrefab名が特定できませんでした(CreateSceneData)。");
            sceneData.objects.Add(mainObjectData);

            SaveModelList(attachedModelsA, "A", modelAPrefabs, sceneData.objects); SaveModelList(attachedModelsB, "B", modelBPrefabs, sceneData.objects);
            SaveModelList(attachedModelsC, "C", modelCPrefabs, sceneData.objects); SaveModelList(attachedModelsD, "D", modelDPrefabs, sceneData.objects);
            SaveModelList(attachedModelsE, "E", modelEPrefabs, sceneData.objects); SaveModelList(attachedModelsF, "F", modelFPrefabs, sceneData.objects);
            SaveModelList(attachedModelsG, "G", modelGPrefabs, sceneData.objects);
        } else {
             sceneData.mainObjectIndex = -1;
        }
        return sceneData;
       }
    void SaveModelList(List<GameObject> modelList, string type, GameObject[] prefabs, List<SavedObjectData> objectsList) {
        foreach (GameObject model in modelList) {
            if (model == null) continue;
            SavedObjectData modelData = new SavedObjectData {
                position = model.transform.position, rotation = model.transform.rotation,
                scale = model.transform.localScale, type = type, prefabIndex = -1
            };
            if (prefabs != null) {
                for (int i = 0; i < prefabs.Length; i++) {
                    if (prefabs[i] != null && model.name.StartsWith(prefabs[i].name)) {
                        modelData.prefabName = prefabs[i].name; modelData.prefabIndex = i; break;
                    }
                }
            }
            if (modelData.prefabIndex == -1 && prefabs != null && prefabs.Length > 0) Debug.LogWarning($"モデル ({type}) のPrefab名が特定できませんでした: {model.name}");
            objectsList.Add(modelData);
        }
       }

    bool LoadWorkStateFromSlotFile(int slotIndex)
    {
        string filePath = Path.Combine(Application.persistentDataPath, $"{SLOT_FILE_BASE_NAME}{slotIndex}{SAVE_FILE_EXTENSION}");
        if (!File.Exists(filePath)) { Debug.LogWarning($"スロット {slotIndex} の保存ファイルが見つかりません: {filePath}"); return false; }
        ResetAllCurrentWorkData();
        string jsonData = ""; try { jsonData = File.ReadAllText(filePath); }
        catch (System.Exception e) { Debug.LogError($"ファイル読込エラー (スロット {slotIndex}): {e.Message}"); return false; }
        SceneData loadedSceneData = null; try { loadedSceneData = JsonUtility.FromJson<SceneData>(jsonData); }
        catch (System.Exception e) { Debug.LogError($"JSONパースエラー (スロット {slotIndex}): {e.Message}"); }
        if (loadedSceneData == null) { Debug.LogError($"スロット {slotIndex} データ読込/パース失敗。"); return false; }
        PopulateCurrentWorkStateFromSceneData(loadedSceneData);
        Debug.Log($"スロット {slotIndex} のデータを現在の作業状態として読み込みました。");
        return true;
    }
    
    bool LoadCurrentWorkStateFromFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, $"{CURRENT_WORK_FILE_NAME}{SAVE_FILE_EXTENSION}");
        if (!File.Exists(filePath)) { Debug.Log($"現在の作業状態ファイル ({CURRENT_WORK_FILE_NAME}) が見つかりません。"); return false; }
        ResetAllCurrentWorkData();
        string jsonData = ""; try { jsonData = File.ReadAllText(filePath); }
        catch (System.Exception e) { Debug.LogError($"現在の作業状態ファイルの読込エラー: {e.Message}"); return false; }
        SceneData loadedSceneData = null; try { loadedSceneData = JsonUtility.FromJson<SceneData>(jsonData); }
        catch (System.Exception e) { Debug.LogError($"現在の作業状態ファイルのJSONパースエラー: {e.Message}"); }
        if (loadedSceneData == null) { Debug.LogError("現在の作業状態データの読込/パース失敗。"); return false; }
        PopulateCurrentWorkStateFromSceneData(loadedSceneData);
        return true;
    }

    void PopulateCurrentWorkStateFromSceneData(SceneData data) {
        currentWorkParameterA = data.parameterA; currentWorkParameterB = data.parameterB; currentWorkParameterC = data.parameterC;
        currentWorkParameterD = data.parameterD; currentWorkParameterE = data.parameterE; currentWorkParameterF = data.parameterF;
        currentWorkParameterG = data.parameterG;
        currentWorkEnlargeFactorA = data.enlargeFactorA; currentWorkEnlargeFactorB = data.enlargeFactorB; currentWorkEnlargeFactorC = data.enlargeFactorC;
        currentWorkEnlargeFactorD = data.enlargeFactorD; currentWorkEnlargeFactorE = data.enlargeFactorE; currentWorkEnlargeFactorF = data.enlargeFactorF;
        currentWorkEnlargeFactorG = data.enlargeFactorG;
        generationThreshold = data.generationThreshold;
        currentWorkEnergy = data.totalEnergy; currentWorkContent = data.totalContent; currentWorkUpset = data.totalUpset;
        currentWorkAggression = data.totalAggression; currentWorkStress = data.totalStress; currentWorkUncertainty = data.totalUncertainty;
        currentWorkExcitement = data.totalExcitement; currentWorkConcentration = data.totalConcentration; currentWorkEmoCog = data.totalEmoCog;
        currentWorkHesitation = data.totalHesitation; currentWorkBrainPower = data.totalBrainPower; currentWorkEmbarrassment = data.totalEmbarrassment;
        currentWorkIntensiveThinking = data.totalIntensiveThinking; currentWorkImaginationActivity = data.totalImaginationActivity;
        currentWorkExtremeEmotion = data.totalExtremeEmotion; currentWorkPassionate = data.totalPassionate;
        currentWorkAtmosphere = data.totalAtmosphere; currentWorkAnticipation = data.totalAnticipation;
        currentWorkDissatisfaction = data.totalDissatisfaction; currentWorkConfidence = data.totalConfidence;
        UpdateWorkSentimentUI();

        bool mainObjectRestored = false;
        if (data.objects != null) {
            foreach (SavedObjectData objData in data.objects) {
                if (objData.type == "main" && data.mainObjectIndex >= 0 && data.mainObjectIndex < glassPrefabs.Length && glassPrefabs[data.mainObjectIndex] != null) {
                    currentMainObject = Instantiate(glassPrefabs[data.mainObjectIndex], objData.position, objData.rotation);
                    currentMainObject.transform.localScale = objData.scale;
                    mainObjectRestored = true;
                } else if (objData.type != "main") {
                    GameObject[] prefabArray = GetPrefabArrayByType(objData.type); List<GameObject> modelList = GetModelListByType(objData.type);
                    if (prefabArray != null && objData.prefabIndex >= 0 && objData.prefabIndex < prefabArray.Length && prefabArray[objData.prefabIndex] != null) {
                        GameObject newModel = Instantiate(prefabArray[objData.prefabIndex], objData.position, objData.rotation);
                        newModel.transform.localScale = objData.scale;
                        if (currentMainObject != null) newModel.transform.SetParent(currentMainObject.transform);
                        if (modelList != null) modelList.Add(newModel);
                    }
                }
            }
        }
        if (!mainObjectRestored) {
             Debug.LogWarning($"メインオブジェクトの復元に失敗しました (Populate)。新規ランダムオブジェクトを作業状態にします。");
             GenerateRandomMainObject(); 
        }
       }
    private void ResetAllCurrentWorkData() {
        DeleteAllCurrentModels();
        if (currentMainObject != null) { Destroy(currentMainObject); currentMainObject = null; }
        currentWorkEnergy = 0; currentWorkContent = 0; currentWorkUpset = 0; currentWorkAggression = 0; currentWorkStress = 0;
        currentWorkUncertainty = 0; currentWorkExcitement = 0; currentWorkConcentration = 0; currentWorkEmoCog = 0;
        currentWorkHesitation = 0; currentWorkBrainPower = 0; currentWorkEmbarrassment = 0; currentWorkIntensiveThinking = 0;
        currentWorkImaginationActivity = 0; currentWorkExtremeEmotion = 0; currentWorkPassionate = 0; currentWorkAtmosphere = 0;
        currentWorkAnticipation = 0; currentWorkDissatisfaction = 0; currentWorkConfidence = 0;
        UpdateWorkSentimentUI();
        currentWorkDerivedJoy = 0f; currentWorkDerivedAnger = 0f; currentWorkDerivedSadness = 0f; currentWorkDerivedEnjoyment = 0f;
        currentWorkDerivedFocus = 0f; currentWorkDerivedAnxiety = 0f; currentWorkDerivedConfidence = 0f;
        SetDefaultModelParametersToCurrentWork();
    }
    private void DeleteAllCurrentModels() {
        DeleteModelList(attachedModelsA); DeleteModelList(attachedModelsB); DeleteModelList(attachedModelsC);
        DeleteModelList(attachedModelsD); DeleteModelList(attachedModelsE); DeleteModelList(attachedModelsF); DeleteModelList(attachedModelsG);
    }
    void GenerateRandomMainObject() {
        if (currentMainObject != null) { Destroy(currentMainObject); currentMainObject = null; DeleteAllCurrentModels(); }
        if (glassPrefabs.Length == 0) { Debug.LogError("glassPrefabsが設定されていません！"); return; }
        int randomIndex = -1; List<int> validIndices = new List<int>();
        for(int i=0; i < glassPrefabs.Length; ++i) if(glassPrefabs[i] != null) validIndices.Add(i);
        if(validIndices.Count == 0) { Debug.LogError("有効なglassPrefabが一つもありません。"); return; }
        randomIndex = validIndices[Random.Range(0, validIndices.Count)];
        currentMainObject = Instantiate(glassPrefabs[randomIndex], new Vector3(0, 1, 0), Quaternion.identity);
        currentWorkEnergy = 0; currentWorkContent = 0; currentWorkUpset = 0; currentWorkAggression = 0; currentWorkStress = 0;
        currentWorkUncertainty = 0; currentWorkExcitement = 0; currentWorkConcentration = 0; currentWorkEmoCog = 0;
        currentWorkHesitation = 0; currentWorkBrainPower = 0; currentWorkEmbarrassment = 0; currentWorkIntensiveThinking = 0;
        currentWorkImaginationActivity = 0; currentWorkExtremeEmotion = 0; currentWorkPassionate = 0; currentWorkAtmosphere = 0;
        currentWorkAnticipation = 0; currentWorkDissatisfaction = 0; currentWorkConfidence = 0;
        UpdateWorkSentimentUI(); 
        SetDefaultModelParametersToCurrentWork(); 
    }

    // ★★★★★ 変更点 1/2 ★★★★★
    // 初期化時の生成パラメータにもスケール係数を適用します。
    void SetDefaultModelParametersToCurrentWork() {
        currentWorkDerivedJoy = 0.1f; currentWorkDerivedAnger = 0f; currentWorkDerivedSadness = 0.1f; currentWorkDerivedEnjoyment = 0.1f;
        currentWorkDerivedFocus = 0.1f; currentWorkDerivedAnxiety = 0f; currentWorkDerivedConfidence = 0.1f;

        // 生成される鉱石の基本スケールを調整するための係数
        const float scaleMultiplier = 5.0f;

        currentWorkParameterA = currentWorkDerivedJoy * scaleMultiplier; 
        currentWorkParameterB = currentWorkDerivedAnger * scaleMultiplier; 
        currentWorkParameterC = currentWorkDerivedSadness * scaleMultiplier;
        currentWorkParameterD = currentWorkDerivedEnjoyment * scaleMultiplier; 
        currentWorkParameterE = currentWorkDerivedFocus * scaleMultiplier; 
        currentWorkParameterF = currentWorkDerivedAnxiety * scaleMultiplier;
        currentWorkParameterG = currentWorkDerivedConfidence * scaleMultiplier;

        // 拡大倍率(enlargeFactor)の計算は、元の正規化された値(0-1)を使い、変更しない
        currentWorkEnlargeFactorA = 1.0f + currentWorkDerivedJoy * 1.0f; currentWorkEnlargeFactorB = 1.0f + currentWorkDerivedAnger * 1.0f;
        currentWorkEnlargeFactorC = 1.0f + currentWorkDerivedSadness * 1.0f; currentWorkEnlargeFactorD = 1.0f + currentWorkDerivedEnjoyment * 1.0f;
        currentWorkEnlargeFactorE = 1.0f + currentWorkDerivedFocus * 0.5f; currentWorkEnlargeFactorF = 1.0f + currentWorkDerivedAnxiety * 1.0f;
        currentWorkEnlargeFactorG = 1.0f + currentWorkDerivedConfidence * 1.0f;
    }

    // ★★★★★ 変更点 2/2 ★★★★★
    // 感情データからパラメータを計算する箇所にスケール係数を適用します。
    public void SetParametersFromJson(string jsonData)
    {
        bool validDataReceived = false;
        try {
            SentimentAnalysisResponse response = JsonUtility.FromJson<SentimentAnalysisResponse>(jsonData); //
            if (response?.sentiment_analysis?.segments != null && response.sentiment_analysis.segments.Count > 0) { //
                int apiEnergy = 0, apiContent = 0, apiUpset = 0, apiAggression = 0, apiStress = 0,
                    apiUncertainty = 0, apiExcitement = 0, apiConcentration = 0, apiEmoCog = 0,
                    apiHesitation = 0, apiBrainPower = 0, apiEmbarrassment = 0, apiIntensiveThinking = 0,
                    apiImaginationActivity = 0, apiExtremeEmotion = 0, apiPassionate = 0, apiAtmosphere = 0,
                    apiAnticipation = 0, apiDissatisfaction = 0, apiConfidence = 0;
                foreach (SentimentSegment seg in response.sentiment_analysis.segments) { //
                    apiEnergy += seg.energy; apiContent += seg.content; apiUpset += seg.upset; apiAggression += seg.aggression; apiStress += seg.stress; apiUncertainty += seg.uncertainty; apiExcitement += seg.excitement; apiConcentration += seg.concentration; apiEmoCog += seg.emo_cog; apiHesitation += seg.hesitation; apiBrainPower += seg.brain_power; apiEmbarrassment += seg.embarrassment; apiIntensiveThinking += seg.intensive_thinking; apiImaginationActivity += seg.imagination_activity; apiExtremeEmotion += seg.extreme_emotion; apiPassionate += seg.passionate; apiAtmosphere += seg.atmosphere; apiAnticipation += seg.anticipation; apiDissatisfaction += seg.dissatisfaction; apiConfidence += seg.confidence; //
                }
                currentWorkEnergy += apiEnergy; currentWorkContent += apiContent; currentWorkUpset += apiUpset; currentWorkAggression += apiAggression; currentWorkStress += apiStress; currentWorkUncertainty += apiUncertainty; currentWorkExcitement += apiExcitement; currentWorkConcentration += apiConcentration; currentWorkEmoCog += apiEmoCog; currentWorkHesitation += apiHesitation; currentWorkBrainPower += apiBrainPower; currentWorkEmbarrassment += apiEmbarrassment; currentWorkIntensiveThinking += apiIntensiveThinking; currentWorkImaginationActivity += apiImaginationActivity; currentWorkExtremeEmotion += apiExtremeEmotion; currentWorkPassionate += apiPassionate; currentWorkAtmosphere += apiAtmosphere; currentWorkAnticipation += apiAnticipation; currentWorkDissatisfaction += apiDissatisfaction; currentWorkConfidence += apiConfidence;
                UpdateWorkSentimentUI();

                currentWorkDerivedJoy = ((float)currentWorkContent+(float)currentWorkExcitement+(float)currentWorkPassionate+Mathf.Max(0, currentWorkAtmosphere)+(float)currentWorkAnticipation)/500.0f;
                currentWorkDerivedAnger = ((float)currentWorkAggression+(float)currentWorkUpset+(float)currentWorkStress+(float)currentWorkExtremeEmotion)/400.0f;
                currentWorkDerivedSadness = ((float)currentWorkDissatisfaction+Mathf.Max(0, -currentWorkAtmosphere)+(float)currentWorkUncertainty+(100.0f-(float)currentWorkEnergy)/2.0f)/450.0f;
                currentWorkDerivedEnjoyment = ((float)currentWorkExcitement+(float)currentWorkAnticipation+(float)currentWorkImaginationActivity+(float)currentWorkEnergy)/400.0f;
                float logicalScore = Mathf.Max(0, currentWorkEmoCog-100);
                currentWorkDerivedFocus = ((float)currentWorkConcentration+(float)currentWorkIntensiveThinking+(float)currentWorkBrainPower+logicalScore)/400.0f;
                currentWorkDerivedAnxiety = ((float)currentWorkHesitation+(float)currentWorkUncertainty+(float)currentWorkStress+(float)currentWorkEmbarrassment)/400.0f;
                currentWorkDerivedConfidence = ((float)currentWorkConfidence+(float)currentWorkEnergy+(float)currentWorkPassionate+(float)currentWorkBrainPower)/400.0f;
                currentWorkDerivedJoy=Mathf.Clamp01(currentWorkDerivedJoy); currentWorkDerivedAnger=Mathf.Clamp01(currentWorkDerivedAnger); currentWorkDerivedSadness=Mathf.Clamp01(currentWorkDerivedSadness);
                currentWorkDerivedEnjoyment=Mathf.Clamp01(currentWorkDerivedEnjoyment); currentWorkDerivedFocus=Mathf.Clamp01(currentWorkDerivedFocus);
                currentWorkDerivedAnxiety=Mathf.Clamp01(currentWorkDerivedAnxiety); currentWorkDerivedConfidence=Mathf.Clamp01(currentWorkDerivedConfidence);
                Debug.Log($"Derived Emotions (Work): Joy={currentWorkDerivedJoy:F2}, Anger={currentWorkDerivedAnger:F2}, Sadness={currentWorkDerivedSadness:F2}, Enjoyment={currentWorkDerivedEnjoyment:F2}, Focus={currentWorkDerivedFocus:F2}, Anxiety={currentWorkDerivedAnxiety:F2}, Confidence={currentWorkDerivedConfidence:F2}");

                // 生成される鉱石の基本スケールを調整するための係数
                const float scaleMultiplier = 2.0f;

                currentWorkParameterA=currentWorkDerivedJoy * scaleMultiplier; 
                currentWorkParameterB=currentWorkDerivedAnger * scaleMultiplier; 
                currentWorkParameterC=currentWorkDerivedSadness * scaleMultiplier; 
                currentWorkParameterD=currentWorkDerivedEnjoyment * scaleMultiplier;
                currentWorkParameterE=currentWorkDerivedFocus * scaleMultiplier; 
                currentWorkParameterF=currentWorkDerivedAnxiety * scaleMultiplier; 
                currentWorkParameterG=currentWorkDerivedConfidence * scaleMultiplier;
                
                // 拡大倍率(enlargeFactor)の計算は、元の正規化された値(0-1)を使い、変更しない
                currentWorkEnlargeFactorA=1.0f+currentWorkDerivedJoy*1.0f; currentWorkEnlargeFactorB=1.0f+currentWorkDerivedAnger*1.0f; currentWorkEnlargeFactorC=1.0f+currentWorkDerivedSadness*1.0f;
                currentWorkEnlargeFactorD=1.0f+currentWorkDerivedEnjoyment*1.0f; currentWorkEnlargeFactorE=1.0f+currentWorkDerivedFocus*0.5f; currentWorkEnlargeFactorF=1.0f+currentWorkDerivedAnxiety*1.0f;
                currentWorkEnlargeFactorG=1.0f+currentWorkDerivedConfidence*1.0f;
                validDataReceived = true;
            } else { Debug.LogWarning("API response data invalid for sentiment processing."); return; }
        } catch (System.Exception e) { Debug.LogError($"Error in SetParametersFromJson: {e.Message}\n{e.StackTrace}"); return; }

        if (validDataReceived) {
            if (currentMainObject == null) GenerateRandomMainObject();
            
            // 既存モデルをスケールアップ
            ScaleUpModelsBasedOnCurrentWorkParams();
            // 新規モデルを追加生成（GenerateAllModelsは既存モデルを削除しなくなった）
            GenerateAllModels(); 
            
            SaveCurrentWorkStateToFile();
        }
    }
    
    void ScaleModelList(List<GameObject> modelList, float scaleFactor) {
        foreach (GameObject model in modelList) {
            if (model != null) model.transform.localScale *= scaleFactor;
        }
    }
    
    void DeleteAllObjects() { ResetAllCurrentWorkData(); Debug.Log("現在の作業状態の全オブジェクト削除＆感情データリセット完了。"); }
    void DeleteModelList(List<GameObject> modelList) { foreach (GameObject model in modelList) { if (model != null) Destroy(model); } modelList.Clear(); }
    void UpdateWorkSentimentUI() {
        if (textEnergy != null) textEnergy.text = $"Energy: {currentWorkEnergy:D}";
        if (textContent != null) textContent.text = $"Content: {currentWorkContent:D}";
        if (textUpset != null) textUpset.text = $"Upset: {currentWorkUpset:D}";
        if (textAggression != null) textAggression.text = $"Aggression: {currentWorkAggression:D}";
        if (textStress != null) textStress.text = $"Stress: {currentWorkStress:D}";
        if (textUncertainty != null) textUncertainty.text = $"Uncertainty: {currentWorkUncertainty:D}";
        if (textExcitement != null) textExcitement.text = $"Excitement: {currentWorkExcitement:D}";
        if (textConcentration != null) textConcentration.text = $"Concentration: {currentWorkConcentration:D}";
        if (textEmoCog != null) textEmoCog.text = $"Emo Cog: {currentWorkEmoCog:D}";
        if (textHesitation != null) textHesitation.text = $"Hesitation: {currentWorkHesitation:D}";
        if (textBrainPower != null) textBrainPower.text = $"Brain Power: {currentWorkBrainPower:D}";
        if (textEmbarrassment != null) textEmbarrassment.text = $"Embarrassment: {currentWorkEmbarrassment:D}";
        if (textIntensiveThinking != null) textIntensiveThinking.text = $"Intensive Thinking: {currentWorkIntensiveThinking:D}";
        if (textImaginationActivity != null) textImaginationActivity.text = $"Imagination Activity: {currentWorkImaginationActivity:D}";
        if (textExtremeEmotion != null) textExtremeEmotion.text = $"Extreme Emotion: {currentWorkExtremeEmotion:D}";
        if (textPassionate != null) textPassionate.text = $"Passionate: {currentWorkPassionate:D}";
        if (textAtmosphere != null) textAtmosphere.text = $"Atmosphere: {currentWorkAtmosphere:D}";
        if (textAnticipation != null) textAnticipation.text = $"Anticipation: {currentWorkAnticipation:D}";
        if (textDissatisfaction != null) textDissatisfaction.text = $"Dissatisfaction: {currentWorkDissatisfaction:D}";
        if (textConfidence != null) textConfidence.text = $"Confidence: {currentWorkConfidence:D}";
    }

    void OnApplicationQuit() {
        SaveCurrentWorkStateToFile();
        ThumbnailGenerator.Cleanup(); //
    }
}