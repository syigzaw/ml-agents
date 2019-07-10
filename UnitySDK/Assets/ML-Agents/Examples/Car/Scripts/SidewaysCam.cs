using UnityEngine;
using System.Collections;

namespace PathfindingForCars
{
    public class SidewaysCam : MonoBehaviour
    {
        //T move the camera to the car's start position
        public Transform toFollowCar;

        //How fast the camera is moving
        private float camMoveSpeed = 30f;

        //Zoom
        private float currentZoom;
        private float zoomSpeed = 50f;
        private float minZoomDistance = 5f;
        private float maxZoomDistance = 500f;

        //Imagine that the camera is looking at this position
        private Vector3 lookAtThisPos;

        //What's the camera's angle
        private float xAngle = 45f;



        void Start()
        {
            //Init the zoom
            currentZoom = 60f;

            //This is the position we want to look at when the simulation starts
            lookAtThisPos = toFollowCar.position;
        }



        // void LateUpdate()
        // {
        //     //Move the camera to the correct position
        //     Vector3 newCamPosition = lookAtThisPos;

        //     //Move the camera away from the car to the distance we want based on the angle we have
        //     //When you multiply a quaternion and a vector, it is essentially a transformation of the 
        //     //vector according to the rotation represented by the quaternion
        //     newCamPosition += Quaternion.Euler(xAngle, 0f, 0f) * (-Vector3.forward * currentZoom);

        //     transform.position = newCamPosition;

        //     //Make sure the camera looks at whatever we want to look at
        //     transform.LookAt(lookAtThisPos);
        // }



        private void Update()
        {
            //Move camera with keys
            float deltaMove = camMoveSpeed * Time.deltaTime;

            //Move left/right
            if (Input.GetKey(KeyCode.A))
            {
                lookAtThisPos.x -= deltaMove;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                lookAtThisPos.x += deltaMove;
            }

            //Move forward/back
            if (Input.GetKey(KeyCode.S))
            {
                lookAtThisPos.z -= deltaMove;
            }
            else if (Input.GetKey(KeyCode.W))
            {
                lookAtThisPos.z += deltaMove;
            }



            //Zoom with keys
            if (Input.GetAxis("Mouse ScrollWheel") > 0f || Input.GetKey(KeyCode.I))
            {
                currentZoom -= zoomSpeed * Time.deltaTime;
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f || Input.GetKey(KeyCode.O))
            {
                currentZoom += zoomSpeed * Time.deltaTime;
            }

            currentZoom = Mathf.Clamp(currentZoom, minZoomDistance, maxZoomDistance);
        }
    }
}
