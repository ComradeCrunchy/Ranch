using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Poseidon
{
    [ExecuteInEditMode]
    public class PReflectionCamera : MonoBehaviour
    {
        [SerializeField]
        private PWater water;
        public PWater Water
        {
            get
            {
                return water;
            }
            set
            {
                water = value;
            }
        }

        private void Update()
        {
            if (Water == null)
            {
                PUtilities.DestroyGameobject(gameObject);
            }
        }

        public void SetUp(Camera srcCam)
        {
            Plane plane = new Plane(Vector3.up, transform.position);
            Vector3 projPos = plane.ClosestPointOnPlane(srcCam.transform.position);
            Vector3 reflPos = projPos - plane.normal * Vector3.Distance(projPos, srcCam.transform.position);
            transform.position = reflPos;

            Vector3 reflDir = Vector3.Reflect(srcCam.transform.forward, Vector3.up);
            transform.forward = reflDir;
            transform.Rotate(Vector3.forward * 180, Space.Self);
        }
    }
}
