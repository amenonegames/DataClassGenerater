using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DataClassGenerator;

public struct CsvInfo : IEquatable<CsvInfo> 
{
    public readonly bool requiredInterface;
    public readonly bool serializable;
    public readonly string nameSpace;
    public readonly string interfaceName;
    public readonly string fileName;
    public readonly PropertyInfo[] PropertyInfos;
    public readonly string[] usings;
    public readonly bool EnableNameSpace => !string.IsNullOrEmpty(nameSpace);

    public readonly bool settingExist = true;
    
    public CsvInfo( AdditionalText additionalText , ConfigData configData)
    {
        fileName = Path.GetFileNameWithoutExtension(additionalText.Path);
        Target? targetSetting = null;
        if (configData.Targets != null)
        {
            targetSetting = configData.Targets.FirstOrDefault(target => PathContainsTargetRootPath(additionalText.Path,target.RootPath));
        }
        
        if( targetSetting == null)
        {
            PropertyInfos = new PropertyInfo[] { }; 
            requiredInterface = false;
            interfaceName = string.Empty;
            nameSpace = string.Empty;
            serializable = false;
            settingExist = false;
            usings = new string[] { };
            return;
        }

        var fullText = additionalText.GetText()?.ToString();

        if (fullText != null)
        {
            using StringReader reader = new StringReader(fullText);
        
            var firstLine = reader.ReadLine();
            reader.Peek();
            var secondLine = reader.ReadLine();
        
            if (firstLine != null && targetSetting != null && secondLine != null)
            {
                var propertyNameColumns = firstLine.Split(targetSetting.CSVSeparator);
                var typeColumns = secondLine.Split(targetSetting.CSVSeparator);
                PropertyInfos = propertyNameColumns.Zip(typeColumns, (propertyName, type) => new PropertyInfo(propertyName, type)).ToArray();
            }
            else
            {
                PropertyInfos = new PropertyInfo[] { }; 
            }
        }
        else
        {
            PropertyInfos = new PropertyInfo[] { }; 
        }

        requiredInterface = targetSetting?.InterfaceEnable ?? false;
        interfaceName = targetSetting?.InterfaceName ?? string.Empty;
        nameSpace = targetSetting?.NameSpace ?? string.Empty;
        serializable = targetSetting?.Serializable ?? false;
        usings = targetSetting?.Usings ?? new string[] { };
        
    }
    
    private static bool PathContainsTargetRootPath(string pathA, string pathB)
    {
        var longerPath = pathA.Length > pathB.Length ? pathA : pathB;
        var shorterPath = pathA.Length > pathB.Length ? pathB : pathA;
        
        string[] longerPathArray = longerPath.Split(Path.DirectorySeparatorChar);
        string[] shorterPathArray = shorterPath.Split(Path.DirectorySeparatorChar);
        
        for (var i = 0 ; i < longerPathArray.Length; i++)
        {
            var pathInLonger = longerPathArray[i];
            if (shorterPathArray.Contains(pathInLonger))
            {
                for(var j = 0; j < shorterPathArray.Length; j++)
                {
                    var pathInShorter = shorterPathArray[j];
                    pathInLonger = longerPathArray[i];
                    
                    if (pathInShorter != pathInLonger)
                    {
                        return false;
                    }

                    i++;
                }
                return true;
            }
        }

        return false;

    }


    public bool Equals(CsvInfo other)
    {
        return requiredInterface == other.requiredInterface && serializable == other.serializable && nameSpace == other.nameSpace && interfaceName == other.interfaceName && fileName == other.fileName && PropertyInfos.Equals(other.PropertyInfos) && usings.Equals(other.usings) && settingExist == other.settingExist;
    }

    public override bool Equals(object? obj)
    {
        return obj is CsvInfo other && Equals(other);
    }
    
    public bool PropertiesEquals(CsvInfo other)
    {
        return PropertyInfos.SequenceEqual(other.PropertyInfos);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = requiredInterface.GetHashCode();
            hashCode = (hashCode * 397) ^ serializable.GetHashCode();
            hashCode = (hashCode * 397) ^ nameSpace.GetHashCode();
            hashCode = (hashCode * 397) ^ interfaceName.GetHashCode();
            hashCode = (hashCode * 397) ^ fileName.GetHashCode();
            hashCode = (hashCode * 397) ^ PropertyInfos.GetHashCode();
            hashCode = (hashCode * 397) ^ usings.GetHashCode();
            hashCode = (hashCode * 397) ^ settingExist.GetHashCode();
            return hashCode;
        }
    }
}


public struct PropertyInfo : IEquatable<PropertyInfo>
{
    public string PropertyName { get; set; }
    public string TypeName { get; set; }
    
    public PropertyInfo(string propertyName, string typeName)
    {
        PropertyName = propertyName;
        TypeName = typeName;
    }

    public bool Equals(PropertyInfo other)
    {
        return PropertyName == other.PropertyName && TypeName == other.TypeName;
    }

    public override bool Equals(object? obj)
    {
        return obj is PropertyInfo info && Equals(info);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (PropertyName.GetHashCode() * 397) ^ TypeName.GetHashCode();
        }
    }
}