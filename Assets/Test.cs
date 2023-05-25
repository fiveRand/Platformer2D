using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public Transform from;
    public Transform to;

    private void OnDrawGizmos() 
    {
        Vector2 direction = (to.position - from.position).normalized;
        var angle = Vector2.Angle(direction, Vector2.up);
        angle *= Mathf.Deg2Rad;
        Vector2 slopeVector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Debug.Log(slopeVector);
    }
}
