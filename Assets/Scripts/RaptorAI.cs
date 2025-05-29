using UnityEngine;
using UnityEngine.AI;

public class RaptorAI : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] float maxHp = 80f;
    private float currentHp;
    private bool isDead = false;
    private bool isHit = false;

    [Header("공격 설정")]
    [SerializeField] float attackRange = 3f;
    [SerializeField] float jumpAttackDamage = 35f;
    [SerializeField] float biteDamage = 25f;
    [SerializeField] float headbuttDamage = 20f;
    [SerializeField] float attackCooldown = 2f;
    [SerializeField] float jumpAttackDistance = 5f; // 점프 공격 최소 거리

    private float attackTimer = 0f;
    private bool isAttacking = false;

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private Collider mainCollider;

    [Header("공격 판정 콜라이더")]
    [SerializeField] AttackCollider jumpAttackCollider;
    [SerializeField] AttackCollider biteCollider;
    [SerializeField] AttackCollider headbuttCollider;

    [Header("이동 설정")]
    [SerializeField] float walkSpeed = 2f;
    [SerializeField] float runSpeed = 5f;
    [SerializeField] float rotationSpeed = 12f;

    private enum RaptorAttackType { JumpAttack, Bite, Headbutt }
    private RaptorAttackType currentAttackType;

    private float animSpeed = 0f;
    private int attackLayerIndex = 1;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();

        // 공격 콜라이더 비활성화
        if (jumpAttackCollider) jumpAttackCollider.gameObject.SetActive(false);
        if (biteCollider) biteCollider.gameObject.SetActive(false);
        if (headbuttCollider) headbuttCollider.gameObject.SetActive(false);

        // 데미지 설정
        if (jumpAttackCollider) jumpAttackCollider.damageAmount = jumpAttackDamage;
        if (biteCollider) biteCollider.damageAmount = biteDamage;
        if (headbuttCollider) headbuttCollider.damageAmount = headbuttDamage;
    }

    void Start()
    {
        currentHp = maxHp;
        FindPlayer();
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // 플레이어 방향
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;

        // 이동 및 회전 처리
        if (!isDead && !isHit)
        {
            if (distance <= attackRange)
            {
                // 공격 범위 내에서는 플레이어를 바라봄
                agent.isStopped = true;
                RotateToTarget(dirToPlayer);

                if (isAttacking)
                {
                    attackTimer -= Time.deltaTime;
                    if (attackTimer <= 0) isAttacking = false;
                }
                else
                {
                    Attack(distance);
                }
            }
            else
            {
                // 추격 시 이동 방향을 바라보도록 수정
                agent.isStopped = false;
                agent.speed = distance > 10f ? runSpeed : walkSpeed;
                agent.SetDestination(player.position);

                if (agent.velocity.magnitude > 0.1f)
                {
                    RotateToTarget(agent.velocity.normalized);
                }
            }
        }

        // 이동 속도에 따른 애니메이션 처리
        float targetSpeed = (!agent.isStopped) ? agent.velocity.magnitude : 0f;
        animSpeed = Mathf.Lerp(animSpeed, targetSpeed, Time.deltaTime * 10f);
        animator.SetFloat("Speed", animSpeed);
    }

    // 회전 처리를 위한 별도 메서드
    private void RotateToTarget(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    void Attack(float distance)
    {
        if (isHit || isAttacking) return;

        currentAttackType = ChooseAttackType(distance);
        
        // 공격 레이어 활성화
        animator.SetLayerWeight(attackLayerIndex, 1f);

        switch (currentAttackType)
        {
            case RaptorAttackType.JumpAttack:
                animator.SetTrigger("JumpAttack");
                break;
            case RaptorAttackType.Bite:
                animator.SetTrigger("Bite");
                break;
            case RaptorAttackType.Headbutt:
                animator.SetTrigger("Headbutt");
                break;
        }

        isAttacking = true;
        attackTimer = attackCooldown;
    }

    RaptorAttackType ChooseAttackType(float distance)
    {
        if (distance >= jumpAttackDistance)
        {
            return RaptorAttackType.JumpAttack;
        }
        
        int r = Random.Range(0, 2);
        return r == 0 ? RaptorAttackType.Bite : RaptorAttackType.Headbutt;
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        else Debug.LogError("Player 태그를 찾을 수 없습니다.");
    }

    // 애니메이션 이벤트용 메서드
    public void EnableAttackCollider()
    {
        switch (currentAttackType)
        {
            case RaptorAttackType.JumpAttack:
                if (jumpAttackCollider) jumpAttackCollider.gameObject.SetActive(true);
                break;
            case RaptorAttackType.Bite:
                if (biteCollider) biteCollider.gameObject.SetActive(true);
                break;
            case RaptorAttackType.Headbutt:
                if (headbuttCollider) headbuttCollider.gameObject.SetActive(true);
                break;
        }
    }

    public void DisableAttackCollider()
    {
        if (jumpAttackCollider) jumpAttackCollider.gameObject.SetActive(false);
        if (biteCollider) biteCollider.gameObject.SetActive(false);
        if (headbuttCollider) headbuttCollider.gameObject.SetActive(false);
    }

    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
        // 공격 레이어 비활성화
        animator.SetLayerWeight(attackLayerIndex, 0f);
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHp -= damageAmount;
        Debug.Log($"랩터 피격! 남은 체력: {currentHp}");

        animator.SetTrigger("Hit");
        isHit = true;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public void OnHitAnimationEnd()
    {
        isHit = false;
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