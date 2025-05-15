using UnityEngine;
using UnityEngine.AI;

public class MiniGolemAI : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] float maxHp = 80f;
    private float currentHp;
    private bool isDead = false;
    private bool isHit = false;

    [Header("공격 설정")]
    [SerializeField] float attackRange = 2.5f;
    [SerializeField] float attackDamage = 15f;
    [SerializeField] float attackCooldown = 2f;
    [SerializeField] AttackCollider punchCollider; // 펀치 공격용 콜라이더

    private float attackTimer = 0f;
    private bool isAttacking = false;

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private Collider mainCollider;

    [Header("이동 설정")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float rotationSpeed = 5f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // 수동 회전을 위해 비활성화
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();

        // 공격 콜라이더 초기화
        if (punchCollider)
        {
            punchCollider.gameObject.SetActive(false);
            punchCollider.damageAmount = attackDamage;
        }

        agent.speed = moveSpeed;
    }

    // Start 메서드에서 처음 플레이어 찾기
    void Start()
    {
        currentHp = maxHp;
        FindPlayer();
    }

    // 플레이어 찾기 메서드
    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) 
        {
            player = playerObj.transform;
        }
        else 
        {
            Debug.LogError("Player 태그가 없는 오브젝트를 찾을 수 없습니다.");
        }
    }

    // Update 메서드 내 플레이어 추적 부분 수정
    void Update()
    {
        if (isDead || player == null) return;

        // 공격 쿨다운 업데이트
        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        if (!isAttacking && !isHit)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            // 플레이어 방향으로 회전
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // 이동 속도 애니메이션 업데이트
            animator.SetFloat("Speed", agent.velocity.magnitude / agent.speed);

            if (distance <= attackRange && attackTimer <= 0)
            {
                Attack();
            }
            else
            {
                // ChasePlayer 대신 직접 추적 로직 구현
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }
    }

    void Attack()
    {
        isAttacking = true;
        agent.isStopped = true;
        animator.SetTrigger("Attack");
        attackTimer = attackCooldown;
    }

    // 애니메이션 이벤트로 호출될 메서드
    public void OnAttackStart()
    {
        if (punchCollider)
            punchCollider.gameObject.SetActive(true);
    }

    public void OnAttackEnd()
    {
        if (punchCollider)
            punchCollider.gameObject.SetActive(false);
        
        isAttacking = false;
        agent.isStopped = false;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHp -= damageAmount;
        animator.SetTrigger("Hit");
        isHit = true;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public void OnHitEnd()
    {
        isHit = false;
    }

    void Die()
    {
        isDead = true;
        animator.SetTrigger("Death");
        mainCollider.enabled = false;
        agent.isStopped = true;
        
        // 3초 후 파괴
        Destroy(gameObject, 3f);
    }
}