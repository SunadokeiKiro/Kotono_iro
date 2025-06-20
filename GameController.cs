// Scripts/GameController.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// (SavedObjectData と SceneData の定義は変更なし)
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

/// <summary>
/// ゲームの主要なロジックを管理します。設定値はConfigアセットから読み込みます。
/// </summary>
public class GameController : MonoBehaviour
{
    [Header("Configuration Assets")]
    [SerializeField] private EmotionCalculationConfig emotionConfig; // ★★★ 修正点: 設定アセットへの参照を追加 ★★★

    [Header("Manager References")]
    [SerializeField] private MainUIManager mainUIManager;

    // (その他の変数の定義は変更なし)
    #region 変数定義
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

    [Header("Prefabs")]
    public GameObject[] glassPrefabs;
    public GameObject[] modelAPrefabs;
    public GameObject[] modelBPrefabs;
    public GameObject[] modelCPrefabs;
    public GameObject[] modelDPrefabs;
    public GameObject[] modelEPrefabs;
    public GameObject[] modelFPrefabs;
    public GameObject[] modelGPrefabs;

    [Header("モデル生成パラメーター")]
    public float currentWorkParameterA = 0.0f;
    public float currentWorkParameterB = 0.0f;
    public float currentWorkParameterC = 0.0f;
    public float currentWorkParameterD = 0.0f;
    public float currentWorkParameterE = 0.0f;
    public float currentWorkParameterF = 0.0f;
    public float currentWorkParameterG = 0.0f;

    [Header("生成半径")]
    public float generationThreshold = 0.3f;

    [Header("モデルの拡大倍率")]
    public float currentWorkEnlargeFactorA = 1.0f;
    public float currentWorkEnlargeFactorB = 1.0f;
    public float currentWorkEnlargeFactorC = 1.0f;
    public float currentWorkEnlargeFactorD = 1.0f;
    public float currentWorkEnlargeFactorE = 1.0f;
    public float currentWorkEnlargeFactorF = 1.0f;
    public float currentWorkEnlargeFactorG = 1.0f;
    
    public float currentWorkDerivedJoy = 0f;
    public float currentWorkDerivedAnger = 0f;
    public float currentWorkDerivedSadness = 0f;
    public float currentWorkDerivedEnjoyment = 0f;
    public float currentWorkDerivedFocus = 0f;
    public float currentWorkDerivedAnxiety = 0f;
    public float currentWorkDerivedConfidence = 0f;

    private List<GameObject> attachedModelsA = new List<GameObject>();
    private List<GameObject> attachedModelsB = new List<GameObject>();
    private List<GameObject> attachedModelsC = new List<GameObject>();
    private List<GameObject> attachedModelsD = new List<GameObject>();
    private List<GameObject> attachedModelsE = new List<GameObject>();
    private List<GameObject> attachedModelsF = new List<GameObject>();
    private List<GameObject> attachedModelsG = new List<GameObject>();

    private GameObject currentMainObject;
    #endregion
    
    void Start()
    {
        // ★★★ 修正点: emotionConfigの参照をチェック ★★★
        if (emotionConfig == null)
        {
            Debug.LogError("GameController: EmotionCalculationConfig is not set in the inspector!");
            enabled = false;
            return;
        }
        if (mainUIManager == null)
        {
            Debug.LogError("GameController: MainUIManager is not set in the inspector!");
            enabled = false;
            return;
        }

        Debug.Log($"PersistentDataPath: {Application.persistentDataPath}");
        lastManuallyAccessedSlot = PlayerPrefs.GetInt("LastAccessedSlot", 0);
        
        LoadInitialState();
    }
    
    // (Update, OnApplicationQuitは変更なし)
    #region Unityライフサイクル
    void Update()
    {
        RotateMainObject();
    }

    void OnApplicationQuit()
    {
        SaveCurrentWorkStateToFile();
        ThumbnailGenerator.Cleanup();
    }
    #endregion

