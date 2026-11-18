using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instancia { get; private set; }

    public bool InCombat { get; private set; }

    void Awake() => Instancia = this;

    public bool TryStartCombat(EnemyController e) => false;
}
