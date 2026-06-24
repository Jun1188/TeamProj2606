using UnityEngine;
[CreateAssetMenu(fileName = "New_GunData", menuName = "ScriptableObjects/GunData", order = 1)]
public class GunData : ScriptableObject
{
    [Header("Gun Settings")]
    public string gunName = "Pistol";
    public float damage = 10f;
    public float fireRate = 0.5f;
    public float range = 100f;
    public GameObject bulletPrefab;

    [Header("Recoil Settings")]
    public float recoilForce = 5f; // The force applied to the player when firing
    public float recoilRecoverySpeed = 10f; // The speed at which the player recovers from recoil
}
