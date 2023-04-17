using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : RaycastController
{
    public CollisionInfo info;
    public Vector3 velocityOld;

    public override void Start()
    {
        base.Start();
    }

    public void Move(Vector3 velocity)
    {
        UpdateRaycastOrigins();
        info.Reset();
        velocityOld = velocity;

        if(velocity.y != 0)
        {
            VerticalCollision(ref velocity);
        }

        transform.Translate(velocity);
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
                velocity.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

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
