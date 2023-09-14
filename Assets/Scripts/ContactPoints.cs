using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class ContactPoints : MonoBehaviour
{
    [SerializeField]
    CapsuleCollider capsule;
    [SerializeField]
    BoxCollider box;

    struct CapsuleData
    {
        public float height, radius;
    }

    struct BoxData
    {
        public float3 size;
    }

    Dictionary<int, CapsuleData> capsules = new Dictionary<int, CapsuleData>();
    Dictionary<int, CapsuleData> boxes = new Dictionary<int, CapsuleData>();


    // Start is called before the first frame update
    void Start()
    {
        capsule.hasModifiableContacts = true;
        box.hasModifiableContacts = true;
        capsule.providesContacts = true;
        box.providesContacts = true;

        CapsuleData data = new CapsuleData { height = capsule.height, radius = capsule.radius };
        capsules.Add(capsule.GetInstanceID(), data); 
        boxes.Add(box.GetInstanceID(), data);

        Physics.ContactModifyEvent += OnContactModifyEvent;
    }

    private void OnContactModifyEvent(PhysicsScene scene, NativeArray<ModifiableContactPair> contactPairs)
    {
        print("Contact! " + contactPairs.Length);
        for(int i = 0; i < contactPairs.Length; i++)
        {
            ModifiableContactPair contactPair = contactPairs[i];
            int id1 = contactPair.colliderInstanceID;
            int id2 = contactPair.otherColliderInstanceID;
            if (capsules.ContainsKey(id1) && !boxes.ContainsKey(id2))
            {
                HandleCollisionCapsule(contactPair.position, contactPair.rotation, contactPair, capsules[id1]);
            }
            else if (boxes.ContainsKey(id1) && !capsules.ContainsKey(id2))
            {
                print("Box 1");
                HandleCollisionBox(contactPair.position, contactPair.rotation, contactPair, boxes[id1]);
            }
            
            if (capsules.ContainsKey(id2) && !boxes.ContainsKey(id1))
            {
                HandleCollisionCapsule(contactPair.otherPosition, contactPair.otherRotation, contactPair, capsules[id2]);
            }
            else if (boxes.ContainsKey(id2) && !capsules.ContainsKey(id1))
            {
                print("Box 2");
                HandleCollisionBox(contactPair.otherPosition, contactPair.otherRotation, contactPair, boxes[id2]);
            }
        }
    }

    void HandleCollisionCapsule(float3 pos, quaternion rotation, ModifiableContactPair pair, CapsuleData data)
    {
        WheelMath wmath = new WheelMath(pos, rotation);
        for(int i = 0; i < pair.contactCount; i++)
        {
            float3 worldContactPoint = pair.GetPoint(i);
            float3 localContactPoint = wmath.CartesianWorldToLocal(worldContactPoint);
            if (math.abs(localContactPoint.x) >= data.height/2 - data.radius)
                pair.IgnoreContact(i);
        }
    }

    void HandleCollisionBox(float3 pos, quaternion rotation, ModifiableContactPair pair, CapsuleData data)
    {
        WheelMath wmath = new WheelMath(pos, rotation);
        for (int i = 0; i < pair.contactCount; i++)
        {
            float3 worldContactPoint = pair.GetPoint(i);
            float3 localContactPoint = wmath.CartesianWorldToLocal(worldContactPoint);
            //print(localContactPoint);
            // x y z no
            // x z y no
            // y x z no
            // y z x change, but no
            // z x y no
            // z y x change, but no
            CylinderCoords coords = CylinderCoords.FromCartesian(localContactPoint.z, localContactPoint.y, localContactPoint.x);
            //if (coords.r <= data.radius)
            //    print(coords.r);
            if (coords.r > data.radius)
                pair.IgnoreContact(i);
        }
    }
}
