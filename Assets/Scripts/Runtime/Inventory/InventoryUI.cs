using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;             
    public GameObject itemSocketPrefab;     
    public Transform slotGridParent;        

    [Header("Mouse Drag Pointer UI")]
    public ItemSocket mouseCarriageSlot;    

    private ItemSocket[] uiSlots;           
    private ItemStack mouseCarriageItem = null; 

    private void Awake()
    {
        // ⭐️ Awake에서는 데이터 검증만 하고 강제 비활성화하지 않습니다.
        if (mouseCarriageSlot != null) mouseCarriageSlot.ClearSlot(); 
    }

    private void Start()
    {
        // Start 시점에 안전하게 슬롯 레이아웃 빌드
        if (inventory != null) 
        {
            InitUISlots(); 
            RefreshAllUI(); 
        }
    }

    private void InitUISlots()
    {
        if (inventory == null || slotGridParent == null || itemSocketPrefab == null) return;

        // 기존에 잔여물이 남아있다면 청소
        foreach (Transform child in slotGridParent) Destroy(child.gameObject);

        int slotCount = inventory.slotCount;
        uiSlots = new ItemSocket[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            GameObject newSlot = Instantiate(itemSocketPrefab, slotGridParent);
            ItemSocket socket = newSlot.GetComponent<ItemSocket>();
            uiSlots[i] = socket;

            InventorySlotUI slotLink = newSlot.GetComponent<InventorySlotUI>();
            if (slotLink == null) slotLink = newSlot.AddComponent<InventorySlotUI>();
            slotLink.Init(i, this);
        }
        Debug.Log($"[인벤토리 UI] {slotCount}개의 UI 슬롯 생성 성공!");
    }

    public void RefreshAllUI()
    {
        if (inventory == null) return;

        // ⭐️ [핵심 패치] 초기화 타이밍 문제로 uiSlots가 생성이 안 된 상태라면 즉시 강제 생성
        if (uiSlots == null || uiSlots.Length == 0)
        {
            InitUISlots();
        }

        if (uiSlots == null) return;

        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (i < inventory.slots.Length && inventory.slots[i] != null)
            {
                uiSlots[i].SetItem(inventory.slots[i].item, inventory.slots[i].amount);
            }
            else
            {
                uiSlots[i].ClearSlot();
            }
        }

        if (mouseCarriageItem != null && mouseCarriageItem.item != null)
        {
            mouseCarriageSlot.SetItem(mouseCarriageItem.item, mouseCarriageItem.amount);
        }
        else
        {
            if (mouseCarriageSlot != null) mouseCarriageSlot.ClearSlot();
        }
    }

    private void Update()
    {
        if (mouseCarriageItem != null && mouseCarriageItem.item != null) 
        {
            if (mouseCarriageSlot != null)
            {
                mouseCarriageSlot.gameObject.SetActive(true); 
                mouseCarriageSlot.transform.position = Input.mousePosition; 
            }
        }
        else
        {
            if (mouseCarriageSlot != null) mouseCarriageSlot.gameObject.SetActive(false);
        }
    }

    public void OnSlotLeftClicked(int clickedIndex)
    {
        if (inventory == null) return;

        ItemStack clickedBackendSlot = inventory.slots[clickedIndex];

        if (mouseCarriageItem == null)
        {
            if (clickedBackendSlot != null && clickedBackendSlot.item != null) 
            {
                mouseCarriageItem = clickedBackendSlot; 
                inventory.slots[clickedIndex] = null; 
            }
        }
        else
        {
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

        RefreshAllUI(); 
    }

    public void OnSlotRightClicked(int clickedIndex)
    {
        // 확장용 구조 유지
    }
}