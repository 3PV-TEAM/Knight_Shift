using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    public Transform weaponHolder;
    public Weapon currentWeapon;
    [SerializeField] WeaponData currentWeaponData;
    [SerializeField] WeaponData StartWeaponData;
    [SerializeField] WeaponData testWeaponData;
    [SerializeField] private Animator animator;
    
    // 싱글톤 인스턴스
    public static WeaponManager Instance { get; private set; }
    
    private void Awake()
    {
        Debug.Log("WeaponManager Awake");
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        currentWeaponData = StartWeaponData;
        EquipWeapon(currentWeaponData);
    }

    public void EquipWeapon(WeaponData newWeaponData)
    {
        // 이전 무기 제거
        if (currentWeapon != null)
        {
            Destroy(currentWeapon.gameObject);
        }

        // 새 무기 프리팹 생성 및 장착
        GameObject newWeaponObj = Instantiate(newWeaponData.weaponModelPrefab, weaponHolder);
        newWeaponObj.transform.localPosition = Vector3.zero;
        newWeaponObj.transform.localRotation = Quaternion.Euler(0, 180f, 0);

        // Weapon 스크립트 가져오기
        currentWeapon = newWeaponObj.GetComponent<Weapon>();

        // 무기 데이터 설정
        if (currentWeapon != null)
        {
            currentWeapon.weaponData = newWeaponData;
            currentWeaponData = newWeaponData; // 현재 무기 데이터도 업데이트
            Debug.Log("무기 장착: " + newWeaponData.weaponName);

            currentWeapon.ApplyWeaponTypeToAnimator(animator);

            Debug.Log($"무기 변경됨: {newWeaponData.weaponName}, 타입: {newWeaponData.weaponType}");
        }
        else
        {
            Debug.LogWarning("무기 프리팹에 Weapon 스크립트가 없습니다!");
        }
    }
}