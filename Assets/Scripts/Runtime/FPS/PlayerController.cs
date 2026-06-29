using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : Entity
{
    #region [1. Variables - Inspector Settings]
    
    [Header("Core Components")]
    public Rigidbody rb;

    [Header("Camera & Mouse Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 1f; 
    public float MAX_CAMERA_ROTATION_X = 90f;
    private float cameraRotationX = 0f;

    [Header("Movement Settings")]
    public float jumpForce = 10f;

    [Header("Gun & Combat Settings")]
    public Gun gun;
    private bool isFiringPressed = false;

    [Header("Recoil Control System")]
    public float maxRecoilVelocity = 12f;      
    public float recoilDecaySpeed = 20f;       
    private Vector3 activeRecoilVelocity;      

    [Header("Inventory Backend")]
    public Inventory playerInventory; 
    private bool isInventoryOpen = false;
    private Inventory currentOpenedInventory = null; 

    [Header("Inventory & HUD UI")]
    public GameObject inventoryUIPanel; 
    public InventoryUI inventoryUI;     
    public InventoryUI chestInventoryUI;
    public GameObject crosshairUI;      
    public TMPro.TextMeshProUGUI promptText; 

    [Header("Debug / Cheat Tools")]
    public ItemDataSO debugTestItem; 

    private Vector2 moveInput;
    private Vector2 mouseInput;
    private bool isJumpPressed;

    #endregion

    #region [2. Unity Lifecycle]

    protected override void Start()
    {
        base.Start();
        
        CloseInventory();
        moveSpeed = 5f; 

        if (InventoryManager.Instance != null && playerInventory != null)
        {
            InventoryManager.Instance.CheckWeaponEquip(playerInventory);
        }
    }

    protected override void Update()
    {
        base.Update();

        if (!isInventoryOpen)
        {
            HandleCameraRotation();
            HandleInteractionRaycast();
        }
        else
        {
            if (promptText != null) promptText.gameObject.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        if (!isInventoryOpen)
        {
            HandleMovement();
            HandleJump();
        }
    }

    #endregion

    #region [3. New Input System Callbacks]

    public void OnInventory(InputValue value)
    {
        //if (!value.isPressed) return;
        ToggleInventory();
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed) return;

        // UI 창이 열려있을 때 E를 누르면 창을 닫아줍니다.
        if (isInventoryOpen)
        {
            CloseInventory();
            return;
        }

        // 창이 닫혀있을 때는 레이캐스트를 발사해 조준 중인 상자를 엽니다.
        Ray ray = new(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 4f, LayerMask.GetMask("Interactable")))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            if (interactable != null)
            {
                interactable.OnInteract(this);
            }
        }
    }

    public void OnMove(InputValue value)
    {
        if (isInventoryOpen) 
        {
            moveInput = Vector2.zero;
            return;
        }
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (isInventoryOpen) 
        {
            mouseInput = Vector2.zero;
            return;
        }
        mouseInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (isInventoryOpen) return;
        isJumpPressed = value.isPressed;
    }

    public void OnFire(InputValue value)
    {
        if (isInventoryOpen || gun == null || gun.gunData == null) return;
        
        isFiringPressed = value.isPressed;
        gun.SetFiringPressed(isFiringPressed);
        
        if (isFiringPressed && !gun.gunData.isAutomatic)
        {
            gun.Fire();
        }
    }

    public void OnReload(InputValue value)
    {
        if (isInventoryOpen || gun == null) return;
        if (value.isPressed) gun.StartReload();
    }

    #endregion

    #region [4. Core Mechanics - Movement & Camera]

    private void HandleCameraRotation()
    {
        float mouseX = mouseInput.x * mouseSensitivity * 0.1f;
        float mouseY = mouseInput.y * mouseSensitivity * 0.1f;

        cameraRotationX -= mouseY;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -MAX_CAMERA_ROTATION_X, MAX_CAMERA_ROTATION_X);
        
        playerCamera.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        Vector3 moveDir = transform.forward * moveInput.y + transform.right * moveInput.x;
        Vector3 targetVelocity = moveDir * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y; 
        
        rb.linearVelocity = targetVelocity;
    }

    private void HandleJump()
    {
        if (isJumpPressed && Mathf.Abs(rb.linearVelocity.y) < 0.001f)
        {
            rb.AddForce(new Vector3(0f, jumpForce, 0f), ForceMode.Impulse);
            isJumpPressed = false;
        }
    }

    public void AddRecoil(Vector3 recoilDirection, float verticalRecoil, float horizontalRecoil)
    {
        cameraRotationX -= verticalRecoil; 
        cameraRotationX = Mathf.Clamp(cameraRotationX, -MAX_CAMERA_ROTATION_X, MAX_CAMERA_ROTATION_X);
        playerCamera.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);

        float currentRecoilSpeed = Vector3.Dot(rb.linearVelocity, recoilDirection);
        if (currentRecoilSpeed < maxRecoilVelocity)
        {
            rb.AddForce(recoilDirection * horizontalRecoil, ForceMode.Impulse);
        }
    }

    #endregion

    #region [5. Core Mechanics - Interaction & Raycast]

    private void HandleInteractionRaycast()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 4f, LayerMask.GetMask("Interactable")))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            
            if (interactable != null)
            {
                if (promptText != null)
                {
                    promptText.gameObject.SetActive(true);
                    promptText.text = $"[E] {interactable.promptMessage}";
                }
                return; 
            }
        }

        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    #endregion

    #region [6. Core Mechanics - Inventory System Management]

    public void ToggleInventory()
    {
        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenPlayerInventory();
        }
    }

    public void OpenPlayerInventory()
    {
        isInventoryOpen = true;
        ResetInputValues();

        if (inventoryUIPanel != null) inventoryUIPanel.SetActive(true);
        if (inventoryUI != null) inventoryUI.RefreshAllUI();
        
        ToggleCursorAndHUD(false);
    }

    public void OpenTargetInventory(Inventory targetInventory)
    {
        isInventoryOpen = true;
        currentOpenedInventory = targetInventory;
        ResetInputValues();

        if (chestInventoryUI != null)
        {
            chestInventoryUI.inventory = targetInventory;
            chestInventoryUI.gameObject.SetActive(true);
            chestInventoryUI.RefreshAllUI();
        }

        if (inventoryUIPanel != null) inventoryUIPanel.SetActive(true);
        if (inventoryUI != null) inventoryUI.RefreshAllUI();

        ToggleCursorAndHUD(false);
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;
        currentOpenedInventory = null;

        if (inventoryUIPanel != null) inventoryUIPanel.SetActive(false);
        if (chestInventoryUI != null) chestInventoryUI.gameObject.SetActive(false);
        if (promptText != null) promptText.gameObject.SetActive(false);

        ToggleCursorAndHUD(true);
    }

    private void ResetInputValues()
    {
        moveInput = Vector2.zero;
        mouseInput = Vector2.zero;
        isFiringPressed = false;
        if (gun != null) gun.SetFiringPressed(false);
        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f); 
    }

    private void ToggleCursorAndHUD(bool gameplayMode)
    {
        if (crosshairUI != null) crosshairUI.SetActive(gameplayMode);
        
        if (gameplayMode)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    #endregion
}