using UnityEngine;

public class Run : MonoBehaviour
{
    public float speed = 10;
    public Animator animator;
    Rigidbody _rb;

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
        var h = Input.GetAxis("Horizontal");
        var v = Input.GetAxis("Vertical");

        _rb.velocity = new Vector3(h, 0, v) * speed;
        animator.SetBool("Move", h != 0 || v != 0);

        if (h != 0)
        {
            animator.transform.localScale = new Vector3(Mathf.Sign(h), 1, 1);
        }
    }
}