using System;
using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;

public class SkillController : MonoBehaviour
{
    private ThirdPersonController _thirdPersonController;
    private PlayerStatus _playerStatus;
    Animator animator;

    void Start()
    {
        _thirdPersonController = GetComponent<ThirdPersonController>();
        _playerStatus = GetComponent<PlayerStatus>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (_thirdPersonController.canMove && _playerStatus.currentSp >= 20)
            {
                _thirdPersonController.canMove = false;
                skillActivate();
            }
        }
    }

    void skillActivate()
    {
        animator.SetTrigger("skill");
        _playerStatus.SkillStamina();
        Debug.Log("Weapon Skill Activated");
    }
}
