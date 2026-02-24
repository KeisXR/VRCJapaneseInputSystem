using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Handles Kana-to-Kanji conversion using a dictionary loaded from Resources.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class KanjiConverter : UdonSharpBehaviour
{
    [Header("Dictionary Settings")]
    [Tooltip("Assign the dictionary.txt file here")]
    [SerializeField] private TextAsset dictionaryFile;
    
    // Arrays to store the dictionary (Parallel Arrays for Udon optimization)
    // dictionaryKeys: "reading"
    // dictionaryValues: "candidate1,candidate2,candidate3..."
    private string[] dictionaryKeys;
    private string[] dictionaryValues;
    private int dictionarySize = 0;
    
    // State
    private bool isInitialized = false;
    private string fullReading = "";
    private string currentReading = "";
    private int matchedReadingLength = 0;
    private string[] currentCandidates;
    private int candidateIndex = 0;

    private const int MAX_PREFIX_MATCHES = 30;

    void Start()
    {
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        if (isInitialized) return;

        if (dictionaryFile == null)
        {
            Debug.LogError($"[KanjiConverter] Dictionary file is not assigned!");
            return;
        }

        string[] lines = dictionaryFile.text.Split('\n');
        int validLines = 0;

        // Count valid lines first for array allocation
        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line) && line.Contains("\t"))
            {
                validLines++;
            }
        }

        dictionaryKeys = new string[validLines];
        dictionaryValues = new string[validLines];
        dictionarySize = 0;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            // Format: Reading\tEx1,Ex2,Ex3...
            int tabIndex = line.IndexOf('\t');
            if (tabIndex > 0)
            {
                dictionaryKeys[dictionarySize] = line.Substring(0, tabIndex);
                dictionaryValues[dictionarySize] = line.Substring(tabIndex + 1);
                dictionarySize++;
            }
        }

        Debug.Log($"[KanjiConverter] Loaded {dictionarySize} entries.");
        isInitialized = true;
    }

    public bool StartConversion(string reading)
    {
        if (!isInitialized)
        {
            InitializeDictionary();
            if (!isInitialized) return false;
        }

        if (string.IsNullOrEmpty(reading))
        {
            Reset();
            return false;
        }

        fullReading = reading;
        candidateIndex = 0;
        
        Debug.Log($"[KanjiConverter] StartConversion: reading=[{reading}] len={reading.Length}");
        
        // 1. Try exact match
        int matchIndex = FindMatchInDictionary(reading);
        if (matchIndex != -1)
        {
            Debug.Log($"[KanjiConverter] Exact match found at index {matchIndex}: key=[{dictionaryKeys[matchIndex]}]");
            SetupCandidates(reading, matchIndex);
            return true;
        }

        // 2. Try prefix match with 2-step look-ahead scoring
        // Collect all prefix matches, then score using (prefix_len + next_segment_len)
        int[] prefixMatchLengths = new int[MAX_PREFIX_MATCHES];
        int[] prefixMatchIndices = new int[MAX_PREFIX_MATCHES];
        int prefixCount = 0;

        for (int i = 0; i < dictionarySize; i++)
        {
            string key = dictionaryKeys[i];
            if (key.Length < reading.Length && StartsWithOrdinal(reading, key))
            {
                if (prefixCount < MAX_PREFIX_MATCHES)
                {
                    prefixMatchLengths[prefixCount] = key.Length;
                    prefixMatchIndices[prefixCount] = i;
                    prefixCount++;
                }
            }
        }

        if (prefixCount > 0)
        {
            int bestScore = -1;
            int bestIdx = -1;
            int bestPrefLen = int.MaxValue;

            for (int p = 0; p < prefixCount; p++)
            {
                int prefLen = prefixMatchLengths[p];
                string remainder = reading.Substring(prefLen);
                int nextLen = FindLongestPrefixLength(remainder);
                int score = prefLen + nextLen;

                // Prefer higher total score; on tie, prefer shorter first segment
                // (isolates particles like は, が, を correctly)
                if (score > bestScore || (score == bestScore && prefLen < bestPrefLen))
                {
                    bestScore = score;
                    bestIdx = prefixMatchIndices[p];
                    bestPrefLen = prefLen;
                }
            }

            if (bestIdx != -1)
            {
                Debug.Log($"[KanjiConverter] Look-ahead match: key=[{dictionaryKeys[bestIdx]}] prefLen={bestPrefLen} score={bestScore}");
                SetupCandidates(dictionaryKeys[bestIdx], bestIdx);
                return true;
            }
        }

        // 3. Fallback: No conversion, just use the hiragana itself as candidate
        Debug.Log($"[KanjiConverter] Fallback: no match for [{reading}]");
        currentReading = reading;
        matchedReadingLength = reading.Length;
        currentCandidates = new string[] { reading }; // Fallback array
        
        // Attempt to create Katakana fallback
        string katakana = HiraganaToKatakana(reading);
        if (katakana != reading)
        {
             // Add katakana to candidates if different
             string[] newCandidates = new string[2];
             newCandidates[0] = reading;
             newCandidates[1] = katakana;
             currentCandidates = newCandidates;
        }

        return true;
    }

    private int FindMatchInDictionary(string key)
    {
        for (int i = 0; i < dictionarySize; i++)
        {
            if (dictionaryKeys[i] == key) return i;
        }
        return -1;
    }

    /// <summary>
    /// Find the length of the longest dictionary key that is a prefix of the given text.
    /// Used for look-ahead scoring.
    /// </summary>
    private int FindLongestPrefixLength(string text)
    {
        int longest = 0;
        for (int i = 0; i < dictionarySize; i++)
        {
            string key = dictionaryKeys[i];
            if (key.Length > longest && StartsWithOrdinal(text, key))
            {
                longest = key.Length;
            }
        }
        return longest;
    }

    private void SetupCandidates(string key, int index)
    {
        currentReading = key;
        matchedReadingLength = key.Length;
        
        string val = dictionaryValues[index];
        string[] rawCandidates = val.Split(',');
        
        // Katakana conversion
        string katakana = HiraganaToKatakana(key);
        
        // Build candidate list: hiragana first, then dictionary entries, then katakana last
        // Hiragana first is critical for particles (は, が, を, etc.)
        int extraCount = 2; // hiragana + katakana
        if (katakana == key) extraCount = 1; // no katakana needed if same
        
        currentCandidates = new string[rawCandidates.Length + extraCount];
        
        // Index 0: dictionary first candidate (most common kanji)
        // Index 1: hiragana reading itself
        // Index 2+: rest of dictionary candidates (trimmed)
        // Last: katakana
        currentCandidates[0] = rawCandidates[0].Trim();
        currentCandidates[1] = key; // hiragana as second candidate
        
        int offset = 2;
        for (int i = 1; i < rawCandidates.Length; i++)
        {
            currentCandidates[offset] = rawCandidates[i].Trim();
            offset++;
        }
        
        // Last: katakana (if different from hiragana)
        if (katakana != key)
        {
            currentCandidates[offset] = katakana;
        }
        
        Debug.Log($"[KanjiConverter] SetupCandidates: key=[{key}] count={currentCandidates.Length} first=[{currentCandidates[0]}] second=[{currentCandidates[1]}]");
    }

    // Helper: Simple Hiragana to Katakana (basic range shift)
    private string HiraganaToKatakana(string hiragana)
    {
        char[] chars = hiragana.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= 0x3041 && chars[i] <= 0x3096)
            {
                chars[i] = (char)(chars[i] + 0x60);
            }
        }
        return new string(chars);
    }

    /// <summary>
    /// Ordinal (character-by-character) StartsWith check.
    /// .NET's String.StartsWith uses culture-sensitive comparison by default,
    /// which causes false matches with Japanese characters
    /// (e.g. ー U+30FC matching は U+306F).
    /// This method avoids that issue entirely.
    /// </summary>
    private bool StartsWithOrdinal(string text, string prefix)
    {
        if (prefix.Length > text.Length) return false;
        for (int i = 0; i < prefix.Length; i++)
        {
            if (text[i] != prefix[i]) return false;
        }
        return true;
    }

    // Interface methods required by IMEController

    public string GetCurrentReading()
    {
        return currentReading;
    }

    public string GetCurrentReadingAsKatakana()
    {
        if (string.IsNullOrEmpty(currentReading)) return "";
        string result = HiraganaToKatakana(currentReading);
        Debug.Log($"[KanjiConverter] GetCurrentReadingAsKatakana: reading=[{currentReading}] result=[{result}]");
        return result;
    }

    /// <summary>
    /// Convert any hiragana text to katakana. Used for direct katakana commit from input mode.
    /// </summary>
    public string GetKatakanaFor(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return HiraganaToKatakana(text);
    }

    public string NextCandidate()
    {
        if (currentCandidates == null || currentCandidates.Length == 0) return fullReading;
        candidateIndex = (candidateIndex + 1) % currentCandidates.Length;
        return currentCandidates[candidateIndex];
    }
    
    public string PreviousCandidate()
    {
        if (currentCandidates == null || currentCandidates.Length == 0) return fullReading;
        candidateIndex--;
        if (candidateIndex < 0) candidateIndex = currentCandidates.Length - 1;
        return currentCandidates[candidateIndex];
    }

    public string GetCurrentCandidate()
    {
         if (currentCandidates == null || currentCandidates.Length == 0) return fullReading;
         if (candidateIndex >= 0 && candidateIndex < currentCandidates.Length)
             return currentCandidates[candidateIndex];
         return "";
    }

    public string GetRemainingReading()
    {
        if (string.IsNullOrEmpty(fullReading)) return "";
        if (matchedReadingLength >= fullReading.Length) return "";
        return fullReading.Substring(matchedReadingLength);
    }

    public string[] GetAllCandidates()
    {
        return currentCandidates;
    }

    public int GetCurrentCandidateIndex()
    {
        return candidateIndex;
    }
    
    public bool TrySelectCandidate(int index)
    {
        if (currentCandidates == null) return false;
        if (index >= 0 && index < currentCandidates.Length)
        {
            candidateIndex = index;
            return true;
        }
        return false;
    }

    // --- Segment Length Control ---

    public int GetMatchedReadingLength()
    {
        return matchedReadingLength;
    }

    /// <summary>
    /// Shrink the current segment by 1 character.
    /// </summary>
    public bool ShrinkSegment()
    {
        if (string.IsNullOrEmpty(fullReading) || matchedReadingLength <= 1) return false;
        return AdjustSegmentLength(matchedReadingLength - 1);
    }

    /// <summary>
    /// Extend the current segment by 1 character.
    /// </summary>
    public bool ExtendSegment()
    {
        if (string.IsNullOrEmpty(fullReading) || matchedReadingLength >= fullReading.Length) return false;
        return AdjustSegmentLength(matchedReadingLength + 1);
    }

    private bool AdjustSegmentLength(int newLength)
    {
        if (string.IsNullOrEmpty(fullReading)) return false;
        if (newLength < 1 || newLength > fullReading.Length) return false;

        string segment = fullReading.Substring(0, newLength);
        candidateIndex = 0;

        // Try dictionary match at this length
        int matchIndex = FindMatchInDictionary(segment);
        if (matchIndex != -1)
        {
            SetupCandidates(segment, matchIndex);
            return true;
        }

        // No dictionary match — use hiragana as-is with katakana fallback
        currentReading = segment;
        matchedReadingLength = newLength;

        string katakana = HiraganaToKatakana(segment);
        if (katakana != segment)
        {
            currentCandidates = new string[] { segment, katakana };
        }
        else
        {
            currentCandidates = new string[] { segment };
        }
        return true;
    }

    public void Reset()
    {
        fullReading = "";
        currentReading = "";
        matchedReadingLength = 0;
        currentCandidates = null;
        candidateIndex = 0;
    }
}
