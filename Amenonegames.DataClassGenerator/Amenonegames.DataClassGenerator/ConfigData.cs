
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataClassGenerator;

public class ConfigData
{
    internal Target[]? Targets { get; set; }
    
    public ConfigData(string jsonStr)
    {
        Targets = ParseJson(jsonStr).ToArray();

        foreach (var target in Targets)
        {
            var directrySeparator = Path.DirectorySeparatorChar;
            if (directrySeparator != '/')
            {
                target.RootPath = target.RootPath.Replace('/', Path.DirectorySeparatorChar );
            }
            else  if (directrySeparator != '\\')
            {
                target.RootPath = target.RootPath.Replace('\\', Path.DirectorySeparatorChar );
            }
            
        }
    }
    
    private List<Target> ParseJson(string json)
    {
        List<Target> targets = new List<Target>();
        
        string caretTrim = json.Trim().TrimStart('{').TrimEnd('}');
        string settingsTrim = RemovePrefix(caretTrim, "\"settings\":");
        // 外側の配列の括弧を取り除く
        string trimmedJson = settingsTrim.Trim().TrimStart('[').TrimEnd(']');

        // 個々のオブジェクトに分割
        string[] jsonObjects = Regex.Split(trimmedJson, @"}\s*,\s*{");
        
        foreach (var jsonObject in jsonObjects)
        {
            Target target = new Target();
            string cleanedJsonObject = jsonObject.Trim().TrimStart('{').TrimEnd('}');

            // キーと値のペアを解析
            var matches = Regex.Matches(cleanedJsonObject, "\"(.*?)\"\\s*:\\s*(true|false|\".*?\"|[\\d]+|\\[.*\\])",RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value.Trim('"');

                switch (key)
                {
                    case "FilePath":
                        target.RootPath = value;
                        break;
                    case "CSVSeparator":  // 修正したCSVSeparatorのスペルミス
                        target.CSVSeparator = value[0];
                        break;
                    case "NameSpace":
                        target.NameSpace = value;
                        break;
                    case "Serializable":
                        target.Serializable = bool.Parse(value.ToLower());
                        break;
                    case "InterfaceEnable":
                        target.InterfaceEnable = bool.Parse(value.ToLower());
                        break;
                    case "InterfaceName":
                        target.InterfaceName = value;
                        break;
                    case "Usings":
                        string arrayStr = value.Trim().Trim(' ', '\n', '\t', '[', ']');
                        string[] rawArrayStr = arrayStr.Split(',');
                        target.Usings = rawArrayStr.Select( str => str.Trim(' ', '\r','\n', '\t','"')).ToArray();
                        break;
                }
            }
            targets.Add(target);
        }

        return targets;
    }
    
    private static string RemovePrefix(string input, string prefix)
    {
        // Trim() を使って文頭の空白を削除
        string trimmedInput = input.Trim();
        
        // 文頭に prefix があるかどうか確認
        if (trimmedInput.StartsWith(prefix))
        {
            // prefix の長さ分だけ文字列を削除
            return trimmedInput.Substring(prefix.Length).TrimStart();
        }
        
        // prefix がなければ、元の文字列をそのまま返す
        return input;
    }
}

internal class Target
{
    public string RootPath { get; set; } = string.Empty;
    public char CSVSeparator { get; set; } = ',';
    public string NameSpace { get; set; } = string.Empty;
    public bool Serializable { get; set; }
    public bool InterfaceEnable { get; set; }
    public string InterfaceName { get; set; } = string.Empty;
    public string[] Usings { get; set; } = Array.Empty<string>();
} 