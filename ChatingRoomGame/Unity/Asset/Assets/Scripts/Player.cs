using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public bool canControl = false;
    public float speedMove;
    public float JumpForce;
    private float moveInput;

    private Rigidbody rigidbody;

    public bool facingRight = true;

    private Collider[] isGroundedColl;
    public bool isGrounded;
    public Transform groundCheck;
    public float checkRadius;
    public LayerMask whatIsGround;
    
    public int extraJumpValue;
    private int extraJump;
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!canControl)
            return;
        
        isGroundedColl = Physics.OverlapSphere(groundCheck.position , checkRadius , whatIsGround);

        if(isGroundedColl.Length != 0){
            isGrounded = true;
        }else {
            isGrounded = false;
        }

        moveInput = Input.GetAxis("Horizontal");
        rigidbody.velocity = new Vector3(moveInput * speedMove , rigidbody.velocity.y , rigidbody.velocity.z);
    }

    void Update(){
        if (!canControl)
            return;
        
        if(isGrounded == true){
            extraJump = extraJumpValue;
        }

        if(Input.GetKeyDown(KeyCode.Space) && extraJump > 0){
            rigidbody.velocity = Vector3.up * JumpForce;
            extraJump--;
        }else if(Input.GetKeyDown(KeyCode.Space) && extraJump == 0 && isGrounded == true){
            rigidbody.velocity = Vector3.up * JumpForce;
        }
    }

}
