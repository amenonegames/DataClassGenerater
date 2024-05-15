using System;
using System.IO;
using System.Linq;
using System.Text;
using Amenonegames.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace DataClassGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class SourceGenerator : IIncrementalGenerator
    {

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var compilation = context.CompilationProvider;
            

            IncrementalValueProvider<ConfigData> configDataProvider = context.AdditionalTextsProvider.Where
            (
                static additionalText => additionalText.Path.EndsWith("CsvToDataSettings.json")
            ).Select
            (
                static (configText,token) => new ConfigData( configText.GetText()!.ToString() )
            ).Collect()
            .Select( static (configs , token )=> configs.First());

            var csvProvider = context.AdditionalTextsProvider.Where
                (static additionalText => additionalText.Path.EndsWith(".csv"))
                .Combine<AdditionalText,ConfigData>(configDataProvider);
                
            var dataProvider = csvProvider.Where
                (
                    static data =>
                    {
                        var (additionalText, config) = data;
                        var path = additionalText.Path;
                        var targets = config.Targets;
                        if (targets != null)
                        {
                            foreach (var target in targets)
                            {
                                if (path.Contains(target.RootPath))
                                {
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                )
                .Select
                (
                    static (data,token) => new CsvInfo(data.Left,data.Right) 
                ).Collect().Combine(compilation);
            
            
            context.RegisterSourceOutput(
                dataProvider,
                static (sourceProductionContext, data) =>
                {
                    try
                    {
                        var writer = new CodeWriter();

                        var ( csvDataArray, compilation) = data;

                        var classSymbol =compilation.GetTypeByMetadataName(
                            "Amenone.DataClassGenerator.Runtime.CompilationFlagForDataClassGenerate");
                        if (classSymbol == null) return;
                        
                        var interfaceGrouped = csvDataArray.GroupBy( csvData => csvData.interfaceName);
                        
                        foreach (var groupedCsvData in interfaceGrouped)
                        {
                            foreach (var csvInfo in groupedCsvData)
                            {
                                if (!csvInfo.settingExist)
                                {
                                    var diagnostic = Diagnostic.Create(new DiagnosticDescriptor(
                                        "SGCSVD001", "Error Generation : Config not found", "Error ", "SourceGenerator", 
                                        DiagnosticSeverity.Error, true),
                                        Location.None,
                                        additionalLocations: null);

                                    sourceProductionContext.ReportDiagnostic(diagnostic);
                                    return;
                                }

                                if (string.IsNullOrEmpty(csvInfo.fileName) )
                                {
                                    var diagnostic = Diagnostic.Create(new DiagnosticDescriptor(
                                            "SGCSVD002", "Error Generation : fileName is Empty", "Error ", "SourceGenerator", 
                                            DiagnosticSeverity.Error, true),
                                        Location.None,
                                        additionalLocations: null);

                                    sourceProductionContext.ReportDiagnostic(diagnostic);
                                    return;
                                }

                                foreach (var usingStr in csvInfo.usings)
                                {
                                    if(string.IsNullOrEmpty(usingStr)) continue;
                                    writer.AppendLine($"using {usingStr};");
                                }
                                
                                if (csvInfo.EnableNameSpace)
                                {
                                    writer.AppendLine($"namespace {csvInfo.nameSpace}");
                                    writer.BeginBlock();
                                }

                                if (csvInfo.serializable)
                                {
                                    writer.AppendLine($"[System.Serializable]");
                                }
                                writer.AppendLine( $"public partial class {csvInfo.fileName}" );

                                if (csvInfo.requiredInterface)
                                {
                                    writer.Append($": {csvInfo.interfaceName}");
                                }
                                
                                writer.AppendLine();
                                writer.BeginBlock();
                                    
                                for (var i = 0; i < csvInfo.PropertyInfos.Length; i++)
                                {
                                    var propertyName = csvInfo.PropertyInfos[i].PropertyName;
                                    var type = csvInfo.PropertyInfos[i].TypeName;
                                    
                                    if (csvInfo.serializable)
                                    {
                                        writer.AppendLine($"public {type} {propertyName} ");
                                        writer.BeginBlock();
                                        writer.AppendLine($"get {{return _{propertyName};}} ");
                                        writer.AppendLine($"set {{_{propertyName} = value;}}"); 
                                        writer.EndBlock();
                                        writer.AppendLine($"[UnityEngine.SerializeField]");
                                        writer.AppendLine($"private {type} _{propertyName};");
                                    }
                                    else writer.AppendLine($"public {type} {propertyName} {{ get; set;}}");
                                    
                                }
                                
                                writer.EndBlock();
                                
                                if (csvInfo.EnableNameSpace)
                                {
                                    writer.EndBlock();
                                }
                                
                                sourceProductionContext.AddSource($"{csvInfo.fileName}.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
                                writer.Clear();
                            }

                            var interfaceName = groupedCsvData.Key;
                            if(interfaceName == string.Empty) continue;
                            
                            var csvInfoSample = groupedCsvData.First();
                            if( !csvInfoSample.requiredInterface) continue;  
                            if( csvInfoSample.settingExist == false) continue;

                            var prevCsv = groupedCsvData.First();
                            foreach (var csv in groupedCsvData)
                            {
                                if (!csv.PropertiesEquals(prevCsv))
                                {
                                    var diagnostic = Diagnostic.Create(new DiagnosticDescriptor(
                                            "SGCSVD003", $"Error Generation : Interface members are not match between {csv.fileName} and {prevCsv.fileName}", "Warning ", "SourceGenerator", 
                                            DiagnosticSeverity.Warning, true),
                                        Location.None,
                                        additionalLocations: null);

                                    sourceProductionContext.ReportDiagnostic(diagnostic);
                                    continue;
                                }
                            }
                            
                            var interfacePropertyName = groupedCsvData
                                .Select(info => info.PropertyInfos.AsEnumerable())
                                .Aggregate((current, next) => current.Intersect(next));
                            
                            var interfaceUsings = groupedCsvData
                                .Select(info => info.usings.AsEnumerable())
                                .Aggregate((current, next) => current.Union(next));

                            foreach (var usingStr in interfaceUsings)
                            {
                                if(string.IsNullOrEmpty(usingStr)) continue;
                                writer.AppendLine($"using {usingStr};");
                            }
                            
                            if (csvInfoSample.EnableNameSpace)
                            {
                                writer.AppendLine($"namespace {csvInfoSample.nameSpace}");
                                writer.BeginBlock();
                            }
                            writer.AppendLine( $"public partial interface {interfaceName}" );
                            writer.BeginBlock();
                            
                            foreach (var propertyInfo in interfacePropertyName)
                            {
                                writer.AppendLine($"{propertyInfo.TypeName} {propertyInfo.PropertyName} {{ get; set;}}");
                            }
                            writer.EndBlock();
                            
                            if (csvInfoSample.EnableNameSpace)
                            {
                                writer.EndBlock();
                            }
                            
                            sourceProductionContext.AddSource($"{interfaceName}.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
                            writer.Clear();
                            
                        }

                    }
                    catch (Exception e)
                    {
                        var diagnostic = Diagnostic.Create(new DiagnosticDescriptor(
                                "SGCSVD000", $"Error Generation : Unknown Error {e.ToString()}", "Error ", "SourceGenerator", 
                                DiagnosticSeverity.Error, true),
                            Location.None,
                            additionalLocations: null);

                        sourceProductionContext.ReportDiagnostic(diagnostic);
                        return;
                    }

                });
        }
        
        static bool IsNativeType(string field)
        {
            string[] nativeTypes = new [] { "int", "uint" ,"float", "double", "bool", "string" ,"Vector2","Vector3" }; // 例
            return nativeTypes.Contains(field);
        }
        
    }
}