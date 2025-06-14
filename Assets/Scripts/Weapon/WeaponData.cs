using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Data/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public WeaponType weaponType;
    public float damage;
    public GameObject weaponModelPrefab;
    public int tier; // 무기 티어 (1, 2, 3, 4)
}
