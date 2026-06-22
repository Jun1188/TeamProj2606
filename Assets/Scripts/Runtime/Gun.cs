using UnityEngine;
using UnityEngine.Pool;

public class Gun : MonoBehaviour
{
    public GunData gunData;
    public PlayerController playerController;
    private float lastFireTime = 0f;

    private IObjectPool<GameObject> bulletPool;
    private void Awake()
    {
        // 오브젝트 풀 초기화 설정
        bulletPool = new ObjectPool<GameObject>(
            createFunc: CreateBullet,       
            actionOnGet: OnGetBullet,       
            actionOnRelease: OnReleaseBullet,
            actionOnDestroy: OnDestroyBullet,
            collectionCheck: true,
            defaultCapacity: 20,            
            maxSize: 50                    
        );
    }
    public void Fire()
    {
        if (gunData == null || gunData.bulletPrefab == null || Time.time < lastFireTime + gunData.fireRate)
        {
            Debug.LogWarning("GunData, bulletPrefab missing or FireRate cooldown.");
            return;
        }

        GameObject bullet = bulletPool.Get();

        
        bullet.transform.position = transform.position + transform.up * 0.5f;
        bullet.transform.rotation = transform.rotation;

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; 
            rb.angularVelocity = Vector3.zero; 
            rb.AddForce(transform.up * gunData.range, ForceMode.Impulse);
        }

        
        playerController?.rb.AddForce(-transform.up * gunData.recoilForce, ForceMode.Impulse);

        lastFireTime = Time.time;
    }

    #region 오브젝트 풀 내부 함수들
    private GameObject CreateBullet()
    {
        GameObject bullet = Instantiate(gunData.bulletPrefab);
        
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript?.SetPool(bulletPool);
        
        return bullet;
    }

    private void OnGetBullet(GameObject bullet)
    {
        bullet.SetActive(true); 
    }

    private void OnReleaseBullet(GameObject bullet)
    {
        bullet.SetActive(false); 
    }

    private void OnDestroyBullet(GameObject bullet)
    {
        Destroy(bullet); 
    }
    #endregion
}
