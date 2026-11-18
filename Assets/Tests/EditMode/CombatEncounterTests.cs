using NUnit.Framework;

public class CombatEncounterTests
{
    static PythonQuestion Q() => new PythonQuestion { id = "q", options = new[] { "a", "b", "c", "d" }, correctIndex = 2 };

    [Test]
    public void Answer_Correct_DamagesEnemy_ReturnsHit()
    {
        var enemy = new Health(2);
        var c = new CombatEncounter(new Health(5), enemy, 1, 1);
        c.Present(Q());
        Assert.AreEqual(AnswerOutcome.Hit, c.Answer(2));
        Assert.AreEqual(1, enemy.Current);
    }

    [Test]
    public void Answer_Correct_KillingBlow_ReturnsEnemyDefeated()
    {
        var c = new CombatEncounter(new Health(5), new Health(1), 1, 1);
        c.Present(Q());
        Assert.AreEqual(AnswerOutcome.EnemyDefeated, c.Answer(2));
        Assert.IsTrue(c.IsOver);
    }

    [Test]
    public void Answer_Wrong_DamagesPlayer_ReturnsMiss()
    {
        var player = new Health(5);
        var c = new CombatEncounter(player, new Health(2), 1, 2);
        c.Present(Q());
        Assert.AreEqual(AnswerOutcome.Miss, c.Answer(0));
        Assert.AreEqual(3, player.Current);
    }

    [Test]
    public void Answer_Wrong_LastHeart_ReturnsPlayerDefeated()
    {
        var c = new CombatEncounter(new Health(1), new Health(2), 1, 1);
        c.Present(Q());
        Assert.AreEqual(AnswerOutcome.PlayerDefeated, c.Answer(1));
        Assert.IsTrue(c.IsOver);
    }

    [Test]
    public void Answer_WhenAlreadyResolved_IsIgnored()
    {
        var enemy = new Health(3);
        var c = new CombatEncounter(new Health(5), enemy, 1, 1);
        c.Present(Q());
        c.Answer(2);
        Assert.AreEqual(AnswerOutcome.Ignored, c.Answer(2));
        Assert.AreEqual(2, enemy.Current);
        Assert.IsNull(c.Current);
    }

    [Test]
    public void Answer_AfterEncounterOver_IsIgnored()
    {
        var c = new CombatEncounter(new Health(5), new Health(1), 1, 1);
        c.Present(Q());
        c.Answer(2);
        c.Present(Q());
        Assert.AreEqual(AnswerOutcome.Ignored, c.Answer(2));
    }
}
