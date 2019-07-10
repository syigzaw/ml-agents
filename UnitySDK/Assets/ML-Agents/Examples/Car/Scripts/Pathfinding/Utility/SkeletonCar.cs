using UnityEngine;
using System.Collections;


namespace PathfindingForCars
{
    /// <summary>
    /// Mathematical methods that simulates a car
    /// </summary>
    public static class SkeletonCar
    {
        /// <summary>
        /// //Calculate the new position of the car after driving distance d with steering angle beta
        /// </summary>
        /// <param name="theta">The car's heading (= rotation) [rad]</param>
        /// <param name="beta">Steering angle [rad]</param>
        /// <param name="d">Driving distance</param>
        /// <param name="currentPos">Current position of the car's rear wheels</param>
        /// <returns>The cars's new position</returns>
        public static Vector3 CalculateNewPosition(float theta, float beta, float d, Vector3 currentPos)
        {
            //The coordinate system is not the same as in class "Programming a self-driving car", 
            //so sin and cos are switched

            Vector3 newPos = Vector3.zero;

            //Two different calculations depending on the size of the turning angle beta

            //Move forward
            if (Mathf.Abs(beta) < 0.001f)
            {
                newPos.x = currentPos.x + d * Mathf.Sin(theta);
                newPos.z = currentPos.z + d * Mathf.Cos(theta);
            }
            //Turn
            else
            {
                //Turning radius 
                float R = d / beta;

                float cx = currentPos.x + Mathf.Cos(theta) * R;
                float cz = currentPos.z - Mathf.Sin(theta) * R;

                newPos.x = cx - Mathf.Cos(theta + beta) * R;
                newPos.z = cz + Mathf.Sin(theta + beta) * R;
            }

            return newPos;
        }



        /// <summary>
        /// Calculate the car's new heading
        /// </summary>
        /// <param name="theta">The car's heading (= rotation)</param>
        /// <param name="beta">Steering angle</param>
        /// <returns>The car's new heading</returns>
        public static float CalculateNewHeading(float theta, float beta)
        {
            //Change heading
            theta = theta + beta;

            //Clamp heading
            if (theta > 2f * Mathf.PI)
            {
                theta = theta - 2f * Mathf.PI;
            }
            if (theta < 0f)
            {
                theta = 2f * Mathf.PI + theta;
            }

            return theta;
        }



        /// <summary>
        /// Calculate the car's corner position
        /// </summary>
        /// <param name="carPos">The car's center position</param>
        /// <param name="carHeading">The car's heading [radians]</param>
        /// <param name="carWidth">The width of the car [m]</param>
        /// <param name="carLength">The length of the car [m]</param>
        /// <returns>The car's corner position coordinates (and maybe center position) in an array</returns>
        public static Rectangle GetCornerPositions(Vector3 carPos, float carHeading, float carWidth, float carLength)
        {
            float halfCarWidth = carWidth / 2f;
            float halfCarLength = carLength / 2f;

            //Stuff we can calculate once to save time
            float carLengthSin = halfCarLength * Mathf.Sin(carHeading);
            float carLengthCos = halfCarLength * Mathf.Cos(carHeading);

            float carWidthSinPlusHeading = halfCarWidth * Mathf.Sin((90f * Mathf.Deg2Rad) + carHeading);
            float carWidthCosPlusHeading = halfCarWidth * Mathf.Cos((90f * Mathf.Deg2Rad) + carHeading);

            float carWidthSinMinusHeading = halfCarWidth * Mathf.Sin((90f * Mathf.Deg2Rad) - carHeading);
            float carWidthCosMinusHeading = halfCarWidth * Mathf.Cos((90f * Mathf.Deg2Rad) - carHeading);


            //Front

            //Push the corners to the front
            float xFront = carPos.x + carLengthSin;
            float zFront = carPos.z + carLengthCos;

            //Push to FR
            float xFR = xFront + carWidthSinPlusHeading;
            float zFR = zFront + carWidthCosPlusHeading;

            //Push to FL
            float xFL = xFront - carWidthSinMinusHeading;
            float zFL = zFront + carWidthCosMinusHeading;


            //Back

            //Push the corners to the back
            float xBack = carPos.x - carLengthSin;
            float zBack = carPos.z - carLengthCos;

            //Push to BR
            float xBR = xBack + carWidthSinPlusHeading;
            float zBR = zBack + carWidthCosPlusHeading;

            //Push to BL
            float xBL = xBack - carWidthSinMinusHeading;
            float zBL = zBack + carWidthCosMinusHeading;


            //Add the positions to a rectangle
            Vector3 FL = new Vector3(xFL, 0f, zFL);
            Vector3 FR = new Vector3(xFR, 0f, zFR);
            Vector3 BR = new Vector3(xBR, 0f, zBR);
            Vector3 BL = new Vector3(xBL, 0f, zBL);

            Rectangle cornerPositions = new Rectangle(FL, FR, BL, BR);


            return cornerPositions;
        }



        /// <summary>
        /// Calculate circle positions that approximate the car for obstacle detection
        /// </summary>
        /// <param name="carPos">The center position of the car</param>
        /// <param name="carHeading">The car's heading [radians]</param>
        /// <returns>The coordinates of the circles</returns>
        public static Vector3[] GetCirclePositions(Vector3 carPos, float carHeading, Vector3[] cornerPositions)
        {
            //The distance we move the circle from center to front/rear [m]
            float circlePush = 1.7f;

            //Stuff we can calculate once to save time
            float carLengthSin = circlePush * Mathf.Sin(carHeading);
            float carLengthCos = circlePush * Mathf.Cos(carHeading);

            //Front circle
            float xFront = carPos.x + carLengthSin;
            float zFront = carPos.z + carLengthCos;

            //Rear circle
            float xBack = carPos.x - carLengthSin;
            float zBack = carPos.z - carLengthCos;


            //Add the coordinates to the array
            //The first circle is at the center of the car
            cornerPositions[0] = carPos;
            cornerPositions[1] = new Vector3(xFront, carPos.y, zFront);
            cornerPositions[2] = new Vector3(xBack, carPos.y, zBack);


            return cornerPositions;
        }
    }
}