    // (LoadInitialStateは変更なし)
    #region 初期化と状態ロード
    private void LoadInitialState()
    {
        bool loadedState = false;

        if (GameDataManager.Instance != null && GameDataManager.Instance.SlotToLoadFromGallery != -1)
        {
            int slotFromGallery = GameDataManager.Instance.SlotToLoadFromGallery;
            GameDataManager.Instance.SlotToLoadFromGallery = -1;
            Debug.Log($"Attempting to load from gallery, slot {slotFromGallery}.");
            if (LoadWorkStateFromSlotFile(slotFromGallery))
            {
                lastManuallyAccessedSlot = slotFromGallery;
                PlayerPrefs.SetInt("LastAccessedSlot", lastManuallyAccessedSlot);
                loadedState = true;
            }
            else
            {
                Debug.LogWarning($"Failed to load slot {slotFromGallery} specified by gallery.");
            }
        }

        if (!loadedState)
        {
            if (LoadCurrentWorkStateFromFile())
            {
                Debug.Log("Loaded previous work session state.");
                loadedState = true;
            }
            else
            {
                Debug.Log("Previous work session file not found.");
            }
        }

        if (!loadedState)
        {
            Debug.Log("No valid save data found. Creating a new object.");
            ResetAllCurrentWorkData();
            GenerateRandomMainObject();
            SaveCurrentWorkStateToFile();
        }

        if(mainUIManager != null)
        {
            mainUIManager.UpdateWorkSentimentUI(CreateSceneDataFromCurrentWork());
            mainUIManager.InitializeTextCycling();
        }
    }
    #endregion

    // (MainUIManagerから呼び出されるメソッド群は変更なし)
    #region MainUIManagerから呼び出されるメソッド
    public void HandleResetAndNewClick()
    {
        Debug.Log("Resetting current work state and generating a new object.");
        ResetAllCurrentWorkData();
        GenerateRandomMainObject();
        SaveCurrentWorkStateToFile();
        if (mainUIManager != null) mainUIManager.UpdateWorkSentimentUI(CreateSceneDataFromCurrentWork());
    }

    public void GenerateRandomMainObjectAndSaveToCurrentWork()
    {
        Debug.Log("Replacing current work object with a new random one.");
        if (currentMainObject != null)
        {
            Destroy(currentMainObject);
            currentMainObject = null;
            DeleteAllCurrentModels();
        }
        GenerateRandomMainObject();
        SaveCurrentWorkStateToFile();
        if (mainUIManager != null) mainUIManager.UpdateWorkSentimentUI(CreateSceneDataFromCurrentWork());
    }

    public void MoveToSettings()
    {
        Debug.Log("Moving to Settings scene.");
        SceneManager.LoadScene("SettingsScene");
    }

    public void SaveCurrentWorkToDesignatedSlot(int slotIndex)
    {
        if (currentMainObject == null)
        {
            Debug.LogWarning("There is no object to save.");
            return;
        }
        string filePath = Path.Combine(Application.persistentDataPath, $"{SLOT_FILE_BASE_NAME}{slotIndex}{SAVE_FILE_EXTENSION}");
        SceneData sceneDataToSave = CreateSceneDataFromCurrentWork();
        string jsonData = JsonUtility.ToJson(sceneDataToSave, true);

        try
        {
            File.WriteAllText(filePath, jsonData);
            string thumbnailPath = Path.Combine(Application.persistentDataPath, $"{THUMBNAIL_SLOT_BASE_NAME}{slotIndex}{THUMBNAIL_FILE_EXTENSION}");
            ThumbnailGenerator.CaptureAndSaveThumbnail(currentMainObject, thumbnailPath);
            Debug.Log($"Current work saved to slot {slotIndex}: {filePath}");
            
            lastManuallyAccessedSlot = slotIndex;
            PlayerPrefs.SetInt("LastAccessedSlot", lastManuallyAccessedSlot);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save to slot {slotIndex}: {e.Message}");
        }
    }
    #endregion

