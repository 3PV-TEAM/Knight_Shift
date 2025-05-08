using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class AttackController : MonoBehaviour
{
    Animator animator;
    ThirdPersonController thirdPersonController;
    private WeaponManager weaponManager;
    public Weapon weapon;

    void Start()    
    {
        animator = GetComponent<Animator>();
        weaponManager = FindFirstObjectByType<WeaponManager>();
        thirdPersonController = GetComponent<ThirdPersonController>();
    }

    void Update()
    {
        weapon = weaponManager.currentWeapon;
    }

    public void attackProcess()
    {
        animator.SetTrigger("Attack");
    }

    public void EnableWeaponCollider()
    {
        animator.SetBool("isAttacking", true);
        weapon.EnableDamageCollider();
    }
    
    public void DisableWeaponCollider()
    {
        animator.SetBool("isAttacking", false);
        weapon.DisableDamageCollider();
    }
}
