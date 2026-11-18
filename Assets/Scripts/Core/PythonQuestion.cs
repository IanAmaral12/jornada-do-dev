using System;

public enum Tier { Junior, Pleno, Senior }

[Serializable]
public class PythonQuestion
{
    public string id;
    public string tier;
    public bool boss;
    public string prompt;
    public string code;
    public string[] options;
    public int correctIndex;
    public string explanation;

    public Tier TierValue => (Tier)Enum.Parse(typeof(Tier), tier);
}

[Serializable]
public class QuestionSet
{
    public PythonQuestion[] questions;
}
