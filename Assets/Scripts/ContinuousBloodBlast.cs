using UnityEngine;

public class ContinuousBloodBlast : MonoBehaviour
{
    private float damage;
    private Transform sourceTransform;
    private Transform targetTransform;
    private float duration;
    private bool isBlasting = false;
    
    private ParticleSystem blastParticle;
    
    [SerializeField] private float damageInterval = 0.4f;
    [SerializeField] private float blastRange = 50f;
    private float lastDamageTime = 0f;
    
    // 실제 파티클 도달 거리 추적
    private float actualParticleRange = 0f;

    public void Initialize(float blastDamage, Transform source, Transform target, float blastDuration, float range = 50f)
    {
        damage = blastDamage;
        sourceTransform = source;
        targetTransform = target;
        duration = blastDuration;
        blastRange = range;
        blastParticle = GetComponent<ParticleSystem>();
        
        Debug.Log($"연속 블라스트 초기화 - 데미지: {damage}, 목표 사거리: {blastRange}m");
    }

    public void StartContinuousBlast()
    {
        isBlasting = true;
        
        if (blastParticle != null)
        {
            var main = blastParticle.main;
            main.duration = duration;
            main.loop = false;
            
            // 파티클이 실제로 목표 거리까지 도달하도록 계산
            float particleSpeed = 25f; // 속도 대폭 증가
            float requiredLifetime = (blastRange * 1.2f) / particleSpeed; // 여유분 20% 추가
            
            main.startSpeed = particleSpeed;
            main.startLifetime = requiredLifetime;
            
            // 실제 파티클 도달 거리 계산
            actualParticleRange = particleSpeed * requiredLifetime;
            
            Debug.Log($"파티클 설정: 속도={particleSpeed}m/s, 생명시간={requiredLifetime}s");
            Debug.Log($"실제 파티클 도달거리: {actualParticleRange}m (목표: {blastRange}m)");
            
            // Shape 설정 - 더 집중된 빔 형태
            var shape = blastParticle.shape;
            if (shape.shapeType == ParticleSystemShapeType.Cone)
            {
                shape.radius = 0.5f; // 시작 반경 줄임
                shape.angle = 8f; // 각도를 좁혀서 더 집중된 빔
                shape.length = 0f;
            }
            else if (shape.shapeType == ParticleSystemShapeType.Box)
            {
                shape.scale = new Vector3(1f, 1f, actualParticleRange);
            }
            
            // 파티클 크기와 밀도 조정
            main.startSize = 0.3f;
            main.maxParticles = 2000; // 파티클 수 증가
            
            var emission = blastParticle.emission;
            emission.rateOverTime = 300; // 더 높은 방출률로 밀도 증가
            
            // Velocity Over Lifetime으로 일정한 속도 보장
            var velocityOverLifetime = blastParticle.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.z = particleSpeed;
            
            // Size Over Lifetime으로 거리에 따른 크기 유지
            var sizeOverLifetime = blastParticle.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);    // 시작: 100% 크기
            sizeCurve.AddKey(0.5f, 1.2f); // 중간: 120% 크기
            sizeCurve.AddKey(1f, 1.5f);   // 끝: 150% 크기 (거리 보상)
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            
            // Color Over Lifetime으로 거리에 따른 투명도 조정
            var colorOverLifetime = blastParticle.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient colorGradient = new Gradient();
            colorGradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.red, 0f), 
                    new GradientColorKey(Color.red, 0.8f),
                    new GradientColorKey(Color.red, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f),    // 시작: 완전 불투명
                    new GradientAlphaKey(0.8f, 0.8f), // 80% 지점: 80% 투명도
                    new GradientAlphaKey(0.3f, 1f)    // 끝: 30% 투명도
                }
            );
            colorOverLifetime.color = colorGradient;
            
            // 방향 설정
            if (targetTransform != null && sourceTransform != null)
            {
                Vector3 direction = (targetTransform.position - sourceTransform.position).normalized;
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            blastParticle.Play();
            Debug.Log($"연속 블라스트 시작! 실제 도달거리: {actualParticleRange}m");
        }
        
        Invoke(nameof(StopContinuousBlast), duration);
    }

    void Update()
    {
        if (!isBlasting || targetTransform == null || sourceTransform == null) return;

        // 하운드 입 위치 따라가기
        transform.position = sourceTransform.position;
        
        // 플레이어 방향으로 계속 회전
        Vector3 direction = (targetTransform.position - sourceTransform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 3f * Time.deltaTime);
        }
        
        // 데미지 적용
        if (Time.time - lastDamageTime >= damageInterval)
        {
            CheckForPlayerDamage();
            lastDamageTime = Time.time;
        }
    }

    void CheckForPlayerDamage()
    {
        if (targetTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);
        Vector3 directionToPlayer = (targetTransform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        
        // 실제 파티클 도달 거리를 데미지 범위로 사용 (약간의 여유분 포함)
        float effectiveDamageRange = Mathf.Min(actualParticleRange * 0.9f, blastRange);
        
        Debug.Log($"데미지 체크: 거리={distanceToPlayer:F1}m, 각도={angle:F1}°, 유효범위={effectiveDamageRange:F1}m");
        
        if (distanceToPlayer <= effectiveDamageRange && angle <= 25f) // 각도도 좁혀서 정확성 향상
        {
            IDamageable damageable = targetTransform.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                Debug.Log($"★ 블라스트 데미지 적용! {damage} 데미지 (거리: {distanceToPlayer:F1}m/{effectiveDamageRange:F1}m)");
            }
        }
        else
        {
            Debug.Log($"데미지 범위 밖: 거리={distanceToPlayer:F1}m > {effectiveDamageRange:F1}m 또는 각도={angle:F1}° > 25°");
        }
    }

    public void StopContinuousBlast()
    {
        isBlasting = false;
        
        if (blastParticle != null)
        {
            blastParticle.Stop();
            Debug.Log("연속 블라스트 정지!");
        }
        
        Destroy(gameObject, 2f); // 파티클이 완전히 사라질 때까지 대기
    }
    
    // 디버깅용 기즈모
    void OnDrawGizmosSelected()
    {
        if (isBlasting && sourceTransform != null)
        {
            // 실제 파티클 도달 범위 표시 (녹색)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(sourceTransform.position, actualParticleRange);
            
            // 설정된 블라스트 범위 표시 (빨간색)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(sourceTransform.position, blastRange);
            
            // 블라스트 방향 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(sourceTransform.position, transform.forward * actualParticleRange);
        }
    }
}
