using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SavePoint : MonoBehaviour
{
    BoxCollider2D col;
    public Vector3 spawnPoint
    {
        get
        {
            return transform.position + Vector3.up * 2;
        }
    }
    private void Awake() {
        if(col == null)
        {
            col = GetComponent<BoxCollider2D>();
        }

        col.isTrigger = true;
    }


}
