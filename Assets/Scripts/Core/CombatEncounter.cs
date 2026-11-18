public enum AnswerOutcome { Hit, Miss, EnemyDefeated, PlayerDefeated, Ignored }

public class CombatEncounter
{
    readonly Health player;
    readonly Health enemy;
    readonly int damageToEnemy;
    readonly int damageToPlayer;

    public PythonQuestion Current { get; private set; }
    public bool IsOver => player.IsDead || enemy.IsDead;

    public CombatEncounter(Health player, Health enemy, int damageToEnemy, int damageToPlayer)
    {
        this.player = player;
        this.enemy = enemy;
        this.damageToEnemy = damageToEnemy;
        this.damageToPlayer = damageToPlayer;
    }

    public void Present(PythonQuestion q)
    {
        if (!IsOver) Current = q;
    }

    public AnswerOutcome Answer(int optionIndex)
    {
        if (Current == null || IsOver) return AnswerOutcome.Ignored;

        bool correta = optionIndex == Current.correctIndex;
        Current = null;

        if (correta)
        {
            enemy.TakeDamage(damageToEnemy);
            return enemy.IsDead ? AnswerOutcome.EnemyDefeated : AnswerOutcome.Hit;
        }

        player.TakeDamage(damageToPlayer);
        return player.IsDead ? AnswerOutcome.PlayerDefeated : AnswerOutcome.Miss;
    }
}
