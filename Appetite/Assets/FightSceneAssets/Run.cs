using UnityEngine;

public class Run : MonoBehaviour
{
    public float speed = 2f;       // 移动速度
    public float jumpForce = 100f;  // 跳跃力度

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

        // 角色在 XZ 平面移动
        Vector3 move = new Vector3(h, 0, v).normalized * speed;
        _rb.velocity = new Vector3(move.x, _rb.velocity.y, move.z);

        // 动画播放
        animator.SetBool("Move", h != 0 || v != 0);

        // AD 控制左右翻转（2D立绘左右镜像）
        if (h != 0)
        {
            Vector3 localScale = animator.transform.localScale;
            localScale.x = Mathf.Sign(h) * Mathf.Abs(localScale.x); // 左右翻转
            animator.transform.localScale = localScale;
        }

        // 跳跃
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, jumpForce, _rb.velocity.z);
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