    private void CalculateDerivedEmotions()
    {
        currentWorkDerivedJoy = ((float)currentWorkContent + (float)currentWorkExcitement + (float)currentWorkPassionate + Mathf.Max(0, currentWorkAtmosphere) + (float)currentWorkAnticipation) / emotionConfig.joyDivisor;
        currentWorkDerivedAnger = ((float)currentWorkAggression + (float)currentWorkUpset + (float)currentWorkStress + (float)currentWorkExtremeEmotion) / emotionConfig.angerDivisor;
        currentWorkDerivedSadness = ((float)currentWorkDissatisfaction + Mathf.Max(0, -currentWorkAtmosphere) + (float)currentWorkUncertainty + (100.0f - (float)currentWorkEnergy) / emotionConfig.sadnessFromEnergyDivisor) / emotionConfig.sadnessDivisor;
        currentWorkDerivedEnjoyment = ((float)currentWorkExcitement + (float)currentWorkAnticipation + (float)currentWorkImaginationActivity + (float)currentWorkEnergy) / emotionConfig.enjoymentDivisor;
        float logicalScore = Mathf.Max(0, currentWorkEmoCog - emotionConfig.emoCogBaseline);
        currentWorkDerivedFocus = ((float)currentWorkConcentration + (float)currentWorkIntensiveThinking + (float)currentWorkBrainPower + logicalScore) / emotionConfig.focusDivisor;
        currentWorkDerivedAnxiety = ((float)currentWorkHesitation + (float)currentWorkUncertainty + (float)currentWorkStress + (float)currentWorkEmbarrassment) / emotionConfig.anxietyDivisor;
        currentWorkDerivedConfidence = ((float)currentWorkConfidence + (float)currentWorkEnergy + (float)currentWorkPassionate + (float)currentWorkBrainPower) / emotionConfig.confidenceDivisor;

        // クランプ処理はそのまま
        currentWorkDerivedJoy = Mathf.Clamp01(currentWorkDerivedJoy);
        currentWorkDerivedAnger = Mathf.Clamp01(currentWorkDerivedAnger);
        currentWorkDerivedSadness = Mathf.Clamp01(currentWorkDerivedSadness);
        currentWorkDerivedEnjoyment = Mathf.Clamp01(currentWorkDerivedEnjoyment);
        currentWorkDerivedFocus = Mathf.Clamp01(currentWorkDerivedFocus);
        currentWorkDerivedAnxiety = Mathf.Clamp01(currentWorkDerivedAnxiety);
        currentWorkDerivedConfidence = Mathf.Clamp01(currentWorkDerivedConfidence);

        Debug.Log($"Derived Emotions: Joy={currentWorkDerivedJoy:F2}, Anger={currentWorkDerivedAnger:F2}, Sadness={currentWorkDerivedSadness:F2}, Enjoyment={currentWorkDerivedEnjoyment:F2}, Focus={currentWorkDerivedFocus:F2}, Anxiety={currentWorkDerivedAnxiety:F2}, Confidence={currentWorkDerivedConfidence:F2}");
    }
    
    private void MapEmotionsToModelParameters()
    {
        currentWorkParameterA = currentWorkDerivedJoy;
        currentWorkParameterB = currentWorkDerivedAnger;
        currentWorkParameterC = currentWorkDerivedSadness;
        currentWorkParameterD = currentWorkDerivedEnjoyment;
        currentWorkParameterE = currentWorkDerivedFocus;
        currentWorkParameterF = currentWorkDerivedAnxiety;
        currentWorkParameterG = currentWorkDerivedConfidence;

        // ★★★ 修正点: ハードコードされた係数をConfigアセットから取得 ★★★
        currentWorkEnlargeFactorA = 1.0f + currentWorkDerivedJoy * emotionConfig.joyEnlargeFactor;
        currentWorkEnlargeFactorB = 1.0f + currentWorkDerivedAnger * emotionConfig.angerEnlargeFactor;
        currentWorkEnlargeFactorC = 1.0f + currentWorkDerivedSadness * emotionConfig.sadnessEnlargeFactor;
        currentWorkEnlargeFactorD = 1.0f + currentWorkDerivedEnjoyment * emotionConfig.enjoymentEnlargeFactor;
        currentWorkEnlargeFactorE = 1.0f + currentWorkDerivedFocus * emotionConfig.focusEnlargeFactor;
        currentWorkEnlargeFactorF = 1.0f + currentWorkDerivedAnxiety * emotionConfig.anxietyEnlargeFactor;
        currentWorkEnlargeFactorG = 1.0f + currentWorkDerivedConfidence * emotionConfig.confidenceEnlargeFactor;
     }

