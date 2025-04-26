using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    public int maxHp = 100;
    public int maxSp = 100;

    public float currentHp;
    public float currentSp;

    public float attackStamina = 15;
    public float jumpStamina = 5;
    public float dashStamina = 10;
    public float runStamina = 2;
    public float skillStamina = 30;

    public float staminaRecoveryRate = 15f; // 초당 회복량
    public float recoveryDelay = 1f;        // 마지막 소모 후 몇 초 뒤에 회복 시작

    public float lastStaminaUseTime;       // 마지막 스태미너 사용 시간

    public PlayerUI playerUI;

    void Start()
    {
        currentHp = maxHp;
        currentSp = maxSp;
        lastStaminaUseTime = Time.time; // 초기화
        playerUI.UpdateUI();
    }

    void Update()
    {
        if (currentSp <= 0)
        {
            currentSp = 0;
        }

        // 1초가 지나고 스태미너가 max가 아니라면 회복 시작
        if (Time.time - lastStaminaUseTime > recoveryDelay && currentSp < maxSp)
        {
            currentSp += staminaRecoveryRate * Time.deltaTime;
            currentSp = Mathf.Min(currentSp, maxSp);
            playerUI.UpdateUI();
        }
    }

    public void RunStamina()
    {
        if (currentSp <= 0) return;
        currentSp -= runStamina;
        lastStaminaUseTime = Time.time;
        playerUI.UpdateUI();
    }

    public void AttackStamina()
    {
        if (currentSp <= 0) return;
        currentSp -= attackStamina;
        lastStaminaUseTime = Time.time;
        playerUI.UpdateUI();
    }

    public void JumpStamina()
    {
        if (currentSp <= 0) return;
        currentSp -= jumpStamina;
        lastStaminaUseTime = Time.time;
        playerUI.UpdateUI();
    }

    public void DashStamina()
    {
        if (currentSp <= 0) return;
        currentSp -= dashStamina;
        lastStaminaUseTime = Time.time;
        playerUI.UpdateUI();
    }

    public void SkillStamina()
    {
        if (currentSp <= 0) return;
        currentSp -= skillStamina; // 스킬 사용 시 소모되는 스태미너
        lastStaminaUseTime = Time.time;
        playerUI.UpdateUI();
    }
}