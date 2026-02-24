
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// ローマ字をひらがなに変換するコンバーター
/// QWERTY配列でのローマ字入力を日本語ひらがなに変換
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class RomajiConverter : UdonSharpBehaviour
{
    // ローマ字→ひらがな変換テーブル
    // 長いパターンから順に検索するためにソート済み
    private string[] romajiPatterns;
    private string[] hiraganaValues;
    
    // 入力バッファ
    private string inputBuffer = "";
    
    // 変換結果
    private string convertedText = "";
    
    void Start()
    {
        InitializeConversionTable();
    }
    
    private void InitializeConversionTable()
    {
        // 長いパターンから順に並べる（最長一致のため）
        romajiPatterns = new string[]
        {
            // 4文字パターン
            "ltsu", "xtsu", "ltu", "xtu",
            
            // 3文字パターン（拗音など）
            "sha", "shi", "shu", "she", "sho",
            "cha", "chi", "chu", "che", "cho",
            "tsu", "thi",
            "dha", "dhi", "dhu", "dhe", "dho",
            "jya", "jyi", "jyu", "jye", "jyo",
            "kya", "kyi", "kyu", "kye", "kyo",
            "gya", "gyi", "gyu", "gye", "gyo",
            "sya", "syi", "syu", "sye", "syo",
            "zya", "zyi", "zyu", "zye", "zyo",
            "tya", "tyi", "tyu", "tye", "tyo",
            "nya", "nyi", "nyu", "nye", "nyo",
            "hya", "hyi", "hyu", "hye", "hyo",
            "bya", "byi", "byu", "bye", "byo",
            "pya", "pyi", "pyu", "pye", "pyo",
            "mya", "myi", "myu", "mye", "myo",
            "rya", "ryi", "ryu", "rye", "ryo",
            "lya", "lyi", "lyu", "lye", "lyo",
            "xya", "xyi", "xyu", "xye", "xyo",
            "wha", "whi", "whu", "whe", "who",
            "tsa", "tsi", "tse", "tso",
            "tha", "thu", "the", "tho",
            "dya", "dyi", "dyu", "dye", "dyo",
            "xwa", "xka", "xke",
            "fya", "fyi", "fyu", "fye", "fyo",
            "vya", "vyi", "vyu", "vye", "vyo",
            
            // 2文字パターン
            "ka", "ki", "ku", "ke", "ko",
            "sa", "si", "su", "se", "so",
            "ta", "ti", "tu", "te", "to",
            "na", "ni", "nu", "ne", "no",
            "ha", "hi", "hu", "he", "ho",
            "ma", "mi", "mu", "me", "mo",
            "ya", "yi", "yu", "ye", "yo",
            "ra", "ri", "ru", "re", "ro",
            "wa", "wi", "wu", "we", "wo",
            "ga", "gi", "gu", "ge", "go",
            "za", "zi", "zu", "ze", "zo",
            "da", "di", "du", "de", "do",
            "ba", "bi", "bu", "be", "bo",
            "pa", "pi", "pu", "pe", "po",
            "fa", "fi", "fu", "fe", "fo",
            "ja", "ji", "ju", "je", "jo",
            "va", "vi", "vu", "ve", "vo",
            "la", "li", "lu", "le", "lo",
            "xa", "xi", "xu", "xe", "xo",
            "nn", "n'",
            
            // 1文字パターン
            "a", "i", "u", "e", "o"
        };
        
        hiraganaValues = new string[]
        {
            // 4文字パターン
            "っ", "っ", "っ", "っ",
            
            // 3文字パターン
            "しゃ", "し", "しゅ", "しぇ", "しょ",
            "ちゃ", "ち", "ちゅ", "ちぇ", "ちょ",
            "つ", "てぃ",
            "でゃ", "でぃ", "でゅ", "でぇ", "でょ",
            "じゃ", "じぃ", "じゅ", "じぇ", "じょ",
            "きゃ", "きぃ", "きゅ", "きぇ", "きょ",
            "ぎゃ", "ぎぃ", "ぎゅ", "ぎぇ", "ぎょ",
            "しゃ", "しぃ", "しゅ", "しぇ", "しょ",
            "じゃ", "じぃ", "じゅ", "じぇ", "じょ",
            "ちゃ", "ちぃ", "ちゅ", "ちぇ", "ちょ",
            "にゃ", "にぃ", "にゅ", "にぇ", "にょ",
            "ひゃ", "ひぃ", "ひゅ", "ひぇ", "ひょ",
            "びゃ", "びぃ", "びゅ", "びぇ", "びょ",
            "ぴゃ", "ぴぃ", "ぴゅ", "ぴぇ", "ぴょ",
            "みゃ", "みぃ", "みゅ", "みぇ", "みょ",
            "りゃ", "りぃ", "りゅ", "りぇ", "りょ",
            "ゃ", "ぃ", "ゅ", "ぇ", "ょ",
            "ゃ", "ぃ", "ゅ", "ぇ", "ょ",
            "うぁ", "うぃ", "う", "うぇ", "うぉ",
            "つぁ", "つぃ", "つぇ", "つぉ",
            "てゃ", "てゅ", "てぇ", "てょ",
            "ぢゃ", "ぢぃ", "ぢゅ", "ぢぇ", "ぢょ",
            "ゎ", "ヵ", "ヶ",
            "ふゃ", "ふぃ", "ふゅ", "ふぇ", "ふょ",
            "ゔゃ", "ゔぃ", "ゔゅ", "ゔぇ", "ゔょ",
            
            // 2文字パターン
            "か", "き", "く", "け", "こ",
            "さ", "し", "す", "せ", "そ",
            "た", "ち", "つ", "て", "と",
            "な", "に", "ぬ", "ね", "の",
            "は", "ひ", "ふ", "へ", "ほ",
            "ま", "み", "む", "め", "も",
            "や", "い", "ゆ", "いぇ", "よ",
            "ら", "り", "る", "れ", "ろ",
            "わ", "うぃ", "う", "うぇ", "を",
            "が", "ぎ", "ぐ", "げ", "ご",
            "ざ", "じ", "ず", "ぜ", "ぞ",
            "だ", "ぢ", "づ", "で", "ど",
            "ば", "び", "ぶ", "べ", "ぼ",
            "ぱ", "ぴ", "ぷ", "ぺ", "ぽ",
            "ふぁ", "ふぃ", "ふ", "ふぇ", "ふぉ",
            "じゃ", "じ", "じゅ", "じぇ", "じょ",
            "ゔぁ", "ゔぃ", "ゔ", "ゔぇ", "ゔぉ",
            "ぁ", "ぃ", "ぅ", "ぇ", "ぉ",
            "ぁ", "ぃ", "ぅ", "ぇ", "ぉ",
            "ん", "ん",
            
            // 1文字パターン
            "あ", "い", "う", "え", "お"
        };
    }
    
    /// <summary>
    /// ローマ字を入力に追加して変換
    /// </summary>
    public string AddInput(string input)
    {
        inputBuffer += input.ToLower();
        return ProcessBuffer();
    }
    
    /// <summary>
    /// バックスペース処理
    /// </summary>
    public void Backspace()
    {
        if (inputBuffer.Length > 0)
        {
            inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
        }
        else if (convertedText.Length > 0)
        {
            convertedText = convertedText.Substring(0, convertedText.Length - 1);
        }
    }
    
    /// <summary>
    /// 入力をクリア
    /// </summary>
    public void Clear()
    {
        inputBuffer = "";
        convertedText = "";
    }
    
    /// <summary>
    /// 現在の変換テキストを取得
    /// </summary>
    public string GetConvertedText()
    {
        return convertedText;
    }
    
    /// <summary>
    /// 未変換のローマ字バッファを取得
    /// </summary>
    public string GetInputBuffer()
    {
        return inputBuffer;
    }
    
    /// <summary>
    /// 変換テキスト + 未変換バッファを取得
    /// </summary>
    public string GetDisplayText()
    {
        return convertedText + inputBuffer;
    }
    
    /// <summary>
    /// 変換を確定して結果を返す
    /// </summary>
    public string Commit()
    {
        // 残りの'n'を'ん'に変換
        if (inputBuffer == "n")
        {
            convertedText += "ん";
            inputBuffer = "";
        }
        
        string result = convertedText + inputBuffer;
        Clear();
        return result;
    }
    
    private string ProcessBuffer()
    {
        bool converted = true;
        
        while (converted && inputBuffer.Length > 0)
        {
            converted = false;

            if (inputBuffer[0] == '-')
            {
                convertedText += "ー";
                inputBuffer = inputBuffer.Substring(1);
                converted = true;
                continue;
            }
            
            // 促音処理（子音の連続）
            if (inputBuffer.Length >= 2)
            {
                char first = inputBuffer[0];
                char second = inputBuffer[1];
                
                // 同じ子音が連続（nn以外）
                if (first == second && first != 'n' && first != 'a' && first != 'i' && 
                    first != 'u' && first != 'e' && first != 'o')
                {
                    convertedText += "っ";
                    inputBuffer = inputBuffer.Substring(1);
                    converted = true;
                    continue;
                }
            }
            
            // 'n' + 子音 で 'ん' に変換
            if (inputBuffer.Length >= 2 && inputBuffer[0] == 'n')
            {
                char next = inputBuffer[1];
                if (next != 'a' && next != 'i' && next != 'u' && next != 'e' && next != 'o' &&
                    next != 'y' && next != 'n')
                {
                    convertedText += "ん";
                    inputBuffer = inputBuffer.Substring(1);
                    converted = true;
                    continue;
                }
            }
            
            // パターンマッチング（長いパターンから）
            for (int i = 0; i < romajiPatterns.Length; i++)
            {
                string pattern = romajiPatterns[i];
                
                if (inputBuffer.Length >= pattern.Length &&
                    inputBuffer.Substring(0, pattern.Length) == pattern)
                {
                    convertedText += hiraganaValues[i];
                    inputBuffer = inputBuffer.Substring(pattern.Length);
                    converted = true;
                    break;
                }
            }
        }
        
        return convertedText;
    }
}
