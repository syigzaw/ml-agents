using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathfindingForCars
{
    //Methods that smooth a path
    public class SmoothPathMethods
    {
        //Smooth a path with gradient descent
        //https://www.youtube.com/watch?v=umAeJ7LMCfU
        public void SmoothPathGradientDescent(List<Node> path, List<bool> isNodeFixed, bool isCircular, float alpha, float beta, float gamma, bool optimizeObstacles)
        {
            //The list with new coordinates
            List<Vector3> tempPath = new List<Vector3>();

            //Add the old positions
            for (int i = 0; i < path.Count; i++)
            {
                tempPath.Add(path[i].carPos);
            }

            float tolerance = 0.001f; //Was 0.000001f in lecture but is too slow here but gives the same result

            float totalChange = 1000f;

            int iterations = 0;

            while (totalChange >= tolerance)
            {
                if (iterations > 100)
                {
                    break;
                }

                iterations += 1;

                totalChange = 0f;

                for (int i = 0; i < path.Count; i++)
                {
                    //Ignore the first and last point if this is not a circular path
                    //if (!isCircular && (i == 0 || i == wayPoints.Count - 1))
                    //{
                    //    continue;
                    //}

                    if (!isCircular && isNodeFixed[i])
                    {
                        continue;
                    }

                    //Clamp when we reach end and beginning of list
                    int i_plus_one = i + 1;

                    if (i_plus_one > path.Count - 1)
                    {
                        i_plus_one = 0;
                    }

                    int i_minus_one = i - 1;

                    if (i_minus_one < 0)
                    {
                        i_minus_one = path.Count - 1;
                    }

                    //Smooth!
                    Vector3 tmp = tempPath[i] + alpha * (path[i].carPos - tempPath[i]);

                    tmp += beta * (tempPath[i_plus_one] + tempPath[i_minus_one] - 2f * tempPath[i]);

                    /*      (MATT)
                    //Maximize the distance to the obstacles
                    if (optimizeObstacles)
                    {
                        IntVector2 cellPos = PathfindingController.ConvertCoordinateToCellPos(tempPath[i]);

                        if (ObstaclesController.distanceToClosestObstacle[cellPos.x, cellPos.z] <= 2)
                        {
                            Vector3 closestObstaclePos = ObstaclesDetection.FindAverageObstaclePosition(tempPath[i], 2f);

                            //2d space
                            closestObstaclePos.y = 0f;

                            tmp += gamma * (tempPath[i] - closestObstaclePos);
                        }
                        //If we are 2 steps away from obstacle, then we dont need to push as much
                        else if (ObstaclesController.distanceToClosestObstacle[cellPos.x, cellPos.z] <= 3)
                        {
                            float gammaMod = gamma * 0.5f;

                            Vector3 closestObstaclePos = ObstaclesDetection.FindAverageObstaclePosition(tempPath[i], 3f);

                            //2d space
                            closestObstaclePos.y = 0f;

                            tmp += gammaMod * (tempPath[i] - closestObstaclePos);
                        }
                    }
                    */

                    totalChange += Mathf.Abs((tmp - tempPath[i]).magnitude);

                    tempPath[i] = tmp;
                }
            }

            //Add the new smooth positions to the original waypoints
            for (int i = 0; i < path.Count; i++)
            {
                path[i].carPos = tempPath[i];
            }
        }
    }
}
