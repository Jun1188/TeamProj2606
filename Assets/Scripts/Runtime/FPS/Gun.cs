using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class Gun : MonoBehaviour
{
    public GunData gunData;
    public PlayerController playerController;
    public Transform muzzlePoint; 
    
    private float lastFireTime = 0f;
    private int currentAmmo;
    private bool isReloading = false;
    private bool isFiringPressed = false; 

    private IObjectPool<GameObject> bulletPool;
    private GameObject previousBulletPrefab; // 이전 총알 프리팹 추적용

    private void Awake()
    {
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

    private void Start()
    {
        if (gunData != null) currentAmmo = gunData.magSize;
    }

    private void Update()
    {
        if (gunData == null) return;

        // 연사 무기(SMG)인 경우 누르고 있으면 자동 사격
        if (isFiringPressed && gunData.isAutomatic && !isReloading)
        {
            Fire();
        }
    }
    public void SetupGunData(GunData newGunData)
    {
        if (newGunData == null) return;

        // 1. 데이터 갈아끼우기
        this.gunData = newGunData;
        
        // 2. 새 총의 최대 탄약 수로 장전 시키기
        currentAmmo = gunData.magSize; 
        isReloading = false;
        StopAllCoroutines(); // 재장전 중 스왑했다면 이전 재장전 코루틴 취소

        // 3. ✨ 총알 프리팹이 바뀌었다면 오브젝트 풀을 새로 갱신해 줍니다.
        if (previousBulletPrefab != gunData.bulletPrefab)
        {
            previousBulletPrefab = gunData.bulletPrefab;
            InitializePool();
        }

        Debug.Log($"[{gunData.gunName}] 장착 완료! 데미지: {gunData.damage}, 탄창: {gunData.magSize}");
    }
    private void InitializePool()
    {
        // 기존 풀이 있었다면 안전하게 비우거나 새로 할당합니다.
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

    public void SetFiringPressed(bool pressed)
    {
        isFiringPressed = pressed;
    }

    public bool Fire()
    {
        if (gunData == null || gunData.bulletPrefab == null) return false;
        if (Time.time < lastFireTime + gunData.fireRate) return false; 
        if (isReloading || currentAmmo <= 0) 
        {
            if (currentAmmo <= 0 && !isReloading) StartReload();
            return false;
        }

        Vector3 spawnPos = muzzlePoint != null ? muzzlePoint.position : transform.position;
        Quaternion spawnRot = muzzlePoint != null ? muzzlePoint.rotation : transform.rotation;

        GameObject bullet = bulletPool.Get();
        bullet.transform.position = spawnPos;
        bullet.transform.rotation = spawnRot;

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Setup(gunData.bulletSpeed, 3f); 
        }

        currentAmmo--;
        lastFireTime = Time.time; 

        if (playerController != null)
        {
            // 반동 방향 계산 (플레이어가 바라보는 방향의 정반대, 수평 유지)
            Vector3 recoilDir = -playerController.transform.forward; 
            recoilDir.y = 0f; // 수평 유지
            recoilDir.Normalize();
            
            // 단 한 번의 호출로 수직 카메라 반동 수치와 수평 몸통 반동 수치를 모두 전달합니다!
            playerController.AddRecoil(recoilDir, gunData.verticalRecoil, gunData.horizontalRecoil);
        }

        return true;
    }

    public void StartReload()
    {
        if (isReloading || gunData == null || currentAmmo == gunData.magSize) return;
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(gunData.reloadTime);
        currentAmmo = gunData.magSize;
        isReloading = false;
        Debug.Log($"{gunData.gunName} 재장전 완료!");
    }

    #region 오브젝트 풀 내부 함수들
    private GameObject CreateBullet()
    {
        GameObject bullet = Instantiate(gunData.bulletPrefab);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript?.SetPool(bulletPool);
        return bullet;
    }
    private void OnGetBullet(GameObject bullet) => bullet.SetActive(true);
    private void OnReleaseBullet(GameObject bullet) => bullet.SetActive(false);
    private void OnDestroyBullet(GameObject bullet) => Destroy(bullet);
    #endregion

    // 기존 Gun.cs 내부에 아래 두 함수를 추가(혹은 덮어쓰기)해 주세요!

    public void ChangeGunData(GunData newGunData)
    {
        gunData = newGunData;
        currentAmmo = gunData.magSize; // 새 총을 장착하면 탄창을 만땅으로 채워줍니다.
        isReloading = false;           // 재장전 중 오작동 방지 초기화
        Debug.Log($"[무기 교체 시스템] 현재 장착 무기: {gunData.gunName}");
    }

    public void ClearGunData()
    {
        gunData = null; // 맨손 상태
        Debug.Log("[무기 해제] 무기를 해제하여 공격할 수 없습니다.");
    }
}