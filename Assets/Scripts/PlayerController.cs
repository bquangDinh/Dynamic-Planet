using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 1.0f;

    public float jumpForce = 1.0f;

    private Rigidbody2D rigid2d;

    // Start is called before the first frame update
    void Start()
    {
        rigid2d = GetComponent<Rigidbody2D>();    
    }

    // Update is called once per frame
    void Update()
    {
        float horizontalMove = Input.GetAxis("Horizontal") * speed;
        float verticalMove = Input.GetAxis("Vertical") * speed;


        rigid2d.velocity = new Vector2(horizontalMove, verticalMove);
    }
}
