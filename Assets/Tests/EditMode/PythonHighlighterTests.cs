using NUnit.Framework;

public class PythonHighlighterTests
{
    [Test]
    public void Keywords_AreColored()
    {
        var r = PythonHighlighter.Highlight("def f(x):\n    return x");
        StringAssert.Contains($"<color={PythonHighlighter.CorPalavra}>def</color>", r);
        StringAssert.Contains($"<color={PythonHighlighter.CorPalavra}>return</color>", r);
    }

    [Test]
    public void KeywordInsideString_IsNotColoredAsKeyword()
    {
        var r = PythonHighlighter.Highlight("print(\"if for\")");
        StringAssert.Contains($"<color={PythonHighlighter.CorTexto}>\"if for\"</color>", r);
        StringAssert.DoesNotContain($"<color={PythonHighlighter.CorPalavra}>if</color>", r);
    }

    [Test]
    public void Numbers_AreColored_ButNotInsideIdentifiers()
    {
        var r = PythonHighlighter.Highlight("x2 = 3.14");
        StringAssert.Contains($"<color={PythonHighlighter.CorNumero}>3.14</color>", r);
        StringAssert.Contains("x2", r);
        StringAssert.DoesNotContain($"<color={PythonHighlighter.CorNumero}>2</color>", r);
    }

    [Test]
    public void AngleBrackets_AreEscapedSoRichTextDoesNotBreak()
    {
        var r = PythonHighlighter.Highlight("print(1 < 2)");
        StringAssert.DoesNotContain("1 < 2", r);
    }
}