    // (以降のメソッドは、上記の修正以外に変更はありません)
    #region APIからのデータ受信とモデル生成
    public void SetParametersFromJson(string jsonData)
    {
        bool validDataReceived = false;
        try
        {
            SentimentAnalysisResponse response = JsonUtility.FromJson<SentimentAnalysisResponse>(jsonData);
            if (response?.sentiment_analysis?.segments != null && response.sentiment_analysis.segments.Count > 0)
            {
                foreach (SentimentSegment seg in response.sentiment_analysis.segments)
                {
                    currentWorkEnergy += seg.energy; currentWorkContent += seg.content; currentWorkUpset += seg.upset;
                    currentWorkAggression += seg.aggression; currentWorkStress += seg.stress; currentWorkUncertainty += seg.uncertainty;
                    currentWorkExcitement += seg.excitement; currentWorkConcentration += seg.concentration; currentWorkEmoCog += seg.emo_cog;
                    currentWorkHesitation += seg.hesitation; currentWorkBrainPower += seg.brain_power; currentWorkEmbarrassment += seg.embarrassment;
                    currentWorkIntensiveThinking += seg.intensive_thinking; currentWorkImaginationActivity += seg.imagination_activity; currentWorkExtremeEmotion += seg.extreme_emotion;
                    currentWorkPassionate += seg.passionate; currentWorkAtmosphere += seg.atmosphere; currentWorkAnticipation += seg.anticipation;
                    currentWorkDissatisfaction += seg.dissatisfaction; currentWorkConfidence += seg.confidence;
                }
                
                CalculateDerivedEmotions();
                MapEmotionsToModelParameters();
                validDataReceived = true;
            }
            else
            {
                Debug.LogWarning("API response did not contain valid sentiment analysis data.");
                return;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in SetParametersFromJson: {e.Message}\n{e.StackTrace}");
            return;
        }

        if (validDataReceived)
        {
            if (currentMainObject == null) GenerateRandomMainObject();
            
            ScaleUpModelsBasedOnCurrentWorkParams();
            GenerateAllModels(); 
            
            SaveCurrentWorkStateToFile();
            
            if (mainUIManager != null) mainUIManager.UpdateWorkSentimentUI(CreateSceneDataFromCurrentWork());
        }
    }
    #endregion
    #region オブジェクト生成・操作
    void GenerateAllModels()
    {
        if (currentMainObject == null) return;
        AttachModelA(modelAPrefabs, ref currentWorkParameterA, attachedModelsA);
        AttachModel(modelBPrefabs, ref currentWorkParameterB, attachedModelsB, true);
        AttachModel(modelCPrefabs, ref currentWorkParameterC, attachedModelsC, true);
        AttachModel(modelDPrefabs, ref currentWorkParameterD, attachedModelsD, true);
        AttachModel(modelEPrefabs, ref currentWorkParameterE, attachedModelsE, true);
        AttachModel(modelFPrefabs, ref currentWorkParameterF, attachedModelsF, true);
        AttachModel(modelGPrefabs, ref currentWorkParameterG, attachedModelsG, true);
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
    
    void ScaleUpModelsBasedOnCurrentWorkParams()
    {
        ScaleModelList(attachedModelsA, currentWorkEnlargeFactorA);
        ScaleModelList(attachedModelsB, currentWorkEnlargeFactorB);
        ScaleModelList(attachedModelsC, currentWorkEnlargeFactorC);
        ScaleModelList(attachedModelsD, currentWorkEnlargeFactorD);
        ScaleModelList(attachedModelsE, currentWorkEnlargeFactorE);
        ScaleModelList(attachedModelsF, currentWorkEnlargeFactorF);
        ScaleModelList(attachedModelsG, currentWorkEnlargeFactorG);
    }

    void ScaleModelList(List<GameObject> modelList, float scaleFactor)
    {
        foreach (GameObject model in modelList)
        {
            if (model != null) model.transform.localScale *= scaleFactor;
        }
    }

    void RotateMainObject()
    {
        if (currentMainObject != null)
        {
            currentMainObject.transform.Rotate(Vector3.up, 6f * Time.deltaTime, Space.World);
        }
    }
    #endregion
    #region データ管理（保存、ロード、リセット）
    void SaveCurrentWorkStateToFile()
    {
        if (currentMainObject == null) return;

        string filePath = Path.Combine(Application.persistentDataPath, $"{CURRENT_WORK_FILE_NAME}{SAVE_FILE_EXTENSION}");
        SceneData sceneDataToSave = CreateSceneDataFromCurrentWork();
        string jsonData = JsonUtility.ToJson(sceneDataToSave, true);

        try
        {
            File.WriteAllText(filePath, jsonData);
            string thumbnailPath = Path.Combine(Application.persistentDataPath, $"{THUMBNAIL_CURRENT_WORK_NAME}session{THUMBNAIL_FILE_EXTENSION}");
            ThumbnailGenerator.CaptureAndSaveThumbnail(currentMainObject, thumbnailPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save current work state: {e.Message}");
        }
    }
    
    bool LoadCurrentWorkStateFromFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, $"{CURRENT_WORK_FILE_NAME}{SAVE_FILE_EXTENSION}");
        return LoadStateFromPath(filePath);
    }
    
    bool LoadWorkStateFromSlotFile(int slotIndex)
    {
        string filePath = Path.Combine(Application.persistentDataPath, $"{SLOT_FILE_BASE_NAME}{slotIndex}{SAVE_FILE_EXTENSION}");
        return LoadStateFromPath(filePath);
    }

    private bool LoadStateFromPath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.Log($"Save file not found: {filePath}");
            return false;
        }
        try
        {
            string jsonData = File.ReadAllText(filePath);
            SceneData loadedSceneData = JsonUtility.FromJson<SceneData>(jsonData);
            if (loadedSceneData == null)
            {
                Debug.LogError("Failed to parse JSON data.");
                return false;
            }

            ResetAllCurrentWorkData();
            PopulateCurrentWorkStateFromSceneData(loadedSceneData);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load state from file ({filePath}): {e.Message}");
            return false;
        }
    }

    void PopulateCurrentWorkStateFromSceneData(SceneData data)
    {
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

        bool mainObjectRestored = false;
        if (data.objects != null)
        {
            foreach (SavedObjectData objData in data.objects)
            {
                if (objData.type == "main" && data.mainObjectIndex >= 0 && data.mainObjectIndex < glassPrefabs.Length && glassPrefabs[data.mainObjectIndex] != null)
                {
                    currentMainObject = Instantiate(glassPrefabs[data.mainObjectIndex], objData.position, objData.rotation);
                    currentMainObject.transform.localScale = objData.scale;
                    mainObjectRestored = true;
                }
            }

            if(mainObjectRestored)
            {
                foreach (SavedObjectData objData in data.objects)
                {
                    if (objData.type != "main")
                    {
                        GameObject[] prefabArray = GetPrefabArrayByType(objData.type);
                        List<GameObject> modelList = GetModelListByType(objData.type);
                        if (prefabArray != null && objData.prefabIndex >= 0 && objData.prefabIndex < prefabArray.Length && prefabArray[objData.prefabIndex] != null)
                        {
                            GameObject newModel = Instantiate(prefabArray[objData.prefabIndex], objData.position, objData.rotation);
                            newModel.transform.localScale = objData.scale;
                            newModel.transform.SetParent(currentMainObject.transform);
                            if (modelList != null) modelList.Add(newModel);
                        }
                    }
                }
            }
        }

        if (!mainObjectRestored)
        {
             Debug.LogWarning("Failed to restore main object from save data. A new random object will be generated.");
             GenerateRandomMainObject(); 
        }
    }
    
    SceneData CreateSceneDataFromCurrentWork()
    {
        SceneData sceneData = new SceneData
        {
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

        if (currentMainObject != null)
        {
            SavedObjectData mainObjectData = new SavedObjectData
            {
                position = currentMainObject.transform.position, rotation = currentMainObject.transform.rotation,
                scale = currentMainObject.transform.localScale, type = "main", prefabIndex = -1
            };
            for (int i = 0; i < glassPrefabs.Length; i++)
            {
                if (glassPrefabs[i] != null && currentMainObject.name.StartsWith(glassPrefabs[i].name))
                {
                    mainObjectData.prefabName = glassPrefabs[i].name;
                    mainObjectData.prefabIndex = i;
                    sceneData.mainObjectIndex = i;
                    break;
                }
            }
            if (mainObjectData.prefabIndex == -1 && glassPrefabs.Length > 0) Debug.LogWarning($"Could not identify prefab name for the main object: {currentMainObject.name}");
            sceneData.objects.Add(mainObjectData);

            SaveModelList(attachedModelsA, "A", modelAPrefabs, sceneData.objects);
            SaveModelList(attachedModelsB, "B", modelBPrefabs, sceneData.objects);
            SaveModelList(attachedModelsC, "C", modelCPrefabs, sceneData.objects);
            SaveModelList(attachedModelsD, "D", modelDPrefabs, sceneData.objects);
            SaveModelList(attachedModelsE, "E", modelEPrefabs, sceneData.objects);
            SaveModelList(attachedModelsF, "F", modelFPrefabs, sceneData.objects);
            SaveModelList(attachedModelsG, "G", modelGPrefabs, sceneData.objects);
        }
        else
        {
             sceneData.mainObjectIndex = -1;
        }
        return sceneData;
    }

    void SaveModelList(List<GameObject> modelList, string type, GameObject[] prefabs, List<SavedObjectData> objectsList)
    {
        foreach (GameObject model in modelList)
        {
            if (model == null) continue;
            SavedObjectData modelData = new SavedObjectData
            {
                position = model.transform.position, rotation = model.transform.rotation,
                scale = model.transform.localScale, type = type, prefabIndex = -1
            };
            if (prefabs != null)
            {
                for (int i = 0; i < prefabs.Length; i++)
                {
                    if (prefabs[i] != null && model.name.StartsWith(prefabs[i].name))
                    {
                        modelData.prefabName = prefabs[i].name;
                        modelData.prefabIndex = i;
                        break;
                    }
                }
            }
            if (modelData.prefabIndex == -1 && prefabs != null && prefabs.Length > 0) Debug.LogWarning($"Could not identify prefab name for model '{type}': {model.name}");
            objectsList.Add(modelData);
        }
    }

    private void ResetAllCurrentWorkData()
    {
        DeleteAllCurrentModels();
        if (currentMainObject != null)
        {
            Destroy(currentMainObject);
            currentMainObject = null;
        }
        
        currentWorkEnergy = 0; currentWorkContent = 0; currentWorkUpset = 0; currentWorkAggression = 0; currentWorkStress = 0;
        currentWorkUncertainty = 0; currentWorkExcitement = 0; currentWorkConcentration = 0; currentWorkEmoCog = 0;
        currentWorkHesitation = 0; currentWorkBrainPower = 0; currentWorkEmbarrassment = 0; currentWorkIntensiveThinking = 0;
        currentWorkImaginationActivity = 0; currentWorkExtremeEmotion = 0; currentWorkPassionate = 0; currentWorkAtmosphere = 0;
        currentWorkAnticipation = 0; currentWorkDissatisfaction = 0; currentWorkConfidence = 0;
        
        SetDefaultModelParametersToCurrentWork();
    }
    
    private void DeleteAllCurrentModels()
    {
        DeleteModelList(attachedModelsA); DeleteModelList(attachedModelsB); DeleteModelList(attachedModelsC);
        DeleteModelList(attachedModelsD); DeleteModelList(attachedModelsE); DeleteModelList(attachedModelsF); DeleteModelList(attachedModelsG);
    }

    void DeleteModelList(List<GameObject> modelList)
    {
        foreach (GameObject model in modelList)
        {
            if (model != null) Destroy(model);
        }
        modelList.Clear();
    }

    void GenerateRandomMainObject()
    {
        if (currentMainObject != null)
        {
            Destroy(currentMainObject);
            DeleteAllCurrentModels();
        }
        List<GameObject> validPrefabs = glassPrefabs.Where(p => p != null).ToList();
        if (validPrefabs.Count == 0)
        {
            Debug.LogError("No valid glassPrefabs are assigned.");
            return;
        }

        GameObject prefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
        currentMainObject = Instantiate(prefab, new Vector3(0, 1, 0), Quaternion.identity);

        SetDefaultModelParametersToCurrentWork();
    }
    
    void SetDefaultModelParametersToCurrentWork()
    {
        currentWorkDerivedJoy = 0.1f; currentWorkDerivedAnger = 0f; currentWorkDerivedSadness = 0.1f; currentWorkDerivedEnjoyment = 0.1f;
        currentWorkDerivedFocus = 0.1f; currentWorkDerivedAnxiety = 0f; currentWorkDerivedConfidence = 0.1f;
        MapEmotionsToModelParameters();
    }
    #endregion
    #region ヘルパーメソッド
    GameObject RandomModel(GameObject[] modelPrefabs)
    {
        if (modelPrefabs == null || modelPrefabs.Length == 0) return null;
        List<GameObject> validPrefabs = modelPrefabs.Where(p => p != null).ToList();
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
        switch (type)
        {
            case "A": return modelAPrefabs; case "B": return modelBPrefabs; case "C": return modelCPrefabs;
            case "D": return modelDPrefabs; case "E": return modelEPrefabs; case "F": return modelFPrefabs;
            case "G": return modelGPrefabs; default: return null;
        }
    }

    List<GameObject> GetModelListByType(string type)
    {
        switch (type)
        {
            case "A": return attachedModelsA; case "B": return attachedModelsB; case "C": return attachedModelsC;
            case "D": return attachedModelsD; case "E": return attachedModelsE; case "F": return attachedModelsF;
            case "G": return attachedModelsG; default: return null;
        }
    }
    #endregion
}