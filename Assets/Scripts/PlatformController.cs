using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlatformController : RaycastController
{
    public LayerMask passengerMask;

    public Vector3[] localWaypoints;
    Vector3[] globalWaypoints;

    public float speed;
    public bool cyclic;
    public float waitTime;
    [Range(0, 2)]
    public float easeAmount;
    public Vector3 velocity;

    List<Passenger> passengers;
    Dictionary<Transform, PlayerController> passengerDictionary = new Dictionary<Transform, PlayerController>();

    int fromWaypointIndex;
    float percentBetweenWaypoints;
    float nextMoveTime;

    public override void Start()
    {
        base.Start();
        passengers = new List<Passenger>(10);
        globalWaypoints = new Vector3[localWaypoints.Length];
        for (int i = 0; i < localWaypoints.Length;i++)
        {
            globalWaypoints[i] = localWaypoints[i] + transform.position;
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        velocity = CalculatePlatformMovement();
        // CalculatePassengerMovement(velocity);
        transform.Translate(velocity);
        // MovePassenger(false);
    }

    Vector3 CalculatePlatformMovement()
    {

        if (Time.time < nextMoveTime)
        {
            return Vector3.zero;
        }

        fromWaypointIndex %= globalWaypoints.Length;
        
        int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;
        float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex]);
        percentBetweenWaypoints += Time.fixedDeltaTime * speed / distanceBetweenWaypoints;
        percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints);
        float easedPercentBetweenWaypoints = Ease(percentBetweenWaypoints);

        Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], easedPercentBetweenWaypoints);

        if (percentBetweenWaypoints >= 1)
        {
            percentBetweenWaypoints = 0;
            fromWaypointIndex++;

            if (!cyclic)
            {
                if (fromWaypointIndex >= globalWaypoints.Length - 1)
                {
                    fromWaypointIndex = 0;
                    System.Array.Reverse(globalWaypoints);
                }
            }
            nextMoveTime = Time.time + waitTime;
        }

        return newPos - transform.position;
    }

    float Ease(float x)
    {
        float a = easeAmount + 1;
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }

    void CalculatePassengerMovement(Vector3 velocity)
    {
        HashSet<Transform> movedPassengers = new HashSet<Transform>();
        passengers.Clear();

        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);
        /*
        if(velocity.y != 0) // y축 충돌처리
        {
            float rayLength = Mathf.Abs(velocity.y) + skinWidth;
            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                rayOrigin += Vector2.right * (verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

                if(hit && hit.distance != 0)
                {
                    if(!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = (directionY == 1) ? velocity.x : 0;
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;

                        Passenger pass = new Passenger(hit.transform, new Vector3(pushX, pushY), directionY == 1, true);

                        passengers.Add(pass);
                    }
                }
            }
        }
        
        if(velocity.x != 0) // x축 충돌처리
        {
            float rayLength = Mathf.Abs(velocity.x) + skinWidth;
            for (int i = 0; i < horizontalRayCount; i++)
            {
                Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        float pushY = -skinWidth;


                        Passenger pass = new Passenger(hit.transform, new Vector3(pushX, pushY), false, true);
                        passengers.Add(pass);
                    }
                }
            }
        
        }
        */

        if(directionY != 0 || velocity.y == 0 && velocity.x != 0) // 플레이어가 플랫폼 위에 탑승할 때
        {
            float rayLength = skinWidth * 2;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);

                if(hit && hit.distance != 0)
                {
                    if(!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        Vector3 vel = new Vector3(velocity.x, velocity.y);

                        passengers.Add(new Passenger(hit.transform, vel, true, false));
                    }
                }
            }
        }
    }

    struct Passenger
    {
        public Transform transform;
        public Vector3 velocity;
        public bool standingOnPlatform;
        public bool moveBeforePlatform;

        public Passenger(Transform transform, Vector3 velocity, bool standingOnPlatform, bool moveBeforePlatform)
        {
            this.transform = transform;
            this.velocity = velocity;
            this.standingOnPlatform = standingOnPlatform;
            this.moveBeforePlatform = moveBeforePlatform;
        }
    }

    private void OnDrawGizmosSelected() {
        if(localWaypoints != null)
        {
            Gizmos.color = Color.cyan;
            float size = .3f;

            for (int i = 0; i < localWaypoints.Length; i++)
            {
                Vector3 pos = (Application.isPlaying) ? globalWaypoints[i] : localWaypoints[i] + transform.position;
                Gizmos.DrawLine(pos + Vector3.down * size, pos + Vector3.up * size);
                Gizmos.DrawLine(pos + Vector3.right * size, pos + Vector3.left * size);
            }
        }
    }
}
