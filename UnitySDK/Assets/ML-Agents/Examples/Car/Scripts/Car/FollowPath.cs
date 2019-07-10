using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PathfindingForCars
{
    //Will make the car follow the given path
    public class FollowPath : MonoBehaviour
    {
        //For debugging to test how far we have become between two waypoints
        public Transform progressTestObj;

        //To slow down when we reach the end of a path
        public AnimationCurve slowDownCurve;

        //This is the waypoint the car is heading towards
        private int currentWayPointIndex = 1;

        //The waypoints the car will follow
        private List<Node> wayPoints;

        //The script that controls if the car is driving forward/reverse/stop
        private CarController carScript;

        //The script with car data
        private CarData carData;

        //Is the path circular?
        private bool isCircular = false;



        void Start()
        {
            carScript = GetComponent<CarController>();

            carData = GetComponent<CarData>();
        }



        void Update()
        {
            //Do we have enough waypoints?
            if (wayPoints != null && wayPoints.Count > 1)
            {
                UpdateCarMovement();

                ChangeWaypoint();

                //Display the waypoint we are heading towards
                //Vector3 waypointPos = GetWayPointPos(currentWayPointIndex);

                //Debug.DrawRay(waypointPos, Vector3.up * 5f, Color.red);
            }
            //If not, then stop the car
            else
            {
                carScript.StopCar();

                //print("Stop car");
            }
        }



        //Change if the car should drive forward, reverse, or brake
        void UpdateCarMovement()
        {
            if (wayPoints[currentWayPointIndex].isReversing)
            {
                carScript.MoveCarReverse();
            }
            else
            {
                carScript.MoveCarForward();
            }

            //Change the car's speed to slow down when curvy path or close to end of path
            //ChangeSpeed();
        }



        //Change the car's speed to slow down when curvy path or close to end of path 
        void ChangeSpeed()
        {
            //Slow down if we reach an endpoint or a if we are about to change driving direction
            bool isGoingTooFast = false;

            //The car's current speed
            float carSpeed = carScript.GetCarSpeed();


            //Change 1
            //If we are driving forward, then change speed depending on angle of the path we are following
            if (!wayPoints[currentWayPointIndex].isReversing)
            {
                //Change speed depending on the angle to the next waypoint
                int lookAheadIndex = currentWayPointIndex + 4;

                //As we get closer to the end we cant look ahead too many waypoints
                lookAheadIndex = Mathf.Clamp(lookAheadIndex, 2, wayPoints.Count - 1);

                Vector3 P2 = GetWayPointPos(lookAheadIndex);

                Vector3 P1 = GetWayPointPos(currentWayPointIndex);

                Vector3 carPos = carScript.transform.position;

                //Angle is 0 degrees if P2 is infront of P1 in the direction of the car
                float angle = Vector3.Angle(P1 - carPos, P2 - carPos);

                //print(angle);

                if (angle > 10f && carSpeed > 15f)
                {
                    isGoingTooFast = true;
                }
            }



            //Change 2
            //Slow down the car as it's getting close to a change, like forward -> reverse
            bool closeToChange = false;
            bool currentDrivingDirection = wayPoints[currentWayPointIndex].isReversing;
            bool isCloseToEnd = false;

            //The number of nodes until change
            int counter = 0;

            //The max number of nodes until we care about a change
            int maxNodesUntilChange = 8;

            for (int i = currentWayPointIndex + 1; i < wayPoints.Count - 0; i++)
            {
                if (counter > maxNodesUntilChange)
                {
                    break;
                }

                if (wayPoints[i].isReversing != currentDrivingDirection)
                {
                    closeToChange = true;

                    //print("Close to change in driving direction");

                    break;
                }

                //Last waypoint
                if (i == wayPoints.Count - 1)
                {
                    closeToChange = true;

                    isCloseToEnd = true;

                    //print("Close to end of path");
                }

                counter += 1;
            }

            //print(counter);


            //Now we need to slow down to something close to 0
            if (closeToChange)
            {
                //The easiest way is to use an animation curve
                float percentage = (float)counter / (float)maxNodesUntilChange;

                //Has to be different or the car will drive very slowly when changing direction
                float minCarSpeed = 5f;

                if (isCloseToEnd)
                {
                    minCarSpeed = 1f;
                }

                float maxCarSpeed = minCarSpeed + (slowDownCurve.Evaluate(percentage) * 20f);

                //print(maxCarSpeed);

                if (carSpeed > maxCarSpeed)
                {
                    isGoingTooFast = true;
                }
            }



            //Change 3
            //Slow down the car if it is deviting too much from the path and heading
            float CTE = Mathf.Abs(CalculateCTE());

            float wantedHeading = wayPoints[currentWayPointIndex].heading * Mathf.Rad2Deg;

            //This is already in degrees
            float currentHeading = carData.GetHeading();

            float headingDiff = Mathf.Abs(wantedHeading - currentHeading);

            if (CTE > 0.2f && headingDiff > 5f)
            {
                isGoingTooFast = true;
            }



            //Stop the car if its going too fast
            if (isGoingTooFast && carScript.GetCarSpeed() > 5f)
            {
                carScript.StopCar();

                //print("Stop car");
            }
            else
            {
                //print("Drive car");
            }
        }



        //Change waypoint if we have passed the waypoint we were heading towards
        void ChangeWaypoint()
        {
            //If we have reached the waypoint we are aiming for
            if (CalculateProgress() > 1f)
            {
                currentWayPointIndex += 1;

                //Clamp when we have reached the last waypoint so we start all over again
                if (currentWayPointIndex > wayPoints.Count - 1 && isCircular)
                {
                    currentWayPointIndex = 0;
                }
                //The path in not circular so we should stop following it
                else if (currentWayPointIndex > wayPoints.Count - 1)
                {
                    wayPoints = null;

                    //Stop the car when we have reached the end of the path
                    carScript.StopCar();
                }

                //Debug.Log(currentWayPointIndex);
            }
        }



        //Calculate cross track error CTE
        //From https://www.udacity.com/course/viewer#!/c-cs373/l-48696626/e-48403941/m-48716166
        public float CalculateCTE()
        {
            //Calculate a different center of the car depending of if reversing or not
            Vector3 carPos = CalculateCarPos();

            Vector3 P2 = GetWayPointPos(currentWayPointIndex);
            Vector3 P1 = GetWayPointPos(currentWayPointIndex - 1);

            //If we are driving forward, we need use the next waypoint and not this waypoint or the car will not follow the path accurately
            if (currentWayPointIndex < wayPoints.Count - 2)
            {
                //Unless we are chaning direction in the next waypoint
                if (!wayPoints[currentWayPointIndex + 1].isReversing)
                {
                    P2 = GetWayPointPos(currentWayPointIndex + 1);
                }
            }

            float Rx = carPos.x - P1.x;
            float Rz = carPos.z - P1.z;

            float deltaX = P2.x - P1.x;
            float deltaZ = P2.z - P1.z;

            //Version from the class but is not giving the correct result
            //float CTE = ((Rz * deltaX) - (Rx * deltaZ)) / ((deltaX * deltaX) + (deltaZ * deltaZ));

            //Vector2 CTE_vec = new Vector2((Rx * deltaZ * deltaZ) - (Rz * deltaX * deltaZ), (Rz * deltaX * deltaX) - (Rx * deltaX * deltaZ));

            //CTE_vec /= ((deltaX * deltaX) + (deltaZ * deltaZ));

            //float CTE = CTE_vec.magnitude;

            //How far have we come in this section
            float progress = ((Rx * deltaX) + (Rz * deltaZ)) / ((deltaX * deltaX) + (deltaZ * deltaZ));

            Vector3 progressCoordinate = ((P2 - P1) * progress) + P1;

            //Display a box at the coordinate of the progress
            //progressTestObj.position = progressCoordinate;

            float CTE = (carPos - progressCoordinate).magnitude;

            //Debug.Log(CTE);

            //Is the car to the right or to the left of the upcoming waypoint
            //http://forum.unity3d.com/threads/left-right-test-function.31420/
            //You can also use the incorrect version of the CTE, which gives the correct sign
            //and multiply that sign with the CTE
            Vector3 toCarVec = carPos - P1;
            Vector3 toWaypointVec = P2 - P1;

            Vector3 perp = Vector3.Cross(toCarVec, toWaypointVec);
            float dir = Vector3.Dot(perp, Vector3.up);

            //The car is right of the waypoint
            if (dir > 0f)
            {
                CTE *= -1f;
            }

            return CTE;
        }



        //Calculate how far we have progressed on the segment from the waypoint to the waypoint we are heading towards
        //From https://www.udacity.com/course/viewer#!/c-cs373/l-48696626/e-48403941/m-48716166
        //Returns > 1 if we have passed the waypoint
        float CalculateProgress()
        {
            //Should always be measured from the rear wheels because that's what is used when generating the path
            Vector3 carPos = carData.GetRearWheelPos();

            Vector3 P2 = GetWayPointPos(currentWayPointIndex);
            Vector3 P1 = GetWayPointPos(currentWayPointIndex - 1);

            float Rx = carPos.x - P1.x;
            float Rz = carPos.z - P1.z;

            float deltaX = P2.x - P1.x;
            float deltaZ = P2.z - P1.z;

            //If progress is > 1 then the car has passed the waypoint
            float progress = ((Rx * deltaX) + (Rz * deltaZ)) / ((deltaX * deltaX) + (deltaZ * deltaZ));

            //Debug.Log(progress);

            return progress;
        }



        //Calculate a different center of the car depending of if reversing or not to make it follow the path better
        Vector3 CalculateCarPos()
        {
            Vector3 carPos = transform.position;

            //When reversing we should measure the CTE from the rear wheels or it will not follow the path accurately
            if (wayPoints[currentWayPointIndex].isReversing)
            {
                carPos = carData.GetRearWheelPos();
            }

            return carPos;
        }



        //Get a clamped waypoint from a list of waypoints
        public Vector3 GetWayPointPos(int index)
        {
            Vector3 waypointPos = Vector3.zero;

            //Debug.Log(index);

            //Clamp
            if (index > wayPoints.Count - 1)
            {
                waypointPos = wayPoints[0].carPos;
            }
            else if (index < 0)
            {
                waypointPos = wayPoints[wayPoints.Count - 1].carPos;
            }
            else
            {
                waypointPos = wayPoints[index].carPos;
            }

            return waypointPos;
        }



        //
        // Set methods
        //

        //Set a new path
        public void SetPath(List<Node> wayPoints)
        {
            this.wayPoints = wayPoints;

            //Restart the path
            //Always begin at 1 because we need the 0 to calculate if we have passed the waypoint
            currentWayPointIndex = 1;
        }
    }
}
