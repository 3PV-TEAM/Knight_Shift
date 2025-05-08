using UnityEngine;
using UnityEngine.AI;

public class BearAI : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] float maxHp = 100f;
    private float currentHp;
    private bool isDead = false;

    [Header("공격 설정")]
    [SerializeField] float attackRange = 2.5f;
    [SerializeField] float biteDamage = 30f;
    [SerializeField] float rightPawDamage = 20f;
    [SerializeField] float leftPawDamage = 20f;
    [SerializeField] float attackCooldown = 1.5f;

    private float attackTimer = 0f;
    private bool isAttacking = false;

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private Collider collider;

    [Header("공격 판정 콜라이더")]
    [SerializeField] Collider biteCollider;
    [SerializeField] Collider rightPawCollider;
    [SerializeField] Collider leftPawCollider;

    private enum BearAttackType { Bite, RightPaw, LeftPaw }
    private BearAttackType currentAttackType;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        collider = GetComponent<Collider>();

        // 공격 콜라이더는 평소에는 비활성화
        if (biteCollider) biteCollider.enabled = false;
        if (rightPawCollider) rightPawCollider.enabled = false;
        if (leftPawCollider) leftPawCollider.enabled = false;
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
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    void Attack()
    {
        // 공격 타입 선택 (예: 랜덤, 거리, 방향 등)
        currentAttackType = ChooseAttackType();

        switch (currentAttackType)
        {
            case BearAttackType.Bite:
                animator.SetTrigger("Bite");
                break;
            case BearAttackType.RightPaw:
                animator.SetTrigger("AttackRight");
                break;
            case BearAttackType.LeftPaw:
                animator.SetTrigger("AttackLeft");
                break;
        }

        agent.isStopped = true;
        isAttacking = true;
        attackTimer = attackCooldown;
    }

    // 공격 타입 선택 로직 (여기선 랜덤)
    BearAttackType ChooseAttackType()
    {
        int r = Random.Range(0, 3);
        return (BearAttackType)r;
    }

    // 애니메이션 이벤트에서 호출
    public void EnableAttackCollider()
    {
        switch (currentAttackType)
        {
            case BearAttackType.Bite:
                if (biteCollider) biteCollider.enabled = true;
                break;
            case BearAttackType.RightPaw:
                if (rightPawCollider) rightPawCollider.enabled = true;
                break;
            case BearAttackType.LeftPaw:
                if (leftPawCollider) leftPawCollider.enabled = true;
                break;
        }
    }
    public void DisableAttackCollider()
    {
        if (biteCollider) biteCollider.enabled = false;
        if (rightPawCollider) rightPawCollider.enabled = false;
        if (leftPawCollider) leftPawCollider.enabled = false;
    }

    // 무기 콜라이더에서 호출 (각 콜라이더에 이 스크립트 연결 필요)
    private void OnTriggerEnter(Collider other)
    {
        if (!isAttacking) return;
        if (!other.CompareTag("Player")) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            switch (currentAttackType)
            {
                case BearAttackType.Bite:
                    damageable.TakeDamage(biteDamage);
                    break;
                case BearAttackType.RightPaw:
                    damageable.TakeDamage(rightPawDamage);
                    break;
                case BearAttackType.LeftPaw:
                    damageable.TakeDamage(leftPawDamage);
                    break;
            }
        }
    }

    // 데미지 처리
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHp -= damageAmount;
        Debug.Log($"{name} 피격! 남은 체력: {currentHp}");

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
