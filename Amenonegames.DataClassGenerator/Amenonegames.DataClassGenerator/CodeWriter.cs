//this code copied from VYaml.SourceGenerator in https://github.com/hadashiA/VYaml?tab=MIT-1-ov-file

// Copyright (c) 2022 hadashiA
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Text;

namespace Amenonegames.SourceGenerator;

class CodeWriter
{
    readonly struct IndentScope : IDisposable
    {
        readonly CodeWriter source;

        public IndentScope(CodeWriter source, string? startLine = null)
        {
            this.source = source;
            source.AppendLine(startLine);
            source.IncreaseIndent();
        }

        public void Dispose()
        {
            source.DecreaseIndent();
        }
    }

    readonly struct BlockScope : IDisposable
    {
        readonly CodeWriter source;

        public BlockScope(CodeWriter source, string? startLine = null)
        {
            this.source = source;
            source.AppendLine(startLine);
            source.BeginBlock();
        }

        public void Dispose()
        {
            source.EndBlock();
        }
    }

    readonly StringBuilder buffer = new();
    int indentLevel;

    public void Append(string value, bool indent = true)
    {
        if (indent)
        {
            buffer.Append($"{new string(' ', indentLevel * 4)} {value}");
        }
        else
        {
            buffer.Append(value);
        }
    }
    
    public void AppendLineBreak( bool indent = true)
    {
        if (indent)
        {
            buffer.Append($"\n{new string(' ', indentLevel * 4)}");
        }
        else
        {
            buffer.Append("\n");
        }
        
    }

    public void AppendLine(string? value = null, bool indent = true)
    {
        if (string.IsNullOrEmpty(value))
        {
            buffer.AppendLine();
        }
        else if (indent)
        {
            buffer.AppendLine($"{new string(' ', indentLevel * 4)} {value}");
        }
        else
        {
            buffer.AppendLine(value);
        }
    }

    public void AppendByteArrayString(byte[] bytes)
    {
        buffer.Append("{ ");
        var first = true;
        foreach (var x in bytes)
        {
            if (!first)
            {
                buffer.Append(", ");
            }
            buffer.Append(x);
            first = false;
        }
        buffer.Append(" }");
    }

    public override string ToString() => buffer.ToString();

    public IDisposable BeginIndentScope(string? startLine = null) => new IndentScope(this, startLine);
    public IDisposable BeginBlockScope(string? startLine = null) => new BlockScope(this, startLine);

    public void IncreaseIndent()
    {
        indentLevel++;
    }

    public void DecreaseIndent()
    {
        if (indentLevel > 0)
            indentLevel--;
    }

    public void BeginBlock()
    {
        AppendLine("{");
        IncreaseIndent();
    }

    public void EndBlock()
    {
        DecreaseIndent();
        AppendLine("}");
    }

    public void Clear()
    {
        buffer.Clear();
    }
}
