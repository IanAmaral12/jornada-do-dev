using NUnit.Framework;

public class HealthTests
{
    [Test]
    public void TakeDamage_ClampsAtZero_AndMarksDead()
    {
        var h = new Health(5);
        h.TakeDamage(7);
        Assert.AreEqual(0, h.Current);
        Assert.IsTrue(h.IsDead);
    }

    [Test]
    public void Changed_FiresWithCurrentAndMax()
    {
        var h = new Health(5);
        int c = -1, m = -1;
        h.Changed += (a, b) => { c = a; m = b; };
        h.TakeDamage(2);
        Assert.AreEqual((3, 5), (c, m));
    }

    [Test]
    public void Reset_RestoresMax()
    {
        var h = new Health(3);
        h.TakeDamage(3);
        h.Reset();
        Assert.AreEqual(3, h.Current);
        Assert.IsFalse(h.IsDead);
    }

    [Test]
    public void PhaseInfo_NextOfSenior_IsNull()
    {
        Assert.AreEqual(Tier.Pleno, PhaseInfo.Next(Tier.Junior));
        Assert.IsNull(PhaseInfo.Next(Tier.Senior));
        Assert.AreEqual("Senior", PhaseInfo.SceneFor(Tier.Senior));
    }
}
