using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 5f;
    public bool is2DBattleMode = false;   // 勾上=战斗场景（只有左右）

    [Header("跳跃")]
    public bool canJump = false;          // 是否允许跳跃
    public float jumpForce = 15f;        // 跳跃力度，调大可跳更高

    private Rigidbody rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (canJump && Input.GetButtonDown("Jump") && isGrounded)
        {
            // 物理跳跃，所有跳跃共用
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;

            if (anim != null)
            {
                // 判断当前是否在移动（用速度判断，不是按键）
                float currentSpeed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
                if (currentSpeed > 0.1f)
                {
                    anim.SetTrigger("Jump");      // 行走跳跃
                }
                else
                {
                    anim.SetTrigger("JumpIdle");  // 站立跳跃
                }
            }
        }
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move;
        if (is2DBattleMode)
        {
            // 战斗模式：只左右移动，Z轴不变，Y轴由物理控制
            move = new Vector3(h, 0, 0).normalized * moveSpeed;
        }
        else
        {
            // 探索模式：水平→X，垂直→Z，前后左右移动
            move = new Vector3(h, 0, v).normalized * moveSpeed;
        }

        // 应用移动（保留Y轴速度以配合跳跃）
        rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);

        // 更新动画速度参数
        float currentSpeed = new Vector2(move.x, move.z).magnitude;
        if (anim != null)
            anim.SetFloat("Speed", currentSpeed);

        // 左右翻转（根据你的测试，如果还是反，改成 h < 0）
        if (h != 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = (h > 0);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}