// Scripts/ThumbnailGenerator.cs
using UnityEngine;
using System.IO;

public static class ThumbnailGenerator
{
    private static Camera thumbnailCamera;
    private static RenderTexture renderTexture;

    // サムネイル撮影用のカメラとRenderTextureを準備
    private static void SetupThumbnailCamera(int width, int height)
    {
        if (thumbnailCamera == null)
        {
            GameObject camObj = new GameObject("ThumbnailCamera");
            thumbnailCamera = camObj.AddComponent<Camera>();
            thumbnailCamera.clearFlags = CameraClearFlags.SolidColor;
            thumbnailCamera.backgroundColor = Color.clear; // 背景を透過させる
            thumbnailCamera.cullingMask = LayerMask.GetMask("Default"); // サムネイル対象オブジェクトのレイヤーに合わせる
            thumbnailCamera.orthographic = true; // オブジェクト全体が収まるように調整
            thumbnailCamera.enabled = false; // 通常は無効にしておく
        }

        if (renderTexture == null || renderTexture.width != width || renderTexture.height != height)
        {
            if (renderTexture != null) renderTexture.Release();
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
        }
        thumbnailCamera.targetTexture = renderTexture;
    }

    // 指定されたGameObjectのサムネイルを撮影し、指定パスにPNGで保存
    public static bool CaptureAndSaveThumbnail(GameObject targetObject, string filePath, int width = 256, int height = 256)
    {
        if (targetObject == null)
        {
            Debug.LogError("ThumbnailGenerator: Target object is null.");
            return false;
        }

        SetupThumbnailCamera(width, height);

        // 対象オブジェクトのレイヤーを設定 (ThumbnailCameraが描画できるように)
        int originalLayer = targetObject.layer;
        RecursiveSetLayer(targetObject.transform, LayerMask.NameToLayer("Default")); // "Default" は例。専用レイヤー推奨

        // カメラの位置と向きを調整してオブジェクトを捉える
        Bounds bounds = CalculateBounds(targetObject);
        thumbnailCamera.transform.position = bounds.center + new Vector3(0, 0, -Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 1.5f); // 少し引く
        thumbnailCamera.transform.LookAt(bounds.center);
        thumbnailCamera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y) * 0.6f; // Boundsの高さか幅の大きい方に合わせる

        // 撮影
        thumbnailCamera.Render();

        // RenderTextureからTexture2Dに読み込み
        RenderTexture.active = renderTexture;
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;

        // 対象オブジェクトのレイヤーを元に戻す
        RecursiveSetLayer(targetObject.transform, originalLayer);

        // PNGとして保存
        try
        {
            byte[] bytes = texture2D.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            Debug.Log($"Thumbnail saved to: {filePath}");
            Object.Destroy(texture2D); // Texture2Dを破棄
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save thumbnail: {e.Message}");
            Object.Destroy(texture2D);
            return false;
        }
    }

    // オブジェクトとその子のレイヤーを再帰的に設定
    private static void RecursiveSetLayer(Transform trans, int layer)
    {
        trans.gameObject.layer = layer;
        foreach (Transform child in trans)
        {
            RecursiveSetLayer(child, layer);
        }
    }
    
    // GameObjectの包括的なバウンディングボックスを計算
    private static Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one); // レンダラーがなければデフォルト

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    // クリーンアップ（シーン終了時などに呼ぶ）
    public static void Cleanup()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Object.Destroy(renderTexture);
            renderTexture = null;
        }
        if (thumbnailCamera != null)
        {
            Object.Destroy(thumbnailCamera.gameObject);
            thumbnailCamera = null;
        }
    }
}