
using System.Collections.Generic;
using System.IO;
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

        // 外側の配列の括弧を取り除く
        string trimmedJson = json.Trim().TrimStart('[').TrimEnd(']');

        // 個々のオブジェクトに分割
        string[] jsonObjects = Regex.Split(trimmedJson, @"}\s*,\s*{");
        
        foreach (var jsonObject in jsonObjects)
        {
            Target target = new Target();
            string cleanedJsonObject = jsonObject.Trim().TrimStart('{').TrimEnd('}');

            // キーと値のペアを解析
            var matches = Regex.Matches(cleanedJsonObject, "\"(.*?)\"\\s*:\\s*(true|false|\".*?\"|[\\d]+)");
            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value.Trim('"');

                switch (key)
                {
                    case "RootPath":
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
                }
            }
            targets.Add(target);
        }

        return targets;
    }
}

internal class Target
{
    public string RootPath { get; set; }
    public char CSVSeparator { get; set; }
    public string NameSpace { get; set; }
    public bool Serializable { get; set; }
    public bool InterfaceEnable { get; set; }
    public string InterfaceName { get; set; }
} 