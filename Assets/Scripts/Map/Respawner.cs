using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawner : MonoBehaviour
{

    SavePoint lastestSavePoint;
    public void Save(SavePoint savePoint)
    {
        lastestSavePoint = savePoint;
    }
    public void Respawn(GameObject gameObject)
    {
        gameObject.transform.position = lastestSavePoint.spawnPoint;
    }
}
