using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : RaycastController
{
    public CollisionInfo info;
    public Vector3 velocityOld;
    public int faceDirection;

    public override void Start()
    {
        base.Start();
    }

    public void Move(Vector3 velocity)
    {
        UpdateRaycastOrigins();
        info.Reset();
        velocityOld = velocity;

        if(velocity.x != 0)
        {
            faceDirection = (int)Mathf.Sign(velocity.x);
        }

        HorizontalCollision(ref velocity);
        if(velocity.y != 0)
        {
            VerticalCollision(ref velocity);
        }

        transform.Translate(velocity);
    }

    void HorizontalCollision(ref Vector3 velocity)
    {

    }

    void VerticalCollision(ref Vector3 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);
            
            
            if(hit)
            {
               
                rayLength = hit.distance;
                velocity.y = (rayLength - skinWidth) * directionY;
                // 땅바닥에 쏜 레이 길이만큼 밀어내게 만들라고 지시, skinWidth은 rayLength 에서 스킨값을 고려하여 차감함

                info.below = directionY == -1;
                info.above = directionY == 1;
            }

        }
    }


    [System.Serializable]
    public struct CollisionInfo
    {
        public bool above, below, left, right;

        public float slopeAngle, slopeAngleOld;
        
        public void Reset()
        {
            above = below = left = right = false;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }
}
