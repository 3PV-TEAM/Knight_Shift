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

    [Header("던지기 설정")]
    [SerializeField] private GameObject rockPrefab; // 돌 프리팹
    [SerializeField] private Transform throwOrigin; // 돌이 생성될 위치

    [Header("바닥 치기 이펙트 설정")]
    [SerializeField] private GameObject groundSmashEffectPrefab; // 바닥 치기 이펙트 프리팹
    [SerializeField] private float groundSmashEffectRadius = 5f; // 이펙트 범위
    [SerializeField] private LayerMask damageableLayer; // 데미지를 입힐 레이어

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
        
        // 디버그 로그 추가
        Debug.Log($"Golem initialized with HP: {currentHp}/{maxHp}");
        Debug.Log($"Golem Collider: {mainCollider.name}, isTrigger: {mainCollider.isTrigger}");
    }

    void Update()
    {
        if (isDead || player == null) return;
        Debug.Log("Golem Speed: " + agent.velocity.magnitude);
        // 애니메이션 상태 체크 - 수정된 부분
        if (isAttacking)
        {
            // Base Layer와 Upper Body Layer 모두 체크
            AnimatorStateInfo baseState = animator.GetCurrentAnimatorStateInfo(0); // Base Layer
            AnimatorStateInfo upperState = animator.GetCurrentAnimatorStateInfo(1); // Upper Body Layer
            
            // 애니메이션이 끝났는데 이벤트가 호출되지 않으면 강제 호출
            if ((baseState.normalizedTime >= 1.0f || upperState.normalizedTime >= 1.0f) && !animator.IsInTransition(0) && !animator.IsInTransition(1))
            {
                Debug.LogWarning("Animation finished but event not called - forcing end");
                OnAttackAnimationEnd();
            }
        }

        float distance = Vector3.Distance(transform.position, player.position);
        UpdateMovement(distance);
        UpdateCombat(distance);
        
        // 디버그: 현재 상태 출력 (너무 많은 로그를 피하기 위해 조건부로)
        if (Time.frameCount % 60 == 0) // 1초마다 한 번씩만 출력
        {
            Debug.Log($"Golem Status - HP: {currentHp}/{maxHp}, Distance: {distance:F1}, IsAttacking: {isAttacking}, IsHit: {isHit}");
        }
    }

    private void UpdateMovement(float distance)
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

    if (distance <= attackRange)
    {
        // 기본 공격 사거리: 정지 후 기본공격만
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
    else if (distance <= jumpAttackRange)
    {
        // 기본공격 사거리보다 멀고, 점프 공격 사거리 이내: 점프 공격(달려오면서 공격)
        ExecuteJumpAttack();
    }
    else if (distance <= throwRange)
    {
        // 던지기 공격
        ExecuteThrowAttack();
    }
    else
    {
        // throwRange 밖: 달려가면서 펀치(혹은 돌진 공격)
        ExecuteMovingAttack(GolemAttackType.Punch1);
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

    private void ExecuteThrowAttack()
    {
        isAttacking = true;
        agent.isStopped = true;

        currentAttackType = GolemAttackType.ThrowRock;
        StartCoroutine(PrepareAttack(GolemAttackType.ThrowRock, anticipationTime));

        attackTimer = isPhase2 ? phase2AttackCooldown : phase1AttackCooldown;

        // 돌 생성 및 던지기 로직 추가
        if (rockPrefab != null && throwOrigin != null)
        {
            GameObject rock = Instantiate(rockPrefab, throwOrigin.position, throwOrigin.rotation);
            Rigidbody rockRigidbody = rock.GetComponent<Rigidbody>();
            if (rockRigidbody != null)
            {
                Vector3 throwDirection = (player.position - throwOrigin.position).normalized;
                rockRigidbody.AddForce(throwDirection * 15f, ForceMode.Impulse); // 던지는 힘 설정
            }
            Debug.Log("Rock thrown!");
        }
        else
        {
            Debug.LogError("Rock prefab or throw origin is not assigned!");
        }
    }

    private void ExecuteGroundSmash()
    {
        isAttacking = true;
        agent.isStopped = true;

        currentAttackType = GolemAttackType.GroundSmash;
        StartCoroutine(PrepareAttack(GolemAttackType.GroundSmash, anticipationTime * 1.5f));

        attackTimer = isPhase2 ? phase2AttackCooldown : phase1AttackCooldown;

        // 바닥 치기 이펙트 생성
        if (groundSmashEffectPrefab != null)
        {
            GameObject effect = Instantiate(groundSmashEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // 이펙트는 2초 후에 제거

            // 이펙트 범위 내의 대상에게 데미지 입히기
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, groundSmashEffectRadius, damageableLayer);
            foreach (Collider hitCollider in hitColliders)
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(groundSmashDamage);
                    Debug.Log($"Ground smash hit: {hitCollider.name}, Damage: {groundSmashDamage}");
                }
            }
        }
        else
        {
            Debug.LogError("Ground smash effect prefab is not assigned!");
        }
    }
    private void ExecuteJumpAttack()
{
    isAttacking = true;
    agent.isStopped = false; // 이동 유지(달려오면서 공격)
    animator.SetTrigger("JumpAttack");
    currentAttackType = GolemAttackType.JumpAttack;
    attackTimer = isPhase2 ? phase2AttackCooldown : phase1AttackCooldown;
    Debug.Log("Started jump attack!");
}


    // TakeDamage 메서드 개선 - 주요 수정 부분
    public void TakeDamage(float damageAmount)
    {
        Debug.Log($"=== TakeDamage called on {gameObject.name} ===");
        Debug.Log($"Damage amount: {damageAmount}");
        Debug.Log($"Current HP before damage: {currentHp}");
        Debug.Log($"Is dead: {isDead}");

        if (isDead)
        {
            Debug.Log("Golem is already dead, ignoring damage");
            return;
        }

        // 데미지 적용
        currentHp -= damageAmount;
        currentHp = Mathf.Max(0, currentHp); // 음수 방지

        Debug.Log($"HP after damage: {currentHp}/{maxHp}");

        // Hit 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger("Hit");
            Debug.Log("Hit animation triggered");
        }

        // Hit 상태 설정
        isHit = true;

        // 공격 중단
        if (isAttacking)
        {
            StopAllCoroutines();
            isAttacking = false;
            isPreparingAttack = false;
            DisableAttackCollider();
        }

        Debug.Log($"Golem took damage: {damageAmount}, Current HP: {currentHp}/{maxHp}");

        // 2페이즈 진입 체크
        if (!isPhase2 && (currentHp / maxHp) <= phase2Threshold)
        {
            Debug.Log("Entering Phase 2!");
            EnterPhase2();
        }

        // 사망 체크
        if (currentHp <= 0)
        {
            Debug.Log("Golem HP reached 0, calling Die()");
            Die();
        }

        Debug.Log("=== TakeDamage completed ===");
    }

    private void EnterPhase2()
    {
        isPhase2 = true;
        animator.SetBool("Phase2", true);
        Debug.Log("Golem entered Phase 2!");
        // 2페이즈 진입 이펙트나 사운드 등 추가
    }

    // 애니메이션 이벤트에서 호출될 메서드들
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
        if (agent != null && agent.enabled && !isHit)
        {
            agent.isStopped = false;
            Debug.Log("Movement resumed");
        }
    }

    public void OnHitAnimationEnd()
    {
        isHit = false;
        Debug.Log("Hit animation ended, resuming normal behavior");
        
        // Hit 애니메이션이 끝나면 이동 재개
        if (agent != null && agent.enabled && !isAttacking)
        {
            agent.isStopped = false;
        }
    }

    private void Die()
    {
        Debug.Log("Golem is dying!");
        isDead = true;
        
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }
        
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        
        // 모든 코루틴 중단
        StopAllCoroutines();
        
        // 공격 콜라이더 비활성화
        DisableAttackCollider();
        
        // 스크립트 비활성화는 Death 애니메이션이 끝난 후에 하도록 수정
        // this.enabled = false; // 이 라인을 주석 처리
        
        Debug.Log("Golem death sequence completed");
    }

    // Death 애니메이션이 끝났을 때 호출될 메서드 (애니메이션 이벤트에서 호출)
    public void OnDeathAnimationEnd()
    {
        this.enabled = false;
        Debug.Log("Golem script disabled after death animation");
    }

    public void SpawnRockDuringAnimation()
    {
        // 돌 생성 및 골렘 손 위치로 이동
        if (rockPrefab != null && throwOrigin != null)
        {
            GameObject rock = Instantiate(rockPrefab, throwOrigin.position, throwOrigin.rotation);
            rock.transform.SetParent(throwOrigin); // 골렘 손 위치에 고정
            rock.transform.localPosition = Vector3.zero; // 손 위치로 이동
            rock.transform.localRotation = Quaternion.identity;

            Debug.Log("Rock spawned and attached to Golem's hand!");
        }
        else
        {
            Debug.LogError("Rock prefab or throw origin is not assigned!");
        }
    }

    public void ThrowRock()
    {
        // 돌 던지기 로직
        if (throwOrigin.childCount > 0)
        {
            Transform rock = throwOrigin.GetChild(0); // 손 위치에 있는 돌 가져오기
            rock.SetParent(null); // 손에서 분리
            Rigidbody rockRigidbody = rock.GetComponent<Rigidbody>();
            if (rockRigidbody != null)
            {
                // 물리 활성화 및 던지는 방향 설정
                rockRigidbody.isKinematic = false; // 물리 활성화
                rockRigidbody.useGravity = true; // 중력 활성화
                Vector3 throwDirection = (player.position - throwOrigin.position).normalized;
                rockRigidbody.AddForce(throwDirection * 15f, ForceMode.Impulse); // 던지는 힘 설정

                Debug.Log("Rock thrown!");
            }
            else
            {
                Debug.LogError("Rock does not have a Rigidbody component!");
            }
        }
        else
        {
            Debug.LogError("No rock found in throwOrigin!");
        }
    }

    private void ExecuteMovingAttack(GolemAttackType attackType)
    {
        isAttacking = true;

        // 상체 공격 애니메이션 트리거 설정
        animator.SetTrigger(attackType.ToString());
        currentAttackType = attackType;

        // 하체는 계속 이동하도록 설정
        agent.isStopped = false;

        // 공격 쿨다운 설정
        attackTimer = isPhase2 ? phase2AttackCooldown : phase1AttackCooldown;

        Debug.Log($"Started moving attack: {attackType}");
    }

    // 디버그용 메서드 - Inspector에서 체력을 직접 확인할 수 있도록
    [System.Serializable]
    public class DebugInfo
    {
        [ReadOnly] public float currentHpDebug;
        [ReadOnly] public bool isDeadDebug;
        [ReadOnly] public bool isHitDebug;
        [ReadOnly] public bool isAttackingDebug;
        [ReadOnly] public bool isPhase2Debug;
    }
    
    [Header("디버그 정보")]
    [SerializeField] private DebugInfo debugInfo = new DebugInfo();

    private void LateUpdate()
    {
        // 디버그 정보 업데이트
        debugInfo.currentHpDebug = currentHp;
        debugInfo.isDeadDebug = isDead;
        debugInfo.isHitDebug = isHit;
        debugInfo.isAttackingDebug = isAttacking;
        debugInfo.isPhase2Debug = isPhase2;
    }
}

// ReadOnly 속성 클래스 (Inspector에서 읽기 전용으로 표시)
public class ReadOnlyAttribute : PropertyAttribute { }