// Scripts/MainUIManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// メインシーンのUIイベントと表示更新をすべて管理します。
/// ユーザーからの入力を受け付け、各専門コントローラーに処理を依頼します。
/// </summary>
public class MainUIManager : MonoBehaviour
{
    [Header("Controller References")]
    [SerializeField] private GameController gameController;
    [SerializeField] private MicrophoneController microphoneController;

    [Header("UI Elements")]
    [SerializeField] private Button recButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button autoRecordOnButton;
    [SerializeField] private Button autoRecordOffButton;
    [SerializeField] private Text autoRecordStatusText;
    [SerializeField] private Button buttonSaveToSlot;
    [SerializeField] private Button buttonGallery;
    [SerializeField] private Button buttonResetAndNew;
    //[SerializeField] private Button buttonNewMainObject; // 元のbutton1

    [Header("感情データ表示用 UI")]
    [SerializeField] private TextMeshProUGUI textEnergy;
    [SerializeField] private TextMeshProUGUI textContent;
    [SerializeField] private TextMeshProUGUI textUpset;
    [SerializeField] private TextMeshProUGUI textAggression;
    [SerializeField] private TextMeshProUGUI textStress;
    [SerializeField] private TextMeshProUGUI textUncertainty;
    [SerializeField] private TextMeshProUGUI textExcitement;
    [SerializeField] private TextMeshProUGUI textConcentration;
    [SerializeField] private TextMeshProUGUI textEmoCog;
    [SerializeField] private TextMeshProUGUI textHesitation;
    [SerializeField] private TextMeshProUGUI textBrainPower;
    [SerializeField] private TextMeshProUGUI textEmbarrassment;
    [SerializeField] private TextMeshProUGUI textIntensiveThinking;
    [SerializeField] private TextMeshProUGUI textImaginationActivity;
    [SerializeField] private TextMeshProUGUI textExtremeEmotion;
    [SerializeField] private TextMeshProUGUI textPassionate;
    [SerializeField] private TextMeshProUGUI textAtmosphere;
    [SerializeField] private TextMeshProUGUI textAnticipation;
    [SerializeField] private TextMeshProUGUI textDissatisfaction;
    [SerializeField] private TextMeshProUGUI textConfidence;

    [Header("テキスト切り替えUI")]
    [SerializeField] private CanvasGroup[] textObjectGroups;
    [SerializeField] private float textDisplayDuration = 5f;
    [SerializeField] private float textFadeDuration = 0.5f;
    private int currentTextGroupIndex = -1;

    [Header("スロット保存UI")]
    [SerializeField] private GameObject saveSlotSelectionPanel;
    [SerializeField] private Button[] saveSlotButtons = new Button[3];
    [SerializeField] private Button closeSaveSlotPanelButton;

    void Start()
    {
        // コントローラーの参照チェック
        if (gameController == null || microphoneController == null)
        {
            Debug.LogError("MainUIManager: Controller references are not set in the inspector.");
            enabled = false; // このコンポーネントを無効化
            return;
        }

        // ボタンにリスナーを設定
        InitializeButtons();

        // 自動録音の初期状態をUIに反映
        UpdateAutoRecordButtons(microphoneController.IsAutoRecordEnabled);
        UpdateAutoRecordStatusText(microphoneController.IsAutoRecordEnabled);
    }

