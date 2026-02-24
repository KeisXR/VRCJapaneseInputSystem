using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

/// <summary>
/// 仮想キーボードの個々のキーを制御
/// IMEControllerと連携して入力を送信
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class KeyButton : UdonSharpBehaviour
{
    [Header("キー設定")]
    [Tooltip("このキーが送信する文字")]
    [SerializeField] private string keyValue = "";
    
    [Tooltip("特殊キータイプ")]
    [SerializeField] private int keyType = 0;
    // 0 = 通常文字キー
    // 1 = Space（変換）
    // 2 = Enter（確定）
    // 3 = Backspace
    // 4 = Escape
    // 5 = IME Toggle
    // 6 = Shift
    // 7 = Shrink Segment（←）
    // 8 = Extend Segment（→）
    // 9 = Commit Hiragana（ひらがな確定）
    // 10 = Commit Katakana（カタカナ確定）
    
    [Header("参照")]
    [SerializeField] private IMEController imeController;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI keyLabel;
    [SerializeField] private Image keyBackground;
    
    [Header("外観")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.4f, 0.4f, 0.8f, 1f);
    
    private bool isShiftActive = false;
    private string originalValue = "";
    
    void Start()
    {
        Debug.Log($"[KeyButton] Start: {gameObject.name}, Value: {keyValue}, Type: {keyType}, IME: {imeController != null}");
        originalValue = keyValue;
        
        if (keyLabel != null && !string.IsNullOrEmpty(keyValue))
        {
            keyLabel.text = keyValue.ToUpper();
        }
    }
    
    /// <summary>
    /// ボタンクリック時（Interaction/UI Button）
    /// </summary>
    public void OnClick()
    {
        Debug.Log($"[KeyButton] OnClick: {keyValue} (Type: {keyType})");
        
        if (imeController == null)
        {
            Debug.LogError($"[KeyButton] Error: IMEController is null on {gameObject.name}");
            return;
        }
        
        // SendCustomEventを使用して確実に呼び出す
        
        switch (keyType)
        {
            case 0: // 通常キー
                imeController.SetProgramVariable("_inputKey", keyValue);
                imeController.SendCustomEvent("_OnInputKey");
                break;
                
            case 1: // Space
                imeController.SendCustomEvent("_OnSpace");
                break;
                
            case 2: // Enter
                imeController.SendCustomEvent("_OnEnter");
                break;
                
            case 3: // Backspace
                imeController.SendCustomEvent("_OnBackspace");
                break;
                
            case 4: // Escape
                imeController.SendCustomEvent("_OnEscape");
                break;
                
            case 5: // IME Toggle
                imeController.SendCustomEvent("_ToggleIME");
                
                // 状態取得
                bool isEnabled = (bool)imeController.GetProgramVariable("imeEnabled");
                if (keyLabel != null)
                {
                    keyLabel.text = isEnabled ? "あ" : "A";
                }
                break;
                
            case 6: // Shift
                ToggleShift();
                break;
                
            case 7: // Shrink Segment（←）
                imeController.SendCustomEvent("_OnShrinkSegment");
                break;
                
            case 8: // Extend Segment（→）
                imeController.SendCustomEvent("_OnExtendSegment");
                break;
                
            case 9: // Commit Hiragana
                imeController.SendCustomEvent("_OnCommitHiragana");
                break;
                
            case 10: // Commit Katakana
                imeController.SendCustomEvent("_OnCommitKatakana");
                break;
        }
        
        // 視覚フィードバック
        FlashKey();
    }
    
    /// <summary>
    /// VRCでのInteract
    /// </summary>
    public override void Interact()
    {
        OnClick();
    }
    
    public void SetIMEController(IMEController controller)
    {
        imeController = controller;
    }
    
    public void SetShift(bool shift)
    {
        isShiftActive = shift;
        
        if (keyType == 0 && !string.IsNullOrEmpty(originalValue))
        {
            keyValue = shift ? originalValue.ToUpper() : originalValue.ToLower();
            
            if (keyLabel != null)
            {
                keyLabel.text = keyValue.ToUpper();
            }
        }
    }
    
    private void ToggleShift()
    {
        isShiftActive = !isShiftActive;
        
        Transform parent = transform.parent;
        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                KeyButton otherKey = parent.GetChild(i).GetComponent<KeyButton>();
                if (otherKey != null)
                {
                    otherKey.SetShift(isShiftActive);
                }
            }
        }
        
        if (keyBackground != null)
        {
            keyBackground.color = isShiftActive ? pressedColor : normalColor;
        }
    }
    
    private void FlashKey()
    {
        if (keyBackground != null)
        {
            keyBackground.color = pressedColor;
            SendCustomEventDelayedSeconds(nameof(ResetKeyColor), 0.1f);
        }
    }
    
    public void ResetKeyColor()
    {
        if (keyBackground != null && keyType != 6) // Shiftキー以外
        {
            keyBackground.color = normalColor;
        }
    }
}
