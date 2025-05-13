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
    private Collider mainCollider;

    [Header("공격 판정 콜라이더")]
    [SerializeField] AttackCollider biteCollider;
    [SerializeField] AttackCollider rightPawCollider;
    [SerializeField] AttackCollider leftPawCollider;

    private enum BearAttackType { Bite, RightPaw, LeftPaw }
    private BearAttackType currentAttackType;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();

        // 공격 콜라이더는 평소에는 비활성화
        if (biteCollider) biteCollider.gameObject.SetActive(false);
        if (rightPawCollider) rightPawCollider.gameObject.SetActive(false);
        if (leftPawCollider) leftPawCollider.gameObject.SetActive(false);
        
        // 각 콜라이더에 데미지 값 설정
        if (biteCollider) biteCollider.damageAmount = biteDamage;
        if (rightPawCollider) rightPawCollider.damageAmount = rightPawDamage;
        if (leftPawCollider) leftPawCollider.damageAmount = leftPawDamage;
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

        // 플레이어를 향해 회전
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0; // Y축 회전만 고려
        transform.forward = dirToPlayer;

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
                if (biteCollider) biteCollider.gameObject.SetActive(true);
                break;
            case BearAttackType.RightPaw:
                if (rightPawCollider) rightPawCollider.gameObject.SetActive(true);
                break;
            case BearAttackType.LeftPaw:
                if (leftPawCollider) leftPawCollider.gameObject.SetActive(true);
                break;
        }
    }
    
    public void DisableAttackCollider()
    {
        if (biteCollider) biteCollider.gameObject.SetActive(false);
        if (rightPawCollider) rightPawCollider.gameObject.SetActive(false);
        if (leftPawCollider) leftPawCollider.gameObject.SetActive(false);
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
        mainCollider.enabled = false;
        agent.isStopped = true;
        Destroy(gameObject, 3f);
    }
}