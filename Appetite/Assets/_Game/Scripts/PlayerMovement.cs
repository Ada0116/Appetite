using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 5f;
    public bool is2DBattleMode = false;

    [Header("跳跃")]
    public bool canJump = false;
    public float jumpForce = 15f;
    public AnimationClip jumpClip;   // 行走跳跃动画，拖入

    private Rigidbody rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded = true;
    private bool hasSwitchedJumpAnim = false;  // 防止一次起跳内多次切换

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 起跳检测
        if (canJump && Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            hasSwitchedJumpAnim = false;   // 每次新起跳，重置切换标记

            float currentSpeed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
            if (currentSpeed > 0.1f)
                anim.SetTrigger("Jump");      // 移动起跳
            else
                anim.SetTrigger("JumpIdle");  // 原地起跳
        }

        // 空中：如果当前是 JumpIdle 动画且按下方向键，则切换到 Jump 动画（仅一次）
        if (!isGrounded && !hasSwitchedJumpAnim && anim != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("JumpIdle"))
            {
                float h = Input.GetAxisRaw("Horizontal");
                if (Mathf.Abs(h) > 0.1f)
                {
                    // 计算 JumpIdle 已经播放的时间（秒）
                    float elapsedTime = stateInfo.normalizedTime * stateInfo.length;

                    // 换算为 Jump 动画中的进度（normalized time）
                    float targetNormTime = 0f;
                    if (jumpClip != null && jumpClip.length > 0f)
                    {
                        // 用同样的已播放秒数占 Jump 总长的比例
                        targetNormTime = Mathf.Clamp01(elapsedTime / jumpClip.length);
                    }
                    else
                    {
                        // 如果没有拖入 jumpClip，就退而求其次用当前进度百分比
                        targetNormTime = stateInfo.normalizedTime;
                    }

                    // 用 CrossFade 切换到 Jump 状态，从 targetNormTime 开始播，过渡很短
                    anim.CrossFade("Jump", 0.05f, 0, targetNormTime);
                    hasSwitchedJumpAnim = true;   // 标记已经切换，不会再次触发
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
            move = new Vector3(h, 0, 0).normalized * moveSpeed;
        else
            move = new Vector3(h, 0, v).normalized * moveSpeed;

        rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);

        float currentSpeed = new Vector2(move.x, move.z).magnitude;
        if (anim != null)
            anim.SetFloat("Speed", currentSpeed);

        if (h != 0 && spriteRenderer != null)
            spriteRenderer.flipX = (h > 0);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}