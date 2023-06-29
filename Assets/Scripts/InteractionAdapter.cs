using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionAdapter 
{
    public void OnInteract(PlayerController player,RaycastHit2D hit)
    {
        IInteractable interactable = null;
        Transform hitTransform = hit.transform;
        while (hitTransform != null)
        {
            interactable = hitTransform.GetComponent<IInteractable>();
            hitTransform = hitTransform.parent;
        }

        switch (interactable)
        {
            case ZipLine zipline:

                player.velocity = Vector2.zero;
                player.transform.position = zipline.OnGrab(hit.point);
                player.status = PlayerController.Status.OnZipline;
                player.zipLine = zipline;
                break;
            case Ladder ladder:
                player.velocity = Vector2.zero;
                player.transform.position = ladder.OnGrab(hit.point);
                player.transform.position += Vector3.up * player.boxCollider.bounds.size.y * 0.5f;
                player.status = PlayerController.Status.OnLadder;
                player.ladder = ladder;
                break;
                default:
                Debug.LogError("Cant Found interactable!");
                break;


        }
    }
}
