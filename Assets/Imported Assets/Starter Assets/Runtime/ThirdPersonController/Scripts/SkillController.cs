using System;
using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;

public class SkillController : MonoBehaviour
{
    private ThirdPersonController _thirdPersonController;
    private PlayerStatus _playerStatus;
    public PlayerUI playerUI;
    public float skillCooldownTime = 10f;
    public float currentSkillCooldown;
    private bool isCooldown = false;
    
    Animator animator;

    void Start()
    {
        _thirdPersonController = GetComponent<ThirdPersonController>();
        _playerStatus = GetComponent<PlayerStatus>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isCooldown)
        {
            currentSkillCooldown -= Time.deltaTime;

            if (currentSkillCooldown <= 0f)
            {
                isCooldown = false;
                playerUI.imgSkillCooldown.fillAmount = 0;
                Debug.Log("스킬 사용 가능");
            }
            else
            {
                playerUI.imgSkillCooldown.fillAmount = currentSkillCooldown / skillCooldownTime;
            }
        }
        
        if (Input.GetKeyDown(KeyCode.C) && !isCooldown)
        {
            if (_thirdPersonController.canMove && _playerStatus.currentSp >= 30)
            {
                _thirdPersonController.canMove = false;
                skillActivate();
            }
        }
    }

    void skillActivate()
    {
        isCooldown = true;
        currentSkillCooldown = skillCooldownTime;
        playerUI.imgSkillCooldown.fillAmount = 1;
        
        animator.SetTrigger("skill");
        _playerStatus.SkillStamina();
        Debug.Log("Weapon Skill Activated");
    }
}
