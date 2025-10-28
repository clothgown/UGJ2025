using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public static CameraMove instance;

    [Header("基础设置")]
    public CinemachineVirtualCamera virtualCamera; // 虚拟摄像机
    public float moveSpeed = 2f;                   // 偏移移动速度
    public float offsetLimit = 5f;                 // 最大偏移限制
    public float returnSpeed = 5f;                 // 返回初始 offset 的平滑速度

    private CinemachineTransposer transposer;
    private Vector3 initialOffset;  // 初始 offset
    private Vector3 currentOffset;
    private Transform currentTarget;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (virtualCamera == null)
        {
            Debug.LogError("未绑定 CinemachineVirtualCamera！");
            return;
        }

        transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        initialOffset = transposer.m_FollowOffset; // 记录初始 offset
        currentOffset = initialOffset;
        currentTarget = virtualCamera.Follow;      // 记录初始跟随对象
    }

    void Update()
    {
        if (transposer == null) return;

        // 获取输入（箭头键 / WASD）
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // 检查是否有输入
        if (moveX != 0f || moveY != 0f)
        {
            // 计算偏移移动
            currentOffset += new Vector3(moveX, moveY, 0f) * moveSpeed * Time.deltaTime;

            // 限制偏移范围
            currentOffset.x = Mathf.Clamp(currentOffset.x, initialOffset.x - offsetLimit, initialOffset.x + offsetLimit);
            currentOffset.y = Mathf.Clamp(currentOffset.y, initialOffset.y - offsetLimit, initialOffset.y + offsetLimit);
        }
        else
        {
            
        }

        // 应用 offset 到相机
        transposer.m_FollowOffset = currentOffset;
    }

    /// <summary>
    /// 动态切换跟随对象
    /// </summary>
    public void ChangeFollow(GameObject target)
    {
        if (target != null)
        {
            virtualCamera.Follow = target.transform;
            currentTarget = target.transform;

            // 可选：切换目标时重置偏移
            currentOffset = initialOffset;
        }
    }

    /// <summary>
    /// 获取当前跟随对象
    /// </summary>
    public Transform GetCurrentFollow()
    {
        return currentTarget;
    }
}
