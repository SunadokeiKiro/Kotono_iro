using UnityEngine;
using System.Collections;

/// <summary>
/// UI要素をScreen.safeAreaに適応させるためのクラス（Canvas強制更新版）
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaManager : MonoBehaviour
{
    private RectTransform panel;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private ScreenOrientation lastScreenOrientation;
    private Canvas rootCanvas;

    void Awake()
    {
        panel = GetComponent<RectTransform>();
        // 親階層からCanvasコンポーネントを探して取得
        rootCanvas = GetComponentInParent<Canvas>();
        lastScreenOrientation = Screen.orientation;
    }

    IEnumerator Start()
    {
        // 初回の適用を、フレームの終わりまで待ってから実行
        yield return new WaitForEndOfFrame();
        
        // Canvas全体のレイアウトを強制的に更新させる
        if (rootCanvas != null)
        {
            Canvas.ForceUpdateCanvases();
        }

        // 強制更新の直後に、セーフエリアを適用
        ApplySafeArea();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.orientation != lastScreenOrientation)
        {
            ApplySafeArea();
        }
    }

    void ApplySafeArea()
    {
        lastSafeArea = Screen.safeArea;
        lastScreenOrientation = Screen.orientation;
        
        // アンカーをストレッチに設定
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;

        Rect safeArea = Screen.safeArea;

        // オフセットを計算
        Vector2 offsetMin = new Vector2(safeArea.position.x, safeArea.position.y);
        Vector2 offsetMax = new Vector2(
            -(Screen.width - (safeArea.position.x + safeArea.size.x)),
            -(Screen.height - (safeArea.position.y + safeArea.size.y))
        );
        
        panel.offsetMin = offsetMin;
        panel.offsetMax = offsetMax;
    }
}