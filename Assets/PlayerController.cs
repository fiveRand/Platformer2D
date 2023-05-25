using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerController : RaycastController
{
    public CollisionInfo info;
    Vector3 velocityOld;
    public float moveSpeed = 6;
    public float accelerationTimeGrounded = .8f;
    public float accelerationTimeAirborne = .1f;
    public float crouchSpeed = 2;

    [Header("Crouch & Sliding")]
    public float slopeIncreaseMultiplier = 0.2f;
    public float drag =0.2f;
    /// <summary>
    /// slide velocity increasement is based on it's gravity and slope multiplier.
    /// slope multiplier for example) 30 deg = 0.5, 45 deg = 0.7
    /// I suggest to try testing many slope (30, 45, 60...)slide to find perfect dragFactor
    /// </summary>
    /// 

    public float airDrag = 0.01f;
    public float minSpeed2Slide = 3;

    [Header("Jump")]
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    [Range(0.01f, 0.5f)] public float coyoteTime = 0.1f;
    // 캐릭터가 달려나가 아슬아슬한 땅끝자락으로부터 점프하려고할 때 
    // 점프입력을 못받아 그대로 추락하는 꼴을 막아준다
    float lastCoyoteTimer;
    [Range(0.01f, 0.5f)] public float jumpInputBufferTime = 0.2f;
    // 점프가 불가능한 상황일때 점프를 눌러 입력받지 않는 걸 방지하여 점프입력의 유연함을 주기위한 값
    float lastPressedJumpTimer;
    float gravity;
    float maxJumpVelocity, minJumpVelocity;

    [Header("Wall")]
    [SerializeField] float wallJumpMultiplier = 0.7f;
    [SerializeField] bool onWall = false;
    [Range(0f, 45f)][SerializeField] float wallJumpAngle = 15f;
    // 45 + 15f = 60f

    public Vector3 velocity;
    float velocityXSmoothing;
    [Header("Slope")]
    public float maxClimbAngle = 70;
    public float maxDescendAngle = 70;
    /// <summary>
    /// 박스 콜라이더 y크기의 기준으로의 길이를
    /// 떨어지는 지점으로부터 아래의 땅바닥 길이를 비교해 
    /// 경사로를 타고 내려오게 만들 것인지를 결정함
    /// </summary>
    [Range(0, 1)]
    public float stepTolerance = 0.25f; 
    [Header("Through platform")]
    public bool fallingThroughPlatform = false;
    public float fallThroughSecond;
    public LayerMask throughableMask;
    public int faceDirection;

    int layers;
    Vector2 inputVector;

    public override void Start()
    {
        base.Start();

        layers = throughableMask | collisionMask;

        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

        float ySize = boxCollider.bounds.size.y;

    }
    private void Update()
    {

        inputVector.x = Input.GetAxisRaw("Horizontal");
        inputVector.y = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            lastPressedJumpTimer = jumpInputBufferTime;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (velocity.y > minJumpVelocity)
            {
                velocity.y = minJumpVelocity;
            }
        }
        if(Input.GetKeyDown(KeyCode.C))
        {
            info.onCrounch = true;
        }
        else if(Input.GetKeyUp(KeyCode.C))
        {
            info.onCrounch = false;
        }
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();
        FixedTickTimer();
        InputVelocity();
        Move(velocity * Time.fixedDeltaTime);
    }

    bool isSliding
    {
        get
        {
            return Mathf.Abs(velocity.x) > minSpeed2Slide && info.onCrounch;
        }
    }

    void InputVelocity()
    {

        if (isSliding)
        {
            float slopeRadian = info.slopeAngle * Mathf.Deg2Rad;
            Vector2 slopeVector = new Vector2(Mathf.Cos(slopeRadian), Mathf.Sin(slopeRadian));
            // velocity.x += slopeVector.y * -gravity * slopeIncreaseMultiplier;
            //velocity.x += Mathf.Sign(info.slopeNormal.x) * Mathf.Abs(velocity.y) * slopeVector.y;

            float ang = Vector2.Angle(velocity.normalized, info.slopeNormal);
            Debug.Log(ang);

            velocity.x -= Mathf.Sign(velocity.x) * Mathf.Abs(slopeVector.x) * drag;
            velocity.x += Mathf.Sign(info.slopeNormal.x) * slopeVector.y * slopeIncreaseMultiplier;
            Debug.DrawRay(transform.position, info.slopeNormal, Color.red);
            Debug.DrawRay(transform.position, velocity.normalized, Color.cyan);

            

        }
        else
        {
            float acceleration = (info.below) ? accelerationTimeGrounded : accelerationTimeAirborne;
            if (inputVector.x != 0)
            {
                velocity.x += inputVector.x * acceleration;
                velocity.x = Mathf.Clamp(velocity.x, -moveSpeed, moveSpeed);
            }
            else
            {
                velocity.x = Mathf.MoveTowards(velocity.x, 0, acceleration);
            }
        }

        OnGravity();


        if (lastPressedJumpTimer > 0)
        {
            OnJump();

            lastPressedJumpTimer = 0;
        }

        
    }


    void OnGravity()
    {
        if (info.above || info.below)
        {

            if (info.slidingDownMaxSlope)
            {
                velocity.y += -info.slopeNormal.y * gravity * Time.fixedDeltaTime;
            }
            else
            {
                velocity.y = 0;
                velocity.y += gravity * Time.fixedDeltaTime;
            }
        }

        else
        {
            velocity.y += gravity * Time.fixedDeltaTime;
        }

    
    }

    void OnJump()
    {
        if (info.below)
        {
            if(info.slidingDownMaxSlope)
            {
                if(inputVector.x != -Mathf.Sign(info.slopeNormal.x))
                {
                    velocity.y = maxJumpVelocity * info.slopeNormal.y;
                    velocity.x = maxJumpVelocity * info.slopeNormal.x;
                }
            }
            else
            {
                velocity.y = maxJumpVelocity;

            }
        }
        else // if mid-air
        {
             if(info.onWall)
            {
                faceDirection = -faceDirection;
                float radian = Mathf.Deg2Rad * (45 + wallJumpAngle);
                Vector2 wallJumpDirection = new Vector2(faceDirection * Mathf.Cos(radian), Mathf.Sin(radian));
                velocity = wallJumpDirection * maxJumpVelocity * wallJumpMultiplier;
            }
        }
    }


    void FixedTickTimer()
    {
        if (lastPressedJumpTimer > 0)
        {
            lastPressedJumpTimer -= Time.fixedDeltaTime;
        }
    }

    public void Move(Vector3 velocity)
    {
        UpdateRaycastOrigins();
        info.Reset();
        velocityOld = velocity;
        if (velocity.x != 0)
        {
            faceDirection = (int)Mathf.Sign(velocity.x);
        }
        if(velocity.y < 0)
        {
            DescendSlope(ref velocity);
        }
        ClimbingSlope(ref velocity);
        HorizontalCollision(ref velocity);
        if (velocity.y != 0)
        {
            VerticalCollision(ref velocity);
        }



        transform.Translate(velocity);
    }




    void ClimbingSlope(ref Vector3 velocity)
    {
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;
        Vector2 rayOrigin = (faceDirection == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
        var hits = Physics2D.RaycastAll(rayOrigin, Vector2.right * faceDirection, rayLength, layers);
        if (hits.Length > 0)
        {
            RaycastHit2D hit = hits[0];
            foreach (var rayHit in hits)
            {
                // Debug.Log($"name {rayHit.collider.gameObject.name}");

                if (1 << rayHit.collider.gameObject.layer != throughableMask.value)
                {
                    hit = rayHit;
                    break;
                }

            }

            // Debug.Log(hit.normal);
            Debug.DrawRay(rayOrigin, Vector2.right * faceDirection * hit.distance, Color.cyan);

            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            // 제대로 경사로에 맞춰 올라가기 위해 시작점 거리 계산을 한다
            if (slopeAngle < maxClimbAngle)
            {


                
                float dist2SlopeStart = 0;
                if (slopeAngle != info.slopeAngleOld)
                {
                    dist2SlopeStart = hit.distance - skinWidth;
                    velocity.x -= dist2SlopeStart * faceDirection;
                }
                
                float AbsVelocityX = Mathf.Abs(velocity.x);
                float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * AbsVelocityX;

                if (velocity.y <= climbVelocityY)
                {
                    velocity.y = climbVelocityY;
                    velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * AbsVelocityX * faceDirection;
                    info.below = true;
                    info.climbingSlope = true;
                    info.slopeAngle = slopeAngle;
                    info.slopeNormal = hit.normal;
                }
                velocity.x += dist2SlopeStart * faceDirection;
            }
        }
    }

    void DescendSlope(ref Vector3 velocity)
    {

        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(velocity.y) + skinWidth, layers);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(velocity.y) + skinWidth, layers);
        if(maxSlopeHitLeft ^ maxSlopeHitRight) // OR 연산자, 값이 서로 다르면 true, 값이 모두 같으면 false를 출력한다
        {
            SlideDownMaxSlope(maxSlopeHitLeft, ref velocity);
            SlideDownMaxSlope(maxSlopeHitRight, ref velocity);
        }
        float cliffTolerance = boxCollider.bounds.size.y * stepTolerance;
        
        if(!info.slidingDownMaxSlope)
        {
            Vector2 rayOrigin = (faceDirection == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;

            var hits = Physics2D.RaycastAll(rayOrigin, Vector2.down, Mathf.Abs(velocity.y) + skinWidth, layers);

            if (hits.Length > 0)
            {
                RaycastHit2D hit = hits[0];
                foreach (var rayHit in hits)
                {
                    if (1 << rayHit.collider.gameObject.layer != throughableMask.value)
                    {
                        hit = rayHit;
                        break;
                    }
                }
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != 0 && slopeAngle <= maxDescendAngle)
                {
                    if (Mathf.Sign(hit.normal.x) == faceDirection)
                    {

                        float rayLength = hit.distance - skinWidth;
                        float moveDist = Mathf.Abs(velocity.x);
                        float minYBump = Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * moveDist + cliffTolerance;

                        /// cliffTolerance 값이 경사로 끝점 둔턱에 따라 경사로에 붙을수도 아닐수도 있다
                        /// 그래서 cliffTolerance 으로 충분한 보정값을 넣어 무시할수 있도록 둔다
                        /// 디버깅 참고
                        /// Debug.DrawRay(rayOrigin, Vector2.down * cliffTolerance, Color.cyan);
                        /// 내 자신에게 메모)
                        /// 만약 해당 이동속도를 넘은 속도로 경사로를 내려가려 할때 경사로 이동을 방지하고 싶다면
                        /// if (rayLength <= cliffTolerance && Mathf.Abs(MaxMoveSpeed) <= moveDist)



                        if (rayLength <= minYBump)
                        {
                            
                            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDist * faceDirection;
                            velocity.y -= Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDist + cliffTolerance;

                            info.slopeAngle = slopeAngle;
                            info.slopeNormal = hit.normal;
                            info.descendingSlope = true;
                            info.below = true;
                        }
                    }
                }
            }
        }
        
    }

    void SlideDownMaxSlope(RaycastHit2D hit, ref Vector3 velocity)
    {
        if(hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if(slopeAngle > maxClimbAngle)
            {
                velocity.x = ((Mathf.Abs(velocity.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad)) * Mathf.Sign(hit.normal.x);
                faceDirection = (int)Mathf.Sign(velocity.x);
                info.slopeAngle = slopeAngle;
                info.slopeNormal = hit.normal;
                info.slidingDownMaxSlope = true;
            }
        }
    }

    void HorizontalCollision(ref Vector3 velocity)
    {
        Vector3 colSize = boxCollider.bounds.size;
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (faceDirection == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);

            var hits = Physics2D.RaycastAll(rayOrigin, Vector2.right * faceDirection, rayLength, layers);
            if (hits.Length > 0)
            {
                RaycastHit2D hit = hits[0];
                foreach (var rayHit in hits)
                {

                    if (1 << rayHit.collider.gameObject.layer != throughableMask.value)
                    {
                        hit = rayHit;
                        break;
                    }
                }

                if (hit.distance == 0 || throughableMask.value == 1 << hit.collider.gameObject.layer) // ThroughPlatform 메서드, 즉 벽 뚫고갈때 충돌을 무시하기 위함
                {
                    continue;
                }

                if (!info.climbingSlope)
                {

                    velocity.x = (hit.distance - skinWidth) * faceDirection;
                    rayLength = hit.distance;

                    if (info.climbingSlope) // 경사로 올라가다가 벽 같은 것에 도달했을때
                    {
                        // x 좌표로 쏜 ray로 각도에 탄젠트와 x 속력을 곱해 올라가는 걸 막는다
                        velocity.y = Mathf.Tan(info.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }
                    
                    rayOrigin = (faceDirection == -1) ? raycastOrigins.topLeft : raycastOrigins.topRight;
                    rayOrigin += Vector2.down * colSize.y * 0.5f;
                    rayOrigin += Vector2.right * faceDirection * verticalRaySpacing;
                    RaycastHit2D downHit = Physics2D.Raycast(rayOrigin, Vector2.down, colSize.y * 0.5f, layers);
                    if (downHit)
                    {
                        if (downHit.distance != 0)
                        {
                            velocity.x = 0;
                            Vector3 pos = downHit.point;
                            pos.x -= colSize.x * 0.5f * faceDirection;
                            pos.y += colSize.y * 0.5f;
                            transform.position = pos;
                            return;
                        }

                    }
                    

                    info.left = faceDirection == -1;
                    info.right = faceDirection == 1;
                }
            }
            // Debug.DrawRay(rayOrigin, Vector3.right * faceDirection * rayLength, Color.cyan);
        }
    }


    void VerticalCollision(ref Vector3 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;
        PlatformController controller = null;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            var hits = Physics2D.RaycastAll(rayOrigin, Vector2.up * directionY, rayLength, layers);
            if (hits.Length > 0)
            {
                RaycastHit2D hit = hits[0];
                foreach(var rayHit in hits)
                {

                    if(1 << rayHit.collider.gameObject.layer != throughableMask.value)
                    {
                        hit = rayHit;
                        break;
                    }
                }


                if (controller == null)
                {
                    controller = hit.transform.GetComponent<PlatformController>();
                }
                if (1 << hit.collider.gameObject.layer == throughableMask.value)
                {

                    if (directionY == 1 || hit.distance == 0 ||fallingThroughPlatform)
                    {

                        continue;
                    }
                    if (!fallingThroughPlatform && inputVector.y == -1)
                    {
                        StartCoroutine(FallThorughPlatformRoutine());
                        continue;
                    }
                }

                rayLength = hit.distance;
                velocity.y = (rayLength - skinWidth) * directionY;
                // 땅바닥에 쏜 레이 길이만큼 밀어내게 만들라고 지시, skinWidth은 rayLength 에서 스킨값을 고려하여 차감함

                if (info.climbingSlope) // 올라가다가 머리 위에 벽이 있을때 
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

        if (controller != null)
        {
            transform.SetParent(controller.transform);
        }
        else
        {
            transform.SetParent(null);
        }
        
        if(info.climbingSlope)
        {
            rayLength = Mathf.Abs(velocity.x) + skinWidth;
            Vector2 rayOrigin = ((faceDirection == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight);
            rayOrigin += Vector2.up * velocity.y;
            var hits = Physics2D.RaycastAll(rayOrigin, Vector2.right * faceDirection, rayLength, layers);
            if(hits.Length > 0)
            {
                RaycastHit2D hit = hits[0];
                foreach (var rayHit in hits)
                {

                    if (1 << rayHit.collider.gameObject.layer != throughableMask.value)
                    {
                        hit = rayHit;
                        break;
                    }
                }

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (1 << hit.collider.gameObject.layer != throughableMask.value)
                {
                    if (slopeAngle != info.slopeAngle)
                    {
                        velocity.x = (hit.distance - skinWidth) * faceDirection;
                        info.slopeAngle = slopeAngle;
                        info.slopeNormal = hit.normal;
                    }
                }
            }
        }
        
        
        

    }

    IEnumerator FallThorughPlatformRoutine()
    {
        fallingThroughPlatform = true;
        yield return new WaitForSeconds(fallThroughSecond);
        fallingThroughPlatform = false;
    }


    [System.Serializable]
    public struct CollisionInfo
    {
        public bool above, below, left, right;
        public bool climbingSlope, descendingSlope, slidingDownMaxSlope;
        public bool onWall
        {
            get
            {
                return (left || right) && !below;
            }
        }
        public bool onCrounch;

        public float slopeAngle, slopeAngleOld;
        public Vector2 slopeNormal;

        public void Reset()
        {
            above = below = left = right = false;
            climbingSlope = descendingSlope =slidingDownMaxSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
            slopeNormal = Vector2.zero;

            
        }
    }


    void CheckCliffDown(ref Vector3 velocity)
    {
        Vector2 rayOrigin = (faceDirection == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
        if (inputVector.y < 0)
        {
            Vector3 colSize = boxCollider.size;
            rayOrigin += Vector2.down * verticalRaySpacing;
            var hit = Physics2D.Raycast(rayOrigin, Vector2.left * faceDirection, colSize.x, layers);
            if (hit.distance == 0)
            {
                return;
            }
            if (hit)
            {

                float moveAwayX = colSize.x - hit.distance;
                float offsetY = colSize.y;
                transform.position += moveAwayX * Vector3.right * faceDirection + Vector3.down * offsetY;
                faceDirection = -faceDirection;
            }
        }
    }

    void DetectCliff()
    {
        Vector3 colSize = boxCollider.bounds.size;
        float rayLength = Mathf.Abs(velocity.y) + colSize.y;
        Vector2 rayOrigin = (faceDirection == -1) ? raycastOrigins.topLeft : raycastOrigins.topRight;
        //var hit = Physics2D.Raycast(rayOrigin, Vector2.right * faceDirection, rayLength, collisionMask);
        // Debug.DrawRay(rayOrigin, Vector2.right * faceDirection * rayLength,Color.cyan);
        rayOrigin += Vector2.up * verticalRaySpacing;
        rayOrigin += Vector2.right * faceDirection * horizontalRaySpacing;
        var yPosHit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, collisionMask);
        Debug.DrawRay(rayOrigin, Vector2.down * rayLength, Color.cyan);

        if (inputVector.y < 0)
        {
            return;
        }

        if (yPosHit)
        {
            Vector3 pos = transform.position;
            pos.y = yPosHit.point.y - colSize.y * 0.5f;
            if (yPosHit.distance == 0)
            {
                return;
            }
            else if (yPosHit.distance <= colSize.y * 0.5f)
            {

                velocity.y = 0;
                velocity.y -= gravity * Time.fixedDeltaTime;
                transform.position = pos;
            }
        }


    }
}
