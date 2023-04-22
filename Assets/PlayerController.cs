using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : RaycastController
{
    public float maxClimbAngle = 70;
    public float maxDescendAngle = 70;


    /// <summary>
    /// 박스 콜라이더 y크기의 기준으로의 길이를
    /// 떨어지는 지점으로부터 아래의 땅바닥 길이를 비교해 
    /// 경사로를 타고 내려오게 만들 것인지를 결정함
    /// </summary>
    [Range(0, 1)]
    public float cliffDescendTolerance = 0.25f; 
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

        if(velocity.y < 0)
        {
            DescendSlope(ref velocity);
        }

        HorizontalCollision(ref velocity);
        if(velocity.y != 0)
        {
            VerticalCollision(ref velocity);
        }

        transform.Translate(velocity);
    }

    void DescendSlope(ref Vector3 velocity)
    {
        Vector2 rayOrigin = (faceDirection == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, Mathf.Infinity, collisionMask);
        if(hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if(slopeAngle != 0 && slopeAngle <= maxDescendAngle)
            {
                if(Mathf.Sign(hit.normal.x) == faceDirection)
                {
                    float cliffTolerance = boxCollider.bounds.size.y * cliffDescendTolerance;
                    float rayLength = hit.distance - skinWidth;
                    float moveDist = Mathf.Abs(velocity.x);
                    float minYBump = Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * moveDist + cliffTolerance;

                    /*
                     minYBump값이 경사로 끝점 둔턱에 따라 경사로에 붙을수도 아닐수도 있다
                     그래서 legLength으로 충분한 보정값을 넣어 무시할수 있도록 둔다

                     디버깅 참고
                     Debug.DrawRay(rayOrigin, Vector2.down * minYBump, Color.cyan);

                     내 자신에게 메모)
                     만약 해당 이동속도를 넘은 속도로 경사로를 내려가려 할때 경사로 이동을 방지하고 싶다면
                     if(rayLength <= minYBump && Mathf.Abs(MaxMoveSpeed) <= moveDist)
                    */

                    if(rayLength <= minYBump)
                    {

                        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDist * faceDirection;
                        velocity.y -= Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDist + cliffTolerance; 

                        info.slopeAngle = slopeAngle;
                        info.descendingSlope = true;
                        info.below = true;
                    }
                }
            }
        }
    }

    void HorizontalCollision(ref Vector3 velocity)
    {

        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (faceDirection == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * faceDirection, rayLength, collisionMask);

            if(hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if(i == 0 && slopeAngle <= maxClimbAngle)
                {
                    float dist2SlopeStart = 0;
                    /*
                    제대로 경사로에 맞춰 올라가기 위해 시작점 거리 계산을 한다
                    */
                    if(slopeAngle != info.slopeAngleOld) 
                    {
                        dist2SlopeStart = hit.distance - skinWidth;
                        velocity.x -= dist2SlopeStart * faceDirection;
                    }
                    ClimbSlope(ref velocity, slopeAngle);
                    velocity.x += dist2SlopeStart * faceDirection;
                }

                // V형 장애물에서 뾰쪽한 부분에서 끼었다 올라가는데 이때 속력이 크게 줄어준다
                // 그래서 속력을 저장시켜 다른 각도에 이동해도 속력을 지속시키기 위한 if문

                if(!info.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * faceDirection;
                    rayLength = hit.distance;

                    if(info.climbingSlope) // 경사로 올라가다가 벽 같은 것에 도달했을때
                    {
                        // x 좌표로 쏜 ray로 각도에 탄젠트와 x 속력을 곱해 올라가는 걸 막는다
                        velocity.y = Mathf.Tan(info.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }

                    info.left = faceDirection == -1;
                    info.right = faceDirection == 1;
                }
            }
        }
    }

    void ClimbSlope(ref Vector3 velocity,float slopeAngle)
    {
        float moveDistance = Mathf.Abs(velocity.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if(velocity.y <= climbVelocityY)
        {
            velocity.y = climbVelocityY;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * faceDirection;
            info.below = true;
            info.climbingSlope = true;
            info.slopeAngle = slopeAngle;
        }
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

                if(info.climbingSlope) // 올라가다가 머리 위에 벽이 있을때 
                {
                    // tan(degree) = y / x
                    // x * tan(degree) = y
                    // x = y / tan(degree)
                    // 머리위의 벽으로부터 각도를 구해 속력 y값으로부터 삼각항법을 하여 x값을 대입한다
                    // 이러함으로 더이상 머리위에 벽이 있어 떠는 행동을 하지않을 것이다
                    velocity.x = velocity.y / Mathf.Tan(info.slopeAngle * Mathf.Deg2Rad) * faceDirection;
                }

                info.below = directionY == -1;
                info.above = directionY == 1;
            }

        }

        if(info.climbingSlope)
        {
            rayLength = Mathf.Abs(velocity.x) + skinWidth;
            Vector2 rayOrigin = ((faceDirection == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight);
            rayOrigin += Vector2.up * velocity.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * faceDirection, rayLength, collisionMask);
            if(hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if(slopeAngle != info.slopeAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * faceDirection;
                    info.slopeAngle = slopeAngle;
                }
            }
        }
    }


    [System.Serializable]
    public struct CollisionInfo
    {
        public bool above, below, left, right;
        public bool climbingSlope, descendingSlope;
        public float slopeAngle, slopeAngleOld; // old를 넣는 이유는 
        
        public void Reset()
        {
            above = below = left = right = false;
            climbingSlope = descendingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }
}
