using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerController : RaycastController
{
    public enum Status
    {
        Idle,
        OnLadder,
        OnZipline,
    }
    public CollisionInfo info;
    Vector3 velocityOld;
    public float moveSpeed = 6;
    public float ladderSpeed = 4;
    public float accelerationTimeGrounded = .8f;
    public float accelerationTimeAirborne = .1f;
    public float crouchSpeed = 2;

    [Header("Crouch & Sliding")]
    public float slopeIncreaseMultiplier = 0.2f;
    [Range(0,1)]public float slidingFriction;
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
    [SerializeField] float wallSlideSpeedMax = 3;
    [SerializeField] float wallStickTime = 0.25f;
    public float lastWallStickTimer = 0;

    int wallDirX;

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
    public LayerMask throughableMask;
    public LayerMask interactableMask;
    public LayerMask passiveMask;
    public LayerMask deadzoneMask;

    [Header("Rope")]
    public float ropeDist = 10;
    public float ropeSpeed = 10;
    public RopePhysics ropePhysic;

    int faceDirection;
    public Status status;

    Vector2 onAirborneVelocity;
    Vector2 onZiplineVelocity;
    InteractionAdapter interactAdapter;
    int layers;
    Vector2 inputVector;
    [HideInInspector]public ZipLine zipLine;
    [HideInInspector] public Ladder ladder;
    public Respawner respawner;

    bool onCrounch;

    public override void Start()
    {
        base.Start();
        interactAdapter = new InteractionAdapter();
        layers = throughableMask | collisionMask | passiveMask | deadzoneMask;
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
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

        if(Input.GetKeyDown(KeyCode.Q))
        {
            SetRope();
        }

        if(Input.GetKeyDown(KeyCode.E))
        {
            Interaction();
        }
        if(Input.GetKeyDown(KeyCode.C))
        {
            CrounchMethod(true);
        }
        else if(Input.GetKeyUp(KeyCode.C))
        {
            CrounchMethod(false);
        }
    }

    bool isOnRope = false;
    Coroutine ropeRoutine;
    void SetRope()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mousePos - transform.position;
        if(ropeRoutine != null)
        {
            StopCoroutine(ropeRoutine);
        }
        ropeRoutine = StartCoroutine(TryRopeCoroutine(dir));
        // RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, ropeDist, collisionMask);
    }

    void CrounchMethod(bool isTrue)
    {
        onCrounch = isTrue;
        if(isTrue)
        {
            Vector3 size = Vector3.one;
            size.y = 0.5f;
            transform.localScale = size;
            CalculateRaySpacing();
        }
        else
        {
            transform.localScale = Vector3.one;
            CalculateRaySpacing();
        }
    }

    IEnumerator TryRopeCoroutine(Vector2 direction)
    {
        direction.Normalize();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, ropeDist, collisionMask);
        float distance = 0;
        float hitDist = 0;
        float returnDist = ropeDist;

        bool isReturning = false;
        if(hit)
        {
            hitDist = hit.distance;
        }

        // Debug.Log(hitDist);


        while(true)
        {
            if(hit && distance >= hitDist)
            {
                ropePhysic.OnHit(transform.position,hit.point,hitDist);
                // Debug.Log($"HitDist {hitDist}, measurement {(ropePhysic.segmentLength * ropePhysic.segmentCount) / hitDist}");
                break;
            }
            else if(distance >= ropeDist)
            {
                isReturning = true;

            }
            
            if(isReturning)
            {
                distance -= ropeSpeed * Time.fixedDeltaTime;
                if(distance <= 0)
                {
                    ropePhysic.OnReturn(transform.position, direction, 0);
                    break;
                }
                ropePhysic.OnReturn(transform.position, direction, distance);
            }
            else
            {
                distance += ropeSpeed * Time.fixedDeltaTime;
                ropePhysic.OnShoot(transform.position, direction, distance);
            }
            yield return new WaitForFixedUpdate();

        }
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();
        FixedTickTimer();
        InputVelocity();
        Move(velocity * Time.fixedDeltaTime);


        if(info.climbingSlope || info.descendingSlope)
        {
            int directionY = (info.climbingSlope) ? 1 : -1;
            velocity.y = directionY * Mathf.Sin(info.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
        }
        else if (info.above || info.below)
        {
            if (info.slidingDownMaxSlope)
            {
                velocity.y += -info.slopeNormal.y * gravity * Time.fixedDeltaTime;
            }
            else
            {
                velocity.y = 0;
            }
        }

        
    }


    bool isWallSliding
    {
        get
        {
            return velocity.y < 0 && !info.below && (info.left || info.right);
        }
    }
    

    void WallHandle()
    {
        wallDirX = (info.left) ? -1 : 1;

        if(isWallSliding && wallDirX == faceDirection)
        {
            if (inputVector.x == wallDirX || inputVector.x == 0)
            {
                lastWallStickTimer = wallStickTime;
            }

            if(lastWallStickTimer > 0)
            {
                if (velocity.y < -wallSlideSpeedMax)
                {
                    velocity.y = -wallSlideSpeedMax;
                }
            }
        }
    }

    void OnIdle()
    {
        float acceleration;
        if(info.below)
        {

            if (onCrounch)
            {
                OnSlide();
                return;
            }

            acceleration = accelerationTimeGrounded;
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
        else
        {
            acceleration = accelerationTimeAirborne;
            if (inputVector.x != 0)
            {
                if (Mathf.Abs(velocity.x) > moveSpeed && faceDirection == inputVector.x) // 같은 방향을 바라보는데 현 속도가 최대속력보다 높다
                {
                    acceleration = 0;
                }
                velocity.x += inputVector.x * acceleration;
            }
        }
    }

    void OnSlide()
    {
        Vector2 rayOrigin = (faceDirection == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, Mathf.Infinity, layers);
        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            velocity.x += Mathf.Abs(onAirborneVelocity.y) * hit.normal.x;
            velocity.x += hit.normal.x * slopeIncreaseMultiplier;
            velocity.x -= Mathf.Sign(velocity.x) * Mathf.Abs(Mathf.Cos(slopeAngle)) * slidingFriction;

        }

        onAirborneVelocity = Vector2.zero;
    }

    void InputVelocity()
    {
        if (status != Status.OnZipline)
        {
            onZiplineVelocity = velocity;
        }
        if(!info.below)
        {
            onAirborneVelocity = velocity;
        }

        switch(status)
        {
            case Status.Idle:
                OnIdle();
                velocity.y += gravity * Time.fixedDeltaTime;
                break;

                case Status.OnLadder:
                velocity.y = inputVector.y * ladderSpeed;
                if (ladder.isReachedDestination(velocity, transform.position, boxCollider.bounds.size))
                {
                    velocity.y = 0;
                }
                break;

                case Status.OnZipline:
                velocity.x += onZiplineVelocity.x;
                onZiplineVelocity = Vector3.zero;
                float speed = Mathf.MoveTowards(velocity.magnitude, zipLine.speed, 0.1f);
                velocity = zipLine.GetDirection(faceDirection) * speed;

                if (zipLine.isReachedDestination(faceDirection, transform.position))
                {
                    status = Status.Idle;
                }
                break;
        }

        WallHandle();

        if (lastPressedJumpTimer > 0)
        {
            OnJump();

            lastPressedJumpTimer = 0;
        }
    }


    void OnJump()
    {
        if(status != Status.Idle)
        {
            velocity.y = maxJumpVelocity ;
            status = Status.Idle;
        }

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
            if(!info.below && (info.left || info.right) && wallDirX == faceDirection)
            {
                faceDirection = -faceDirection;
                float radian = Mathf.Deg2Rad * (45 + wallJumpAngle);
                Vector2 wallJumpDirection = new Vector2(faceDirection * Mathf.Cos(radian), Mathf.Sin(radian));
                velocity = wallJumpDirection * maxJumpVelocity * wallJumpMultiplier;
            }
        }
    }

    void Interaction()
    {
        if (status != Status.Idle)
        {
            status = Status.Idle;
            return;
        }

        zipLine = null;
        float rayLength = boxCollider.bounds.size.x;
        int halfHorizontalRayCount = Mathf.CeilToInt(horizontalRayCount * 0.5f);
        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (faceDirection == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            var hit = Physics2D.Raycast(rayOrigin, Vector2.right * faceDirection, rayLength, interactableMask);
            if (hit)
            {
                interactAdapter.OnInteract(this,hit);
                break;
            }
        }

    }


    void FixedTickTimer()
    {
        if (lastPressedJumpTimer > 0)
        {
            lastPressedJumpTimer -= Time.fixedDeltaTime;
        }

        if(lastWallStickTimer > 0)
        {
            lastWallStickTimer -= Time.fixedDeltaTime;
        }
    }

    public void Move(Vector3 moveAmount)
    {

        UpdateRaycastOrigins();
        info.Reset();
        if (moveAmount.x != 0)
        {
            faceDirection = (int)Mathf.Sign(moveAmount.x);
        }

        if (moveAmount.y < 0)
        {
            DescendSlope(ref moveAmount);
        }
        ClimbingSlope(ref moveAmount);
        HorizontalCollision(ref moveAmount);
        if (moveAmount.y != 0)
        {
            VerticalCollision(ref moveAmount);
        }

        transform.Translate(moveAmount);    
    }


    void ClimbingSlope(ref Vector3 moveAmount)
    {
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
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
            // Debug.DrawRay(rayOrigin, Vector2.right * faceDirection * hit.distance, Color.cyan);

            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            // 제대로 경사로에 맞춰 올라가기 위해 시작점 거리 계산을 한다
            if (slopeAngle < maxClimbAngle)
            {


                
                float dist2SlopeStart = 0;
                if (slopeAngle != info.slopeAngleOld)
                {
                    dist2SlopeStart = hit.distance - skinWidth;
                    moveAmount.x -= dist2SlopeStart * faceDirection;
                }
                
                float AbsVelocityX = Mathf.Abs(moveAmount.x);
                float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * AbsVelocityX;

                if (moveAmount.y <= climbVelocityY)
                {
                    moveAmount.y = climbVelocityY;
                    moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * AbsVelocityX * faceDirection;
                    info.below = true;
                    info.climbingSlope = true;
                    info.slopeAngle = slopeAngle;
                    info.slopeNormal = hit.normal;
                }
                moveAmount.x += dist2SlopeStart * faceDirection;
            }
        }
    }

    void DescendSlope(ref Vector3 moveAmount)
    {

        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, layers);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, layers);
        if(maxSlopeHitLeft ^ maxSlopeHitRight) // OR 연산자, 값이 서로 다르면 true, 값이 모두 같으면 false를 출력한다
        {
            SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
            SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
        }
        float cliffTolerance = boxCollider.bounds.size.y * stepTolerance;
        
        if(!info.slidingDownMaxSlope)
        {
            Vector2 rayOrigin = (faceDirection == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;

            var hits = Physics2D.RaycastAll(rayOrigin, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, layers);

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
                        float moveDist = Mathf.Abs(moveAmount.x);
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
                            
                            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDist * faceDirection;
                            moveAmount.y -= Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDist + cliffTolerance;

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

    void SlideDownMaxSlope(RaycastHit2D hit, ref Vector3 moveAmount)
    {
        if(hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if(slopeAngle > maxClimbAngle)
            {
                moveAmount.x = ((Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad)) * Mathf.Sign(hit.normal.x);
                faceDirection = (int)Mathf.Sign(moveAmount.x);
                info.slopeAngle = slopeAngle;
                info.slopeNormal = hit.normal;
                info.slidingDownMaxSlope = true;
            }
        }
    }

    void HorizontalCollision(ref Vector3 moveAmount)
    {
        Vector3 colSize = boxCollider.bounds.size;
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth * 2;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (faceDirection == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);

            var hits = Physics2D.RaycastAll(rayOrigin, Vector2.right * faceDirection, rayLength, layers);
            Debug.DrawRay(rayOrigin, Vector2.right * faceDirection * rayLength, Color.cyan);
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
                if (1 << hit.collider.gameObject.layer == passiveMask.value)
                {
                    SavePoint savePoint = hit.collider.gameObject.GetComponent<SavePoint>();
                    respawner.Save(savePoint);
                    continue;
                }

                if (1 << hit.collider.gameObject.layer == deadzoneMask.value)
                {
                    velocity = Vector3.zero;
                    respawner.Respawn(this.gameObject);
                    continue;
                }

                if (hit.distance == 0 || throughableMask.value == 1 << hit.collider.gameObject.layer) // ThroughPlatform 메서드, 즉 벽 뚫고갈때 충돌을 무시하기 위함
                {
                    continue;
                }

                if (!info.climbingSlope)
                {

                    moveAmount.x = (hit.distance - skinWidth) * faceDirection;
                    rayLength = hit.distance;

                    if (info.climbingSlope) // 경사로 올라가다가 벽 같은 것에 도달했을때
                    {
                        // x 좌표로 쏜 ray로 각도에 탄젠트와 x 속력을 곱해 올라가는 걸 막는다
                        moveAmount.y = Mathf.Tan(info.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                    }
                    /*
                    해당 코드는 계단식 이동하기 위해 써놨으나 버그 투성이라 냅뒀다
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
                    */
                    

                    info.left = faceDirection == -1;
                    info.right = faceDirection == 1;
                }
            }
            // Debug.DrawRay(rayOrigin, Vector3.right * faceDirection * rayLength, Color.cyan);
        }
    }

    GameObject ignorePlatform;

    void VerticalCollision(ref Vector3 moveAmount)
    {
        float directionY = Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;
        PlatformController controller = null;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
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

                if (1 << hit.collider.gameObject.layer == passiveMask.value)
                {
                    SavePoint savePoint = hit.collider.gameObject.GetComponent<SavePoint>();
                    respawner.Save(savePoint);
                    continue;
                }

                if (1 << hit.collider.gameObject.layer == deadzoneMask.value)
                {
                    velocity = Vector3.zero;
                    respawner.Respawn(this.gameObject);
                    continue;
                }


                if (controller == null)
                {
                    controller = hit.transform.GetComponent<PlatformController>();
                }
                if (1 << hit.collider.gameObject.layer == throughableMask.value)
                {
                    if(directionY == 1)
                    {
                        ignorePlatform = null;
                    }
                    if(directionY == 1 || hit.distance == 0|| hit.collider.gameObject == ignorePlatform)
                    {
                        continue;
                    }

                    if(inputVector.y == -1)
                    {
                        ignorePlatform = hit.collider.gameObject;
                        continue;
                    }
                }

                rayLength = hit.distance;
                moveAmount.y = (rayLength - skinWidth) * directionY;
                // 땅바닥에 쏜 레이 길이만큼 밀어내게 만들라고 지시, skinWidth은 rayLength 에서 스킨값을 고려하여 차감함

                if (info.climbingSlope) // 올라가다가 머리 위에 벽이 있을때 
                {
                    // tan(degree) = y / x
                    // x * tan(degree) = y
                    // x = y / tan(degree)
                    // 머리위의 벽으로부터 각도를 구해 속력 y값으로부터 삼각항법을 하여 x값을 대입한다
                    // 이러함으로 더이상 머리위에 벽이 있어 떠는 행동을 하지않을 것이다
                    moveAmount.x = moveAmount.y / Mathf.Tan(info.slopeAngle * Mathf.Deg2Rad) * faceDirection;
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
            rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
            Vector2 rayOrigin = ((faceDirection == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight);
            rayOrigin += Vector2.up * moveAmount.y;
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
                        moveAmount.x = (hit.distance - skinWidth) * faceDirection;
                        info.slopeAngle = slopeAngle;
                        info.slopeNormal = hit.normal;
                    }
                }
            }
        }
        
        
        

    }


    [System.Serializable]
    public struct CollisionInfo
    {
        public bool above, below, left, right;
        public bool climbingSlope, descendingSlope, slidingDownMaxSlope;

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
