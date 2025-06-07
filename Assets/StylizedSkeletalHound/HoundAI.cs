using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class HoundAI : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] float maxHp = 100f;
    private float currentHp;
    private bool isDead = false;

    [Header("보스 설정")]
    [SerializeField] bool isBoss = true;
    
    [Header("이동 설정")]
    [SerializeField] float walkSpeed = 2f;
    [SerializeField] float runSpeed = 5f;
    [SerializeField] float runThreshold = 6f;

    [Header("공격 설정")]
    [SerializeField] float attackRange = 3.5f;
    [SerializeField] float closeRange = 2f;
    [SerializeField] float jumpAttackRange = 5f;
    [SerializeField] float optimalAttackDistance = 2.8f;
    [SerializeField] float frontPawDamage = 25f;
    [SerializeField] float leftPawDamage = 20f;
    [SerializeField] float rightPawDamage = 20f;
    [SerializeField] float lickBiteDamage = 35f;
    [SerializeField] float jumpAttackDamage = 40f;
    [SerializeField] float attackCooldown = 1.5f;
    [SerializeField] float jumpAttackRecoveryTime = 2f;

    [Header("점프 공격 설정 (NavMesh 전용)")]
    [SerializeField] float jumpSpeed = 8f;
    [SerializeField] float jumpDuration = 1f;
    private Vector3 jumpTargetPosition;
    private bool isJumpingToTarget = false;
    private float jumpTimer = 0f;
    private bool hasFallingTriggered = false;

    [Header("데미지 방지 설정")]
    [SerializeField] float damageImmunityTime = 0.5f;
    private float lastDamageTime = -1f;

    [Header("자연스러운 충돌 방지 설정")]
    [SerializeField] float collisionAvoidanceRadius = 1.5f;
    [SerializeField] float avoidanceForce = 2f;
    [SerializeField] float smoothAvoidance = 2f;
    [SerializeField] float minSeparationDistance = 1.2f;
    
    private Vector3 avoidanceDirection = Vector3.zero;
    private bool isAvoiding = false;

    [Header("공격 방향 체크 설정")]
    [SerializeField] float attackAngleThreshold = 30f;
    [SerializeField] float rotationBeforeAttack = 4f;
    [SerializeField] float maxRotationTime = 2f;
    
    private bool isRotatingToAttack = false;
    private float rotationTimer = 0f;

    [Header("파티클 쉴드 패턴 설정")]
    [SerializeField] GameObject houndShieldPrefab;
    [SerializeField] GameObject safeZonePrefab;
    [SerializeField] GameObject dangerAreaPrefab;
    [SerializeField] float safeZoneRadius = 5f;
    [SerializeField] float dangerAreaSize = 100f; // 위험지대 크기 (새로 추가)
    [SerializeField] float maxSafeZoneDistance = 15f;
    [SerializeField] float minSafeZoneDistance = 8f;
    [SerializeField] float patternDuration = 5f;
    [SerializeField] float massiveDamage = 80f;

    private bool isShieldPatternActive = false;
    private GameObject houndShield;
    private GameObject safeZoneEffect;
    private GameObject dangerAreaEffect;
    private Vector3 safeZonePosition;

    [Header("행동 확률 설정")]
    [SerializeField] float retreatProbability = 0.05f;
    [SerializeField] float stayAndAttackProbability = 0.95f;
    [SerializeField] float aggressiveModeProbability = 0.4f;
    [SerializeField] float consecutiveAttackChance = 0.9f;
    [SerializeField] float feintAttackChance = 0.1f;

    private float lastRetreatTime = 0f;
    [SerializeField] float retreatCooldown = 8f;

    [Header("근거리 행동 확률")]
    [SerializeField] float closeRangeRetreatProbability = 0.08f;

    private bool isAggressiveMode = false;
    private int consecutiveAttacks = 0;
    private int maxConsecutiveAttacks = 3;

    private float attackTimer = 0f;
    private float jumpRecoveryTimer = 0f;
    private bool isAttacking = false;
    private bool isJumping = false;
    private bool isBackingAway = false;
    private bool isRecoveringFromJump = false;

    [Header("레이어 관리")]
    [SerializeField] float upperBodyLayerWeight = 1f;
    private int upperBodyLayerIndex;

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private Collider mainCollider;
    private Rigidbody playerRb;

    [Header("공격 판정 콜라이더")]
    [SerializeField] AttackCollider frontPawCollider;
    [SerializeField] AttackCollider leftPawCollider;
    [SerializeField] AttackCollider rightPawCollider;
    [SerializeField] AttackCollider lickBiteCollider;
    [SerializeField] AttackCollider jumpAttackCollider;

    [Header("플레이어 타입 설정")]
    [SerializeField] bool playerUsesCharacterController = true;
    [SerializeField] float characterControllerPushForce = 5f;

    private CharacterController playerCharacterController;

    [Header("자연스러운 회전 설정")]
    [SerializeField] float rotationSpeed = 2f;
    [SerializeField] bool useNavMeshRotation = true;
    
    private Quaternion targetRotation;

    [Header("후퇴 설정")]
    [SerializeField] float backAwayDuration = 1.5f;

    [Header("후퇴 시 오로라 블라스트 공격 설정")]
    [SerializeField] GameObject auroraEffectPrefab;
    [SerializeField] GameObject bloodBlastPrefab;
    [SerializeField] Transform mouthTransform;
    [SerializeField] float bloodBlastDamage = 12f;
    [SerializeField] float blastDuration = 8f;
    [SerializeField] float retreatDuration = 2f;

    private bool isShootingBloodBlast = false;
    private GameObject currentAuroraEffect;

    [Header("점프 공격 범위 데미지 설정")]
    [SerializeField] GameObject jumpAreaEffectPrefab;
    [SerializeField] float jumpAreaRadius = 4f;
    [SerializeField] float jumpAreaDamage = 35f;
    [SerializeField] float jumpAreaEffectDuration = 2f;

    private float backAwayTimer = 0f;

    private enum HoundAttackType
    {
        FrontPaw, LeftPaw, RightPaw, LickBite, JumpAttack
    }
    private HoundAttackType currentAttackType;

    void ForceRetreatWithBloodBlast()
    {
        if (isShootingBloodBlast)
        {
            Debug.Log("이미 블라스트 공격 중 - 무시");
            return;
        }

        isAttacking = false;
        isJumping = false;
        isRecoveringFromJump = false;
        isRotatingToAttack = false;

        SafeStopAgent();
        DisableAttackCollider();

        isBackingAway = true;
        backAwayTimer = retreatDuration;

        lastRetreatTime = Time.time;

        Debug.Log("R키 - 2초 후퇴 후 제자리 8초 블라스트!");

        StartCoroutine(RetreatThenBlastAttack());
    }

    void PerformShieldPattern()
    {
        if (isShieldPatternActive) return;

        Debug.Log("하운드 쉴드 + 안전장판 패턴 시작!");
        StartCoroutine(ShieldAndSafeZonePattern());
    }

    IEnumerator ShieldAndSafeZonePattern()
    {
        isShieldPatternActive = true;

        if (houndShieldPrefab != null)
        {
            houndShield = Instantiate(houndShieldPrefab, transform.position, Quaternion.identity);
            houndShield.transform.SetParent(transform);
            Debug.Log("하운드 쉴드 생성!");
        }

        SafeStopAgent();
        agent.isStopped = true;
        Debug.Log("하운드 이동 금지!");

        safeZonePosition = GetRandomSafeZonePosition();
        if (safeZonePrefab != null)
        {
            safeZoneEffect = Instantiate(safeZonePrefab, safeZonePosition, Quaternion.identity);
            safeZoneEffect.transform.localScale = Vector3.one * safeZoneRadius;
            Debug.Log($"안전장판 생성! 위치: {safeZonePosition}, 반경: {safeZoneRadius}m");
        }

        if (dangerAreaPrefab != null)
        {
            dangerAreaEffect = Instantiate(dangerAreaPrefab, transform.position, Quaternion.identity);
            Debug.Log("데미지장판 생성 (5초 후 활성화)");
            ParticleSystem[] dangerParticles = dangerAreaEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in dangerParticles)
            {
                if (ps != null)
                {
                    var shape = ps.shape;

                    // 검색 결과 5번 적용: Shape 타입에 따른 크기 조정
                    if (shape.shapeType == ParticleSystemShapeType.Box)
                    {
                        shape.scale = new Vector3(dangerAreaSize, 2f, dangerAreaSize); // 100x100 크기
                    }
                    else if (shape.shapeType == ParticleSystemShapeType.Circle)
                    {
                        shape.radius = dangerAreaSize * 0.5f; // 반경 50m
                    }
                    else if (shape.shapeType == ParticleSystemShapeType.Sphere)
                    {
                        shape.radius = dangerAreaSize * 0.5f; // 반경 50m
                    }

                    Debug.Log($"위험지대 파티클 크기 조정: {dangerAreaSize}x{dangerAreaSize}");
                }
                
            }
            
    
        }
        //전체 이펙트 크기도 조정
        dangerAreaEffect.transform.localScale = Vector3.one * (dangerAreaSize / 50f); // 기본 50 기준으로 스케일링
        
        Debug.Log($"대형 위험지대 생성! 크기: {dangerAreaSize}x{dangerAreaSize}");
    

        

        yield return new WaitForSeconds(patternDuration);

        CheckPlayerSafetyAndDamage();

        yield return new WaitForSeconds(1f);

        if (houndShield != null)
        {
            Destroy(houndShield);
            Debug.Log("하운드 쉴드 제거!");
        }

        if (safeZoneEffect != null)
        {
            Destroy(safeZoneEffect);
            Debug.Log("안전장판 제거!");
        }

        if (dangerAreaEffect != null)
        {
            Destroy(dangerAreaEffect);
            Debug.Log("데미지장판 제거!");
        }

        agent.isStopped = false;
        Debug.Log("하운드 이동 재개!");

        isShieldPatternActive = false;
        attackTimer = attackCooldown * 2f;
        Debug.Log("쉴드 패턴 완료!");
    }

    void CheckPlayerSafetyAndDamage()
    {
        if (player == null) return;
        
        float distanceFromSafeZone = Vector3.Distance(player.position, safeZonePosition);
        
        if (distanceFromSafeZone <= safeZoneRadius)
        {
            Debug.Log($"플레이어가 안전장판 내부에 있음! (거리: {distanceFromSafeZone:F1}m <= {safeZoneRadius}m) - 데미지 없음");
        }
        else
        {
            IDamageable damageable = player.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(massiveDamage);
                Debug.Log($"플레이어가 안전장판 밖에 있음! (거리: {distanceFromSafeZone:F1}m > {safeZoneRadius}m) - {massiveDamage} 데미지!");
            }
        }
    }

    Vector3 GetRandomSafeZonePosition()
    {
        Vector3 randomPosition = Vector3.zero;
        int attempts = 0;
        int maxAttempts = 10;
        
        while (attempts < maxAttempts)
        {
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0;
            
            float randomDistance = Random.Range(minSafeZoneDistance, maxSafeZoneDistance);
            Vector3 candidatePosition = transform.position + (randomDirection.normalized * randomDistance);
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidatePosition, out hit, 5f, NavMesh.AllAreas))
            {
                randomPosition = hit.position;
                Debug.Log($"안전장판 위치 결정: {randomPosition} (하운드로부터 {randomDistance:F1}m)");
                break;
            }
            
            attempts++;
        }
        
        if (attempts >= maxAttempts)
        {
            randomPosition = transform.position + Vector3.forward * 10f;
            Debug.LogWarning("안전장판 위치를 찾지 못함 - 기본 위치 사용");
        }
        
        return randomPosition;
    }

    void SafeStopAgent()
    {
        if (agent != null && agent.isOnNavMesh && agent.enabled)
        {
            agent.isStopped = true;
        }
    }

    void SafeResumeAgent()
    {
        if (agent != null && agent.isOnNavMesh && agent.enabled)
        {
            agent.isStopped = false;
        }
    }

    void SafeSetDestination(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh && agent.enabled)
        {
            agent.SetDestination(destination);
        }
    }

    void SafeWarpAgent(Vector3 position)
    {
        if (agent != null && agent.enabled)
        {
            agent.Warp(position);
        }
    }

    void SafeSetSpeed(float speed)
    {
        if (agent != null && agent.isOnNavMesh && agent.enabled)
        {
            agent.speed = speed;
        }
    }

    bool IsLookingAtPlayer()
    {
        if (player == null) return false;
        
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;
        
        if (dirToPlayer.magnitude < 0.01f) return false;
        
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        return angle <= attackAngleThreshold;
    }

    void RotateTowardsPlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;
        
        if (dirToPlayer.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationBeforeAttack * Time.deltaTime
            );
        }
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (useNavMeshRotation)
        {
            agent.updateRotation = true;
            agent.angularSpeed = 120f;
        }
        else
        {
            agent.updateRotation = false;
        }
        
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            DestroyImmediate(rb);
            Debug.Log("Rigidbody 제거됨 - NavMesh 전용 모드");
        }

        upperBodyLayerIndex = animator.GetLayerIndex("Upper Body Layer");
        if (upperBodyLayerIndex == -1)
        {
            Debug.LogError("Upper Body Layer를 찾을 수 없습니다!");
        }

        InitializeAttackColliders();
    }

    void InitializeAttackColliders()
    {
        var colliders = new AttackCollider[] 
        { 
            frontPawCollider, leftPawCollider, rightPawCollider, 
            lickBiteCollider, jumpAttackCollider 
        };
        
        var damages = new float[] 
        { 
            frontPawDamage, leftPawDamage, rightPawDamage, 
            lickBiteDamage, jumpAttackDamage 
        };

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].gameObject.SetActive(false);
                colliders[i].damageAmount = damages[i];
                colliders[i].SetDamageSource(this);
            }
        }
    }

    void Start()
    {
        currentHp = maxHp;
        FindPlayer();
        
        if (upperBodyLayerIndex != -1)
        {
            animator.SetLayerWeight(upperBodyLayerIndex, upperBodyLayerWeight);
        }

        DisableAttackCollider();
    }

    void FindPlayer()
{
    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
    if (playerObj != null) 
    {
        player = playerObj.transform;
        
        // CharacterController 확인
        playerCharacterController = playerObj.GetComponent<CharacterController>();
        if (playerCharacterController != null)
        {
            Debug.Log("플레이어 CharacterController 감지됨 - OverlapSphere 방식 사용");
        }
        else
        {
            playerRb = playerObj.GetComponent<Rigidbody>();
            if (playerRb == null)
            {
                Debug.LogWarning("플레이어에 CharacterController나 Rigidbody가 없습니다.");
            }
        }
    }
    else Debug.LogError("Player 태그를 찾을 수 없습니다.");
}


    void Update()
    {
        if (isDead || player == null) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R키 눌림 - 강제 오로라 블라스트 공격 실행!");
            ForceRetreatWithBloodBlast();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("T키 눌림 - 강제 쉴드 패턴 실행!");
            PerformShieldPattern();
        }

        float distance = Vector3.Distance(transform.position, player.position);
        
        UpdateTimers();
        HandleNaturalCollisionAvoidance(distance);
        HandleCombatState(distance);
        HandleMovementWithBlendTree(distance);
        
        if (isJumpingToTarget)
        {
            HandleNavMeshJump();
        }
    }

    void HandleNaturalCollisionAvoidance(float distance)
    {
        if (isJumping || isBackingAway)
        {
            isAvoiding = false;
            avoidanceDirection = Vector3.Lerp(avoidanceDirection, Vector3.zero, 
                smoothAvoidance * Time.deltaTime);
            return;
        }

        if (distance < collisionAvoidanceRadius && !isAttacking && !isAggressiveMode)
        {
            Vector3 dirToPlayer = (player.position - transform.position);
            if (dirToPlayer.magnitude > 0.01f)
            {
                dirToPlayer = dirToPlayer.normalized;
                Vector3 targetAvoidanceDirection = -dirToPlayer;
                
                avoidanceDirection = Vector3.Slerp(avoidanceDirection, targetAvoidanceDirection, 
                    smoothAvoidance * Time.deltaTime);
                
                isAvoiding = true;
                
                if (distance < minSeparationDistance && distance < 0.8f)
                {
                    if (playerUsesCharacterController && playerCharacterController != null)
                    {
                        if (Random.Range(0f, 1f) < 0.3f)
                        {
                            HandleCharacterControllerAvoidance(dirToPlayer);
                        }
                    }
                    else if (!playerUsesCharacterController && playerRb != null)
                    {
                        Vector3 pushForce = dirToPlayer * avoidanceForce;
                        playerRb.AddForce(pushForce, ForceMode.Force);
                    }
                    else
                    {
                        if (distance < minSeparationDistance * 0.6f && Random.Range(0f, 1f) < 0.2f)
                        {
                            StartBackingAway();
                        }
                    }
                }
            }
        }
        else
        {
            isAvoiding = false;
            avoidanceDirection = Vector3.Lerp(avoidanceDirection, Vector3.zero, 
                smoothAvoidance * Time.deltaTime);
        }
    }

    void UpdateTimers()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        if (isRecoveringFromJump)
        {
            jumpRecoveryTimer -= Time.deltaTime;
            if (jumpRecoveryTimer <= 0)
            {
                isRecoveringFromJump = false;
            }
        }

        if (isBackingAway)
        {
            backAwayTimer -= Time.deltaTime;
            if (backAwayTimer <= 0)
            {
                isBackingAway = false;
            }
        }

        if (isRotatingToAttack)
        {
            rotationTimer += Time.deltaTime;
            if (rotationTimer >= maxRotationTime)
            {
                isRotatingToAttack = false;
                rotationTimer = 0f;
                Debug.Log("회전 타임아웃 - 강제 종료");
            }
        }

        if (isJumpingToTarget)
        {
            jumpTimer -= Time.deltaTime;
        }
    }

    void HandleCombatState(float distance)
    {
        if (isRecoveringFromJump || isJumping)
        {
            return;
        }

        if (!isAggressiveMode && Random.Range(0f, 1f) < 0.4f)
        {
            isAggressiveMode = true;
            consecutiveAttacks = 0;
            Debug.Log("공격적 모드 활성화!");
        }

        if (distance <= closeRange && !isAttacking && !isBackingAway)
        {
            bool canRetreat = Time.time - lastRetreatTime > 8f;
            
            if (Random.Range(0f, 1f) < 0.05f && !isAggressiveMode && canRetreat)
            {
                StartBackingAway();
                lastRetreatTime = Time.time;
                Debug.Log("근거리 후퇴 선택 (5% 확률)");
            }
            else
            {
                if (attackTimer <= 0 && IsLookingAtPlayer())
                {
                    PerformMeleeAttack();
                    Debug.Log("근거리 공격 실행");
                }
            }
            return;
        }
        else if (isBackingAway)
        {
            return;
        }
        else if (distance > closeRange && distance <= attackRange)
        {
            if (attackTimer <= 0 && !isAttacking)
            {
                if (IsLookingAtPlayer())
                {
                    if (isAggressiveMode && consecutiveAttacks < maxConsecutiveAttacks)
                    {
                        if (Random.Range(0f, 1f) < 0.9f)
                        {
                            PerformMeleeAttack();
                            consecutiveAttacks++;
                            attackTimer = attackCooldown * 0.5f;
                            return;
                        }
                        else
                        {
                            isAggressiveMode = false;
                            consecutiveAttacks = 0;
                            Debug.Log("공격적 모드 종료");
                        }
                    }
                    
                    float actionRoll = Random.Range(0f, 1f);
                    if (actionRoll < 0.88f) // 88% 확률로 공격
                    {
                        PerformMeleeAttack();
                    }
                    else if (actionRoll < 0.98f) // 10% 확률로 쉴드 패턴
                    {
                        if (!isShieldPatternActive)
                        {
                            PerformShieldPattern();
                        }
                        else
                        {
                            PerformMeleeAttack();
                        }
                    }
                    else // 2% 확률로만 후퇴
                    {
                        bool canRetreat = Time.time - lastRetreatTime > 8f;
                        if (canRetreat)
                        {
                            StartBackingAway();
                            lastRetreatTime = Time.time;
                            Debug.Log("중거리 후퇴 선택 (2% 확률)");
                        }
                        else
                        {
                            PerformMeleeAttack();
                        }
                    }
                    
                    isRotatingToAttack = false;
                    rotationTimer = 0f;
                }
                else
                {
                    isRotatingToAttack = true;
                    RotateTowardsPlayer();
                    SafeStopAgent();
                    SetAnimationParameters(0f, 0f, 0f);
                }
            }
        }
        else if (distance > attackRange && distance <= jumpAttackRange)
        {
            if (attackTimer <= 0 && !isAttacking)
            {
                if (IsLookingAtPlayer())
                {
                    float attackChoice = Random.Range(0f, 1f);
                    if (attackChoice < 0.5f)
                    {
                        PerformJumpAttack();
                    }
                    else
                    {
                        PerformMovingAttack();
                    }
                    
                    isRotatingToAttack = false;
                    rotationTimer = 0f;
                }
                else
                {
                    isRotatingToAttack = true;
                    RotateTowardsPlayer();
                    SafeStopAgent();
                    SetAnimationParameters(0f, 0f, 0f);
                }
            }
        }
        else
        {
            isRotatingToAttack = false;
            rotationTimer = 0f;
        }
    }

    void HandleMovementWithBlendTree(float distance)
    {
        // 쉴드 패턴 중이면 모든 이동 차단
        if (isShieldPatternActive)
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
            return;
        }

        // 블라스트 공격 중이면 모든 이동 차단
        if (isShootingBloodBlast)
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
            return;
        }

        Vector3 dirToPlayer = (player.position - transform.position);
        dirToPlayer.y = 0;
        
        if (!isDead && !isJumping && !isRotatingToAttack)
        {
            if (dirToPlayer.magnitude > 0.01f)
            {
                dirToPlayer = dirToPlayer.normalized;
                
                if (!useNavMeshRotation)
                {
                    targetRotation = Quaternion.LookRotation(dirToPlayer);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation, 
                        targetRotation, 
                        rotationSpeed * Time.deltaTime
                    );
                }
            }
        }

        if (isJumping || isRotatingToAttack)
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
            return;
        }

        if (isBackingAway)
        {
            HandleBackingAway();
        }
        else if (isRecoveringFromJump)
        {
            HandleRecoveryMovement(distance);
        }
        else if (isAvoiding)
        {
            HandleAvoidanceMovement();
        }
        else if (distance > jumpAttackRange && !isAttacking)
        {
            HandleNormalMovement(distance);
        }
        else if (distance > attackRange && distance <= jumpAttackRange && !isAttacking)
        {
            HandleCautiousApproach();
        }
        else if (distance > closeRange && distance <= attackRange && !isAttacking)
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
        }
        else if (!isAttacking)
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
        }
    }

    void HandleAvoidanceMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            SafeResumeAgent();
            
            Vector3 avoidancePosition = transform.position + avoidanceDirection * 1.5f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(avoidancePosition, out hit, 2f, NavMesh.AllAreas))
            {
                SafeSetDestination(hit.position);
                SafeSetSpeed(walkSpeed * 0.6f);
                
                Vector3 localAvoidanceDirection = transform.InverseTransformDirection(avoidanceDirection);
                SetAnimationParameters(localAvoidanceDirection.x, localAvoidanceDirection.z, 0.3f);
            }
        }
    }

    void HandleRecoveryMovement(float distance)
    {
        if (distance > attackRange)
        {
            SafeResumeAgent();
            SafeSetDestination(player.position);
            SafeSetSpeed(walkSpeed * 0.5f);
            
            Vector3 localDirection = agent != null ? transform.InverseTransformDirection(agent.velocity.normalized) : Vector3.zero;
            SetAnimationParameters(localDirection.x, localDirection.z, 0.2f);
        }
        else
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
        }
    }

    void SetAnimationParameters(float moveX, float moveY, float speed)
    {
        animator.SetFloat("MoveX", moveX);
        animator.SetFloat("MoveY", moveY);
        animator.SetFloat("Speed", speed);
    }

    void HandleCharacterControllerAvoidance(Vector3 dirToPlayer)
    {
        if (!isBackingAway)
        {
            StartBackingAway();
        }
        
        avoidanceForce *= 1.5f;
        
        Vector3 strongAvoidanceDirection = -dirToPlayer;
        Vector3 extraAvoidancePosition = transform.position + strongAvoidanceDirection * 2.5f;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(extraAvoidancePosition, out hit, 3f, NavMesh.AllAreas))
        {
            SafeSetDestination(hit.position);
            SafeSetSpeed(walkSpeed * 1.5f);
        }
        
        Debug.Log("CharacterController 플레이어 감지 - 하운드 강화 회피");
    }

    void StartBackingAway()
    {
        if (isBackingAway || isAggressiveMode) return;

        isAttacking = false;
        SafeStopAgent();
        DisableAttackCollider();
        
        isBackingAway = true;
        
        backAwayTimer = retreatDuration;
        
        lastRetreatTime = Time.time;
        Debug.Log("2초 후퇴 시작 - 후퇴 완료 후 제자리에서 블라스트 공격");

        if (auroraEffectPrefab != null && currentAuroraEffect == null)
        {
            currentAuroraEffect = Instantiate(auroraEffectPrefab, transform.position, Quaternion.identity);
            currentAuroraEffect.transform.SetParent(transform);
            Debug.Log("오로라 이펙트 생성 (후퇴 시작 시)!");
        }

        StartCoroutine(RetreatThenBlastAttack());
    }

    IEnumerator RetreatThenBlastAttack()
    {
        Debug.Log("후퇴 단계 시작 - 2초 동안 후퇴");
        
        float retreatElapsed = 0f;
        while (retreatElapsed < retreatDuration)
        {
            retreatElapsed += Time.deltaTime;
            yield return null;
        }
        
        SafeStopAgent();
        isBackingAway = false;
        
        Debug.Log("후퇴 완료 - 제자리에서 8초 블라스트 공격 시작!");
        
        yield return StartCoroutine(StationaryBloodBlastAttack());

        if (currentAuroraEffect != null)
        {
            Destroy(currentAuroraEffect);
            currentAuroraEffect = null;
            Debug.Log("오로라 이펙트 제거 (블라스트 종료 시)!");
        }

        isShootingBloodBlast = false;
        Debug.Log("오로라 블라스트 공격 완료!");
    }

    IEnumerator StationaryBloodBlastAttack()
    {
        isShootingBloodBlast = true;

        agent.isStopped = true;
        Debug.Log("하운드 이동 중지 (블라스트 공격 중)");

        GameObject continuousBlast = null;
        if (bloodBlastPrefab != null && mouthTransform != null)
        {
            continuousBlast = Instantiate(bloodBlastPrefab, mouthTransform.position, Quaternion.identity);

            ContinuousBloodBlast blastScript = continuousBlast.GetComponent<ContinuousBloodBlast>();
            if (blastScript == null)
            {
                blastScript = continuousBlast.AddComponent<ContinuousBloodBlast>();
            }

            blastScript.Initialize(bloodBlastDamage, mouthTransform, player, blastDuration, 50f);
            blastScript.StartContinuousBlast();

            Debug.Log($"제자리에서 {blastDuration}초 연속 블라스트 시작!");
        }

        yield return new WaitForSeconds(blastDuration);

        if (continuousBlast != null)
        {
            ContinuousBloodBlast blastScript = continuousBlast.GetComponent<ContinuousBloodBlast>();
            if (blastScript != null)
            {
                blastScript.StopContinuousBlast();
            }
        }

        agent.isStopped = false;
        Debug.Log("하운드 이동 재개 (블라스트 공격 종료)");

        isShootingBloodBlast = false;
        Debug.Log("제자리 블라스트 공격 완료!");

        attackTimer = attackCooldown * 0.5f;
    }

    void HandleBackingAway()
    {
        SafeStopAgent();
        
        Vector3 awayDirection = (transform.position - player.position);
        awayDirection.y = 0;
        
        if (awayDirection.magnitude > 0.01f)
        {
            awayDirection = awayDirection.normalized;
            
            Vector3 lookDirection = (player.position - transform.position).normalized;
            if (lookDirection.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2f * Time.deltaTime);
            }
            
            float backDistance = playerUsesCharacterController ? 
                walkSpeed * 2.5f * Time.deltaTime : 
                walkSpeed * 2f * Time.deltaTime;
            
            Vector3 backPosition = transform.position + awayDirection * backDistance;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(backPosition, out hit, 1f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
            
            SetAnimationParameters(0f, -0.5f, 0.5f);
        }
    }

    void HandleNormalMovement(float distance)
    {
        SafeResumeAgent();
        SafeSetDestination(player.position);
        
        bool shouldRun = distance > runThreshold;
        SafeSetSpeed(shouldRun ? runSpeed : walkSpeed);
        
        if (useNavMeshRotation)
        {
            Vector3 velocity = agent != null ? agent.velocity : Vector3.zero;
            if (velocity.magnitude > 0.1f)
            {
                Vector3 localDirection = transform.InverseTransformDirection(velocity.normalized);
                float speedParameter = shouldRun ? 1f : 0.5f;
                SetAnimationParameters(localDirection.x, localDirection.z, speedParameter);
            }
            else
            {
                SetAnimationParameters(0f, 0f, 0f);
            }
        }
        else
        {
            Vector3 localDirection = agent != null ? transform.InverseTransformDirection(agent.velocity.normalized) : Vector3.zero;
            float speedParameter = shouldRun ? 1f : 0.5f;
            SetAnimationParameters(localDirection.x, localDirection.z, speedParameter);
        }
    }

    void HandleCautiousApproach()
    {
        SafeResumeAgent();
        SafeSetDestination(player.position);
        SafeSetSpeed(walkSpeed * 0.7f);

        if (useNavMeshRotation)
        {
            Vector3 velocity = agent != null ? agent.velocity : Vector3.zero;
            if (velocity.magnitude > 0.1f)
            {
                Vector3 localDirection = transform.InverseTransformDirection(velocity.normalized);
                SetAnimationParameters(localDirection.x, localDirection.z, 0.35f);
            }
            else
            {
                SetAnimationParameters(0f, 0f, 0f);
            }
        }
        else
        {
            Vector3 localDirection = agent != null ? transform.InverseTransformDirection(agent.velocity.normalized) : Vector3.zero;
            SetAnimationParameters(localDirection.x, localDirection.z, 0.35f);
        }
    }

    void PerformMeleeAttack()
    {
        if (!IsLookingAtPlayer())
        {
            return;
        }

        SafeStopAgent();
        
        int randomAttack = Random.Range(0, 4);
        currentAttackType = (HoundAttackType)randomAttack;

        switch (currentAttackType)
        {
            case HoundAttackType.FrontPaw:
                animator.SetTrigger("FrontPawAttack");
                break;
            case HoundAttackType.LeftPaw:
                animator.SetTrigger("LeftPawAttack");
                break;
            case HoundAttackType.RightPaw:
                animator.SetTrigger("RightPawAttack");
                break;
            case HoundAttackType.LickBite:
                animator.SetTrigger("LickBite");
                break;
        }

        isAttacking = true;
        attackTimer = attackCooldown;
    }

    void PerformJumpAttack()
    {
        if (!IsLookingAtPlayer())
        {
            return;
        }

        currentAttackType = HoundAttackType.JumpAttack;
        jumpTargetPosition = player.position;
        
        animator.SetTrigger("JumpAttack");
        
        isJumping = true;
        isAttacking = true;
        attackTimer = attackCooldown * 2f;
        jumpTimer = jumpDuration;
        hasFallingTriggered = false;
        
        Debug.Log($"NavMesh 점프 공격 실행! 목표: {jumpTargetPosition}");
    }

    void PerformMovingAttack()
    {
        if (!IsLookingAtPlayer())
        {
            return;
        }

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;
        
        if (dirToPlayer.magnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(dirToPlayer);
        }

        SafeResumeAgent();
        SafeSetDestination(player.position);
        SafeSetSpeed(runSpeed * 0.8f);

        int randomAttack = Random.Range(0, 4);
        currentAttackType = (HoundAttackType)randomAttack;

        switch (currentAttackType)
        {
            case HoundAttackType.FrontPaw:
                animator.SetTrigger("WalkFrontPawAttack");
                break;
            case HoundAttackType.LeftPaw:
                animator.SetTrigger("WalkLeftPawAttack");
                break;
            case HoundAttackType.RightPaw:
                animator.SetTrigger("WalkRightPawAttack");
                break;
            case HoundAttackType.LickBite:
                animator.SetTrigger("WalkLickBite");
                break;
        }

        isAttacking = true;
        attackTimer = attackCooldown * 0.8f;
    }

    void HandleNavMeshJump()
    {
        if (jumpTimer <= 0)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(jumpTargetPosition, out hit, 5f, NavMesh.AllAreas))
            {
                SafeWarpAgent(hit.position);
            }
            else
            {
                SafeWarpAgent(jumpTargetPosition);
            }
            
            isJumpingToTarget = false;
            OnJumpComplete();
            return;
        }

        Vector3 currentPos = transform.position;
        Vector3 targetPos = jumpTargetPosition;
        
        float progress = 1f - (jumpTimer / jumpDuration);
        Vector3 newPosition = Vector3.Lerp(currentPos, targetPos, progress);
        
        if (progress >= 0.5f && !hasFallingTriggered)
        {
            animator.SetTrigger("StartFalling");
            hasFallingTriggered = true;
            Debug.Log("StartFalling 트리거 발동 - Jump_End로 전환");
        }
        
        float height = Mathf.Sin(progress * Mathf.PI) * 2f;
        newPosition.y = currentPos.y + height;
        
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(newPosition, out navHit, 10f, NavMesh.AllAreas))
        {
            SafeWarpAgent(navHit.position);
        }
        else
        {
            SafeWarpAgent(newPosition);
        }
    }

    public void EnableAttackCollider()
    {
        DisableAttackCollider();

        switch (currentAttackType)
        {
            case HoundAttackType.FrontPaw:
                if (frontPawCollider) frontPawCollider.gameObject.SetActive(true);
                break;
            case HoundAttackType.LeftPaw:
                if (leftPawCollider) leftPawCollider.gameObject.SetActive(true);
                break;
            case HoundAttackType.RightPaw:
                if (rightPawCollider) rightPawCollider.gameObject.SetActive(true);
                break;
            case HoundAttackType.LickBite:
                if (lickBiteCollider) lickBiteCollider.gameObject.SetActive(true);
                break;
            case HoundAttackType.JumpAttack:
                if (jumpAttackCollider) jumpAttackCollider.gameObject.SetActive(true);
                break;
        }
    }

    public void DisableAttackCollider()
    {
        var colliders = new AttackCollider[] 
        { 
            frontPawCollider, leftPawCollider, rightPawCollider, 
            lickBiteCollider, jumpAttackCollider 
        };

        foreach (var collider in colliders)
        {
            if (collider) collider.gameObject.SetActive(false);
        }
    }

    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
    }

    public void OnWalkAttackEnd()
    {
        isAttacking = false;
    }

    public void OnJumpStart()
    {
        Debug.Log("NavMesh 점프 시작!");
        SafeStopAgent();
        isJumpingToTarget = true;
    }

    public void OnLanding()
    {
        if (currentAttackType == HoundAttackType.JumpAttack)
        {
            PerformJumpAreaDamage();
        }
    }

    void PerformJumpAreaDamage()
    {
        Debug.Log("=== 점프 착지 - 범위 데미지 실행! ===");

        Vector3 landingPosition = transform.position;
        landingPosition.y = 0.1f;

        // 즉시 데미지 체크 제거 - 파티클에서만 처리하도록 변경
        // CheckJumpAreaDamageImmediate(); // 이 줄 주석 처리

        if (jumpAreaEffectPrefab != null)
        {
            Debug.Log($"파티클 프리팹 생성 시작: {jumpAreaEffectPrefab.name}");

            GameObject areaEffect = Instantiate(jumpAreaEffectPrefab, landingPosition, Quaternion.identity);
            areaEffect.transform.localScale = Vector3.one * jumpAreaRadius;

            // JumpAreaDamage 컴포넌트 확인/추가
            JumpAreaDamage damageComponent = areaEffect.GetComponent<JumpAreaDamage>();
            if (damageComponent == null)
            {
                damageComponent = areaEffect.AddComponent<JumpAreaDamage>();
            }

            // 컴포넌트 초기화 - 약간의 지연 후 실행
            StartCoroutine(InitializeDamageComponent(damageComponent));

            // 파티클 시스템 활성화
            ParticleSystem[] particles = areaEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particles)
            {
                if (ps != null)
                {
                    ps.Play();
                }
            }

            Destroy(areaEffect, jumpAreaEffectDuration);
        }
        else
        {
            // 파티클이 없는 경우에만 즉시 데미지
            CheckJumpAreaDamageImmediate();
        }
    }
