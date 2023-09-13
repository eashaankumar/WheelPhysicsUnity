using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
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
    struct WheelHit
    {
        public Vector3 pointInside;
        public Vector3 pointOnSurface;
        public Vector3 normal;
        public Collider collider;
    }
    List<WheelHit> hits;

    #region getters
    public Vector3 LeftCircleNormal
    {
        get
        {
            return -transform.up;
        }
    }

    public Vector3 RightCircleNormal
    {
        get
        {
            return transform.up;
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
        WheelMath wheelMath = new WheelMath(transform.position, transform.rotation);
        foreach (Collider col in Physics.OverlapCapsule(LeftCircleCenter + LeftCircleNormal * radius, RightCircleCenter + RightCircleNormal * radius, radius))
        {
            Vector3 contactPoint = col.ClosestPointOnBounds(COM);

            if (IsInCylinder(in wheelMath, in contactPoint, out Vector3 pointOnWheelWorld, out CylinderCoords clampedCC))
            {
                CylinderCoords curveCC = wheelMath.ClampCylinderToCylinderCurve(clampedCC, radius);
                CylinderCoords diskCC = wheelMath.ClampCylinderToCylinderDisk(clampedCC, radius);

                float3 pointOnWheelCurveWorld = wheelMath.CartesianLocalToWorld(wheelMath.CylindricalPosYToCartesian(curveCC));
                float3 pointOnWheelDiskWorld = wheelMath.CartesianLocalToWorld(wheelMath.CylindricalPosYToCartesian(diskCC));

                hits.Add(new WheelHit
                {
                    pointInside = pointOnWheelWorld,
                    pointOnSurface = pointOnWheelCurveWorld,
                    collider = col,
                });

                hits.Add(new WheelHit
                {
                    pointInside = pointOnWheelWorld,
                    pointOnSurface = pointOnWheelDiskWorld,
                    collider = col,
                });
            }            
        }
    }

    bool IsInCylinder(in WheelMath wheelMath, in Vector3 point, out Vector3 pointOnWheelWorld, out CylinderCoords clampedCC)
    {        
        float3 camPosLocal = wheelMath.CartesianWorldToLocal(point);
        CylinderCoords cc = wheelMath.CartesianToCylindricalPosY(camPosLocal);

        clampedCC = wheelMath.ClampCylindricalPosY(cc, radius, width);
        float3 newCamPosLocal = wheelMath.CylindricalPosYToCartesian(clampedCC);

        pointOnWheelWorld = wheelMath.CartesianLocalToWorld(newCamPosLocal);

        return cc.r <= radius * 2 && math.abs(cc.h_origin) <= width / 2f;
    }

    struct WheelMath
    {
        float3 wheelWorldCenter;
        quaternion wheelWorldRot;
        float4x4 wheelTRS;

        public WheelMath(float3 _wheelWorldCenter, quaternion _wheelWorldRot)
        {
            wheelWorldCenter = _wheelWorldCenter;
            wheelWorldRot = _wheelWorldRot;
            wheelTRS = float4x4.TRS(wheelWorldCenter, wheelWorldRot, new float3(1, 1, 1));
        }

        public float3 CartesianWorldToLocal(float3 worldPoint)
        {
            return math.mul(math.inverse(wheelTRS), new float4(worldPoint, 1)).xyz; // transform.InverseTransformPoint(hit.point);
        }

        public CylinderCoords CartesianToCylindricalPosY(float3 cartesian)
        {
            return CylinderCoords.FromCartesian(cartesian.x, cartesian.z, cartesian.y);
        }

        public CylinderCoords ClampCylindricalPosY(CylinderCoords cc, float cylinderR, float cylinderW)
        {
            float clampR = math.clamp(cc.r, 0, cylinderR);  
            float clampHO = math.clamp(cc.h_origin, -cylinderW / 2, +cylinderW / 2);
            return new CylinderCoords { r=clampR, theta=cc.theta, h_origin=clampHO };
        }

        public float3 CylindricalPosYToCartesian(CylinderCoords cc)
        {
            return CylinderCoords.ToCartesian(cc.r, cc.theta, cc.h_origin).xzy;
        }

        public float3 CartesianLocalToWorld(float3 localPoint)
        {
            return math.mul(wheelTRS, new float4(localPoint, 1)).xyz;
        }

        public CylinderCoords ClampCylinderToCylinderCurve(CylinderCoords toClamp, float clampingR)
        {
            return new CylinderCoords { h_origin = toClamp.h_origin, r = clampingR, theta = toClamp.theta };
        }

        public CylinderCoords ClampCylinderToCylinderDisk(CylinderCoords toClamp, float clampingW)
        {
            int sign = 1;
            if (toClamp.h_origin < 0)
                sign = -1;
            return new CylinderCoords { h_origin = sign * clampingW / 2f, r = toClamp.r, theta = toClamp.theta };
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
                Gizmos.DrawSphere(hit.pointOnSurface, 0.02f);
                Gizmos.DrawLine(hit.pointInside, hit.pointOnSurface);
            }
        }

        /*Gizmos.color = Handles.color;
        Gizmos.DrawWireSphere(LeftCircleCenter + LeftCircleNormal * radius, radius);
        Gizmos.DrawWireSphere(RightCircleCenter + RightCircleNormal * radius, radius);*/
    }
#endif

}
public struct CylinderCoords
{
    public float r;
    public float theta;
    public float h_origin;

    public static float3 ToCartesian(float _r, float _theta, float _h_origin)
    {
        return new float3(_r * math.cos(_theta), _r * math.sin(_theta), _h_origin);
    }

    public static CylinderCoords FromCartesian(float discA, float discB, float h_origin)
    {
        return new CylinderCoords
        {
            r = math.sqrt(discA * discA + discB * discB),
            theta = math.atan2(discB, discA),
            h_origin = h_origin,
        };
    }
}