using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EarthquakeEffect : MonoBehaviour
{
    [Header("이펙트 설정")]
    [SerializeField] private ParticleSystem shockwaveParticle; // 충격파 파티클

    private float damage;
    private float duration;
    private float tickRate = 0.5f; // 데미지를 주는 간격
    private float radius = 5f;
    private float timer = 0f;

    public void Initialize(float dmg, float dur)
    {
        damage = dmg;
        duration = dur;
        
        // 파티클 시스템이 있다면 재생
        if (shockwaveParticle != null)
        {
            shockwaveParticle.Play();
        }
        
        StartCoroutine(DamageRoutine());
    }

    IEnumerator DamageRoutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // 범위 내 플레이어 검사 및 데미지
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                    damageable?.TakeDamage(damage);
                }
            }

            elapsedTime += tickRate;
            yield return new WaitForSeconds(tickRate);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}