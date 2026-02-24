using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

/// <summary>
/// Controls the Japanese IME system, coordinating input, display, and conversion.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class IMEController : UdonSharpBehaviour
{
    [Header("Components")]
    [SerializeField] private RomajiConverter romajiConverter;
    [SerializeField] private KanjiConverter kanjiConverter;

    [Header("Display")]
    [SerializeField] private TextMeshProUGUI inputDisplayText;
    [SerializeField] private TextMeshProUGUI candidateDisplayText;
    [SerializeField] private TextMeshProUGUI imeStatusText;
    
    [Header("Output")]
    [Tooltip("Input Field to output text to (optional)")]
    [SerializeField] private TMP_InputField targetInputField;

    // Public variable for Input from KeyButton
    [HideInInspector] public string _inputKey;
    [HideInInspector] public bool imeEnabled = true;

    // State Constants
    private const int STATE_INPUT = 0;
    private const int STATE_CONVERT = 1;

    private int currentState = STATE_INPUT;
    private string preConversionHiragana = "";
    private string committedText = "";

    void Start()
    {
        UpdateStatusDisplay();
        ClearDisplay();
    }

    // --- Events called by KeyButton ---

    public void _OnInputKey()
    {
        OnKeyPress(_inputKey);
    }

    public void _OnSpace()
    {
        OnSpace();
    }

    public void _OnEnter()
    {
        OnEnter();
    }

    public void _OnBackspace()
    {
        OnBackspace();
    }

    public void _OnEscape()
    {
        OnEscape();
    }

    public void _ToggleIME()
    {
        ToggleIME();
    }

    public void _OnShrinkSegment()
    {
        if (currentState == STATE_CONVERT && kanjiConverter != null)
        {
            if (kanjiConverter.ShrinkSegment())
            {
                UpdateCandidateDisplay();
            }
        }
    }

    public void _OnExtendSegment()
    {
        if (currentState == STATE_CONVERT && kanjiConverter != null)
        {
            if (kanjiConverter.ExtendSegment())
            {
                UpdateCandidateDisplay();
            }
        }
    }

    public void _OnCommitHiragana()
    {
        if (currentState == STATE_INPUT)
        {
            // Input mode: commit romaji buffer as hiragana directly
            if (romajiConverter != null)
            {
                string text = romajiConverter.Commit();
                if (!string.IsNullOrEmpty(text))
                {
                    OutputText(text);
                }
            }
            ClearDisplay();
            return;
        }
        
        if (currentState != STATE_CONVERT || kanjiConverter == null) return;
        
        // Convert mode: force current segment as hiragana
        string reading = kanjiConverter.GetCurrentReading();
        string remainder = kanjiConverter.GetRemainingReading();
        
        committedText += reading;
        kanjiConverter.Reset();
        
        if (!string.IsNullOrEmpty(remainder) && kanjiConverter.StartConversion(remainder))
        {
            preConversionHiragana = remainder;
            UpdateCandidateDisplay();
        }
        else
        {
            if (!string.IsNullOrEmpty(remainder)) committedText += remainder;
            CommitAllAndFinish();
        }
    }

    public void _OnCommitKatakana()
    {
        if (currentState == STATE_INPUT)
        {
            // Input mode: commit romaji buffer as katakana directly
            if (romajiConverter != null)
            {
                string text = romajiConverter.Commit();
                if (!string.IsNullOrEmpty(text) && kanjiConverter != null)
                {
                    string katakana = kanjiConverter.GetKatakanaFor(text);
                    OutputText(katakana);
                }
            }
            ClearDisplay();
            return;
        }
        
        if (currentState != STATE_CONVERT || kanjiConverter == null) return;
        
        // Convert mode: force current segment as katakana
        string katakanaSegment = kanjiConverter.GetCurrentReadingAsKatakana();
        string remainder = kanjiConverter.GetRemainingReading();
        
        committedText += katakanaSegment;
        kanjiConverter.Reset();
        
        if (!string.IsNullOrEmpty(remainder) && kanjiConverter.StartConversion(remainder))
        {
            preConversionHiragana = remainder;
            UpdateCandidateDisplay();
        }
        else
        {
            if (!string.IsNullOrEmpty(remainder)) committedText += remainder;
            CommitAllAndFinish();
        }
    }

    // --- Core Logic ---

    public void OnKeyPress(string key)
    {
        if (!imeEnabled)
        {
            OutputText(key);
            return;
        }

        if (currentState == STATE_CONVERT)
        {
            if (TrySelectCandidateByKey(key)) return;
            CommitConversion();
        }

        if (romajiConverter != null)
        {
            romajiConverter.AddInput(key);
            UpdateDisplay();
        }
    }

    public void OnSpace()
    {
        if (!imeEnabled)
        {
            OutputText(" ");
            return;
        }

        if (currentState == STATE_INPUT)
        {
            StartConversion();
        }
        else if (currentState == STATE_CONVERT)
        {
            if (kanjiConverter != null)
            {
                kanjiConverter.NextCandidate();
                UpdateCandidateDisplay();
            }
        }
    }

    public void OnEnter()
    {
        if (!imeEnabled)
        {
            OutputText("\n");
            return;
        }

        if (currentState == STATE_CONVERT)
        {
            CommitConversion();
            // CommitConversion handles display/state internally now
        }
        else
        {
            if (romajiConverter != null)
            {
                string text = romajiConverter.Commit();
                OutputText(text);
            }
            ClearDisplay();
        }
    }

    public void OnBackspace()
    {
        if (currentState == STATE_CONVERT)
        {
            CancelConversion();
        }
        else
        {
            if (romajiConverter != null)
            {
                romajiConverter.Backspace();
            }
        }
        UpdateDisplay();
    }

    public void OnEscape()
    {
        if (currentState == STATE_CONVERT)
        {
            CancelConversion();
        }
        else
        {
            if (romajiConverter != null)
            {
                romajiConverter.Clear();
            }
        }
        ClearDisplay();
    }

    public void ToggleIME()
    {
        imeEnabled = !imeEnabled;
        UpdateStatusDisplay();
        
        if (!imeEnabled)
        {
            OnEscape();
        }
    }

    // --- Helper Methods ---

    private bool TrySelectCandidateByKey(string key)
    {
        // Allow selecting candidates with number keys 1-9
        if (key.Length == 1 && key[0] >= '1' && key[0] <= '9')
        {
            int index = (int)(key[0] - '1');
            if (kanjiConverter != null)
            {
               if (kanjiConverter.TrySelectCandidate(index))
               {
                   CommitConversion();
                   return true;
               }
            }
        }
        return false;
    }

    private void StartConversion()
    {
        if (romajiConverter == null || kanjiConverter == null) return;

        preConversionHiragana = romajiConverter.Commit();
        if (string.IsNullOrEmpty(preConversionHiragana)) return;

        if (kanjiConverter.StartConversion(preConversionHiragana))
        {
            currentState = STATE_CONVERT;
            UpdateCandidateDisplay();
        }
        else
        {
            // Conversion failed or empty, just output hiragana
            OutputText(preConversionHiragana);
            ClearDisplay();
        }
    }

    private void CommitConversion()
    {
        if (kanjiConverter == null) return;

        string result = kanjiConverter.GetCurrentCandidate();
        string remainder = kanjiConverter.GetRemainingReading();
        
        Debug.Log($"[IMEController] CommitConversion: result=[{result}] remainder=[{remainder}] committedSoFar=[{committedText}]");
        
        // Accumulate confirmed text
        committedText += result;
        kanjiConverter.Reset();
        
        if (!string.IsNullOrEmpty(remainder))
        {
            // Try conversion on the remaining reading
            if (kanjiConverter.StartConversion(remainder))
            {
                preConversionHiragana = remainder;
                UpdateCandidateDisplay();
                // Stay in STATE_CONVERT
            }
            else
            {
                // No conversion possible for remainder, finish everything
                committedText += remainder;
                CommitAllAndFinish();
            }
        }
        else
        {
            // No remainder, output everything
            CommitAllAndFinish();
        }
    }

    private void CommitAllAndFinish()
    {
        if (!string.IsNullOrEmpty(committedText))
        {
            OutputText(committedText);
        }
        committedText = "";
        preConversionHiragana = "";
        currentState = STATE_INPUT;
        ClearDisplay();
    }

    private void CancelConversion()
    {
        if (kanjiConverter != null) kanjiConverter.Reset();
        currentState = STATE_INPUT;
        
        // If we had accumulated text, output it and show remaining hiragana
        if (!string.IsNullOrEmpty(committedText))
        {
            OutputText(committedText);
            committedText = "";
        }
        
        if (inputDisplayText != null)
        {
            inputDisplayText.text = preConversionHiragana;
        }
    }

    private void OutputText(string text)
    {
        if (targetInputField != null)
        {
            targetInputField.text += text;
        }
        Debug.Log($"[IMEController] Output: {text}");
    }

    private void UpdateDisplay()
    {
        if (inputDisplayText != null && romajiConverter != null)
        {
            inputDisplayText.text = romajiConverter.GetDisplayText();
        }
        if (candidateDisplayText != null)
        {
            candidateDisplayText.text = "";
        }
    }

    private void UpdateCandidateDisplay()
    {
        if (kanjiConverter == null) return;

        // Show current candidate in input area (preview)
        if (inputDisplayText != null)
        {
            string current = kanjiConverter.GetCurrentCandidate();
            string remain = kanjiConverter.GetRemainingReading();
            // Show: confirmed text + [current candidate] + remaining reading
            inputDisplayText.text = committedText + current + remain;
        }

        // Show list in candidate area
        if (candidateDisplayText != null)
        {
            string[] candidates = kanjiConverter.GetAllCandidates();
            int currentIndex = kanjiConverter.GetCurrentCandidateIndex();
            
            string display = "";
            int maxShow = 9;
            int count = candidates != null ? Mathf.Min(candidates.Length, maxShow) : 0;

            for (int i = 0; i < count; i++)
            {
                string marker = (i == currentIndex) ? "<color=yellow>" : "";
                string endMarker = (i == currentIndex) ? "</color>" : "";
                display += $"{marker}{i+1}. {candidates[i]}{endMarker}  ";
            }
            if (candidates != null && candidates.Length > maxShow)
            {
                display += "...";
            }
            
            candidateDisplayText.text = display;
        }
    }

    private void UpdateStatusDisplay()
    {
        if (imeStatusText != null)
        {
            imeStatusText.text = imeEnabled ? "あ" : "A";
        }
    }

    private void ClearDisplay()
    {
        if (inputDisplayText != null) inputDisplayText.text = "";
        if (candidateDisplayText != null) candidateDisplayText.text = "";
        committedText = "";
    }
}