    /// <summary>
    /// 全てのUIボタンにリスナーを割り当てます。
    /// </summary>
    private void InitializeButtons()
    {
        // 手動・自動録音
        recButton.onClick.AddListener(OnRecButtonClick);
        autoRecordOnButton.onClick.AddListener(OnAutoRecordOnButtonClick);
        autoRecordOffButton.onClick.AddListener(OnAutoRecordOffButtonClick);

        // 機能ボタン
        buttonResetAndNew.onClick.AddListener(() => gameController.HandleResetAndNewClick());
        //buttonNewMainObject.onClick.AddListener(() => gameController.GenerateRandomMainObjectAndSaveToCurrentWork());
        buttonSaveToSlot.onClick.AddListener(ShowSaveSlotSelection);

        // シーン遷移ボタン
        buttonGallery.onClick.AddListener(() => SceneManager.LoadScene("GalleryScene"));
        settingsButton.onClick.AddListener(() => gameController.MoveToSettings());

        // 保存パネル
        if (saveSlotSelectionPanel != null)
        {
            saveSlotSelectionPanel.SetActive(false);
            if(closeSaveSlotPanelButton != null)
            {
               closeSaveSlotPanelButton.onClick.AddListener(() => saveSlotSelectionPanel.SetActive(false));
            }
        }
    }

    #region UIイベントハンドラ

    private void OnRecButtonClick()
    {
        // 手動録音をマイクコントローラーに依頼
        StartCoroutine(microphoneController.StartManualRecording());
    }

    private void OnAutoRecordOnButtonClick()
    {
        microphoneController.StartAutoRecording();
        UpdateAutoRecordButtons(true);
        UpdateAutoRecordStatusText(true);
    }

    private void OnAutoRecordOffButtonClick()
    {
        microphoneController.StopAutoRecording();
        UpdateAutoRecordButtons(false);
        UpdateAutoRecordStatusText(false);
    }

    private void ShowSaveSlotSelection()
    {
        if (saveSlotSelectionPanel != null)
        {
            saveSlotSelectionPanel.SetActive(true);
            for (int i = 0; i < saveSlotButtons.Length; i++)
            {
                if (saveSlotButtons[i] != null)
                {
                    int slotIndex = i; // クロージャで発生する問題を避けるためのローカルコピー
                    saveSlotButtons[i].onClick.RemoveAllListeners(); // 古いリスナーを削除
                    saveSlotButtons[i].onClick.AddListener(() => {
                        gameController.SaveCurrentWorkToDesignatedSlot(slotIndex);
                        saveSlotSelectionPanel.SetActive(false);
                    });
                }
            }
        }
        else
        {
            Debug.LogWarning("Save Slot Selection Panel is not assigned.");
        }
    }

    #endregion

    #region UI表示更新

    private void UpdateAutoRecordButtons(bool isAuto)
    {
        if (autoRecordOnButton != null) autoRecordOnButton.gameObject.SetActive(!isAuto);
        if (autoRecordOffButton != null) autoRecordOffButton.gameObject.SetActive(isAuto);
    }

    private void UpdateAutoRecordStatusText(bool isAuto)
    {
        if (autoRecordStatusText != null)
        {
            autoRecordStatusText.text = isAuto ? "自動録音: オン" : "自動録音: オフ";
            autoRecordStatusText.color = isAuto ? Color.green : Color.red;
        }
    }

