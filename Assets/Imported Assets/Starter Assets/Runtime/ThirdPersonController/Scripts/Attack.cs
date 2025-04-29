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
        thirdPersonController.canMove = false;
        animator.SetTrigger("Attack");
    }

    public void attackEnd()
    {
        thirdPersonController.canMove = true;
    }

    public void EnableWeaponCollider()
    {
        weapon.EnableDamageCollider();
    }
    
    public void DisableWeaponCollider()
    {
        weapon.DisableDamageCollider();
    }
}
