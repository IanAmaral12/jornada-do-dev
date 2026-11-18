using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class QuestionBankTests
{
    static string Q(string id, string tier, bool boss, int correct = 0) =>
        $"{{\"id\":\"{id}\",\"tier\":\"{tier}\",\"boss\":{(boss ? "true" : "false")},\"prompt\":\"p\",\"code\":\"c\"," +
        $"\"options\":[\"a\",\"b\",\"c\",\"d\"],\"correctIndex\":{correct},\"explanation\":\"e\"}}";

    static string Set(params string[] qs) => "{\"questions\":[" + string.Join(",", qs) + "]}";

    [Test]
    public void Next_DoesNotRepeatUntilTierExhausted()
    {
        var bank = QuestionBank.FromJson(Set(Q("a", "Junior", false), Q("b", "Junior", false), Q("c", "Junior", false), Q("x", "Pleno", false)), new System.Random(1));
        var ids = new HashSet<string>();
        for (int i = 0; i < 3; i++) ids.Add(bank.Next(Tier.Junior, false).id);
        CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, ids);
    }

    [Test]
    public void Next_WhenTierExhausted_ReshufflesWithoutImmediateRepeat()
    {
        var bank = QuestionBank.FromJson(Set(Q("a", "Junior", false), Q("b", "Junior", false)), new System.Random(7));
        string last = null;
        for (int i = 0; i < 50; i++)
        {
            var id = bank.Next(Tier.Junior, false).id;
            Assert.AreNotEqual(last, id);
            last = id;
        }
    }

    [Test]
    public void Next_Boss_PrefersBossQuestions_FallsBackToTier()
    {
        var bank = QuestionBank.FromJson(Set(Q("a", "Junior", false), Q("b", "Junior", true), Q("c", "Pleno", false)), new System.Random(3));
        Assert.AreEqual("b", bank.Next(Tier.Junior, true).id);
        Assert.AreEqual("c", bank.Next(Tier.Pleno, true).id);
        Assert.AreEqual(1, bank.Count(Tier.Junior, true));
        Assert.AreEqual(2, bank.Count(Tier.Junior, false));
    }

    [Test]
    public void FromJson_InvalidCorrectIndex_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => QuestionBank.FromJson(Set(Q("ruim", "Junior", false, 9)), new System.Random(1)));
        StringAssert.Contains("ruim", ex.Message);
    }

    [Test]
    public void ShippedJson_LoadsAndHasTenPerTier()
    {
        var asset = Resources.Load<TextAsset>("Questions");
        Assert.IsNotNull(asset);
        var bank = QuestionBank.FromJson(asset.text, new System.Random(1));
        foreach (Tier t in Enum.GetValues(typeof(Tier)))
        {
            Assert.GreaterOrEqual(bank.Count(t, false), 10, t.ToString());
            Assert.GreaterOrEqual(bank.Count(t, true), 3, t.ToString());
        }
    }
}
