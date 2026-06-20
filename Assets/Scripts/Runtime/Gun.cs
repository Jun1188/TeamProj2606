using UnityEngine;

public class Gun : MonoBehaviour
{
    public GunData gunData;
    public PlayerController playerController;
    private float lastFireTime = 0f;

    private void Update()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= lastFireTime + gunData.fireRate)
        {
            Fire();
            lastFireTime = Time.time;
        }
    }

    void Fire()
    {
        // Instantiate the bullet prefab at the gun's position and rotation
        GameObject bullet = Instantiate(gunData.bulletPrefab, transform.position, transform.rotation);
        // Add force to the bullet to propel it forward
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(transform.right * gunData.range, ForceMode.Impulse);

        playerController?.rb.AddForce(-transform.right * gunData.recoilForce, ForceMode.Impulse);
    }
}
