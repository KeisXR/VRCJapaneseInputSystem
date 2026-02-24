using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Events;
using UdonSharp;
using UdonSharpEditor;
using VRC.Udon;
using VRC.SDKBase;
using TMPro;
using System.Collections.Generic;

public class IMESetupTool : EditorWindow
{
    private IMEController imeController;
    private GameObject keyboardRoot;
    private string keyboardAreaName = "KeyboardArea";
    
    [MenuItem("Tools/Japanese IME/Setup Helper")]
    public static void ShowWindow()
    {
        GetWindow<IMESetupTool>("IME Setup Helper");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("IME 完全自動セットアップ", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "1. キーボードのボタンに KeyButton スクリプトをアタッチ\n" +
            "2. キーの文字（KeyValue）を自動設定\n" +
            "3. IMEController を割り当て\n" +
            "4. 画面表示の自動リンク\n" +
            "5. Buttonクリックイベントの自動登録\n" +
            "これらを一括で行います。", 
            MessageType.Info
        );
        
        EditorGUILayout.Space();
        
        imeController = (IMEController)EditorGUILayout.ObjectField("IME Controller", imeController, typeof(IMEController), true);
        keyboardRoot = (GameObject)EditorGUILayout.ObjectField("Keyboard Root", keyboardRoot, typeof(GameObject), true);
        keyboardAreaName = EditorGUILayout.TextField("Keyboard Area Name", keyboardAreaName);
        
        EditorGUILayout.Space();
        
        EditorGUI.BeginDisabledGroup(imeController == null || keyboardRoot == null);
        if (GUILayout.Button("全自動セットアップ実行", GUILayout.Height(40)))
        {
            SetupAll();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space();
        GUILayout.Label("参照の一括設定", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "KeyButton の IMEController 参照だけを一括で設定します。\n" +
            "KeyboardArea が見つからない場合は Keyboard Root 全体を対象にします。",
            MessageType.None
        );
        EditorGUI.BeginDisabledGroup(imeController == null || keyboardRoot == null);
        if (GUILayout.Button("IMEController参照のみ一括設定", GUILayout.Height(28)))
        {
            AssignImeControllerOnly();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "注意: セットアップ後、シーンを保存してください（Ctrl+S）\n" +
            "保存後、プレイモードで動作を確認してください。", 
            MessageType.Warning
        );
    }
    
    private void AssignImeControllerOnly()
    {
        if (imeController == null || keyboardRoot == null) return;

        Transform areaRoot = keyboardRoot.transform;
        if (!string.IsNullOrEmpty(keyboardAreaName))
        {
            Transform found = SearchTransform(areaRoot, keyboardAreaName);
            if (found != null)
            {
                areaRoot = found;
            }
            else
            {
                Debug.LogWarning($"[IMESetupTool] '{keyboardAreaName}' が見つからないため、Keyboard Root 全体を対象にします。");
            }
        }

        KeyButton[] keyButtons = areaRoot.GetComponentsInChildren<KeyButton>(true);
        if (keyButtons == null || keyButtons.Length == 0)
        {
            Debug.LogWarning("[IMESetupTool] KeyButton が見つかりません。必要なら全自動セットアップを実行してください。");
            EditorUtility.DisplayDialog("IME参照設定", "KeyButton が見つかりませんでした。", "OK");
            return;
        }

        int updated = 0;
        int alreadySet = 0;

        foreach (var keyButton in keyButtons)
        {
            if (keyButton == null) continue;

            SerializedObject so = new SerializedObject(keyButton);
            var prop = so.FindProperty("imeController");
            if (prop == null) continue;

            if (prop.objectReferenceValue == imeController)
            {
                alreadySet++;
                continue;
            }

            Undo.RecordObject(keyButton, "Assign IMEController");
            prop.objectReferenceValue = imeController;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(keyButton);
            PrefabUtility.RecordPrefabInstancePropertyModifications(keyButton);
            updated++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
        AssetDatabase.SaveAssets();

        string targetName = areaRoot != null ? areaRoot.name : "(unknown)";
        string message =
            $"対象: {targetName}\n" +
            $"更新: {updated} / 既に設定済み: {alreadySet}";
        Debug.Log($"[IMESetupTool] IMEController参照を更新: {updated} 件（既に設定済み: {alreadySet} 件）");
        EditorUtility.DisplayDialog("IME参照設定", message, "OK");
    }
    
    private void SetupAll()
    {
        if (imeController == null || keyboardRoot == null) return;
        
        // ---------------------------------------------------------
        // 1. IMEControllerのUI参照設定
        // ---------------------------------------------------------
        Debug.Log("[IMESetupTool] UI要素を検索中...");
        
        bool uiUpdated = false;
        
        var inputText = SearchComponent<TextMeshProUGUI>(keyboardRoot.transform, "InputText");
        var candidateText = SearchComponent<TextMeshProUGUI>(keyboardRoot.transform, "CandidateText");
        var statusText = SearchComponent<TextMeshProUGUI>(keyboardRoot.transform, "IMEStatus");
        
        if (inputText != null || candidateText != null || statusText != null)
        {
            Undo.RecordObject(imeController, "Setup IMEController UI");
            SerializedObject imeSo = new SerializedObject(imeController);
            
            if (inputText != null) imeSo.FindProperty("inputDisplayText").objectReferenceValue = inputText;
            if (candidateText != null) imeSo.FindProperty("candidateDisplayText").objectReferenceValue = candidateText;
            if (statusText != null) imeSo.FindProperty("imeStatusText").objectReferenceValue = statusText;
            
            imeSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(imeController);
            uiUpdated = true;
        }
        
        // ---------------------------------------------------------
        // 2. キーボタンの設定
        // ---------------------------------------------------------
        
        int count = 0;
        int errorCount = 0;
        
        Button[] buttons = keyboardRoot.GetComponentsInChildren<Button>(true);
        Debug.Log($"[IMESetupTool] {buttons.Length} 個のボタンを検出");
        
        foreach (var btn in buttons)
        {
            GameObject obj = btn.gameObject;
            
            // キー名から値を推測
            string keyName = obj.name;
            string keyValue = "";
            int keyType = 0; // 0=Normal
            
            if (keyName.StartsWith("Key_"))
            {
                keyValue = keyName.Substring(4);
            }
            else
            {
                continue;
            }
            
            if (keyValue == "Space") { keyType = 1; keyValue = " "; }
            else if (keyValue == "Enter") { keyType = 2; keyValue = ""; }
            else if (keyValue == "BS") { keyType = 3; keyValue = ""; }
            else if (keyValue == "IME") { keyType = 5; keyValue = ""; }
            else { keyValue = keyValue.ToLower(); }
            
            // KeyButton取得または追加
            KeyButton keyButton = obj.GetComponent<KeyButton>();
            bool isNew = false;
            
            if (keyButton == null)
            {
                UdonBehaviour existingUdon = obj.GetComponent<UdonBehaviour>();
                if (existingUdon != null) Undo.DestroyObjectImmediate(existingUdon);
                keyButton = Undo.AddComponent<KeyButton>(obj);
                isNew = true;
            }
            
            if (keyButton == null)
            {
                errorCount++;
                continue;
            }
            
            // SerializedObjectで変数設定
            if (isNew) Undo.RegisterCreatedObjectUndo(keyButton, "Setup KeyButton");
            else Undo.RecordObject(keyButton, "Setup KeyButton");
            
            SerializedObject so = new SerializedObject(keyButton);
            SetProperty(so, "keyValue", keyValue);
            SetProperty(so, "keyType", keyType);
            SetProperty(so, "imeController", imeController);
            SetProperty(so, "keyLabel", obj.GetComponentInChildren<TextMeshProUGUI>());
            SetProperty(so, "keyBackground", obj.GetComponent<Image>());
            so.ApplyModifiedProperties();
            
            // 重要: ButtonのOnClick設定
            // UdonBehaviour.Interact() を呼び出すように設定
            UdonBehaviour udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(keyButton);
            if (udon != null)
            {
                Undo.RecordObject(btn, "Setup Button Event");
                
                // 既存のリスナーをクリア（重複防止）
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, udon.Interact);
                
                // 新しくInteractを登録
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    btn.onClick, 
                    new UnityAction(udon.Interact)
                );
                
                // 設定を保存
                EditorUtility.SetDirty(btn);
            }
            else
            {
                Debug.LogWarning($"[IMESetupTool] {obj.name}: Backing UdonBehaviour not found");
            }
            
            EditorUtility.SetDirty(keyButton);
            EditorUtility.SetDirty(obj);
            
            count++;
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
        AssetDatabase.SaveAssets();
        
        string message = $"{count} 個のボタンをセットアップしました！\nクリックイベントも登録されました。";
        Debug.Log($"[IMESetupTool] 完了: {count} 個成功");
        EditorUtility.DisplayDialog("完了", message, "OK");
    }
    
    private Transform SearchTransform(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }
    
    private T SearchComponent<T>(Transform root, string name) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            if (component.gameObject.name == name) return component;
        }
        return null;
    }
    
    private void SetProperty(SerializedObject so, string name, object value)
    {
        var prop = so.FindProperty(name);
        if (prop == null) return;
        
        if (value is string s) prop.stringValue = s;
        else if (value is int i) prop.intValue = i;
        else if (value is Object o) prop.objectReferenceValue = o;
    }
}
