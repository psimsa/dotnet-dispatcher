using System.Text;

namespace DotnetDispatcher.Generator;

internal class IndentedStringBuilder
{
    public StringBuilder Append(char value)
    {
        return _sb.Append(value);
    }

    public StringBuilder Append(char[] value)
    {
        return _sb.Append(value);
    }

    public StringBuilder Append(string value)
    {
        return _sb.Append(value);
    }

    public StringBuilder AppendLine()
    {
        return _sb.AppendLine();
    }

    public StringBuilder AppendLine(string value)
    {
        AppendIndent();
        return _sb.AppendLine(value);
    }

    private StringBuilder _sb;

    private int _indentLevel = 0;
    private string _indentString = "    ";

    public IndentedStringBuilder()
    {
        _sb = new StringBuilder();
    }

    public void Indent()
    {
        _indentLevel++;
    }

    public void Unindent()
    {
        _indentLevel--;
    }

    public override string ToString()
    {
        return _sb.ToString();
    }

    public void AppendIndent()
    {
        for (int i = 0; i < _indentLevel; i++)
        {
            _sb.Append(_indentString);
        }
    }
}