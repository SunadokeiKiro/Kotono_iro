using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

// オブジェクト情報を保存するためのクラス
[System.Serializable]
public class SavedObjectData
{
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public string type; // "main", "A", "B", "C", ... など
    public int prefabIndex;
}

[System.Serializable]
public class SceneData
{
    public List<SavedObjectData> objects = new List<SavedObjectData>();
    public float parameterA;
    public float parameterB;
    public float parameterC;
    public float parameterD;
    public float parameterE;
    public float parameterF;
    public float parameterG;
    public float enlargeFactorA;
    public float enlargeFactorB;
    public float enlargeFactorC;
    public float enlargeFactorD;
    public float enlargeFactorE;
    public float enlargeFactorF;
    public float enlargeFactorG;
    public float generationThreshold;
    public int mainObjectIndex = -1; // メインオブジェクトのインデックス（-1は未選択）
    public float totalEnergy;
    public float totalContent;
    public float totalUpset;
    public float totalAggression;
    public float totalStress;
    public float totalUncertainty;
    public float totalExcitement;
    public float totalConcentration;
    public float totalEmoCog;
    public float totalHesitation;
    public float totalBrainPower;
    public float totalEmbarrassment;
    public float totalIntensiveThinking;
    public float totalImaginationActivity;
    public float totalExtremeEmotion;
    public float totalPassionate;
    public float totalAtmosphere;
    public float totalAnticipation;
    public float totalDissatisfaction;
    public float totalConfidence;
}

// 感情データ表示用 UI Text
[Header("感情データ表示用 UI")]
public Text textEnergy;        // Energy
public Text textContent;       // Content
public Text textUpset;         // Upset
public Text textAggression;    // Aggression
public Text textStress;        // Stress
public Text textUncertainty;   // Uncertainty
public Text textExcitement;    // Excitement 
public Text textConcentration; // Concentration
public Text textEmoCog;        // Emo Cog
public Text textHesitation;    // Hesitation
public Text textBrainPower;    // Brain Power
public Text textEmbarrassment; // Embarrassment
public Text textIntensiveThinking;  // Intensive Thinking
public Text textImaginationActivity; // Imagination Activity
public Text textExtremeEmotion;  // Extreme Emotion
public Text textPassionate;    // Passionate
public Text textAtmosphere;    // Atmosphere
public Text textAnticipation;  // Anticipation
public Text textDissatisfaction; // Dissatisfaction
public Text textConfidence;    // Confidence

[Header("感情データの累積値")]
private float totalEnergy = 0f;
private float totalContent = 0f;
private float totalUpset = 0f;
private float totalAggression = 0f;
private float totalStress = 0f;
private float totalUncertainty = 0f;
private float totalExcitement = 0f;
private float totalConcentration = 0f;
private float totalEmoCog = 0f;
private float totalHesitation = 0f;
private float totalBrainPower = 0f;
private float totalEmbarrassment = 0f;
private float totalIntensiveThinking = 0f;
private float totalImaginationActivity = 0f;
private float totalExtremeEmotion = 0f;
private float totalPassionate = 0f;
private float totalAtmosphere = 0f;
private float totalAnticipation = 0f;
private float totalDissatisfaction = 0f;
private float totalConfidence = 0f;

public class GameController : MonoBehaviour
{
    // ファイルパスの定数
    private string saveFilePath;
    private const string saveFileName = "glass_art_save.json";

    // ボタン追加
    public Button buttonSave;
    public Button buttonLoad;

    // ── メインオブジェクト用Prefab配列 ──
    public GameObject[] glassPrefabs;

    // ── 各モデル用Prefab配列 ──
    public GameObject[] modelAPrefabs;
    public GameObject[] modelBPrefabs;
    public GameObject[] modelCPrefabs;
    public GameObject[] modelDPrefabs;
    public GameObject[] modelEPrefabs;
    public GameObject[] modelFPrefabs;
    public GameObject[] modelGPrefabs;
    // 必要ならモデルH用も追加

    // ── 制御用パラメーター（初期値） ──
    [Header("各モデル生成パラメーター (初期値)")]
    public float parameterA = 0.0f;
    public float parameterB = 0.0f;
    public float parameterC = 0.0f;
    public float parameterD = 0.0f;
    public float parameterE = 0.0f;
    public float parameterF = 0.0f;
    public float parameterG = 0.0f;

    [Header("各パラメーター総計")]
    public float totalA = 0.0f;
    public float totalB = 0.0f;
    public float totalC = 0.0f;
    public float totalD = 0.0f;
    public float totalE = 0.0f;
    public float totalF = 0.0f;
    public float totalG = 0.0f;


    [Header("生成半径（候補点取得時の閾値）")]
    public float generationThreshold = 0.3f;

