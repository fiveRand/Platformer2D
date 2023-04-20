using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class Player : MonoBehaviour
{
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;

    public float accelerationTimeGrounded = .1f;
    public float moveSpeed = 6;

    public float jumpInputBufferSecond = 0.2f;

    float gravity;
    float maxJumpVelocity,minJumpVelocity;
    Vector3 velocity;
    float velocityXSmoothing;

    Vector2 inputVector;
    PlayerController controller;

    float lastPressedJumpTimer;


    private void Start() {
        controller = GetComponent<PlayerController>();

        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        
    }

    private void Update() {
        if(controller.info.above || controller.info.below)
        {
            velocity.y = 0;
        }
        inputVector.x = Input.GetAxisRaw("Horizontal");
        inputVector.y = Input.GetAxisRaw("Vertical");

        if(Input.GetKeyDown(KeyCode.Space))
        {
            lastPressedJumpTimer = jumpInputBufferSecond;
        }

        if(Input.GetKeyUp(KeyCode.Space))
        {
            if(velocity.y > minJumpVelocity)
            {
                velocity.y = minJumpVelocity;
            }
        }
        float targetVelocityX = inputVector.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, accelerationTimeGrounded);
    }

    private void FixedUpdate() 
    {
        FixedTickTimer();
        velocity.y += gravity * Time.fixedDeltaTime;

        if(lastPressedJumpTimer > 0)
        {
            if(controller.info.below)
            {


                velocity.y = maxJumpVelocity;

                
            }
            lastPressedJumpTimer = 0;
        }

        controller.Move(velocity * Time.fixedDeltaTime);
    }

    void FixedTickTimer()
    {
        if(lastPressedJumpTimer > 0)
        {
            lastPressedJumpTimer -= Time.fixedDeltaTime;
        }
    }
}
