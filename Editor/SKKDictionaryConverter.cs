using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Net;

/// <summary>
/// SKK辞書をダウンロードしてUdon用フォーマットに変換するエディタツール
/// </summary>
public class SKKDictionaryConverter : EditorWindow
{
    private const string SKK_JISYO_L_URL = "https://raw.githubusercontent.com/skk-dev/dict/master/SKK-JISYO.L";
    private const string OUTPUT_PATH = "Assets/JapaneseIME/Resources/dictionary.txt";
    
    private bool isDownloading = false;
    private string statusMessage = "";
    
    [MenuItem("Tools/Japanese IME/Download SKK Dictionary")]
    public static void ShowWindow()
    {
        GetWindow<SKKDictionaryConverter>("SKK Dictionary Converter");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("SKK辞書ダウンロード & 変換", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "SKK-JISYO.L（大サイズ）をダウンロードして\nUdon用のフォーマットに変換します。",
            MessageType.Info
        );
        
        EditorGUILayout.Space();
        
        EditorGUI.BeginDisabledGroup(isDownloading);
        if (GUILayout.Button("ダウンロード & 変換", GUILayout.Height(40)))
        {
            DownloadAndConvert();
        }
        EditorGUI.EndDisabledGroup();
        
        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(statusMessage, MessageType.None);
        }
    }
    
    private void DownloadAndConvert()
    {
        isDownloading = true;
        statusMessage = "ダウンロード中...";
        
        try
        {
            // SKK辞書をダウンロード
            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.GetEncoding("EUC-JP");
                string rawData = client.DownloadString(SKK_JISYO_L_URL);
                
                statusMessage = "変換中...";
                
                // 変換処理
                var entries = ParseSKKDictionary(rawData);
                
                // 出力
                SaveDictionary(entries);
                
                statusMessage = $"完了！ {entries.Count} エントリを変換しました。\n出力: {OUTPUT_PATH}";
            }
        }
        catch (System.Exception e)
        {
            statusMessage = $"エラー: {e.Message}";
            Debug.LogError(e);
        }
        finally
        {
            isDownloading = false;
        }
    }
    
    private Dictionary<string, List<string>> ParseSKKDictionary(string rawData)
    {
        var entries = new Dictionary<string, List<string>>();
        var lines = rawData.Split('\n');
        
        foreach (var line in lines)
        {
            // コメント行とヘッダをスキップ
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
                continue;
            
            // SKK形式: よみ /候補1/候補2/候補3/
            int spaceIndex = line.IndexOf(' ');
            if (spaceIndex <= 0) continue;
            
            string reading = line.Substring(0, spaceIndex);
            string candidatePart = line.Substring(spaceIndex + 1);
            
            // 送り仮名付きエントリをスキップ（アルファベットで終わるもの）
            if (reading.Length > 0 && char.IsLetter(reading[reading.Length - 1]) && 
                reading[reading.Length - 1] < 128)
                continue;
            
            // 候補をパース
            var candidates = new List<string>();
            var parts = candidatePart.Split('/');
            
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;
                
                // アノテーション（;以降）を除去
                string candidate = part;
                int annotationIndex = candidate.IndexOf(';');
                if (annotationIndex > 0)
                {
                    candidate = candidate.Substring(0, annotationIndex);
                }
                
                // 特殊な候補（記号のみなど）をスキップ
                if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= 20)
                {
                    candidates.Add(candidate);
                }
            }
            
            if (candidates.Count > 0)
            {
                if (entries.ContainsKey(reading))
                {
                    // 既存エントリに追加
                    foreach (var c in candidates)
                    {
                        if (!entries[reading].Contains(c))
                        {
                            entries[reading].Add(c);
                        }
                    }
                }
                else
                {
                    entries[reading] = candidates;
                }
            }
        }
        
        return entries;
    }
    
    private void SaveDictionary(Dictionary<string, List<string>> entries)
    {
        // 出力ディレクトリを確保
        string dir = Path.GetDirectoryName(OUTPUT_PATH);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        
        // タブ区切り形式で出力: よみ\t候補1,候補2,候補3
        var sb = new StringBuilder();
        foreach (var kvp in entries)
        {
            sb.Append(kvp.Key);
            sb.Append('\t');
            sb.Append(string.Join(",", kvp.Value));
            sb.Append('\n');
        }
        
        File.WriteAllText(OUTPUT_PATH, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
    }
}
