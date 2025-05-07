using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] float maxHp = 100f;
    private float currentHp;
    private bool isDead = false; // 💀 사망 상태 플래그

    [Header("공격 설정")]
    [SerializeField] float attackRange = 2f;
    [SerializeField] float attackDamage = 20f;
    [SerializeField] float attackCooldown = 1f;

    private float attackTimer = 0f;
    private bool isAttacking = false;

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private Collider collider;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        collider = GetComponent<Collider>();
    }

    void Start()
    {
        currentHp = maxHp;
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        else Debug.LogError("Player 태그가 없는 오브젝트를 찾을 수 없습니다.");
    }

    void Update()
    {
        if (isDead || player == null) return;

        animator.SetFloat("Speed", agent.velocity.magnitude);

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            if (isAttacking)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0) isAttacking = false;
            }
            else Attack();
        }
        else ChasePlayer();
    }

    void Attack()
    {
        animator.SetTrigger("Attack");
        agent.isStopped = true;
        isAttacking = true;
        attackTimer = attackCooldown;
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    // 🩹 데미지 처리 로직 (IDamageable 구현)
    public void TakeDamage(float damageAmount)
{
    if (isDead) return;

    currentHp -= damageAmount;
    Debug.Log($"{name} 피격! 남은 체력: {currentHp}"); // ✅ 로그 확인

    animator.SetTrigger("Hit");

    if (currentHp <= 0) 
    {
        Die();
    }
}

    void Die()
    {
        isDead = true;
        animator.SetTrigger("Death");
        collider.enabled = false;
        agent.isStopped = true;
        Destroy(gameObject, 3f);
    }
}
