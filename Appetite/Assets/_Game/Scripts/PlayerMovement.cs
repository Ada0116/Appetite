using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 5f;
    public bool is2DBattleMode = false;

    [Header("跳跃")]
    public bool canJump = false;
    public float jumpForce = 15f;
    public AnimationClip jumpClip;

    private Rigidbody rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded = true;
    private bool hasSwitchedJumpAnim = false;

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
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            hasSwitchedJumpAnim = false;

            float currentSpeed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
            if (currentSpeed > 0.1f)
                anim.SetTrigger("PlayerJump");
            else
                anim.SetTrigger("PlayerJumpIdle");
        }

        if (!isGrounded && !hasSwitchedJumpAnim && anim != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("JumpIdle"))
            {
                float h = Input.GetAxisRaw("Horizontal");
                if (Mathf.Abs(h) > 0.1f)
                {
                    float elapsedTime = stateInfo.normalizedTime * stateInfo.length;
                    float targetNormTime = 0f;
                    if (jumpClip != null && jumpClip.length > 0f)
                        targetNormTime = Mathf.Clamp01(elapsedTime / jumpClip.length);
                    else
                        targetNormTime = stateInfo.normalizedTime;

                    anim.CrossFade("PlayerJump", 0.05f, 0, targetNormTime);
                    hasSwitchedJumpAnim = true;
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
            move = new Vector3(h, 0, 0).normalized * moveSpeed;
        }
        else
        {
            // ★ 关键行：W向后（Z轴负方向），S向前（Z轴正方向）
            move = new Vector3(h, 0, -v).normalized * moveSpeed;

            // 如果想反过来（W前S后），把 -v 改成 v 即可
        }

        rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);

        float currentSpeed = new Vector2(move.x, move.z).magnitude;
        if (anim != null)
        {
            anim.SetFloat("Speed", currentSpeed);
            bool isForward = (v > 0.1f && Mathf.Abs(v) >= Mathf.Abs(h));
            anim.SetBool("IsMovingForward", isForward);
        }

        if (spriteRenderer != null)
        {
            if (Mathf.Abs(h) > 0.1f && !(v > 0.1f && Mathf.Abs(v) >= Mathf.Abs(h)))
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