using System.Text;
using System.Text.RegularExpressions;

public static class PythonHighlighter
{
    public const string CorPalavra = "#ff79c6";
    public const string CorTexto = "#f1fa8c";
    public const string CorNumero = "#bd93f9";
    public const string CorFuncao = "#8be9fd";

    static readonly Regex Token = new(
        @"(?<str>""[^""]*""|'[^']*')|(?<num>\b\d+(\.\d+)?\b)|(?<id>[A-Za-z_]\w*)|(?<outro>.)",
        RegexOptions.Singleline);

    static readonly string[] Palavras =
    {
        "def", "return", "if", "elif", "else", "for", "while", "in", "not", "and", "or",
        "lambda", "nonlocal", "global", "True", "False", "None", "import", "from", "class", "pass", "break", "continue"
    };

    static readonly string[] Embutidas = { "print", "len", "range", "sum", "sorted", "round", "abs", "divmod", "str", "int", "float", "list" };

    public static string Highlight(string code)
    {
        var sb = new StringBuilder();
        foreach (Match m in Token.Matches(code))
        {
            string v = Escapar(m.Value);
            if (m.Groups["str"].Success) sb.Append(Cor(CorTexto, v));
            else if (m.Groups["num"].Success) sb.Append(Cor(CorNumero, v));
            else if (m.Groups["id"].Success && System.Array.IndexOf(Palavras, m.Value) >= 0) sb.Append(Cor(CorPalavra, v));
            else if (m.Groups["id"].Success && System.Array.IndexOf(Embutidas, m.Value) >= 0) sb.Append(Cor(CorFuncao, v));
            else sb.Append(v);
        }
        return sb.ToString();
    }

    static string Cor(string cor, string v) => $"<color={cor}>{v}</color>";

    // espaço de largura zero depois do "<" impede o rich text de interpretar código como tag
    static string Escapar(string v) => v.Replace("<", "<​");
}
