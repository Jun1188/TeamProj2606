using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;             // 백엔드 데이터 (여기에 플레이어 Inventory를 넣든, 건물 Inventory를 넣든 다 대응됨!)
    public GameObject itemSocketPrefab;    
    public Transform slotGridParent;       

    private ItemSocket[] uiSlots;          

    private void Start()
    {
        if (inventory == null) return;
        InitUISlots();
        RefreshAllUI();
    }

    private void OnEnable()
    {
        if (inventory != null) RefreshAllUI();
    }

    public void InitUISlots()
    {
        foreach (Transform child in slotGridParent)
        {
            Destroy(child.gameObject);
        }

        uiSlots = new ItemSocket[inventory.slotCount];

        for (int i = 0; i < inventory.slotCount; i++)
        {
            GameObject go = Instantiate(itemSocketPrefab, slotGridParent);
            
            InventorySlotUI slotLink = go.GetComponent<InventorySlotUI>();
            if (slotLink == null) slotLink = go.AddComponent<InventorySlotUI>();
            slotLink.Init(i, this);

            uiSlots[i] = go.GetComponent<ItemSocket>();

            // 🎨 [시각적 기능 추가] 플레이어 인벤토리의 0번 슬롯(장비창) 디자인 차별화
            if (InventoryManager.Instance != null && InventoryManager.Instance.playerController != null)
            {
                // 이 UI가 띄우고 있는 백엔드가 플레이어 가방이 맞고, 현재 생성 중인 칸이 0번(무기)일 때
                if (inventory == InventoryManager.Instance.playerController.playerInventory && i == 0)
                {
                    // 프리팹에 붙어있는 배경 Image 컴포넌트를 찾아 색상 변경
                    UnityEngine.UI.Image slotImage = go.GetComponent<UnityEngine.UI.Image>();
                    if (slotImage != null)
                    {
                        // 💡 예시: 장비칸임을 직관적으로 알 수 있도록 투명도 있는 황금빛/주황빛 색상 적용
                        slotImage.color = new Color(1.0f, 0.75f, 0.2f, 0.4f); 
                    }
                }
            }
        }
    }

    public void RefreshAllUI()
    {
        if (inventory == null || uiSlots == null) return;

        if (uiSlots.Length != inventory.slotCount)
        {
            InitUISlots();
        }

        for (int i = 0; i < inventory.slotCount; i++)
        {
            ItemStack itemStack = inventory.slots[i];
            if (itemStack != null && itemStack.item != null && itemStack.amount > 0)
            {
                uiSlots[i].SetItem(itemStack.item, itemStack.amount);
            }
            else
            {
                uiSlots[i].ClearSlot();
            }
        }
    }

    // 슬롯이 좌클릭 되었을 때 독단적으로 처리하지 않고 중앙 타워(InventoryManager)로 던집니다!
    public void OnSlotLeftClicked(int clickedIndex)
    {
        if (inventory == null) return;
        
        InventoryManager.Instance.HandleSlotLeftClick(inventory, clickedIndex, this);
    }

    public void OnSlotRightClicked(int clickedIndex)
    {
        // 필요시 마크식 절반 나누기 구현 공간
    }
}