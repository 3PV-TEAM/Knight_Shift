using UnityEngine;

public class BodyWeapon : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private Collider damageCollider;
    [SerializeField] public string targetTag;

    private void Awake()
    {
        if (damageCollider == null)
            damageCollider = GetComponent<Collider>();
        DisableDamageCollider();
    }

    public void EnableDamageCollider()
    {
        damageCollider.enabled = true;
    }

    public void DisableDamageCollider()
    {
        damageCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
{
    Debug.Log($"충돌 감지: {other.name}"); // ✅ 1. 충돌 감지 여부 확인
    if (!damageCollider.enabled) return;
    if (!other.CompareTag(targetTag)) return;

    IDamageable damageable = other.GetComponent<IDamageable>();
    if (damageable != null)
    {
        Debug.Log($"{other.name}에게 데미지 {damage} 적용!"); // ✅ 2. 데미지 전달 확인
        damageable.TakeDamage(damage);
    }
}

}
