using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class GolemAI : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] private float maxHp = 200f;
    [SerializeField] private float phase2Threshold = 0.5f; // 2페이즈 진입 체력 비율
    private float currentHp;
    private bool isDead = false;
    private bool isHit = false;
    private bool isPhase2 = false;
    private bool isAttacking = false;

    [Header("공격 설정")]
    [SerializeField] private float attackRange = 4f;        // 기본 공격 범위 증가
    [SerializeField] private float jumpAttackRange = 7f;    // 점프 공격 범위 증가
    [SerializeField] private float throwRange = 12f;        // 던지기 범위 증가
    [SerializeField] private float phase1AttackCooldown = 2f;
    [SerializeField] private float phase2AttackCooldown = 1.2f;
    
    [Header("데미지 설정")]
    [SerializeField] private float punch1Damage = 20f;
    [SerializeField] private float punch2Damage = 25f;
    [SerializeField] private float jumpAttackDamage = 35f;
    [SerializeField] private float groundSmashDamage = 40f;
    [SerializeField] private float throwDamage = 30f;

    [Header("전투 강화 설정")]
    [SerializeField] private float anticipationTime = 0.5f; // 공격 예비 동작 시간
    [SerializeField] private float comboChance = 0.6f;     // 콤보 확률
    
    private float attackTimer = 0f;
    private int comboCount = 0;
    private bool isPreparingAttack = false;

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private Collider mainCollider;

    [Header("공격 판정 콜라이더")]
    [SerializeField] private AttackCollider punch1Collider;
    [SerializeField] private AttackCollider punch2Collider;
    [SerializeField] private AttackCollider jumpAttackCollider;
    [SerializeField] private AttackCollider groundSmashCollider;

    [Header("회전 설정")]
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float phase2RotationSpeed = 12f;

    private enum GolemAttackType { Punch1, Punch2, JumpAttack, GroundSmash, ThrowRock }
    private GolemAttackType currentAttackType;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;  // 회전은 수동으로 처리
        agent.radius = 1f;
        agent.height = 2.5f;
        agent.stoppingDistance = attackRange * 0.8f;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.areaMask = NavMesh.AllAreas;  // 모든 NavMesh Area에서 이동 가능하도록 설정
        
        Debug.Log($"NavMeshAgent initialized: Radius={agent.radius}, Height={agent.height}, StoppingDistance={agent.stoppingDistance}");
        
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();

        // 콜라이더 초기화 및 데미지 설정
        InitializeColliders();
    }

    private void InitializeColliders()
    {
        if (punch1Collider) {
            punch1Collider.gameObject.SetActive(false);
            punch1Collider.damageAmount = punch1Damage;
        }
        if (punch2Collider) {
            punch2Collider.gameObject.SetActive(false);
            punch2Collider.damageAmount = punch2Damage;
        }
        if (jumpAttackCollider) {
            jumpAttackCollider.gameObject.SetActive(false);
            jumpAttackCollider.damageAmount = jumpAttackDamage;
        }
        if (groundSmashCollider) {
            groundSmashCollider.gameObject.SetActive(false);
            groundSmashCollider.damageAmount = groundSmashDamage;
        }
    }

    void Start()
    {
        currentHp = maxHp;
        FindPlayer();
        
        // 초기 상태 리셋
        isAttacking = false;
        isPreparingAttack = false;
        isHit = false;
        
        // NavMeshAgent 설정 재확인
        if (agent != null)
        {
            agent.isStopped = false;
            agent.stoppingDistance = attackRange * 0.8f;
            agent.speed = 3.5f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.autoBraking = true;

            // NavMesh 상태 체크
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                Debug.Log($"Found valid NavMesh position: {hit.position}");
            }
            else
            {
                Debug.LogError("Object is not on NavMesh!");
            }
        }
    }

    void Update()
    {
        if (isAttacking)
    {
        // Base Layer(0) 대신 Upper Body Layer(1) 확인
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(1); // 1번 레이어
        Debug.Log($"Upper Body State: {currentState.shortNameHash}, NormalizedTime: {currentState.normalizedTime}");
        
        // Upper Body 레이어의 애니메이션 클립 이름 확인
        if (animator.GetCurrentAnimatorClipInfo(1).Length > 0)
        {
            string clipName = animator.GetCurrentAnimatorClipInfo(1)[0].clip.name;
            Debug.Log($"Current Upper Body Clip: {clipName}");
        }
        
        // 애니메이션이 끝났는데 이벤트가 호출되지 않으면 강제 호출
        if (currentState.normalizedTime >= 1.0f)
        {
            Debug.LogWarning("Upper Body animation finished but event not called - forcing end");
            OnAttackAnimationEnd();
        }
    }

    float distance = Vector3.Distance(transform.position, player.position);
    UpdateMovement(distance);
    UpdateCombat(distance);
        // 디버그: 현재 상태 출력
        Debug.Log($"Position: {transform.position}, Player: {player.position}, Distance: {Vector3.Distance(transform.position, player.position)}");
        
        
        
    }    private void UpdateMovement(float distance)
    {
        if (isDead || player == null) {
            Debug.LogWarning("Dead or no player found");
            return;
        }
        
        // 공격중이나 피격중이면 이동만 멈추고 회전은 계속함
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;

        // 회전 속도는 페이즈에 따라 다르게 적용
        float currentRotationSpeed = isPhase2 ? phase2RotationSpeed : rotationSpeed;
        
        Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
            currentRotationSpeed * Time.deltaTime);

        // 공격/피격 중에는 이동 제한
        if (isHit || isAttacking || isPreparingAttack) 
        {
            agent.isStopped = true;
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("InputX", 0f);
            animator.SetFloat("InputY", 0f);
            return;
        }

        // 공격 중이 아닐 때는 플레이어를 향해 이동
        agent.isStopped = false;
        agent.SetDestination(player.position);
        
        // 실제로 이동 중인지 확인
        float currentSpeed = agent.velocity.magnitude;
        
        if (currentSpeed > 0.1f)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity.normalized);
            animator.SetFloat("InputX", localVelocity.x, 0.1f, Time.deltaTime);
            animator.SetFloat("InputY", localVelocity.z, 0.1f, Time.deltaTime);
            animator.SetFloat("Speed", currentSpeed);
        }
        else
        {
            animator.SetFloat("InputX", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("InputY", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("Speed", 0f);
        }
    }

    private void UpdateCombat(float distance)
    {
        if (isHit || isAttacking) return;

        float currentAttackCooldown = isPhase2 ? phase2AttackCooldown : phase1AttackCooldown;

        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            return;
        }

        // 랜덤 공격 선택
        if (distance <= attackRange)
        {
            float randomValue = Random.value;
            if (isPhase2 && randomValue < comboChance)
            {
                StartCoroutine(ExecuteComboAttack());
            }
            else if (randomValue < 0.5f)
            {
                ExecuteBasicAttack(GolemAttackType.Punch1);
            }
            else
            {
                ExecuteBasicAttack(GolemAttackType.Punch2);
            }
        }
        else if (distance <= jumpAttackRange && (isPhase2 || Random.value < 0.3f))
        {
            ExecuteJumpAttack();
        }
        else if (distance <= throwRange)
        {
            ExecuteThrowAttack();
        }
    }

    private IEnumerator ExecuteComboAttack()
    {
        isAttacking = true;
        agent.isStopped = true;

        // 첫 번째 펀치
        yield return StartCoroutine(PrepareAttack(GolemAttackType.Punch1, 0.3f));
        
        // 두 번째 펀치
        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(PrepareAttack(GolemAttackType.Punch2, 0.3f));

        attackTimer = isPhase2 ? phase2AttackCooldown : phase1AttackCooldown;
    }

    private IEnumerator PrepareAttack(GolemAttackType attackType, float prepTime)
    {
        isPreparingAttack = true;
        
        // 공격 예비 동작
        animator.SetFloat("AttackAnticipation", (float)attackType);
        
        yield return new WaitForSeconds(prepTime);
        
        // 실제 공격 실행
        animator.SetTrigger(attackType.ToString());
        currentAttackType = attackType;
        
        yield return new WaitForSeconds(0.2f);
        isPreparingAttack = false;
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) {
            player = playerObj.transform;
            Debug.Log($"Found player at position: {player.position}");
        }
        else {
            Debug.LogError("Player 태그를 찾을 수 없습니다!");
        }
    }

    private void ExecuteBasicAttack(GolemAttackType attackType)
    {
        StopAllCoroutines();
        isAttacking = true;
        isPreparingAttack = false;
        agent.isStopped = true;

        currentAttackType = attackType;
        StartCoroutine(PrepareAttack(attackType, anticipationTime));

        attackTimer = isPhase2 ? phase2AttackCooldown : phase1AttackCooldown;
        Debug.Log($"Started basic attack: {attackType}");
    }

    private void ExecuteJumpAttack()
    {
        isAttacking = true;
        agent.isStopped = true;

        currentAttackType = GolemAttackType.JumpAttack;
        StartCoroutine(PrepareAttack(GolemAttackType.JumpAttack, anticipationTime * 1.2f));

        attackTimer = isPhase2 ? phase2AttackCooldown : phase1AttackCooldown;
    }

    private void ExecuteThrowAttack()
    {
        isAttacking = true;
        agent.isStopped = true;

        currentAttackType = GolemAttackType.ThrowRock;
        StartCoroutine(PrepareAttack(GolemAttackType.ThrowRock, anticipationTime));

        attackTimer = isPhase2 ? phase2AttackCooldown : phase1AttackCooldown;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHp -= damageAmount;
        animator.SetTrigger("Hit");
        isHit = true;

        // 2페이즈 진입 체크
        if (!isPhase2 && (currentHp / maxHp) <= phase2Threshold)
        {
            EnterPhase2();
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void EnterPhase2()
    {
        isPhase2 = true;
        animator.SetBool("Phase2", true);
        // 2페이즈 진입 이펙트나 사운드 등 추가
    }    // 애니메이션 이벤트에서 호출될 메서드들
    public void EnableAttackCollider()
    {
        Debug.Log($"Enabling attack collider for {currentAttackType}");
        switch (currentAttackType)
        {
            case GolemAttackType.Punch1:
                if (punch1Collider) 
                {
                    punch1Collider.gameObject.SetActive(true);
                    Debug.Log("Punch1 collider enabled");
                }
                break;
            case GolemAttackType.Punch2:
                if (punch2Collider)
                {
                    punch2Collider.gameObject.SetActive(true);
                    Debug.Log("Punch2 collider enabled");
                }
                break;
            case GolemAttackType.JumpAttack:
            case GolemAttackType.GroundSmash:
                if (groundSmashCollider) 
                {
                    // 콜라이더를 골렘의 발 아래에 위치시킴
                    Vector3 groundPosition = transform.position;
                    groundPosition.y = Mathf.Max(groundPosition.y - 0.1f, 0f); // 바닥 아래로 가지 않도록
                    groundSmashCollider.transform.position = groundPosition;
                    groundSmashCollider.gameObject.SetActive(true);
                    Debug.Log($"Ground attack collider enabled at {groundPosition}");
                }
                break;
            case GolemAttackType.ThrowRock:
                Debug.Log("ThrowRock attack - no collider implementation yet");
                break;
        }
    }

    public void DisableAttackCollider()
    {
        if (punch1Collider) punch1Collider.gameObject.SetActive(false);
        if (punch2Collider) punch2Collider.gameObject.SetActive(false);
        if (jumpAttackCollider) jumpAttackCollider.gameObject.SetActive(false);
        if (groundSmashCollider) groundSmashCollider.gameObject.SetActive(false);
    }

    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
        isPreparingAttack = false;
        Debug.Log($"Attack animation ended, currentAttackType: {currentAttackType}");
        
        // 공격 끝난 후 이동 재개
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            Debug.Log("Movement resumed");
        }
    }

    public void OnHitAnimationEnd()
    {
        isHit = false;
    }

    private void Die()
    {
        isDead = true;
        animator.SetTrigger("Death");
        mainCollider.enabled = false;
        agent.isStopped = true;
        this.enabled = false;
    }
}