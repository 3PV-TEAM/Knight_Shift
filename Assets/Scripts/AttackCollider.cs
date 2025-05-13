using UnityEngine;

public class AttackCollider : MonoBehaviour
{
    public float damageAmount = 10f;
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            Debug.Log($"공격 콜라이더 {name}가 {other.name}에게 {damageAmount} 데미지를 줍니다.");
            damageable.TakeDamage(damageAmount);
        }
    }
}