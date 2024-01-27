using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace DataClassGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class SourceGenerator : IIncrementalGenerator
    {

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {

            var csvProvider = context.AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(".csv"));

            var namesAndContents =
                csvProvider
                    .Select(
                        static (text, cancellationToken) =>
                        {
                            var name = Path.GetFileNameWithoutExtension(text.Path);
                            var content = text.GetText(cancellationToken)!.ToString();
                            
                            StringReader reader = new StringReader(content);

                            var firstLine = reader.ReadLine();
                            var propertyNameClumns = firstLine.Split(',');
                            string[] typeStr = new string[] { };
                            while (reader.Peek() != -1)
                            {
                                var line = reader.ReadLine();
                                if (line != null)
                                {
                                    var columns = line.Split(',');
                                
                                    if (IsNativeType(columns[0]))
                                    {
                                        typeStr = columns;
                                        break;
                                    }
                                }
                            }
                                
                            return (name , propertyNameClumns , typeStr);
                        })
                    .Collect();
            
            context.RegisterSourceOutput(
                namesAndContents,
                static (sourceProductionContext, csvDataArray) =>
                {
                    var builder = new StringBuilder();
                    foreach (var csvData in csvDataArray)
                    {
                        var (fileName,  propertyNameClumns ,  typeStr) = csvData;
                        
                        builder.Append($@"
    public class {fileName}
    {{");

                        for (int i = 0; i < propertyNameClumns.Length; i++)
                        {
                            var propertyName = propertyNameClumns[i];
                            var typeName = typeStr[i];
                            
                            builder.Append($@"
        public {typeName} {propertyName} {{ get; set; }}");
                            
                        }
                        
                        builder.Append(@"
    }");

                        
                        sourceProductionContext.AddSource($"{fileName}.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
                        
                        builder.Clear();
                        
                    }
                    


                });
        }
        
        static bool IsNativeType(string field)
        {
            string[] nativeTypes = new [] { "int", "uint" ,"float", "double", "bool", "string" ,"Vector2","Vector3" }; // 例
            return nativeTypes.Contains(field);
        }
        
        static void SetAttribute(IncrementalGeneratorPostInitializationContext context)
        {
            const string AttributeText = @"
using System;
namespace SourceGeneratorSample
{
    [AttributeUsage(AttributeTargets.Class,
                    Inherited = false, AllowMultiple = false)]
        sealed class SampleAttribute : Attribute
    {
    
        public SampleAttribute()
        {   
        }
        
    }
}
";                
            context.AddSource
            (
                "SampleAttribute.cs",
                SourceText.From(AttributeText,Encoding.UTF8)
            );
        }
    }
}