    [Header("各モデルの拡大倍率")]
    public float enlargeFactorA = 0.0f;  // モデルA用
    public float enlargeFactorB = 0.0f;  // モデルB用
    public float enlargeFactorC = 0.0f;  // モデルC用
    public float enlargeFactorD = 0.0f;  // モデルD用
    public float enlargeFactorE = 0.0f;  // モデルE用
    public float enlargeFactorF = 0.0f;  // モデルF用
    public float enlargeFactorG = 0.0f;  // モデルG用

    // ── 各モデル生成後の管理リスト ──
    private List<GameObject> attachedModelsA = new List<GameObject>();
    private List<GameObject> attachedModelsB = new List<GameObject>();
    private List<GameObject> attachedModelsC = new List<GameObject>();
    private List<GameObject> attachedModelsD = new List<GameObject>();
    private List<GameObject> attachedModelsE = new List<GameObject>();
    private List<GameObject> attachedModelsF = new List<GameObject>();
    private List<GameObject> attachedModelsG = new List<GameObject>();

    // ── その他 ──
    public Camera mainCamera;
    public Button button1;
    public Button buttonA;
    public Button buttonB;
    public Button buttonC;
    public Button buttonD;
    public Button buttonE;
    public Button buttonF;
    public Button buttonG;
    public Button buttonAll;
    public Button button2;
    public Button button3;
    //public Button buttonGallery;
    //public Button buttonSettings;

    private GameObject currentMainObject;

    void Start()
    {
        button1.onClick.AddListener(GenerateRandomMainObject);
        buttonA.onClick.AddListener(() => AttachModelA(modelAPrefabs, ref parameterA, attachedModelsA));
        buttonB.onClick.AddListener(() => AttachModel(modelBPrefabs, ref parameterB, attachedModelsB, true));
        buttonC.onClick.AddListener(() => AttachModel(modelCPrefabs, ref parameterC, attachedModelsC, true));
        buttonD.onClick.AddListener(() => AttachModel(modelDPrefabs, ref parameterD, attachedModelsD, true));
        buttonE.onClick.AddListener(() => AttachModel(modelEPrefabs, ref parameterE, attachedModelsE, true));
        buttonF.onClick.AddListener(() => AttachModel(modelFPrefabs, ref parameterF, attachedModelsF, true));
        buttonG.onClick.AddListener(() => AttachModel(modelGPrefabs, ref parameterG, attachedModelsG, true));
        buttonAll.onClick.AddListener(GenerateAllModels);

        button2.onClick.AddListener(ScaleUpModels);
        button3.onClick.AddListener(DeleteAllObjects);

        //buttonGallery.onClick.AddListener(MoveToGallery);
        //buttonSettings.onClick.AddListener(MoveToSettings);

        // ボタンの参照チェック
        Debug.Log($"Save Button is null: {buttonSave == null}, Load Button is null: {buttonLoad == null}");

        // 新規ボタンのリスナー追加
        if (buttonSave != null) buttonSave.onClick.AddListener(SaveScene);
        if (buttonLoad != null) buttonLoad.onClick.AddListener(LoadScene);

        // 保存ファイルパスの設定
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        Debug.Log($"保存ファイルパス: {saveFilePath}");

        // Start() メソッド内で保存ファイルパスを設定した後に追加
        if (File.Exists(saveFilePath))
        {
            Debug.Log($"保存ファイルが見つかりました: {saveFilePath}");
            string fileContent = File.ReadAllText(saveFilePath);
            Debug.Log($"ファイル内容の一部: {fileContent.Substring(0, System.Math.Min(100, fileContent.Length))}");
        }
        else
        {
            Debug.Log($"保存ファイルが見つかりません: {saveFilePath}");
        }

        if (File.Exists(saveFilePath))
        {
            LoadScene();
        }
    }

