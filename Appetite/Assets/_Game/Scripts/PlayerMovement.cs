using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 假设角色初始朝右，美术资源默认向右
        // 如果资源朝左，需要调整下面翻转逻辑。
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, 0, v).normalized * moveSpeed;
        rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);

        // 更新动画参数
        float currentSpeed = move.magnitude;
        if (anim != null)
            anim.SetFloat("Speed", currentSpeed);

        // 处理左右翻转
        if (h != 0 && spriteRenderer != null)
        {
            // 向右走（h > 0）时，不翻转；向左走（h < 0）时，翻转
            spriteRenderer.flipX = (h > 0);
        }
        // 如果角色停止，保持最后的朝向不翻转回来？通常保留最后的flipX。
    }
}