    /// <summary>
    /// GameControllerからデータを受け取り、感情値のUIテキストを更新します。
    /// </summary>
    public void UpdateWorkSentimentUI(SceneData data)
    {
        if (data == null) return;
        if (textEnergy != null) textEnergy.text = $"Energy: {data.totalEnergy:D}";
        if (textContent != null) textContent.text = $"Content: {data.totalContent:D}";
        if (textUpset != null) textUpset.text = $"Upset: {data.totalUpset:D}";
        if (textAggression != null) textAggression.text = $"Aggression: {data.totalAggression:D}";
        if (textStress != null) textStress.text = $"Stress: {data.totalStress:D}";
        if (textUncertainty != null) textUncertainty.text = $"Uncertainty: {data.totalUncertainty:D}";
        if (textExcitement != null) textExcitement.text = $"Excitement: {data.totalExcitement:D}";
        if (textConcentration != null) textConcentration.text = $"Concentration: {data.totalConcentration:D}";
        if (textEmoCog != null) textEmoCog.text = $"Emo Cog: {data.totalEmoCog:D}";
        if (textHesitation != null) textHesitation.text = $"Hesitation: {data.totalHesitation:D}";
        if (textBrainPower != null) textBrainPower.text = $"Brain Power: {data.totalBrainPower:D}";
        if (textEmbarrassment != null) textEmbarrassment.text = $"Embarrassment: {data.totalEmbarrassment:D}";
        if (textIntensiveThinking != null) textIntensiveThinking.text = $"Intensive Thinking: {data.totalIntensiveThinking:D}";
        if (textImaginationActivity != null) textImaginationActivity.text = $"Imagination Activity: {data.totalImaginationActivity:D}";
        if (textExtremeEmotion != null) textExtremeEmotion.text = $"Extreme Emotion: {data.totalExtremeEmotion:D}";
        if (textPassionate != null) textPassionate.text = $"Passionate: {data.totalPassionate:D}";
        if (textAtmosphere != null) textAtmosphere.text = $"Atmosphere: {data.totalAtmosphere:D}";
        if (textAnticipation != null) textAnticipation.text = $"Anticipation: {data.totalAnticipation:D}";
        if (textDissatisfaction != null) textDissatisfaction.text = $"Dissatisfaction: {data.totalDissatisfaction:D}";
        if (textConfidence != null) textConfidence.text = $"Confidence: {data.totalConfidence:D}";
    }

    /// <summary>
    /// テキストのサイクリング表示を開始します。
    /// </summary>
    public void InitializeTextCycling()
    {
        currentTextGroupIndex = -1;
        if (textObjectGroups == null || textObjectGroups.Length == 0) return;

        // 最初の有効なグループを見つけて表示
        for (int i = 0; i < textObjectGroups.Length; i++)
        {
            if (textObjectGroups[i] != null)
            {
                if (currentTextGroupIndex == -1)
                {
                    currentTextGroupIndex = i;
                    textObjectGroups[i].alpha = 1f;
                    textObjectGroups[i].gameObject.SetActive(true);
                }
                else
                {
                    textObjectGroups[i].alpha = 0f;
                    textObjectGroups[i].gameObject.SetActive(false);
                }
            }
        }

        // 有効なグループが2つ以上あれば、サイクリングを開始
        if (currentTextGroupIndex != -1)
        {
            int validGroupCount = 0;
            foreach (var group in textObjectGroups)
            {
                if (group != null) validGroupCount++;
            }
            if (validGroupCount > 1)
            {
                StartCoroutine(CycleTextGroupsCoroutine());
            }
        }
    }

    private IEnumerator CycleTextGroupsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(textDisplayDuration);

            if (textObjectGroups == null || textObjectGroups.Length <= 1 || currentTextGroupIndex < 0) yield break;
            
            CanvasGroup currentGroup = textObjectGroups[currentTextGroupIndex];

            // 次に表示する有効なグループを探す
            int nextValidIndex = currentTextGroupIndex;
            int attempts = 0;
            do
            {
                nextValidIndex = (nextValidIndex + 1) % textObjectGroups.Length;
                if (++attempts > textObjectGroups.Length)
                {
                    Debug.LogError("Could not find next valid text group for cycling.");
                    yield break;
                }
            } while (textObjectGroups[nextValidIndex] == null);

            CanvasGroup nextGroup = textObjectGroups[nextValidIndex];

            // フェードアウト
            if (currentGroup != null)
            {
                yield return StartCoroutine(FadeCanvasGroup(currentGroup, 1f, 0f, textFadeDuration));
                currentGroup.gameObject.SetActive(false);
            }

            // フェードイン
            if (nextGroup != null)
            {
                nextGroup.gameObject.SetActive(true);
                yield return StartCoroutine(FadeCanvasGroup(nextGroup, 0f, 1f, textFadeDuration));
            }
            
            currentTextGroupIndex = nextValidIndex;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        if (cg == null) yield break;
        float currentTime = 0f;
        cg.alpha = startAlpha;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, currentTime / duration);
            yield return null;
        }
        cg.alpha = endAlpha;
    }
    #endregion
}