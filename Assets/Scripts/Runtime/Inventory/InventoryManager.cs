using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Global Mouse Carriage (마우스 쥐고 있는 임시 슬롯)")]
    public ItemSocket mouseCarriageSlot;     
    private ItemStack mouseCarriageItem = null; 

    [Header("Player References")]
    public PlayerController playerController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (mouseCarriageSlot != null) mouseCarriageSlot.ClearSlot();
    }

    private void Update()
    {
        if (mouseCarriageItem != null && mouseCarriageItem.item != null && mouseCarriageItem.amount > 0)
        {
            mouseCarriageSlot.gameObject.SetActive(true);
            mouseCarriageSlot.transform.position = Input.mousePosition;
        }
        else
        {
            if (mouseCarriageSlot != null) mouseCarriageSlot.gameObject.SetActive(false);
        }
    }

    public void HandleSlotLeftClick(Inventory inventory, int clickedIndex, InventoryUI uiSource)
    {
        ItemStack clickedBackendSlot = inventory.slots[clickedIndex];

        if (mouseCarriageItem == null || mouseCarriageItem.item == null)
        {
            if (clickedBackendSlot != null && clickedBackendSlot.item != null)
            {
                mouseCarriageItem = clickedBackendSlot;
                inventory.slots[clickedIndex] = null;
            }
        }
        else
        {
            // 🔒 [버그 수정] Enum 기반 규칙 검사 통과 여부 확인
            if (!IsSlotPlacementAllowed(inventory, clickedIndex, mouseCarriageItem.item))
            {
                Debug.LogWarning($"[제한] {clickedIndex}번 슬롯에는 Weapon 타입만 장착할 수 있습니다.");
                return; 
            }

            if (clickedBackendSlot == null || clickedBackendSlot.item == null)
            {
                inventory.slots[clickedIndex] = mouseCarriageItem;
                mouseCarriageItem = null;
            }
            else if (clickedBackendSlot.item != mouseCarriageItem.item)
            {
                ItemStack temp = clickedBackendSlot;
                inventory.slots[clickedIndex] = mouseCarriageItem;
                mouseCarriageItem = temp;
            }
            else if (clickedBackendSlot.item == mouseCarriageItem.item)
            {
                int maxStack = mouseCarriageItem.maxStackSize;
                int canAdd = maxStack - clickedBackendSlot.amount;

                if (canAdd > 0)
                {
                    int transferAmount = Mathf.Min(canAdd, mouseCarriageItem.amount);
                    clickedBackendSlot.amount += transferAmount;
                    mouseCarriageItem.amount -= transferAmount;

                    if (mouseCarriageItem.amount <= 0) mouseCarriageItem = null;
                }
            }
        }

        if (mouseCarriageItem != null)
            mouseCarriageSlot.SetItem(mouseCarriageItem.item, mouseCarriageItem.amount);
        else
            mouseCarriageSlot.ClearSlot();

        if (playerController != null && inventory == playerController.playerInventory)
        {
            CheckWeaponEquip(inventory);
        }

        InventoryUI[] allActiveUIs = FindObjectsByType<InventoryUI>(FindObjectsSortMode.None);
        foreach (InventoryUI ui in allActiveUIs)
        {
            if (ui.gameObject.activeSelf) ui.RefreshAllUI();
        }
    }

    private bool IsSlotPlacementAllowed(Inventory inventory, int slotIndex, ItemDataSO item)
    {
        if (item == null) return true;

        // 플레이어 가방의 0번 슬롯(장비칸) 검사 규칙
        if (playerController != null && inventory == playerController.playerInventory && slotIndex == 0)
        {
            // 💡 킹펀치! 아이템이 WeaponItemSO 스크립트를 사용하는 '무기'인지 검사합니다.
            return item is WeaponItemSO;
        }

        return true; // 일반 슬롯이나 상자 슬롯은 제약 없음
    }

    public void CheckWeaponEquip(Inventory playerInventory)
    {
        if (playerInventory.slots.Length > 0)
        {
            ItemStack firstSlot = playerInventory.slots[0];
            
            // 💡 패턴 매칭: 0번 칸에 아이템이 있고, 그 아이템이 WeaponItemSO 타입이 맞다면
            // 자동으로 'weaponItem'이라는 변수로 형변환(Casting)을 시켜줍니다!
            if (firstSlot != null && firstSlot.item is WeaponItemSO weaponItem)
            {
                if (playerController.gun != null)
                {
                    playerController.gun.gameObject.SetActive(true); // 총 오브젝트 활성화
                    
                    // 꽂혀있는 무기 아이템(WeaponItemSO)의 고유 gunData를 총에 주입!
                    playerController.gun.SetupGunData(weaponItem.gunData); 
                    
                    Debug.Log($"[장착 완료] {weaponItem.name}을(를) 장착했습니다. (공격력: {weaponItem.gunData.damage})");
                }
            }
            // 0번 칸이 비어있거나, 장착된 아이템이 무기가 아닐 때 (예외 상황 방지)
            else
            {
                if (playerController.gun != null)
                {
                    playerController.gun.ClearGunData();
                    playerController.gun.gameObject.SetActive(false); // 총 오브젝트 비활성화 (숨김)
                }
            }
        }
    }
}