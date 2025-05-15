using UnityEngine;

public class FallingRock : MonoBehaviour
{
    private float damage;
    private Vector3 targetPosition;
    private float fallSpeed = 15f;

    public void Initialize(float dmg, Vector3 targetPos)
    {
        damage = dmg;
        targetPosition = targetPos;
    }

    void Update()
    {
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        if (transform.position.y <= targetPosition.y)
        {
            OnImpact();
        }
    }

    void OnImpact()
    {
        // 충돌 범위 내의 플레이어에게 데미지
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                damageable?.TakeDamage(damage);
            }
        }

        // 이펙트 재생 (필요시 추가)
        Destroy(gameObject);
    }
}