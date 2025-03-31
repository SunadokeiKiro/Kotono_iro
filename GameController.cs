using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
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
    public float parameterA = 3.0f;
    public float parameterB = 3.0f;

    public float parameterC = 3.0f;
    public float parameterD = 3.0f;
    public float parameterE = 3.0f;
    public float parameterF = 3.0f;
    public float parameterG = 3.0f;

    [Header("生成半径（候補点取得時の閾値）")]
    public float generationThreshold = 0.3f;

    [Header("拡大倍率")]
    public float enlargeFactor = 1.5f;      // モデルB～G用
    public float enlargeFactorAlt = 1.4f;  // モデルA用

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
    public Button buttonGallery;
    public Button buttonSettings;

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

        buttonGallery.onClick.AddListener(MoveToGallery);
        buttonSettings.onClick.AddListener(MoveToSettings);
    }

    // このメソッドを感情データ受信後に呼び出す
    public void SetParametersFromJson(string jsonData)
    {
        // SentimentAnalysisResponse は、前述のJSONパース用クラス
        SentimentAnalysisResponse response = JsonUtility.FromJson<SentimentAnalysisResponse>(jsonData);
        if (response != null && response.sentiment_analysis != null && response.sentiment_analysis.segments != null && response.sentiment_analysis.segments.Count > 0)
        {
            SentimentSegment seg = response.sentiment_analysis.segments[0];

            // パラメータをいじる場所

            // 任意の感情パラメータとGameControllerのパラメータをマッピング
            // ストレスの値とか、高かったら生成半径を狭めてもいいかも
            parameterA = seg.energy;
            parameterB = seg.excitement;
            parameterC = seg.upset;
            parameterD = seg.aggression;
            parameterE = seg.stress;
            parameterF = seg.uncertainty;
            parameterG = seg.concentration;

            Debug.Log("パラメータを更新しました: " +
                "\nparameterA (energy): " + parameterA +
                "\nparameterB (excitement): " + parameterB +
                "\nparameterC (upset): " + parameterC +
                "\nparameterD (aggression): " + parameterD +
                "\nparameterE (stress): " + parameterE +
                "\nparameterF (uncertainty): " + parameterF +
                "\nparameterG (concentration): " + parameterG);
        }
        else
        {
            Debug.LogError("感情データのパースに失敗しました。");
        }
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
        // モデルAは別の倍率で拡大
        ScaleModelList(attachedModelsA, enlargeFactorAlt);

        // モデルB～Gも同様に拡大
        ScaleModelList(attachedModelsB, enlargeFactor);
        ScaleModelList(attachedModelsC, enlargeFactor);
        ScaleModelList(attachedModelsD, enlargeFactor);
        ScaleModelList(attachedModelsE, enlargeFactor);
        ScaleModelList(attachedModelsF, enlargeFactor);
        ScaleModelList(attachedModelsG, enlargeFactor);

        Debug.Log("すべてのモデルを拡大しました！");
    }

    // 新しいヘルパーメソッドを追加
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

        Debug.Log("すべてのオブジェクトを削除しました！");
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
}