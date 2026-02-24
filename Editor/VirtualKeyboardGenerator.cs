using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 仮想キーボードPrefabを自動生成するエディタツール
/// </summary>
public class VirtualKeyboardGenerator : EditorWindow
{
    private float keyWidth = 60f;
    private float keyHeight = 60f;
    private float keySpacing = 5f;
    private float canvasWidth = 800f;
    private float canvasHeight = 320f;
    
    [MenuItem("Tools/Japanese IME/Generate Virtual Keyboard")]
    public static void ShowWindow()
    {
        GetWindow<VirtualKeyboardGenerator>("Virtual Keyboard Generator");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("仮想キーボード生成", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        keyWidth = EditorGUILayout.FloatField("キー幅", keyWidth);
        keyHeight = EditorGUILayout.FloatField("キー高さ", keyHeight);
        keySpacing = EditorGUILayout.FloatField("キー間隔", keySpacing);
        canvasWidth = EditorGUILayout.FloatField("Canvas幅", canvasWidth);
        canvasHeight = EditorGUILayout.FloatField("Canvas高さ", canvasHeight);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("キーボード生成", GUILayout.Height(40)))
        {
            GenerateKeyboard();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "生成後、Prefabとして保存してください。\n" +
            "IMEController、RomajiConverter、KanjiConverterをシーンに配置し、\n" +
            "キーボードにアタッチしてください。",
            MessageType.Info
        );
    }
    
    private void GenerateKeyboard()
    {
        // ルートオブジェクト作成
        GameObject root = new GameObject("VirtualKeyboard");
        
        // Canvas設定
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;
        
        root.AddComponent<GraphicRaycaster>();
        
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(canvasWidth, canvasHeight);
        rootRect.localScale = Vector3.one * 0.001f; // World Spaceスケール
        
        // 背景パネル
        GameObject bgPanel = CreatePanel(root.transform, "Background", canvasWidth, canvasHeight);
        Image bgImage = bgPanel.GetComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        
        // 入力表示エリア
        float displayHeight = 50f;
        GameObject displayArea = CreatePanel(bgPanel.transform, "DisplayArea", canvasWidth - 20, displayHeight);
        RectTransform displayRect = displayArea.GetComponent<RectTransform>();
        displayRect.anchoredPosition = new Vector2(0, (canvasHeight - displayHeight) / 2 - 10);
        displayArea.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 1f);
        
        // 入力テキスト
        GameObject inputText = CreateText(displayArea.transform, "InputText", "");
        RectTransform inputTextRect = inputText.GetComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = new Vector2(0.6f, 1f);
        inputTextRect.offsetMin = new Vector2(10, 5);
        inputTextRect.offsetMax = new Vector2(-5, -5);
        
        // 候補テキスト
        GameObject candidateText = CreateText(displayArea.transform, "CandidateText", "");
        RectTransform candRect = candidateText.GetComponent<RectTransform>();
        candRect.anchorMin = new Vector2(0.6f, 0);
        candRect.anchorMax = Vector2.one;
        candRect.offsetMin = new Vector2(5, 5);
        candRect.offsetMax = new Vector2(-10, -5);
        TextMeshProUGUI candTmp = candidateText.GetComponent<TextMeshProUGUI>();
        candTmp.fontSize = 16;
        candTmp.color = new Color(0.7f, 0.7f, 0.7f);
        
        // キーボードエリア
        GameObject keyboardArea = CreatePanel(bgPanel.transform, "KeyboardArea", canvasWidth - 20, canvasHeight - displayHeight - 30);
        RectTransform kbRect = keyboardArea.GetComponent<RectTransform>();
        kbRect.anchoredPosition = new Vector2(0, -(displayHeight + 10) / 2);
        keyboardArea.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        
        // キー配列定義
        string[][] rows = new string[][]
        {
            new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" },
            new string[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "[", "]" },
            new string[] { "a", "s", "d", "f", "g", "h", "j", "k", "l", ";", "'", "BS" },
            new string[] { "IME", "z", "x", "c", "v", "b", "n", "m", ",", ".", "/", "Enter" },
            new string[] { "Space" }
        };
        
        float[] rowOffsets = { 0, 0, 10, 20, 0 };
        float startY = (canvasHeight - displayHeight - 30) / 2 - keyHeight / 2 - 10;
        
        for (int row = 0; row < rows.Length; row++)
        {
            string[] keys = rows[row];
            float totalWidth = 0;
            
            // 各キーの幅を計算
            for (int col = 0; col < keys.Length; col++)
            {
                totalWidth += GetKeyWidth(keys[col]) + keySpacing;
            }
            totalWidth -= keySpacing;
            
            float startX = -totalWidth / 2 + rowOffsets[row];
            float currentX = startX;
            float y = startY - row * (keyHeight + keySpacing);
            
            for (int col = 0; col < keys.Length; col++)
            {
                string key = keys[col];
                float kw = GetKeyWidth(key);
                
                GameObject keyObj = CreateKey(keyboardArea.transform, key, currentX + kw / 2, y, kw, keyHeight);
                currentX += kw + keySpacing;
            }
        }
        
        // IMEステータス表示
        GameObject statusText = CreateText(bgPanel.transform, "IMEStatus", "あ");
        RectTransform statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 1);
        statusRect.anchorMax = new Vector2(0, 1);
        statusRect.sizeDelta = new Vector2(40, 40);
        statusRect.anchoredPosition = new Vector2(30, -30);
        TextMeshProUGUI statusTmp = statusText.GetComponent<TextMeshProUGUI>();
        statusTmp.fontSize = 24;
        statusTmp.alignment = TextAlignmentOptions.Center;
        statusTmp.color = new Color(0.3f, 0.8f, 0.3f);
        
        // 選択して完了を通知
        Selection.activeGameObject = root;
        Debug.Log("[VirtualKeyboardGenerator] キーボード生成完了。Prefabとして保存してください。");
    }
    
    private float GetKeyWidth(string key)
    {
        switch (key)
        {
            case "Space":
                return keyWidth * 5 + keySpacing * 4;
            case "Enter":
            case "BS":
            case "IME":
                return keyWidth * 1.5f;
            default:
                return keyWidth;
        }
    }
    
    private GameObject CreatePanel(Transform parent, string name, float width, float height)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0);
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        return panel;
    }
    
    private GameObject CreateText(Transform parent, string name, string text)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        
        return textObj;
    }
    
    private GameObject CreateKey(Transform parent, string key, float x, float y, float width, float height)
    {
        GameObject keyObj = new GameObject("Key_" + key);
        keyObj.transform.SetParent(parent);
        
        RectTransform rect = keyObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, y);
        rect.localScale = Vector3.one;
        rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0);
        
        // 背景
        Image image = keyObj.AddComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.3f, 1f);
        
        // ボタン
        Button button = keyObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.25f, 0.25f, 0.3f, 1f);
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.5f, 1f);
        colors.pressedColor = new Color(0.4f, 0.4f, 0.7f, 1f);
        button.colors = colors;
        
        // ラベル
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(keyObj.transform);
        
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelRect.localScale = Vector3.one;
        labelRect.localPosition = new Vector3(0, 0, 0);
        
        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = GetKeyLabel(key);
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        
        // Note: UdonBehaviourはエディタで直接追加できないため、
        // 手動でKeyButtonスクリプトをアタッチする必要あり
        
        return keyObj;
    }
    
    private string GetKeyLabel(string key)
    {
        switch (key)
        {
            case "BS":
                return "←";
            case "Enter":
                return "↵";
            case "Space":
                return "Space";
            case "IME":
                return "あ/A";
            default:
                return key.ToUpper();
        }
    }
}
