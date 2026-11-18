using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class FaseJuniorTests
{
    TaskPanel painel;
    PlayerController heroi;
    EnemyController mob;

    IEnumerator CarregarEEntrarEmCombate()
    {
        SceneManager.LoadScene("Junior");
        yield return null;
        yield return null;
        painel = Object.FindFirstObjectByType<TaskPanel>();
        heroi = Object.FindFirstObjectByType<PlayerController>();
        mob = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).First(e => !e.isBoss);
        heroi.transform.position = mob.transform.position - new Vector3(1.2f, 0f, 0f);
        yield return Esperar(() => painel.AguardandoResposta, 5f, "o combate não abriu a task");
    }

    static IEnumerator Esperar(System.Func<bool> condicao, float limite, string erro)
    {
        float t = 0f;
        while (!condicao())
        {
            t += Time.unscaledDeltaTime;
            if (t > limite) Assert.Fail(erro);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator EncostarNoMob_AbreTask_ETravaMovimento()
    {
        yield return CarregarEEntrarEmCombate();
        Assert.IsTrue(CombatManager.Instancia.InCombat);
        Assert.IsTrue(heroi.InputLocked);
        Assert.AreEqual(Tier.Junior, painel.PerguntaAtual.TierValue);
    }

    [UnityTest]
    public IEnumerator NovaPergunta_LimpaBotaoSelecionado_ParaEnterNaoResponderSozinho()
    {
        yield return CarregarEEntrarEmCombate();
        var botao = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b => b.name == "Opcao1");
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(botao.gameObject);
        painel.Responder((painel.PerguntaAtual.correctIndex + 1) % 4);
        yield return Esperar(() => painel.AguardandoResposta, 8f, "a segunda pergunta não apareceu");
        Assert.IsNull(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject);
    }

    [UnityTest]
    public IEnumerator AcertarDuasVezes_DerrotaMob_ELiberaHeroi()
    {
        yield return CarregarEEntrarEmCombate();
        var vida = mob.Health;
        painel.Responder(painel.PerguntaAtual.correctIndex);
        yield return Esperar(() => painel.AguardandoResposta, 8f, "a segunda pergunta não apareceu");
        Assert.AreEqual(1, vida.Current);
        Assert.AreEqual(1f, Time.timeScale);

        painel.Responder(painel.PerguntaAtual.correctIndex);
        yield return Esperar(() => !CombatManager.Instancia.InCombat, 8f, "o combate não terminou");
        Assert.IsTrue(mob.IsDead);
        Assert.IsFalse(heroi.InputLocked);
        Assert.AreEqual(5, GameManager.Instancia.PlayerHealth.Current);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator ErrarCincoVezes_GameOver_ETentarDeNovoRecarrega()
    {
        yield return CarregarEEntrarEmCombate();
        for (int i = 0; i < 5; i++)
        {
            painel.Responder((painel.PerguntaAtual.correctIndex + 1) % 4);
            if (i < 4) yield return Esperar(() => painel.AguardandoResposta, 8f, $"pergunta {i + 2} não apareceu");
        }
        yield return Esperar(() => GameManager.Instancia.Acabou, 8f, "o game over não aconteceu");
        Assert.AreEqual(0, GameManager.Instancia.PlayerHealth.Current);
        var gameOver = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(r => r.name == "GameOver");
        Assert.IsTrue(gameOver.gameObject.activeInHierarchy);

        GameManager.Instancia.Retry();
        yield return null;
        yield return null;
        Assert.IsFalse(GameManager.Instancia.Acabou);
        Assert.AreEqual(5, GameManager.Instancia.PlayerHealth.Current);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator DerrotarBoss_AbreAPorta_EAvancaParaPleno()
    {
        SceneManager.LoadScene("Junior");
        yield return null;
        yield return null;
        painel = Object.FindFirstObjectByType<TaskPanel>();
        heroi = Object.FindFirstObjectByType<PlayerController>();
        var boss = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).First(e => e.isBoss);
        heroi.transform.position = boss.transform.position - new Vector3(2f, 0f, 0f);

        for (int i = 0; i < 3; i++)
        {
            yield return Esperar(() => painel.AguardandoResposta, 8f, $"pergunta {i + 1} do boss não apareceu");
            Assert.IsTrue(painel.PerguntaAtual.boss);
            painel.Responder(painel.PerguntaAtual.correctIndex);
        }
        yield return Esperar(() => !CombatManager.Instancia.InCombat, 8f, "o combate com o boss não terminou");

        var porta = Object.FindFirstObjectByType<PhaseExit>();
        heroi.transform.position = porta.transform.position;
        yield return Esperar(() => SceneManager.GetActiveScene().name == "Pleno", 5f, "não avançou para a fase Pleno");
    }
}
