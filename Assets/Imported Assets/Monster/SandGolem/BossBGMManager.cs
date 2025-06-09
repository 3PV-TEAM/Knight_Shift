using UnityEngine;
using System.Collections;

public class BossBGMManager : MonoBehaviour
{
    [Header("BGM 클립")]
    [SerializeField] private AudioClip bossBattleBGM;
    
    [Header("보스 몬스터 할당")]
    [SerializeField] private GameObject[] bossMonsters; // 하운드, 골렘 할당
    
    [Header("볼륨 설정")]
    [SerializeField] private float bossVolume = 0.7f;
    [SerializeField] private float fadeSpeed = 2f;
    
    [Header("보스 감지 설정")]
    [SerializeField] private float bossDetectionRange = 20f;
    [SerializeField] private bool useProximityDetection = true;
    
    private bool isBossBattleActive = false;
    private Transform player;
    private GameObject currentActiveBoss;
    private AudioSource audioSource;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        SetupAudioSource();
    }
    
    void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.clip = bossBattleBGM;
    }
    
    void Update()
    {
        if (useProximityDetection && player != null && !isBossBattleActive)
        {
            CheckAssignedBossesNearby();
        }
        
        // 할당된 보스들의 생존 상태 체크
        CheckBossAliveStatus();
    }
    
    void CheckAssignedBossesNearby()
    {
        foreach (GameObject boss in bossMonsters)
        {
            if (boss == null || !boss.activeInHierarchy) continue;
            
            // 보스가 살아있는지 확인
            if (!IsBossAlive(boss)) continue;
            
            // 플레이어와의 거리 체크
            float distance = Vector3.Distance(player.position, boss.transform.position);
            if (distance <= bossDetectionRange)
            {
                StartBossBattle(boss);
                return;
            }
        }
    }
    
    void CheckBossAliveStatus()
    {
        if (!isBossBattleActive || currentActiveBoss == null) return;
        
        // 현재 활성 보스가 죽었는지 확인
        if (!IsBossAlive(currentActiveBoss))
        {
            OnBossDefeated();
        }
    }
    
    bool IsBossAlive(GameObject boss)
    {
        var houndAI = boss.GetComponent<HoundAI>();
        if (houndAI != null) return houndAI.currentHp > 0;
        
        var golemAI = boss.GetComponent<GolemAI>();
        if (golemAI != null) return golemAI.currentHp > 0;
        
        return true;
    }
    
    public void StartBossBattle(GameObject boss)
    {
        if (isBossBattleActive) return;
        
        currentActiveBoss = boss;
        isBossBattleActive = true;
        
        Debug.Log($"보스 전투 시작: {boss.name}");
        
        // BGM 시작
        StartCoroutine(FadeInBossBGM());
        
        // 체력바 표시
        ShowBossHealthBar(boss);
    }
    
    public void OnBossDefeated()
    {
        if (!isBossBattleActive) return;
        
        Debug.Log($"보스 처치: {currentActiveBoss?.name}");
        
        isBossBattleActive = false;
        currentActiveBoss = null;
        
        // BGM 정지
        StartCoroutine(FadeOutBossBGM());
        
        // 체력바 숨기기
        var healthBar = FindObjectOfType<SimpleBossHealthBar>();
        healthBar?.HideBossHealthBar();
    }
    
    void ShowBossHealthBar(GameObject boss)
    {
        var healthBar = FindObjectOfType<SimpleBossHealthBar>();
        if (healthBar != null)
        {
            float maxHp = GetBossMaxHP(boss);
            float currentHp = GetBossCurrentHP(boss);
            healthBar.ShowBossHealthBar(boss.name, maxHp, currentHp);
        }
    }
    
    IEnumerator FadeInBossBGM()
    {
        if (bossBattleBGM != null && audioSource != null)
        {
            audioSource.Play();
            
            float elapsed = 0f;
            float duration = 1f / fadeSpeed;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                audioSource.volume = Mathf.Lerp(0f, bossVolume, progress);
                yield return null;
            }
            
            audioSource.volume = bossVolume;
        }
    }
    
    IEnumerator FadeOutBossBGM()
    {
        if (audioSource != null)
        {
            float elapsed = 0f;
            float duration = 1f / fadeSpeed;
            float startVolume = audioSource.volume;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                audioSource.volume = Mathf.Lerp(startVolume, 0f, progress);
                yield return null;
            }
            
            audioSource.volume = 0f;
            audioSource.Stop();
        }
    }
    
    float GetBossMaxHP(GameObject boss)
    {
        var houndAI = boss.GetComponent<HoundAI>();
        if (houndAI != null) return houndAI.maxHp;
        
        var golemAI = boss.GetComponent<GolemAI>();
        if (golemAI != null) return golemAI.maxHp;
        
        return 100f;
    }
    
    float GetBossCurrentHP(GameObject boss)
    {
        var houndAI = boss.GetComponent<HoundAI>();
        if (houndAI != null) return houndAI.currentHp;
        
        var golemAI = boss.GetComponent<GolemAI>();
        if (golemAI != null) return golemAI.currentHp;
        
        return 100f;
    }
    
    void OnDrawGizmosSelected()
    {
        if (player != null && useProximityDetection)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, bossDetectionRange);
        }
        
        // 할당된 보스들 표시
        Gizmos.color = Color.yellow;
        foreach (GameObject boss in bossMonsters)
        {
            if (boss != null)
            {
                Gizmos.DrawWireSphere(boss.transform.position, 2f);
            }
        }
    }
}
