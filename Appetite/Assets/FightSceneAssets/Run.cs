using UnityEngine;

public class Run : MonoBehaviour
{
    // 🔧 可调整参数（在 Inspector 中直接修改）
    [Header("移动与跳跃")]
    public float speed = 2f;           // 水平移动速度
    public float jumpForce = 14f;       // 跳跃初速度（配合自定义重力使用）

    [Header("重力控制")]
    public float customGravity = -18f;  // 自定义重力值（负值向下），数值越大下落越快
    public bool useCustomGravity = true;// 是否启用自定义重力（关闭则使用物理系统的全局重力）

    [Header("动画")]
    public Animator animator;

    private Rigidbody _rb;
    private bool isGrounded;

    void Start()
    {
        if (!animator)
        {
            animator = GetComponentInChildren<Animator>();
        }

        _rb = GetComponent<Rigidbody>();
        // 如果使用自定义重力，建议将 Rigidbody 的 Use Gravity 设为 false
        if (useCustomGravity)
        {
            _rb.useGravity = false;
        }
    }

    void Update()
    {
        // WASD 输入
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;

        // 移动（水平速度直接设置）
        Vector3 move = new Vector3(h, 0, v).normalized * speed;
        _rb.velocity = new Vector3(move.x, _rb.velocity.y, move.z);

        // 动画
        animator.SetBool("Move", h != 0 || v != 0);

        // 翻转逻辑
        if (h != 0)
        {
            Vector3 localScale = animator.transform.localScale;
            localScale.x = -Mathf.Sign(h) * Mathf.Abs(localScale.x);
            animator.transform.localScale = localScale;
        }

        // 跳跃
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, jumpForce, _rb.velocity.z);
        }
    }

    void FixedUpdate()
    {
        // 自定义重力：只在未接地时施加额外向下的速度
        if (useCustomGravity && !isGrounded)
        {
            _rb.velocity += Vector3.up * customGravity * Time.fixedDeltaTime;
        }
    }

    // 地面检测
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }
}