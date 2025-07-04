using System;
using Firebase.Auth;
using TMPro;
using Unity.Cinemachine;
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
    public GameObject OptionPanel;
    private CinemachineCamera vcam;
    
    SkillController skillController;
    PlayerDataManager playerDataManager;
    FirebaseInit firebaseInit;

    public PlayerStatus playerStatus;

    private void Start()
    {
        skillController = FindFirstObjectByType<SkillController>();
        playerDataManager = FindFirstObjectByType<PlayerDataManager>();
        vcam = FindFirstObjectByType<CinemachineCamera>();
        
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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleOptionPanel();
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
    
    public void ToggleOptionPanel()
    {
        if (OptionPanel != null)
        {
            OptionPanel.SetActive(!OptionPanel.activeSelf);
            Time.timeScale = OptionPanel.activeSelf ? 0f : 1f;
            Cursor.lockState = OptionPanel.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
            vcam.gameObject.SetActive(!OptionPanel.activeSelf);
        }
    }
}
