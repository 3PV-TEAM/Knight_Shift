using UnityEngine;
using System.Collections;

public class GroundBlastArea : MonoBehaviour
{
    private float damage;
    private float radius;

    public void Initialize(float blastDamage, float blastRadius, float duration)
    {
        damage = blastDamage;
        radius = blastRadius;
        
        StartCoroutine(BlastSequence(duration));
    }

    IEnumerator BlastSequence(float duration)
    {
        yield return new WaitForSeconds(0.2f); // 폭발 예고 시간
        
        // 즉시 데미지 적용
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    Debug.Log($"땅장판 폭발 데미지! {damage} 데미지");
                }
            }
        }
        
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
}
