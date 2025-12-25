using UnityEngine;

/// <summary>
/// 玩家控制器 - 支持第一人称和第三人称视角切换
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("视角设置")]
    [SerializeField] private bool isFirstPerson = true;   // true=第一人称, false=第三人称
    [SerializeField] private Camera firstPersonCamera;     // 第一人称摄像机
    [SerializeField] private Camera thirdPersonCamera;     // 第三人称摄像机
    [SerializeField] private Transform thirdPersonPivot;   // 第三人称摄像机支点

    [Header("移动设置")]
    [SerializeField] private float walkSpeed = 5f;        // 行走速度
    [SerializeField] private float sprintSpeed = 8f;      // 奔跑速度
    [SerializeField] private float sneakSpeed = 2.5f;     // 潜行速度
    [SerializeField] private float jumpForce = 2f;        // 跳跃力度（减小以实现合理跳跃高度）
    [SerializeField] private float gravity = -9.81f;      // 重力

    [Header("视角控制")]
    [SerializeField] private float mouseSensitivity = 2f;  // 鼠标灵敏度
    [SerializeField] private float minVerticalAngle = -80f;// 垂直视角最小角度
    [SerializeField] private float maxVerticalAngle = 80f; // 垂直视角最大角度
    
    [Header("第三人称设置")]
    [SerializeField] private float thirdPersonDistance = 5f;  // 第三人称摄像机距离
    [SerializeField] private float thirdPersonHeight = 2f;    // 第三人称摄像机高度
    [SerializeField] private float cameraSmooth = 10f;        // 摄像机平滑度

    [Header("地面检测")]
    [SerializeField] private Transform groundCheck;        // 地面检测点
    [SerializeField] private float groundDistance = 0.4f;  // 地面检测距离
    [SerializeField] private LayerMask groundMask;         // 地面层级

    // 组件引用
    private CharacterController controller;

    // 状态变量
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;
    private float yRotation = 0f;
    private bool isSneaking = false;

    // 公共属性
    public bool IsSneaking => isSneaking;
    public bool IsFirstPerson => isFirstPerson;
    public Vector3 Velocity => controller != null ? controller.velocity : Vector3.zero;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        // 自动查找摄像机（如果未在 Inspector 中赋值）
        if (firstPersonCamera == null)
        {
            Transform fpCam = transform.Find("FirstPersonCamera");
            if (fpCam != null) firstPersonCamera = fpCam.GetComponent<Camera>();
        }
        if (thirdPersonCamera == null)
        {
            Transform tpCam = transform.Find("ThirdPersonCamera");
            if (tpCam != null) thirdPersonCamera = tpCam.GetComponent<Camera>();
        }
        if (groundCheck == null)
        {
            groundCheck = transform.Find("GroundCheck");
        }
    }

    private void Start()
    {
        // 锁定鼠标光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 初始化视角
        SetCameraMode(isFirstPerson);

        // 初始化旋转
        yRotation = transform.eulerAngles.y;
    }

    private void Update()
    {
        // 如果 CharacterController 被禁用（训练模式），跳过所有输入处理
        if (controller == null || !controller.enabled)
            return;

        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused))
            return;

        HandleGroundCheck();
        HandleMovement();
        HandleCameraRotation();
        HandleViewToggle();
    }

    private void LateUpdate()
    {
        if (!isFirstPerson && thirdPersonCamera != null)
        {
            UpdateThirdPersonCamera();
        }
    }

    /// <summary>
    /// 切换视角模式 (按V键)
    /// </summary>
    private void HandleViewToggle()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            isFirstPerson = !isFirstPerson;
            SetCameraMode(isFirstPerson);
            Debug.Log($"切换视角: {(isFirstPerson ? "第一人称" : "第三人称")}");
        }
    }

    /// <summary>
    /// 设置摄像机模式
    /// </summary>
    public void SetCameraMode(bool firstPerson)
    {
        isFirstPerson = firstPerson;
        
        if (firstPersonCamera != null)
            firstPersonCamera.gameObject.SetActive(firstPerson);
        
        if (thirdPersonCamera != null)
            thirdPersonCamera.gameObject.SetActive(!firstPerson);
    }

    /// <summary>
    /// 地面检测
    /// </summary>
    private void HandleGroundCheck()
    {
        // 优先使用 CharacterController 的内置地面检测
        // 只有当 groundCheck 和 groundMask 都配置时才使用 Physics.CheckSphere
        if (groundCheck != null && groundMask != 0)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else
        {
            // 使用 CharacterController 的内置检测
            isGrounded = controller.isGrounded;
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    /// <summary>
    /// 处理移动输入
    /// </summary>
    private void HandleMovement()
    {
        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 检测潜行状态
        isSneaking = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        // 计算移动速度
        float currentSpeed = walkSpeed;
        if (isSneaking)
            currentSpeed = sneakSpeed;
        else if (isSprinting)
            currentSpeed = sprintSpeed;

        // 计算移动方向
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        // 跳跃
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // 应用重力
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// 处理摄像机旋转
    /// </summary>
    private void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 水平旋转 - 旋转玩家
        yRotation += mouseX;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // 垂直旋转 - 仅旋转摄像机
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        if (isFirstPerson && firstPersonCamera != null)
        {
            firstPersonCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    /// <summary>
    /// 更新第三人称摄像机位置
    /// </summary>
    private void UpdateThirdPersonCamera()
    {
        if (thirdPersonCamera == null) return;

        // 计算目标位置
        Vector3 targetPosition = transform.position 
            - transform.forward * thirdPersonDistance 
            + Vector3.up * thirdPersonHeight;

        // 射线检测防止穿墙
        RaycastHit hit;
        Vector3 direction = targetPosition - transform.position;
        if (Physics.Raycast(transform.position + Vector3.up, direction.normalized, out hit, direction.magnitude, groundMask))
        {
            targetPosition = hit.point;
        }

        // 平滑移动摄像机
        thirdPersonCamera.transform.position = Vector3.Lerp(
            thirdPersonCamera.transform.position,
            targetPosition,
            cameraSmooth * Time.deltaTime
        );

        // 摄像机看向玩家
        Vector3 lookTarget = transform.position + Vector3.up * 1.5f;
        thirdPersonCamera.transform.LookAt(lookTarget);

        // 应用垂直旋转偏移
        thirdPersonCamera.transform.rotation *= Quaternion.Euler(xRotation * 0.5f, 0f, 0f);
    }

    /// <summary>
    /// 获取当前活动的摄像机
    /// </summary>
    public Camera GetActiveCamera()
    {
        return isFirstPerson ? firstPersonCamera : thirdPersonCamera;
    }

    /// <summary>
    /// 设置鼠标灵敏度
    /// </summary>
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }

    /// <summary>
    /// 重置位置（用于训练）
    /// </summary>
    public void ResetPosition(Vector3 position, Quaternion rotation)
    {
        controller.enabled = false;
        transform.position = position;
        transform.rotation = rotation;
        controller.enabled = true;
        velocity = Vector3.zero;
        xRotation = 0f;
        yRotation = rotation.eulerAngles.y;
    }
}