IEnumerator InitializeDamageComponent(JumpAreaDamage damageComponent)
{
    yield return new WaitForFixedUpdate(); // 물리 업데이트 대기
    
    if (damageComponent != null)
    {
        damageComponent.Initialize(jumpAreaDamage, jumpAreaRadius, jumpAreaEffectDuration);
        Debug.Log($"JumpAreaDamage 초기화 완료 - 데미지: {jumpAreaDamage}");
    }
}

    public void OnJumpPeak() { }
    void CheckJumpAreaDamageImmediate()
{
    Debug.Log($"=== 즉시 범위 데미지 체크 시작 ===");
    Debug.Log($"체크 위치: {transform.position}, 반경: {jumpAreaRadius}");
    
    Collider[] hitColliders = Physics.OverlapSphere(transform.position, jumpAreaRadius);
    Debug.Log($"반경 내 {hitColliders.Length}개 콜라이더 발견");
    
    for (int i = 0; i < hitColliders.Length; i++)
    {
        Collider hitCollider = hitColliders[i];
        Debug.Log($"콜라이더 {i}: {hitCollider.name}, 태그: {hitCollider.tag}, 거리: {Vector3.Distance(transform.position, hitCollider.transform.position):F2}m");
        
        if (hitCollider.CompareTag("Player"))
        {
            Debug.Log($"★ 플레이어 발견: {hitCollider.name}");
            
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(jumpAreaDamage);
                Debug.Log($"★★★ 즉시 점프 범위 데미지! 플레이어에게 {jumpAreaDamage} 데미지! ★★★");
            }
            else
            {
                Debug.LogError($"플레이어에게 IDamageable 컴포넌트가 없습니다: {hitCollider.name}");
                
                // 대안 방법 시도
                MonoBehaviour[] scripts = hitCollider.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour script in scripts)
                {
                    if (script.GetType().GetMethod("TakeDamage") != null)
                    {
                        script.GetType().GetMethod("TakeDamage").Invoke(script, new object[] { jumpAreaDamage });
                        Debug.Log($"★★★ 대안 방법으로 즉시 데미지 적용! {script.GetType().Name}.TakeDamage({jumpAreaDamage}) ★★★");
                        break;
                    }
                }
            }
        }
    }
    
    if (hitColliders.Length == 0)
    {
        Debug.Log("반경 내에 아무것도 없음");
    }
}


    public void OnJumpComplete()
    {
        Debug.Log("NavMesh 점프 완료!");
        isJumping = false;
        isAttacking = false;
        isJumpingToTarget = false;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            SafeWarpAgent(hit.position);
            Debug.Log($"지면 착지 확인: {hit.position}");
        }

        isRecoveringFromJump = true;
        jumpRecoveryTimer = jumpAttackRecoveryTime;

        DisableAttackCollider();
    }

    public void OnLeaveGround()
    {
        Debug.Log("지면 이탈!");
    }

    public void OnStartFalling()
    {
        Debug.Log("낙하 시작!");
    }

    public void OnAboutToLand()
    {
        Debug.Log("착지 준비!");
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        if (Time.time - lastDamageTime < damageImmunityTime)
        {
            return;
        }

        lastDamageTime = Time.time;
        currentHp -= damageAmount;

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
        SafeStopAgent();
        
        if (upperBodyLayerIndex != -1)
        {
            animator.SetLayerWeight(upperBodyLayerIndex, 0f);
        }
        
        Destroy(gameObject, 3f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, closeRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, jumpAttackRange);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, jumpAreaRadius);
        
        if (player != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Vector3 forward = transform.forward * attackRange;
            Vector3 leftBoundary = Quaternion.Euler(0, -attackAngleThreshold, 0) * forward;
            Vector3 rightBoundary = Quaternion.Euler(0, attackAngleThreshold, 0) * forward;
            
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
            
            Vector3 dirToPlayer = (player.position - transform.position).normalized * attackRange;
            Gizmos.color = IsLookingAtPlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, transform.position + dirToPlayer);
        }
        
        if (isJumpingToTarget)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(jumpTargetPosition, 0.5f);
            Gizmos.DrawLine(transform.position, jumpTargetPosition);
        }
    }
}
