using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Image hpBar;
    public Image spBar;
    public Image imgSkillCooldown;

    public PlayerStatus playerStatus;

    private void Start()
    {
        imgSkillCooldown.fillAmount = 0f;
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
}
