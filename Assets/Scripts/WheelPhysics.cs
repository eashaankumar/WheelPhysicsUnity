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

    Rigidbody rb;

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

    public Vector3 LeftCircleCenter
    {
        get
        {
            return transform.position + LeftCircleNormal * width/2;
        }
    }

    public Vector3 RightCircleCenter
    {
        get
        {
            return transform.position + RightCircleNormal * width / 2;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        RaycastHit hit;
        //if (Physics.OverlapCapsule() 
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = Color.Lerp(Color.green, Color.white, 0.5f);
        Handles.DrawWireDisc(LeftCircleCenter, LeftCircleNormal, radius);
        Handles.DrawWireDisc(RightCircleCenter, RightCircleNormal, radius);
    }
#endif
}