    // シーンを保存するメソッド
    public void SaveScene()
    {
        if (currentMainObject == null)
        {
            Debug.LogWarning("保存するメインオブジェクトがありません！");
            return;
        }

        SceneData sceneData = new SceneData();

        // パラメータを保存
        sceneData.parameterA = parameterA;
        sceneData.parameterB = parameterB;
        sceneData.parameterC = parameterC;
        sceneData.parameterD = parameterD;
        sceneData.parameterE = parameterE;
        sceneData.parameterF = parameterF;
        sceneData.parameterG = parameterG;
        sceneData.totalA = totalA;
        sceneData.totalB = totalB;
        sceneData.totalC = totalC;
        sceneData.totalD = totalD;
        sceneData.totalE = totalE;
        sceneData.totalF = totalF;
        sceneData.totalG = totalG;
        sceneData.enlargeFactorA = enlargeFactorA;
        sceneData.enlargeFactorB = enlargeFactorB;
        sceneData.enlargeFactorC = enlargeFactorC;
        sceneData.enlargeFactorD = enlargeFactorD;
        sceneData.enlargeFactorE = enlargeFactorE;
        sceneData.enlargeFactorF = enlargeFactorF;
        sceneData.enlargeFactorG = enlargeFactorG;
        sceneData.generationThreshold = generationThreshold;

        // 感情データの累積値を保存
        sceneData.totalEnergy = totalEnergy;
        sceneData.totalContent = totalContent;
        sceneData.totalUpset = totalUpset;
        sceneData.totalAggression = totalAggression;
        sceneData.totalStress = totalStress;
        sceneData.totalUncertainty = totalUncertainty;
        sceneData.totalExcitement = totalExcitement;
        sceneData.totalConcentration = totalConcentration;
        sceneData.totalEmoCog = totalEmoCog;
        sceneData.totalHesitation = totalHesitation;
        sceneData.totalBrainPower = totalBrainPower;
        sceneData.totalEmbarrassment = totalEmbarrassment;
        sceneData.totalIntensiveThinking = totalIntensiveThinking;
        sceneData.totalImaginationActivity = totalImaginationActivity;
        sceneData.totalExtremeEmotion = totalExtremeEmotion;
        sceneData.totalPassionate = totalPassionate;
        sceneData.totalAtmosphere = totalAtmosphere;
        sceneData.totalAnticipation = totalAnticipation;
        sceneData.totalDissatisfaction = totalDissatisfaction;
        sceneData.totalConfidence = totalConfidence;

        // メインオブジェクトの情報を保存
        SavedObjectData mainObjectData = new SavedObjectData
        {
            position = currentMainObject.transform.position,
            rotation = currentMainObject.transform.rotation,
            scale = currentMainObject.transform.localScale,
            type = "main"
        };

        // メインオブジェクトのプレハブ名とインデックスを特定
        for (int i = 0; i < glassPrefabs.Length; i++)
        {
            if (currentMainObject.name.Contains(glassPrefabs[i].name))
            {
                mainObjectData.prefabName = glassPrefabs[i].name;
                mainObjectData.prefabIndex = i;
                sceneData.mainObjectIndex = i;
                break;
            }
        }

        sceneData.objects.Add(mainObjectData);

        // 各モデルの情報を保存
        SaveModelList(attachedModelsA, "A", modelAPrefabs, sceneData.objects);
        SaveModelList(attachedModelsB, "B", modelBPrefabs, sceneData.objects);
        SaveModelList(attachedModelsC, "C", modelCPrefabs, sceneData.objects);
        SaveModelList(attachedModelsD, "D", modelDPrefabs, sceneData.objects);
        SaveModelList(attachedModelsE, "E", modelEPrefabs, sceneData.objects);
        SaveModelList(attachedModelsF, "F", modelFPrefabs, sceneData.objects);
        SaveModelList(attachedModelsG, "G", modelGPrefabs, sceneData.objects);

        // データをJSONに変換
        string jsonData = JsonUtility.ToJson(sceneData, true);

        // ファイルに保存
        File.WriteAllText(saveFilePath, jsonData);
        Debug.Log($"保存したJSONデータ: {jsonData}");

        Debug.Log($"シーンを保存しました: {saveFilePath}");
    }

    // モデルリストの情報を保存するヘルパーメソッド
    void SaveModelList(List<GameObject> modelList, string type, GameObject[] prefabs, List<SavedObjectData> objectsList)
    {
        foreach (GameObject model in modelList)
        {
            if (model == null) continue;

            SavedObjectData modelData = new SavedObjectData
            {
                position = model.transform.position,
                rotation = model.transform.rotation,
                scale = model.transform.localScale,
                type = type
            };

            // プレハブ名とインデックスを特定
            for (int i = 0; i < prefabs.Length; i++)
            {
                if (model.name.Contains(prefabs[i].name))
                {
                    modelData.prefabName = prefabs[i].name;
                    modelData.prefabIndex = i;
                    break;
                }
            }

            objectsList.Add(modelData);
        }
    }

