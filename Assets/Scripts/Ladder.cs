using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ladder : MonoBehaviour, IInteractable
{
    BoxCollider2D col;
    Vector2 size
    {
        get
        {
            return col.bounds.size;
        }
    }

    private void Awake() {
        col = GetComponent<BoxCollider2D>();
    }
    public Vector2 OnGrab(Vector2 hitPoint)
    {
        return new Vector2(transform.position.x, hitPoint.y);
    }

    public bool isReachedDestination(Vector2 velocity,Vector2 position,Vector2 size)
    {
        int directionY = (int)Mathf.Sign(velocity.y);
        Vector2 destination = transform.position + (Vector3.up * directionY) * col.bounds.size.y * 0.5f;
        float distSqr = Vector2.SqrMagnitude(destination - position);
        return (distSqr < 0.1f) ? true : false;
    }

    public bool isReachedDestination(int directionY,Vector2 position)
    {
        Vector2 pos = transform.position + (Vector3.up * directionY)* col.bounds.size.y  * 0.5f;
        float distSqr = Vector2.SqrMagnitude(pos - position);
        return (distSqr < 0.1f) ? true : false;
    }
}
