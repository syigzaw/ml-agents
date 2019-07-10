using UnityEngine;
using System.Collections;

namespace PathfindingForCars
{
    //Creates a skeleton car to test the math needed for the Hybrid A*
    public class SkeletonCarTest : MonoBehaviour
    {
        //Drags
        public GameObject cornerFL;
        public GameObject cornerFR;
        public GameObject cornerBL;
        public GameObject cornerBR;

        public GameObject[] circleOBj;


        //Holds the positions of the car's 4 corners
        Vector3[] cornerPos;
        //Hold the positions of the car's 3 circles that approximates its area
        Vector3[] circlePositions;



        void Start()
        {
            this.cornerPos = new Vector3[4];

            //Add init values to the corners
            for (int i = 0; i < cornerPos.Length; i++)
            {
                cornerPos[i] = Vector3.zero;
            }

            this.circlePositions = new Vector3[3];

            //Add init values to the corners
            for (int i = 0; i < circlePositions.Length; i++)
            {
                circlePositions[i] = Vector3.zero;
                //All obstacles are at position 0.5
                circlePositions[i].y = 0.5f;
            }
        }



        void Update()
        {
            TestDrive();
        }



        void TestDrive()
        {
            //Distance between the wheels (= wheelbase)
            float L = 2.959f;
            //Driving distance each update
            float d = 10f;

            //Steering angle in radians
            float alpha = 20f * Mathf.Deg2Rad;
            //Heading direction in radians
            float theta = transform.eulerAngles.y * Mathf.Deg2Rad;

            //Manual control
            d = 0.1f * Input.GetAxis("Vertical");
            alpha *= Input.GetAxis("Horizontal");

            //Turning angle
            float beta = (d / L) * Mathf.Tan(alpha);


            //Get the new position
            Vector3 newPos = SkeletonCar.CalculateNewPosition(theta, beta, d, transform.position);

            //Get the new heading
            float newHeading = SkeletonCar.CalculateNewHeading(theta, beta);


            //Add the new position to the car
            transform.position = newPos;

            //Add the new heading to the car
            Vector3 currentRot = transform.rotation.eulerAngles;

            Vector3 newRotation = new Vector3(currentRot.x, newHeading * Mathf.Rad2Deg, currentRot.z);

            transform.rotation = Quaternion.Euler(newRotation);


            UpdateCorners(newPos, newHeading);

            UpdateCircles(newPos, newHeading);
        }



        //Update the corners to see if we can identify the coordinates from geometry
        void UpdateCorners(Vector3 carPos, float carHeading)
        {
            float carWidth = 0.95f * 2f;
            float carLength = 2.44f * 2f;

            Rectangle cornerPos = SkeletonCar.GetCornerPositions(carPos, carHeading, carWidth, carLength);

            //Add the coordinates to the spheres
            AddCoordinates(cornerFR, cornerPos.FR.x, cornerPos.FR.z);
            AddCoordinates(cornerFL, cornerPos.FL.x, cornerPos.FL.z);
            AddCoordinates(cornerBR, cornerPos.BR.x, cornerPos.BR.z);
            AddCoordinates(cornerBL, cornerPos.BL.x, cornerPos.BL.z);
        }



        //Update the circles to see if we can identify the coordinates from geometry
        void UpdateCircles(Vector3 carPos, float carHeading)
        {
            circlePositions = SkeletonCar.GetCirclePositions(carPos, carHeading, circlePositions);

            for (int i = 0; i < circleOBj.Length; i++)
            {
                AddCoordinates(circleOBj[i], circlePositions[i].x, circlePositions[i].z);
            }
        }



        //Add new coordinates to a gameobject
        void AddCoordinates(GameObject gameObj, float newX, float newZ)
        {
            Vector3 newPos = gameObj.transform.position;

            newPos.x = newX;
            newPos.z = newZ;

            gameObj.transform.position = newPos;
        }
    }
}
