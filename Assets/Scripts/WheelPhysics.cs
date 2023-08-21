using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WheelPhysics : MonoBehaviour
{
    [SerializeField]
    float width;
    [SerializeField]
    float radius;
    [SerializeField]
    LayerMask layerMask;

    Rigidbody rb;
    class WheelHit
    {
        public Vector3 closestPointOnOther;
        public Vector3 closestPointOnWheel;
        public Collider collider;
    }
    List<WheelHit> hits;

    #region getters
    public Vector3 LeftCircleNormal
    {
        get
        {
            return -transform.right;
        }
    }

    public Vector3 RightCircleNormal
    {
        get
        {
            return transform.right;
        }
    }

    public Vector3 COM
    {
        get { return transform.position; }
    }

    public Vector3 LeftCircleCenter
    {
        get
        {
            return COM + LeftCircleNormal * width/2;
        }
    }

    public Vector3 RightCircleCenter
    {
        get
        {
            return COM + RightCircleNormal * width / 2;
        }
    }
    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        hits = new List<WheelHit>();
    }

    private void FixedUpdate()
    {
        hits.Clear();
        Plane leftCirclePlane = new Plane(LeftCircleNormal, LeftCircleCenter);
        Plane rightCirclePlane = new Plane(RightCircleNormal, RightCircleCenter);
        foreach (Collider col in Physics.OverlapCapsule(LeftCircleCenter + LeftCircleNormal * radius, RightCircleCenter + RightCircleNormal * radius, radius))
        {
            Vector3 contactPoint = col.ClosestPointOnBounds(COM);


            if (leftCirclePlane.GetSide(contactPoint) || rightCirclePlane.GetSide(contactPoint))
                continue;

            Vector3 vecFromCOMToContactPoint = contactPoint - COM;
            float disFromCOMToContactPoint = vecFromCOMToContactPoint.magnitude;
            Vector3 dirFromCOMToContactPoint = vecFromCOMToContactPoint / disFromCOMToContactPoint;

            Vector3 closestPointOnWheel = FindPointOnWheel(contactPoint, leftCirclePlane, rightCirclePlane, dirFromCOMToContactPoint);

            WheelHit wheelHit = new WheelHit { collider = col, closestPointOnOther = contactPoint, closestPointOnWheel= closestPointOnWheel };
            hits.Add(wheelHit);

            ApplyPhysics(wheelHit);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = Color.Lerp(Color.green, Color.white, 0.5f);
        Handles.DrawWireDisc(LeftCircleCenter, LeftCircleNormal, radius);
        Handles.DrawWireDisc(RightCircleCenter, RightCircleNormal, radius);
        Handles.DrawLine(LeftCircleCenter, LeftCircleCenter + RightCircleNormal * width);
        if (hits != null)
        {
            foreach (WheelHit hit in hits)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(hit.closestPointOnOther, 0.02f);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(hit.closestPointOnWheel, 0.02f);
            }
        }
    }
#endif

    bool InHemisphere(Vector3 point, Vector3 circleCenter, Vector3 circleNorm)
    {
        Vector3 dirToContactPoint = (point - circleCenter).normalized;
        return Vector3.Dot(dirToContactPoint, circleNorm) > 0;
    }

    Vector3 FindPointOnWheel(Vector3 contactPoint, Plane leftPlane, Plane rightPlane, Vector3 dirCOMToCP)
    {
        float t;
        Ray r = new Ray(COM, dirCOMToCP);
        if (leftPlane.Raycast(r, out t))
        {
            // collides with left plane
            Vector3 pointOnPlane = r.GetPoint(t);
            if (CheckIfPointInCircle(pointOnPlane, LeftCircleCenter)) return pointOnPlane;
        }
        else if(rightPlane.Raycast(r, out t))
        {
            Vector3 pointOnPlane = r.GetPoint(t);
            // collides with right plane
            if (CheckIfPointInCircle(pointOnPlane, RightCircleCenter)) return pointOnPlane;
        }
        return Vector3.zero;
    }

    bool CheckIfPointInCircle(Vector3 pointOnPlane, Vector3 circleCenterOnPlane)
    {
        float sqrDFromCenter = Vector3.Distance(pointOnPlane, circleCenterOnPlane);
        return sqrDFromCenter <= radius * radius;
    }

    void ApplyPhysics(WheelHit hit)
    {


        Vector3 vecToContactPoint = hit.closestPointOnOther - COM;
        //rb.AddForceAtPosition(hit.contactPoint, -vecToContactPoint.normalized * 1e-5f, ForceMode.Impulse);
        if(hit.collider.attachedRigidbody)
        {
            //hit.collider.attachedRigidbody.AddForceAtPosition(hit.contactPoint, vecToContactPoint, ForceMode.VelocityChange);
        }
    }
}