    // 保存されたシーンを読み込むメソッド
    public void LoadScene()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning($"保存ファイルが見つかりません: {saveFilePath}");
            return;
        }

        // 現在のオブジェクトをすべて削除
        DeleteAllObjects();

        // ファイルからJSONを読み込み
        string jsonData = File.ReadAllText(saveFilePath);
        SceneData sceneData = JsonUtility.FromJson<SceneData>(jsonData);

        if (sceneData == null)
        {
            Debug.LogError("保存データの読み込みに失敗しました");
            return;
        }

        // パラメータを復元
        parameterA = sceneData.parameterA;
        parameterB = sceneData.parameterB;
        parameterC = sceneData.parameterC;
        parameterD = sceneData.parameterD;
        parameterE = sceneData.parameterE;
        parameterF = sceneData.parameterF;
        parameterG = sceneData.parameterG;
        enlargeFactorA = sceneData.enlargeFactorA;
        enlargeFactorB = sceneData.enlargeFactorB;
        enlargeFactorC = sceneData.enlargeFactorC;
        enlargeFactorD = sceneData.enlargeFactorD;
        enlargeFactorE = sceneData.enlargeFactorE;
        enlargeFactorF = sceneData.enlargeFactorF;
        enlargeFactorG = sceneData.enlargeFactorG;
        generationThreshold = sceneData.generationThreshold;

        // 感情データの累積値を復元
        totalEnergy = sceneData.totalEnergy;
        totalContent = sceneData.totalContent;
        totalUpset = sceneData.totalUpset;
        totalAggression = sceneData.totalAggression;
        totalStress = sceneData.totalStress;
        totalUncertainty = sceneData.totalUncertainty;
        totalExcitement = sceneData.totalExcitement;
        totalConcentration = sceneData.totalConcentration;
        totalEmoCog = sceneData.totalEmoCog;
        totalHesitation = sceneData.totalHesitation;
        totalBrainPower = sceneData.totalBrainPower;
        totalEmbarrassment = sceneData.totalEmbarrassment;
        totalIntensiveThinking = sceneData.totalIntensiveThinking;
        totalImaginationActivity = sceneData.totalImaginationActivity;
        totalExtremeEmotion = sceneData.totalExtremeEmotion;
        totalPassionate = sceneData.totalPassionate;
        totalAtmosphere = sceneData.totalAtmosphere;
        totalAnticipation = sceneData.totalAnticipation;
        totalDissatisfaction = sceneData.totalDissatisfaction;
        totalConfidence = sceneData.totalConfidence;

        // UI表示を更新
        UpdateSentimentUI();

        // オブジェクトを復元
        foreach (SavedObjectData objData in sceneData.objects)
        {
            if (objData.type == "main" && sceneData.mainObjectIndex >= 0 && sceneData.mainObjectIndex < glassPrefabs.Length)
            {
                // メインオブジェクトを復元
                currentMainObject = Instantiate(
                    glassPrefabs[sceneData.mainObjectIndex],
                    objData.position,
                    objData.rotation
                );
                currentMainObject.transform.localScale = objData.scale;
            }
            else
            {
                // 各モデルを復元
                GameObject[] prefabArray = GetPrefabArrayByType(objData.type);
                List<GameObject> modelList = GetModelListByType(objData.type);

                if (prefabArray != null && objData.prefabIndex >= 0 && objData.prefabIndex < prefabArray.Length)
                {
                    GameObject newModel = Instantiate(
                        prefabArray[objData.prefabIndex],
                        objData.position,
                        objData.rotation
                    );
                    newModel.transform.localScale = objData.scale;

                    if (currentMainObject != null)
                    {
                        newModel.transform.SetParent(currentMainObject.transform);
                    }

                    if (modelList != null)
                    {
                        modelList.Add(newModel);
                    }
                }
            }
        }

        Debug.Log("シーンを読み込みました");
    }

    // タイプに基づいてプレハブ配列を取得するヘルパーメソッド
    GameObject[] GetPrefabArrayByType(string type)
    {
        switch (type)
        {
            case "A": return modelAPrefabs;
            case "B": return modelBPrefabs;
            case "C": return modelCPrefabs;
            case "D": return modelDPrefabs;
            case "E": return modelEPrefabs;
            case "F": return modelFPrefabs;
            case "G": return modelGPrefabs;
            default: return null;
        }
    }

    // タイプに基づいてモデルリストを取得するヘルパーメソッド
    List<GameObject> GetModelListByType(string type)
    {
        switch (type)
        {
            case "A": return attachedModelsA;
            case "B": return attachedModelsB;
            case "C": return attachedModelsC;
            case "D": return attachedModelsD;
            case "E": return attachedModelsE;
            case "F": return attachedModelsF;
            case "G": return attachedModelsG;
            default: return null;
        }
    }


    // このメソッドを感情データ受信後に呼び出す
    public void SetParametersFromJson(string jsonData)
    {
        bool validDataReceived = false;

        try
        {
            // SentimentAnalysisResponse は、前述のJSONパース用クラス
            SentimentAnalysisResponse response = JsonUtility.FromJson<SentimentAnalysisResponse>(jsonData);
            if (response != null && response.sentiment_analysis != null && response.sentiment_analysis.segments != null && response.sentiment_analysis.segments.Count > 0)
            {
                SentimentSegment seg = response.sentiment_analysis.segments[0];

                // int型からfloat型へ変換
                float energy = (float)seg.energy;
                float dissatisfaction = (float)seg.dissatisfaction;
                float excitement = (float)seg.excitement;
                float anticipation = (float)seg.anticipation;
                float hesitation = (float)seg.hesitation;
                float atmosphere = (float)seg.atmosphere;

                // 感情データの累積値に加算
                totalEnergy += energy;
                totalContent += (float)seg.content;
                totalUpset += (float)seg.upset;
                totalAggression += (float)seg.aggression;
                totalStress += (float)seg.stress;
                totalUncertainty += (float)seg.uncertainty;
                totalExcitement += excitement;
                totalConcentration += (float)seg.concentration;
                totalEmoCog += (float)seg.emo_cog;
                totalHesitation += hesitation;
                totalBrainPower += (float)seg.brain_power;
                totalEmbarrassment += (float)seg.embarrassment;
                totalIntensiveThinking += (float)seg.intensive_thinking;
                totalImaginationActivity += (float)seg.imagination_activity;
                totalExtremeEmotion += (float)seg.extreme_emotion;
                totalPassionate += (float)seg.passionate;
                totalAtmosphere += atmosphere;
                totalAnticipation += anticipation;
                totalDissatisfaction += dissatisfaction;
                totalConfidence += (float)seg.confidence;

                // UI表示を更新
                UpdateSentimentUI();

                energy = energy / 2;
                dissatisfaction = dissatisfaction / 5.0f;

                if (excitement > 15.0f)
                {
                    excitement = (excitement - 15.0f) / 2.5f;
                }
                else
                {
                    excitement = 0.0f;
                }

                anticipation = anticipation / 13.3f;

                if (hesitation > 15.0f)
                {
                    hesitation = (hesitation - 15.0f) / 2.5f;
                }
                else
                {
                    hesitation = 0.0f;
                }

                if (atmosphere != 0.0f)
                {
                    if (atmosphere >= 1.0f)
                    {
                        if (atmosphere > 10.0f)
                        {
                            atmosphere = 10.0f;
                        }
                        excitement = excitement * (1.0f + atmosphere * 0.1f);
                        anticipation = anticipation * (1.0f + anticipation * 0.1f);
                    }
                    else
                    {
                        atmosphere = atmosphere * -1.0f;
                        if (atmosphere > 10.0f)
                        {
                            atmosphere = 10.0f;
                        }
                        dissatisfaction = dissatisfaction * (1.0f + dissatisfaction * 0.1f);
                        hesitation = hesitation * (1.0f + hesitation * 0.1f);
                    }
                }

                // 任意の感情パラメータとGameControllerのパラメータをマッピング
                // ストレスの値とか、高かったら生成半径を狭めてもいいかも
                parameterA = energy;
                parameterB = 0.0f;
                parameterC = dissatisfaction;
                parameterD = excitement;
                parameterE = anticipation;
                parameterF = 0.0f;
                parameterG = hesitation;

                enlargeFactorA = 1.0f + energy / 300.0f;
                enlargeFactorB = 1.0f + 0.0f;
                enlargeFactorC = 1.0f + dissatisfaction / 100.0f;
                enlargeFactorD = 1.0f + excitement / 100.0f;
                enlargeFactorE = 1.0f + anticipation / 100.0f;
                enlargeFactorF = 1.0f + 0.0f;
                enlargeFactorG = 1.0f + hesitation / 100.0f;

                Debug.Log("パラメータを更新しました");
                validDataReceived = true;
            }
            else
            {
                Debug.LogWarning("感情データが空または不正な形式です。モデルは生成されません。");
                // データが不正の場合はモデルを生成しない
                return;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"感情データのパースに失敗しました: {e.Message}");
            // 例外発生時はモデルを生成しない
            return;
        }

        // メインオブジェクトが存在しない場合は生成
        if (currentMainObject == null)
        {
            GenerateRandomMainObject();
        }

        // 有効なデータを受信した場合のみ、自動的にモデルを生成
        if (validDataReceived)
        {
            GenerateAllModels();
            ScaleUpModels();
        }
    }

    // デフォルトパラメータを設定するヘルパーメソッド
    private void SetDefaultParameters()
    {
        // デフォルトの感情パラメータ値を設定
        parameterA = 1.0f;  // energy
        parameterB = 0.0f;
        parameterC = 0.5f;  // dissatisfaction
        parameterD = 0.5f;  // excitement
        parameterE = 0.5f;  // anticipation
        parameterF = 0.0f;
        parameterG = 0.5f;  // hesitation

        // デフォルトの拡大倍率を設定
        enlargeFactorA = 1.0f;
        enlargeFactorB = 1.0f;
        enlargeFactorC = 1.0f;
        enlargeFactorD = 1.0f;
        enlargeFactorE = 1.0f;
        enlargeFactorF = 1.0f;
        enlargeFactorG = 1.0f;

        Debug.Log("デフォルトパラメータを設定しました");
    }


    void MoveToGallery()
    {
        Debug.Log("ギャラリーボタンが押されました！");
        return;
    }

    void MoveToSettings()
    {
        Debug.Log("セッティングボタンが押されました！");
        return;
    }

    void Update()
    {
        RotateMainObject();
    }

    // ── メインオブジェクト生成 ──
    void GenerateRandomMainObject()
    {
        if (currentMainObject != null)
        {
            Debug.LogWarning("既にメインオブジェクトが存在します！");
            return;
        }
        if (glassPrefabs.Length == 0)
        {
            Debug.LogError("glassPrefabs が設定されていません！");
            return;
        }
        int randomIndex = Random.Range(0, glassPrefabs.Length);
        GameObject selectedPrefab = glassPrefabs[randomIndex];
        currentMainObject = Instantiate(selectedPrefab, new Vector3(0, 1, 0), Quaternion.identity);
        Debug.Log($"メインオブジェクト {selectedPrefab.name} を生成しました！");
        SaveScene();
    }

    // ── AttachModelA：ボタンA専用 ──
    // 毎回メインオブジェクト表面からランダムな三角形を選び、新たな座標でモデルAを生成
    void AttachModelA(GameObject[] prefabArray, ref float parameter, List<GameObject> modelList)
    {
        float currentParameter = parameter; // 初期値をローカル変数にコピー

        if (currentMainObject == null)
        {
            Debug.LogError("メインオブジェクトが存在しません！");
            return;
        }
        MeshFilter meshFilter = currentMainObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("MeshFilter またはメッシュが存在しません！");
            return;
        }
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        if (triangles.Length == 0 || vertices.Length == 0)
        {
            Debug.LogError("メッシュの頂点または三角形データが存在しません！");
            return;
        }
        while (currentParameter > 0)
        {
            int triIndex = Random.Range(0, triangles.Length / 3) * 3;
            Vector3 v0 = vertices[triangles[triIndex]];
            Vector3 v1 = vertices[triangles[triIndex + 1]];
            Vector3 v2 = vertices[triangles[triIndex + 2]];
            Vector3 randomPoint = GetRandomPointInTriangle(v0, v1, v2);
            randomPoint = currentMainObject.transform.TransformPoint(randomPoint);

            // 基準：メインオブジェクトの中心 (0,1,0) から randomPoint への直線
            Vector3 center = new Vector3(0, 1, 0);
            Vector3 direction = (randomPoint - center).normalized;
            Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, direction);

            Vector3 triangleCenter = (v0 + v1 + v2) / 3f;
            Vector3 randomNearPoint = Vector3.zero;
            int attempts = 0;
            Quaternion randomRotation = GetRandomRotation(-10f, 10f);
            while (true)
            {
                randomNearPoint = GetRandomPointInTriangle(v0, v1, v2);
                float dist = Vector3.Distance(randomNearPoint, triangleCenter);
                if (dist < generationThreshold)
                    break;
                attempts++;
                if (attempts > 10000)
                    break;
            }
            randomNearPoint = currentMainObject.transform.TransformPoint(randomNearPoint);
            float randomSize = (currentParameter < 1.0f) ? currentParameter : Random.Range(0.1f, 0.5f);
            currentParameter -= randomSize;

            GameObject newModelPrefab = RandomModel(prefabArray);
            GameObject newModel = Instantiate(newModelPrefab, randomNearPoint, baseRotation * randomRotation);
            newModel.transform.localScale *= randomSize;
            newModel.transform.SetParent(currentMainObject.transform);
            modelList.Add(newModel);
        }
        Debug.Log("ボタンA専用処理で、モデルAを生成しました！");
    }

    // ── AttachModel：その他ボタン専用 ──
    // 生成後、生成したモデルと同じ座標・角度で、かつ同時にモデルAも生成する（generateExtraA==trueの場合）
    void AttachModel(GameObject[] prefabArray, ref float parameter, List<GameObject> modelList, bool generateExtraA)
    {
        float currentParameter = parameter; // ローカルコピー
        if (currentMainObject == null)
        {
            Debug.LogError("メインオブジェクトが存在しません！");
            return;
        }
        MeshFilter meshFilter = currentMainObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("MeshFilter またはメッシュが存在しません！");
            return;
        }
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        if (triangles.Length == 0 || vertices.Length == 0)
        {
            Debug.LogError("メッシュの頂点または三角形データが存在しません！");
            return;
        }
        int triIndex = Random.Range(0, triangles.Length / 3) * 3;
        Vector3 v0 = vertices[triangles[triIndex]];
        Vector3 v1 = vertices[triangles[triIndex + 1]];
        Vector3 v2 = vertices[triangles[triIndex + 2]];
        Vector3 randomPoint = GetRandomPointInTriangle(v0, v1, v2);
        randomPoint = currentMainObject.transform.TransformPoint(randomPoint);
        Vector3 center = new Vector3(0, 1, 0);
        Vector3 direction = (randomPoint - center).normalized;
        Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, direction);
        while (currentParameter > 0)
        {
            Vector3 triangleCenter = (v0 + v1 + v2) / 3f;
            Vector3 randomNearPoint = Vector3.zero;
            int attempts = 0;
            Quaternion randomRotation = GetRandomRotation(-15f, 15f);
            while (true)
            {
                randomNearPoint = GetRandomPointInTriangle(v0, v1, v2);
                float dist = Vector3.Distance(randomNearPoint, triangleCenter);
                if (dist < generationThreshold)
                    break;
                attempts++;
                if (attempts > 10000)
                    break;
            }
            randomNearPoint = currentMainObject.transform.TransformPoint(randomNearPoint);
            float randomSize = (currentParameter < 1.0f) ? currentParameter : Random.Range(0.1f, 1.0f);
            currentParameter -= randomSize;
            GameObject newModelPrefab = RandomModel(prefabArray);
            GameObject newModel = Instantiate(newModelPrefab, randomNearPoint, baseRotation * randomRotation);
            newModel.transform.localScale *= randomSize;
            newModel.transform.SetParent(currentMainObject.transform);
            modelList.Add(newModel);
            // 同時に、生成したモデルと同じ座標・角度でモデルAを生成（generateExtraA==true の場合のみ）
            if (generateExtraA)
            {
                GameObject extraAPrefab = RandomModel(modelAPrefabs);
                GameObject extraA = Instantiate(extraAPrefab, randomNearPoint, baseRotation * randomRotation);
                extraA.transform.localScale *= randomSize;
                extraA.transform.SetParent(currentMainObject.transform);
                attachedModelsA.Add(extraA);
            }
        }
        Debug.Log("その他モデルを生成し、各生成時に同じ座標・角度でモデルAも生成しました！");
    }

    // ── GenerateAllModels：ボタンAll専用 ──
    // メインオブジェクトが存在しなければエラー、存在する場合は各モデルの生成処理を一括実行
    void GenerateAllModels()
    {
        if (currentMainObject == null)
        {
            Debug.LogError("メインオブジェクトが存在しません！ボタンAllは実行できません。");
            return;
        }
        AttachModelA(modelAPrefabs, ref parameterA, attachedModelsA);
        AttachModel(modelBPrefabs, ref parameterB, attachedModelsB, true);
        AttachModel(modelCPrefabs, ref parameterC, attachedModelsC, true);
        AttachModel(modelDPrefabs, ref parameterD, attachedModelsD, true);
        AttachModel(modelEPrefabs, ref parameterE, attachedModelsE, true);
        AttachModel(modelFPrefabs, ref parameterF, attachedModelsF, true);
        AttachModel(modelGPrefabs, ref parameterG, attachedModelsG, true);
        Debug.Log("すべてのモデルを一括生成しました！");
        SaveScene();
    }

    // ── ヘルパー関数 ──
    GameObject RandomModel(GameObject[] modelPrefabs)
    {
        if (modelPrefabs.Length == 0)
        {
            Debug.LogError("モデルPrefab配列が空です！");
            return null;
        }
        int index = Random.Range(0, modelPrefabs.Length);
        return modelPrefabs[index];
    }

    Vector3 GetRandomPointInTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        float u = Random.value;
        float v = Random.value;
        if (u + v > 1)
        {
            u = 1 - u;
            v = 1 - v;
        }
        return v0 + u * (v1 - v0) + v * (v2 - v0);
    }

    Quaternion GetRandomRotation(float minAngle, float maxAngle)
    {
        float randomX = Random.Range(minAngle, maxAngle) * (Random.value > 0.5f ? 1 : -1);
        float randomY = Random.Range(minAngle, maxAngle) * (Random.value > 0.5f ? 1 : -1);
        float randomZ = Random.Range(minAngle, maxAngle) * (Random.value > 0.5f ? 1 : -1);
        return Quaternion.Euler(randomX, randomY, randomZ);
    }

    void RotateMainObject()
    {
        if (currentMainObject != null)
        {
            currentMainObject.transform.Rotate(Vector3.up, 6f * Time.deltaTime, Space.World);
        }
    }

    void ScaleUpModels()
    {
        // 各モデルを個別の拡大倍率で拡大
        ScaleModelList(attachedModelsA, enlargeFactorA);
        ScaleModelList(attachedModelsB, enlargeFactorB);
        ScaleModelList(attachedModelsC, enlargeFactorC);
        ScaleModelList(attachedModelsD, enlargeFactorD);
        ScaleModelList(attachedModelsE, enlargeFactorE);
        ScaleModelList(attachedModelsF, enlargeFactorF);
        ScaleModelList(attachedModelsG, enlargeFactorG);

        Debug.Log("すべてのモデルを拡大しました！各モデルごとに個別の拡大倍率が適用されました。");
        SaveScene();
    }

    // 拡大用ヘルパーメソッド
    void ScaleModelList(List<GameObject> modelList, float scaleFactor)
    {
        foreach (GameObject model in modelList)
        {
            if (model != null)
                model.transform.localScale *= scaleFactor;
        }
    }

    void DeleteAllObjects()
    {
        if (currentMainObject != null)
        {
            Destroy(currentMainObject);
            currentMainObject = null;
        }

        // モデルAを含むすべてのモデルを削除
        DeleteModelList(attachedModelsA);
        DeleteModelList(attachedModelsB);
        DeleteModelList(attachedModelsC);
        DeleteModelList(attachedModelsD);
        DeleteModelList(attachedModelsE);
        DeleteModelList(attachedModelsF);
        DeleteModelList(attachedModelsG);

        // 感情データの累積値をリセット
        ResetSentimentTotals();
        UpdateSentimentUI();

        Debug.Log("すべてのオブジェクトを削除し、感情データの累積値をリセットしました！");
    }

    // モデルリストを削除するヘルパーメソッド
    void DeleteModelList(List<GameObject> modelList)
    {
        foreach (GameObject model in modelList)
        {
            if (model != null)
                Destroy(model);
        }
        modelList.Clear();
    }

    // 累積値をリセットするメソッド
    void ResetSentimentTotals()
    {
        totalEnergy = 0f;
        totalContent = 0f;
        totalUpset = 0f;
        totalAggression = 0f;
        totalStress = 0f;
        totalUncertainty = 0f;
        totalExcitement = 0f;
        totalConcentration = 0f;
        totalEmoCog = 0f;
        totalHesitation = 0f;
        totalBrainPower = 0f;
        totalEmbarrassment = 0f;
        totalIntensiveThinking = 0f;
        totalImaginationActivity = 0f;
        totalExtremeEmotion = 0f;
        totalPassionate = 0f;
        totalAtmosphere = 0f;
        totalAnticipation = 0f;
        totalDissatisfaction = 0f;
        totalConfidence = 0f;
    }

    // UI表示を更新するメソッド
    void UpdateSentimentUI()
    {
        // 各UI Textコンポーネントがnullでないことを確認して値を更新
        if (textEnergy != null) textEnergy.text = $"Energy: {totalEnergy:F2}";
        if (textContent != null) textContent.text = $"Content: {totalContent:F2}";
        if (textUpset != null) textUpset.text = $"Upset: {totalUpset:F2}";
        if (textAggression != null) textAggression.text = $"Aggression: {totalAggression:F2}";
        if (textStress != null) textStress.text = $"Stress: {totalStress:F2}";
        if (textUncertainty != null) textUncertainty.text = $"Uncertainty: {totalUncertainty:F2}";
        if (textExcitement != null) textExcitement.text = $"Excitement: {totalExcitement:F2}";
        if (textConcentration != null) textConcentration.text = $"Concentration: {totalConcentration:F2}";
        if (textEmoCog != null) textEmoCog.text = $"Emo Cog: {totalEmoCog:F2}";
        if (textHesitation != null) textHesitation.text = $"Hesitation: {totalHesitation:F2}";
        if (textBrainPower != null) textBrainPower.text = $"Brain Power: {totalBrainPower:F2}";
        if (textEmbarrassment != null) textEmbarrassment.text = $"Embarrassment: {totalEmbarrassment:F2}";
        if (textIntensiveThinking != null) textIntensiveThinking.text = $"Intensive Thinking: {totalIntensiveThinking:F2}";
        if (textImaginationActivity != null) textImaginationActivity.text = $"Imagination Activity: {totalImaginationActivity:F2}";
        if (textExtremeEmotion != null) textExtremeEmotion.text = $"Extreme Emotion: {totalExtremeEmotion:F2}";
        if (textPassionate != null) textPassionate.text = $"Passionate: {totalPassionate:F2}";
        if (textAtmosphere != null) textAtmosphere.text = $"Atmosphere: {totalAtmosphere:F2}";
        if (textAnticipation != null) textAnticipation.text = $"Anticipation: {totalAnticipation:F2}";
        if (textDissatisfaction != null) textDissatisfaction.text = $"Dissatisfaction: {totalDissatisfaction:F2}";
        if (textConfidence != null) textConfidence.text = $"Confidence: {totalConfidence:F2}";
    }
}