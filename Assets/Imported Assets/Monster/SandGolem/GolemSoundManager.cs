using UnityEngine;

public class GolemSoundManager : MonoBehaviour
{
    [Header("골렘 공격 효과음")]
    [SerializeField] private AudioClip[] leftPunchSounds;
    [SerializeField] private AudioClip[] rightPunchSounds;
    [SerializeField] private AudioClip[] groundSlamSounds; // 바닥치기 (바위송곳 포함)
    
    [Header("골렘 상태 효과음")]
    [SerializeField] private AudioClip[] shieldPatternSounds; // 쉴드 패턴 종료 시 재생
    [SerializeField] private AudioClip[] earthquakeSounds;
    [SerializeField] private AudioClip[] deathSounds;
    
    [Header("골렘 이동 효과음")]
    [SerializeField] private AudioClip[] footstepSounds;
    
    [Header("볼륨 설정")]
    [SerializeField] private float attackVolume = 0.8f;
    [SerializeField] private float movementVolume = 0.6f;
    [SerializeField] private float specialVolume = 1.0f;
    
    private AudioSource audioSource;
    private GolemAI golemAI;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        golemAI = GetComponent<GolemAI>();
    }
    
    void Start()
    {
        // 오디오 소스 기본 설정
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D 사운드
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 20f;
    }
    
    // 왼손 펀치 사운드
    public void PlayLeftPunchSound()
    {
        PlayRandomSound(leftPunchSounds, attackVolume);
        Debug.Log("왼손 펀치 효과음 재생!");
    }
    
    // 오른손 펀치 사운드
    public void PlayRightPunchSound()
    {
        PlayRandomSound(rightPunchSounds, attackVolume);
        Debug.Log("오른손 펀치 효과음 재생!");
    }
    
    // 바닥치기 사운드 (바위송곳 포함)
    public void PlayGroundSlamSound()
    {
        PlayRandomSound(groundSlamSounds, specialVolume);
        Debug.Log("바닥치기 효과음 재생!");
    }
    
    // 쉴드 패턴 종료 시 사운드 (2페이즈 효과음)
    public void PlayShieldPatternEndSound()
    {
        PlayRandomSound(shieldPatternSounds, specialVolume);
        Debug.Log("쉴드 패턴 종료 효과음 재생! (2페이즈 완료)");
    }
    
    // 지진 사운드
    public void PlayEarthquakeSound()
    {
        PlayRandomSound(earthquakeSounds, specialVolume);
        Debug.Log("지진 효과음 재생!");
    }
    
    // 발소리 사운드
    public void PlayFootstepSound()
    {
        PlayRandomSound(footstepSounds, movementVolume);
    }
    
    // 사망 사운드
    public void PlayDeathSound()
    {
        PlayRandomSound(deathSounds, specialVolume);
        Debug.Log("골렘 사망 효과음 재생!");
    }
    
    // 페이즈별 볼륨 조정
    public void PlayAttackSoundWithPhase(AudioClip[] sounds)
    {
        float volume = attackVolume;
        if (golemAI != null && golemAI.currentHp <= golemAI.maxHp * 0.5f) // 2페이즈
        {
            volume *= 1.2f; // 2페이즈에서는 볼륨 증가
        }
        PlayRandomSound(sounds, volume);
    }
    
    // 랜덤 사운드 재생 헬퍼 메서드
    private void PlayRandomSound(AudioClip[] sounds, float volume)
    {
        if (sounds == null || sounds.Length == 0) return;
        
        AudioClip randomClip = sounds[Random.Range(0, sounds.Length)];
        if (randomClip != null)
        {
            audioSource.PlayOneShot(randomClip, volume);
        }
    }
    
    // 특정 사운드 재생 (배열이 아닌 단일 클립용)
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
    
    // 거리 기반 볼륨 조정
    public void PlaySoundWithDistance(AudioClip[] sounds, float baseVolume)
    {
        if (sounds == null || sounds.Length == 0) return;
        
        // 플레이어와의 거리 계산
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            float volumeMultiplier = Mathf.Clamp01(1f - (distance / audioSource.maxDistance));
            PlayRandomSound(sounds, baseVolume * volumeMultiplier);
        }
        else
        {
            PlayRandomSound(sounds, baseVolume);
        }
    }
}
