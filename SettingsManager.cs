using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.IO;

public class SettingsManager : MonoBehaviour
{
    public InputField apiKeyInput;
    public Button saveButton;
    public Button backButton;
    public Text statusText; // 操作結果を表示するためのテキスト（オプション）

    private string apiKeyFilePath;

    void Start()
    {
        // APIキーファイルのパスを設定
        apiKeyFilePath = Path.Combine(Application.persistentDataPath, "apikey.txt");
        
        // ボタンにリスナーを設定
        saveButton.onClick.AddListener(SaveApiKey);
        backButton.onClick.AddListener(GoBackToMainScene);
        
        // 保存されているAPIキーがあれば読み込む
        LoadApiKey();
    }

    void LoadApiKey()
    {
        try
        {
            if (File.Exists(apiKeyFilePath))
            {
                string savedApiKey = File.ReadAllText(apiKeyFilePath).Trim();
                apiKeyInput.text = savedApiKey;
                
                if (statusText != null)
                    statusText.text = "保存されたAPIキーを読み込みました";
            }
        }
        catch (Exception e)
        {
            Debug.LogError("APIキーの読み込みに失敗: " + e.Message);
            if (statusText != null)
                statusText.text = "APIキーの読み込みに失敗しました";
        }
    }

    void SaveApiKey()
    {
        try
        {
            if (!string.IsNullOrEmpty(apiKeyInput.text))
            {
                File.WriteAllText(apiKeyFilePath, apiKeyInput.text.Trim());
                Debug.Log("APIキーを保存しました: " + apiKeyFilePath);
                
                if (statusText != null)
                    statusText.text = "APIキーを保存しました";
            }
            else
            {
                Debug.LogError("保存するAPIキーが入力されていません");
                if (statusText != null)
                    statusText.text = "APIキーが入力されていません";
            }
        }
        catch (Exception e)
        {
            Debug.LogError("APIキーの保存に失敗: " + e.Message);
            if (statusText != null)
                statusText.text = "APIキーの保存に失敗しました";
        }
    }

    void GoBackToMainScene()
    {
        // メインシーンに戻る（シーン名は実際のメインシーン名に変更する）
        SceneManager.LoadScene("MainScene");
    }
}