using System;
using Firebase.Auth;
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
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI userNameText;
    
    SkillController skillController;
    PlayerDataManager playerDataManager;
    FirebaseInit firebaseInit;

    public PlayerStatus playerStatus;

    private void Start()
    {
        skillController = FindFirstObjectByType<SkillController>();
        playerDataManager = FindFirstObjectByType<PlayerDataManager>();
        imgSkillCooldown.fillAmount = 0f;
        
        // 유저 정보 출력
        var user = FirebaseAuth.DefaultInstance.CurrentUser;

        if (user != null)
        {
            // 추후 DisplayName을 Firebase에서 가져오는 로직 추가 예정
            string displayName = string.IsNullOrEmpty(user.Email) ? "게스트" : user.Email;
            userNameText.text = $"{displayName}";
        }
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
    
    public void UpdateGold()
    {
        if (goldText != null)
        {
            goldText.text =  playerDataManager.Gold + " G";
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
