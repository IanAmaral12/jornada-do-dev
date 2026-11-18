using System.Collections;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] TaskPanel taskPanel;
    [SerializeField] CombatFx fx;
    [SerializeField] float tempoAcerto = 0.6f;
    [SerializeField] float tempoExplicacao = 2.5f;

    public static CombatManager Instancia { get; private set; }

    public bool InCombat { get; private set; }

    int? resposta;

    void Awake()
    {
        Instancia = this;
        taskPanel.Answered += i => resposta = i;
    }

    public bool TryStartCombat(EnemyController e)
    {
        if (InCombat || e == null || e.IsDead || GameManager.Instancia == null || GameManager.Instancia.Acabou) return false;
        StartCoroutine(Combate(e));
        return true;
    }

    IEnumerator Combate(EnemyController e)
    {
        InCombat = true;
        var gm = GameManager.Instancia;
        var encontro = new CombatEncounter(gm.PlayerHealth, e.Health, 1, e.damageToPlayer);

        player.InputLocked = true;
        player.FaceTowards(e.transform.position.x);
        e.FaceTowards(player.transform.position.x);
        HudController.Instancia?.MostrarInimigo(e);

        AnswerOutcome resultado = AnswerOutcome.Ignored;
        while (!encontro.IsOver)
        {
            var q = gm.Bank.Next(gm.CurrentTier, e.isBoss);
            encontro.Present(q);
            resposta = null;
            taskPanel.Show(e.displayName, q);
            yield return new WaitUntil(() => resposta.HasValue);

            resultado = encontro.Answer(resposta.Value);
            bool acertou = resultado is AnswerOutcome.Hit or AnswerOutcome.EnemyDefeated;
            taskPanel.ShowFeedback(acertou, q.explanation);
            yield return new WaitForSecondsRealtime(acertou ? tempoAcerto : tempoExplicacao);

            // o painel sai da frente para o golpe aparecer inteiro
            taskPanel.Hide();
            if (acertou)
                yield return fx.PlayerStrike(player.transform, e, 1, resultado == AnswerOutcome.EnemyDefeated);
            else
                yield return fx.EnemyStrike(e, player.transform, e.damageToPlayer);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        HudController.Instancia?.EsconderInimigo();
        InCombat = false;

        if (resultado == AnswerOutcome.EnemyDefeated)
        {
            e.Death();
            player.InputLocked = false;
            if (e.isBoss) gm.OnBossDefeated();
        }
        else if (resultado == AnswerOutcome.PlayerDefeated)
        {
            gm.OnPlayerDefeated();
        }
    }
}
