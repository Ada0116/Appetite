using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public Transform firepos;
    public GameObject bullet;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    public void Shoot()
    {
        Instantiate(bullet, firepos.position, firepos.rotation);
    }
}