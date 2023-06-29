using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopePhysics : MonoBehaviour
{
    EdgeCollider2D col;
    public LineRenderer lr;
    public int segmentCount = 15;
    // 로프를 구성하는 조각의 갯수
    // 로프의 길이를 담당하나, 클수록 
    public float segmentLength = 0.3f; 
    // 로프를 구성하는 한 기다란 조각?의 길이
    // 적을수록 부드러워지고 클수록 딱딱해진다
    public float ropeWidth = 0.1f; // 로프의 너비, 비주얼적 효과다.
    public int constraintLoop = 15; // 통제력, 높을수록 탄성이 없어지나 그만큼 느려짐
    float gravity = -9.8f;
    [Space(10f)]
    public Transform startTransform;
    Vector2 startPos;
    Vector2 endPos;
    public LayerMask hitLayer;

    List<Segment> segments = new List<Segment>();

    private void Reset() {
        TryGetComponent(out lr);
        TryGetComponent(out col);
    }

    private void Awake() {
        startPos = startTransform.position;
        Vector2 segPos = startPos;
        for (int i = 0; i < segmentCount; i++)
        {
            segments.Add(new Segment(segPos));
            segPos.y -= segmentLength;
        }
    }

    public void SetSegment(Vector3 position,float distance)
    {
        segmentCount = Mathf.CeilToInt(distance / segmentLength);
        Vector2 pos = position;
        segments.Clear();
        for (int i = 0; i < segmentCount; i++)
        {
            segments.Add(new Segment(pos));
            pos.y -= segmentLength;
        }
    }

    public void OnHit(Vector3 position, Vector3 hitPosition, float distance)
    {
        this.startPos = position;
        this.endPos = hitPosition;
    }

    public void OnShoot(Vector3 position,Vector3 direction,float distance)
    {
        this.startPos = position;
        this.endPos = position + direction * distance;
    
    }

    public void OnReturn(Vector3 position, Vector3 direction, float distance)
    {
        if(distance <= 0)
        {
            distance = 0.001f;
        }
        this.startPos = position;
        this.endPos = position + direction * distance;
    }

    private void FixedUpdate() {
        UpdateSegments();

        for (int i = 0; i < constraintLoop; i++)
        {
            ApplyConstraint();
            AdjustCollision();
        }
        DrawRope();
    }

    void UpdateSegments()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].velocity = segments[i].pos - segments[i].prevPos;
            segments[i].prevPos = segments[i].pos;
            segments[i].pos.y += gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
            segments[i].pos += segments[i].velocity;
        }
    }

    void ApplyConstraint()
    {
        bool hasEndPoint = (endPos != Vector2.zero) ? true : false;
        segments[0].pos = startPos;
        if(hasEndPoint)
        {
            segments[segments.Count - 1].pos = endPos;
        }
        for (int i = 0; i < segments.Count - 1; i++)
        {
            float dist = (segments[i].pos - segments[i + 1].pos).magnitude;
            float diff = segmentLength - dist;
            Vector2 dir = (segments[i + 1].pos - segments[i].pos).normalized;

            Vector2 movement = dir * diff;
            if(i==0) // 첫번째는 스킵
            {
                segments[i + 1].pos += movement;
            }
            else if(i == segments.Count - 2 && hasEndPoint) // 마지막 번호도 스킵
            {
                segments[i].pos -= movement;
            }
            else // 나머지들은 영향을 주게 둔다
            {
                segments[i].pos -= movement * 0.5f;
                segments[i + 1].pos += movement * 0.5f;
            }
        }
    }

    void DrawRope()
    {
        lr.startWidth = ropeWidth;
        lr.endWidth = ropeWidth;
        Vector3[] segmentPos = new Vector3[segments.Count];
        Vector2[] colPos = new Vector2[segments.Count];
        for (int i = 0; i < segments.Count; i++)
        {
            segmentPos[i] = segments[i].pos;
            colPos[i] = segments[i].pos;
        }
        lr.positionCount = segmentPos.Length;
        lr.SetPositions(segmentPos);

        if(col)
        {
            col.edgeRadius = ropeWidth;
            col.points = colPos;
        }
    }

    void AdjustCollision()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            Vector2 dir = segments[i].pos - segments[i].prevPos;
            RaycastHit2D hit = Physics2D.CircleCast(segments[i].pos, ropeWidth * 0.5f, dir.normalized, 0f,hitLayer);
            if(hit)
            {
                segments[i].pos = hit.point + hit.normal * ropeWidth * 0.5f;
                segments[i].prevPos = segments[i].pos;
            }
        }
    }

    public class Segment
    {
        public Vector2 prevPos;
        public Vector2 pos;
        public Vector2 velocity;

        public Segment(Vector2 pos)
        {
            prevPos = pos;
            this.pos = pos;
            velocity = Vector2.zero;
        }
    }

}
