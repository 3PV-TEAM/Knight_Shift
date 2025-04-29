using System;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Image hpBar;
    public Image spBar;
    public Image imgSkillCooldown;
    public TextMeshProUGUI skillCooldownText;
    SkillController skillController;

    public PlayerStatus playerStatus;

    private void Start()
    {
        skillController = FindFirstObjectByType<SkillController>();
        imgSkillCooldown.fillAmount = 0f;
    }

    private void Update()
    {
        UpdateSkillCooldown(skillController.currentSkillCooldown);

        if (skillController.currentSkillCooldown <= 0)
        {
            skillCooldownText.enabled = false;
        }
        else
        {
            skillCooldownText.enabled = true;
        }
    }

    public void UpdateUI()
    {
        if (playerStatus != null)
        {
            if (hpBar != null)
            {
                hpBar.fillAmount = playerStatus.currentHp / playerStatus.maxHp;
            }
            
            if (spBar != null)
            {
                spBar.fillAmount = playerStatus.currentSp / playerStatus.maxSp;
            }
        }
    }
    
    public void UpdateSkillCooldown(float cooldown)
    {
        if (skillCooldownText != null)
        {
            skillCooldownText.text = cooldown.ToString("F1");
        }
    }
}
