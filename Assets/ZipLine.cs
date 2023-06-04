using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZipLine : MonoBehaviour
{
    PolygonCollider2D col;
    LineRenderer lr;
    Transform Arod; Transform APointer;
    Transform Brod; Transform BPointer;
    Vector2 rightDirection, leftDirection;
    public float speed = 5;

    private void Awake() 
    {
        Arod = transform.GetChild(0);
        APointer = Arod.GetChild(0);
        Brod = transform.GetChild(1);
        BPointer = Brod.GetChild(0);
        col = GetComponent<PolygonCollider2D>();
        lr = GetComponent<LineRenderer>();
        lr.SetPosition(0, APointer.position);
        lr.SetPosition(1, BPointer.position);

        Vector2 direction = (BPointer.position - APointer.position).normalized;
        float lineSize = lr.startWidth * 0.5f;
        Vector2[] points = new Vector2[4];

        points[0] = points[1] = APointer.position;
        points[0] += new Vector2(-direction.y, direction.x) * lineSize;
        points[1] += new Vector2(direction.y, -direction.x)* lineSize;
        
        points[2] = points[3] = BPointer.position;
        points[2] += new Vector2(direction.y, -direction.x) * lineSize;
        points[3] += new Vector2(-direction.y, direction.x) * lineSize;
        col.SetPath(0, points);

        rightDirection =(BPointer.position - APointer.position).normalized;
        leftDirection = (APointer.position - BPointer.position).normalized;
    }
    public Vector2 OnGrab(Vector2 hitPoint)
    {
        return GetClosestPointOnLine(APointer.position, BPointer.position, hitPoint);
    }
    
    public Vector3 GetDirection(int faceDirection)
    {
        return (faceDirection == 1) ? rightDirection : leftDirection;
    }

    public bool isReachedDestination(int faceDirection,Vector2 position)
    {
        Vector2 destination = (faceDirection == 1) ? BPointer.position : APointer.position;

        float distSqr = Vector2.SqrMagnitude(destination - position);

        bool result = (distSqr < 0.1f) ? true : false;
        return result;
    }

    Vector2 GetClosestPointOnLine(Vector2 start, Vector2 end,Vector2 point)
    {
        Vector2 ab = end - start;
        Vector2 ap = point - start;
        float distance = Vector2.Dot(ap, ab) / Vector2.SqrMagnitude(ab);
        float epsilon = 0.0001f;
        if(distance < 0f - epsilon)
        {
            return start;
        }
        else if(distance > 1f + epsilon)
        {
            return end;
        }
        else
        {
            return start + ab * distance;
        }
    }
}
