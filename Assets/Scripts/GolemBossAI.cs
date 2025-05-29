using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class GolemBossAI : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] float maxHp = 500f;
    private float currentHp;
    private bool isDead = false;
    private bool isHit = false;

    [Header("공격 설정")]
    [SerializeField] float basicAttackRange = 3f;
    [SerializeField] float basicAttackDamage = 25f;
    [SerializeField] float attackCooldown = 2f;
    [SerializeField] GameObject rockPrefab;
    [SerializeField] GameObject minionPrefab;
    
    [Header("스킬 데미지")]
    [SerializeField] float fallingRockDamage = 40f;
    [SerializeField] float earthquakeDamage = 50f;

    [Header("스킬 쿨다운")]
    [SerializeField] float rockAttackCooldown = 15f;
    [SerializeField] float earthquakeCooldown = 20f;
    [SerializeField] float summonCooldown = 25f;

    [SerializeField] GameObject earthquakeEffectPrefab;

    private float basicAttackTimer = 0f;
    private float rockAttackTimer = 0f;
    private float earthquakeTimer = 0f;
    private float summonTimer = 0f;

    private bool isAttacking = false;
    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private Collider mainCollider;

    [Header("공격 판정 콜라이더")]
    [SerializeField] AttackCollider basicAttackCollider;
    [SerializeField] AttackCollider earthquakeCollider;

    private enum GolemAttackType { Basic, FallingRocks, Earthquake, Summon }
    private GolemAttackType currentAttackType;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();

        if (basicAttackCollider) 
        {
            basicAttackCollider.gameObject.SetActive(false);
            basicAttackCollider.damageAmount = basicAttackDamage;
        }
        if (earthquakeCollider)
        {
            earthquakeCollider.gameObject.SetActive(false);
            earthquakeCollider.damageAmount = earthquakeDamage;
        }
    }

    void Start()
    {
        currentHp = maxHp;
        FindPlayer();
        InitializeTimers();
    }
    void FindPlayer()
    {
        // PlayerArmature를 찾습니다
        GameObject playerObj = GameObject.Find("PlayerArmature");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("PlayerArmature 오브젝트를 찾을 수 없습니다.");
        }
    }

    void InitializeTimers()
    {
        rockAttackTimer = rockAttackCooldown;
        earthquakeTimer = earthquakeCooldown;
        summonTimer = summonCooldown;
    }
    private float moveSpeed = 0f; // 블렌드 트리용 이동 속도 파라미터
    void Update()
    {
        // 테스트용 키 입력 체크
        if (!isAttacking && !isDead)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) // 1키: 기본 공격
            {
                currentAttackType = GolemAttackType.Basic;
                ExecuteAttack();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2)) // 2키: 바위 떨어뜨리기
            {
                currentAttackType = GolemAttackType.FallingRocks;
                ExecuteAttack();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3)) // 3키: 지진 공격
            {
                currentAttackType = GolemAttackType.Earthquake;
                ExecuteAttack();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4)) // 4키: 미니언 소환
            {
                currentAttackType = GolemAttackType.Summon;
                ExecuteAttack();
            }
        }

        // 기존 Update 코드
        if (isDead || player == null) return;

        UpdateTimers();
        float distance = Vector3.Distance(transform.position, player.position);

        if (!isAttacking && !isHit)
        {
            // 플레이어 방향으로 회전
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
            // 이동 속도 업데이트
            float targetSpeed = agent.velocity.magnitude / agent.speed; // 정규화된 속도
            moveSpeed = Mathf.Lerp(moveSpeed, targetSpeed, Time.deltaTime * 10f);
            animator.SetFloat("Speed", moveSpeed);
            // 공격 패턴 결정
            if (distance <= basicAttackRange && basicAttackTimer <= 0)
            {
                DecideAndExecuteAttack();
            }
            else if (!isAttacking)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }
    }

    void UpdateTimers()
    {
        if (basicAttackTimer > 0) basicAttackTimer -= Time.deltaTime;
        if (rockAttackTimer > 0) rockAttackTimer -= Time.deltaTime;
        if (earthquakeTimer > 0) earthquakeTimer -= Time.deltaTime;
        if (summonTimer > 0) summonTimer -= Time.deltaTime;
    }

    void DecideAndExecuteAttack()
    {
        List<GolemAttackType> availableAttacks = new List<GolemAttackType>();

        // 사용 가능한 공격 수집
        availableAttacks.Add(GolemAttackType.Basic);
        if (rockAttackTimer <= 0) availableAttacks.Add(GolemAttackType.FallingRocks);
        if (earthquakeTimer <= 0) availableAttacks.Add(GolemAttackType.Earthquake);
        if (summonTimer <= 0) availableAttacks.Add(GolemAttackType.Summon);

        // 랜덤하게 공격 선택
        currentAttackType = availableAttacks[Random.Range(0, availableAttacks.Count)];
        ExecuteAttack();
    }

    void ExecuteAttack()
    {
        isAttacking = true;
        agent.isStopped = true;

        switch (currentAttackType)
        {
            case GolemAttackType.Basic:
                animator.SetTrigger("BasicAttack");
                basicAttackTimer = attackCooldown;
                break;

            case GolemAttackType.FallingRocks:
                animator.SetTrigger("RockAttack");
                StartCoroutine(FallingRocksAttack());
                rockAttackTimer = rockAttackCooldown;
                break;

            case GolemAttackType.Earthquake:
                animator.SetTrigger("Earthquake");
                StartCoroutine(EarthquakeAttack());
                earthquakeTimer = earthquakeCooldown;
                break;

            case GolemAttackType.Summon:
                animator.SetTrigger("Summon");
                StartCoroutine(SummonMinions());
                summonTimer = summonCooldown;
                break;
        }
    }

    IEnumerator FallingRocksAttack()
    {
        yield return new WaitForSeconds(1f);

        List<GameObject> rocks = new List<GameObject>();
        float radius = 3f; // 골렘 주위를 도는 반경
        float orbitSpeed = 120f; // 초당 회전 각도
        float currentAngle = 0f;

        // 바위 4개 생성
        for (int i = 0; i < 4; i++)
        {
            float angle = i * (360f / 4); // 90도 간격으로 배치
            Vector3 spawnPos = transform.position + (Quaternion.Euler(0, angle, 0) * (Vector3.forward * radius));
            spawnPos.y = transform.position.y + 2f;
            
            GameObject rock = Instantiate(rockPrefab, spawnPos, Quaternion.identity);
            rocks.Add(rock);
        }

        // 바위들이 골렘 주위를 도는 단계
        float orbitDuration = 3f;
        float orbitTimer = 0f;
        
        while (orbitTimer < orbitDuration)
        {
            currentAngle += orbitSpeed * Time.deltaTime;
            for (int i = 0; i < rocks.Count; i++)
            {
                if (rocks[i] != null)
                {
                    float angle = currentAngle + (i * (360f / 4));
                    Vector3 orbitPosition = transform.position + (Quaternion.Euler(0, angle, 0) * (Vector3.forward * radius));
                    orbitPosition.y = transform.position.y + 2f;
                    rocks[i].transform.position = orbitPosition;
                }
            }
            orbitTimer += Time.deltaTime;
            yield return null;
        }

        // 순차적으로 바위 발사
        for (int i = 0; i < rocks.Count; i++)
        {
            if (rocks[i] != null)
            {
                StartCoroutine(LaunchRock(rocks[i]));
                yield return new WaitForSeconds(0.5f);
            }
        }

        yield return new WaitForSeconds(2f);
        isAttacking = false;
    }

    IEnumerator LaunchRock(GameObject rock)
    {
        if (rock == null) yield break;

        Vector3 startPos = rock.transform.position;
        Vector3 targetPos = player.position;
        float launchDuration = 0.5f;
        float timer = 0f;

        while (timer < launchDuration)
        {
            if (rock == null) yield break;

            timer += Time.deltaTime;
            float progress = timer / launchDuration;
            rock.transform.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }

        // 바위가 닿으면 데미지를 주고 파괴
        if (rock != null)
        {
            Collider[] hitColliders = Physics.OverlapSphere(rock.transform.position, 1f);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                    damageable?.TakeDamage(fallingRockDamage);
                }
            }
            Destroy(rock);
        }
    }

    IEnumerator EarthquakeAttack()
    {
        yield return new WaitForSeconds(1f);
        
        // 지진 이펙트 생성
        GameObject effect = Instantiate(earthquakeEffectPrefab, transform.position, Quaternion.identity);
        effect.GetComponent<EarthquakeEffect>().Initialize(earthquakeDamage, 2f);
        
        yield return new WaitForSeconds(2f);
        isAttacking = false;
    }

    IEnumerator SummonMinions()
    {
        yield return new WaitForSeconds(1f);

        for (int i = 0; i < 3; i++)
        {
            Vector3 randomPos = transform.position + Random.insideUnitSphere * 5f;
            randomPos.y = transform.position.y;
            Instantiate(minionPrefab, randomPos, Quaternion.identity);
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1f);
        isAttacking = false;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHp -= damageAmount;
        Debug.Log($"골렘 보스 피격! 남은 체력: {currentHp}");

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
        Destroy(gameObject, 5f);
    }

    // 애니메이션 이벤트로 호출될 메서드들
    public void OnBasicAttackEnd()
    {
        isAttacking = false;
        agent.isStopped = false;
    }

    public void OnRockAttackEnd()
    {
        isAttacking = false;
        agent.isStopped = false;
    }

    public void OnEarthquakeEnd()
    {
        isAttacking = false;
        agent.isStopped = false;
    }

    public void OnSummonEnd()
    {
        isAttacking = false;
        agent.isStopped = false;
    }
}