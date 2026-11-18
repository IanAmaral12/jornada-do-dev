using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] public string displayName = "Bug";
    [SerializeField] public bool isBoss;
    [SerializeField] int maxHp = 2;
    [SerializeField] public int damageToPlayer = 1;
    [SerializeField] Transform player;
    [SerializeField] Transform pointA;
    [SerializeField] Transform pointB;
    [SerializeField] float patrolSpeed = 1f;
    [SerializeField] float chaseSpeed = 2.5f;
    [SerializeField] float detectionRange = 5f;
    [SerializeField] float combatRange = 1.5f;

    public Health Health { get; private set; }
    public bool IsDead { get; private set; }

    Rigidbody2D rb2d;
    SpriteRenderer spriteRenderer;
    Animator animator;

    State current;
    PatrolState patrol;
    ChaseState chase;
    CombatState combat;

    public float DistanceToPlayer() => Mathf.Abs(transform.position.x - player.position.x);

    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        Health = new Health(maxHp);

        patrol = new PatrolState(this);
        chase = new ChaseState(this);
        combat = new CombatState(this);
    }

    void Start() => ChangeState(patrol);

    void Update() => current?.Execute();

    void ChangeState(State nextState)
    {
        current = nextState;
        current.Enter();
    }

    void MoveTowards(float x, float speed)
    {
        float dir = Mathf.Sign(x - transform.position.x);
        rb2d.linearVelocity = new Vector2(dir * speed, rb2d.linearVelocity.y);
        spriteRenderer.flipX = dir < 0;
    }

    void Stop() => rb2d.linearVelocity = new Vector2(0f, rb2d.linearVelocity.y);

    public void FaceTowards(float x) => spriteRenderer.flipX = x < transform.position.x;

    public void EnterCombat() => ChangeState(combat);

    public void ExitCombat()
    {
        if (!IsDead) ChangeState(patrol);
    }

    abstract class State
    {
        protected readonly EnemyController ai;
        protected State(EnemyController enemyCon) { ai = enemyCon; }
        public abstract void Enter();
        public abstract void Execute();
    }

    class PatrolState : State
    {
        Transform target;

        public PatrolState(EnemyController enemyCon) : base(enemyCon) { }

        public override void Enter()
        {
            if (ai.animator) ai.animator.SetBool("Chasing", false);
            target ??= ai.pointA;
        }

        public override void Execute()
        {
            if (ai.DistanceToPlayer() <= ai.detectionRange)
            {
                ai.ChangeState(ai.chase);
                return;
            }

            if (target == null)
            {
                ai.Stop();
                return;
            }

            ai.MoveTowards(target.position.x, ai.patrolSpeed);
            if (Mathf.Abs(ai.transform.position.x - target.position.x) < 0.2f)
                target = target == ai.pointA ? ai.pointB : ai.pointA;
        }
    }

    class ChaseState : State
    {
        public ChaseState(EnemyController enemyCon) : base(enemyCon) { }

        public override void Enter()
        {
            if (ai.animator) ai.animator.SetBool("Chasing", true);
        }

        public override void Execute()
        {
            float distance = ai.DistanceToPlayer();

            if (distance <= ai.combatRange)
            {
                ai.Stop();
                if (CombatManager.Instancia != null && CombatManager.Instancia.TryStartCombat(ai))
                    ai.ChangeState(ai.combat);
                return;
            }

            if (distance > ai.detectionRange * 1.5f)
            {
                ai.ChangeState(ai.patrol);
                return;
            }

            ai.MoveTowards(ai.player.position.x, ai.chaseSpeed);
        }
    }

    class CombatState : State
    {
        public CombatState(EnemyController enemyCon) : base(enemyCon) { }

        public override void Enter()
        {
            if (ai.animator) ai.animator.SetBool("Chasing", false);
            ai.Stop();
        }

        public override void Execute()
        {
            ai.Stop();
            ai.FaceTowards(ai.player.position.x);
        }
    }

    public void Death()
    {
        if (IsDead) return;

        IsDead = true;
        current = null;
        Stop();
        rb2d.simulated = false;
        foreach (var coll in GetComponentsInChildren<Collider2D>())
            coll.enabled = false;

        if (animator) animator.SetTrigger("Death");
        DestroyEnemy();
    }

    public void DestroyEnemy() => Destroy(gameObject, 1.5f);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, combatRange);
    }
}
