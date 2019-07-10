using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace PathfindingForCars
{
    //Smooths a path given in a list of nodes
    public class SmoothPathController
    {
        SmoothPathMethods smoothingMethods = new SmoothPathMethods();



        //Transfrom a non-smooth path to a smooth path
        public List<Node> GetSmoothPath(List<Node> finalPath, bool isCircular)
        {
            List<Node> smoothPath = new List<Node>();
            //Store the nodes than cant be moved here
            List<bool> isNodeFixed = new List<bool>();

            //Modify the waypoints by removing some of them to get a path with a better shape
            ModifyWaypoints(finalPath, smoothPath, isNodeFixed);

            //Smooth parameters
            float alpha = 0.2f;
            float beta = 0.1f;
            //Push away from obstacles parameter
            float gamma = 0.2f;

            //bool optimizeDistanceToObstacles = true;

            smoothingMethods.SmoothPathGradientDescent(smoothPath, isNodeFixed, isCircular, alpha, beta, gamma, false);

            //But since we have removed waypoints, this path will be kinda squary, 
            //so we need to add waypoints and smooth it again
            AddWayPoints(smoothPath, isNodeFixed);

            //Then we need to smooth it again
            smoothingMethods.SmoothPathGradientDescent(smoothPath, isNodeFixed, isCircular, alpha, beta, gamma, true);

            //If we failed to smooth the path, then return the unsmooth path because gradient descent can sometimes be unstable
            if (smoothPath.Count > 0)
            {
                return smoothPath;
            }
            else
            {
                //Debug.Log("Couldnt smooth path");

                return finalPath;
            }
        }



        //To smooth the path we need to remove some waypoints and we also need to know which nodes are fixed and shouldnt be smoothed
        private void ModifyWaypoints(List<Node> finalPath, List<Node> smoothPath, List<bool> isNodeFixed)
        {
            //Step 1
            //Find the points where we change direction from forward -> reverse or the opposite and fix them
            for (int i = 0; i < finalPath.Count; i++)
            {
                //Need to clone the nodes or they will be the same as in the fixedPath because of objects
                Node clonedNode = new Node();

                clonedNode.carPos = finalPath[i].carPos;
                clonedNode.isReversing = finalPath[i].isReversing;

                //Always add the first and the last wp
                if (i == 0 || i == finalPath.Count - 1)
                {
                    smoothPath.Add(clonedNode);
                    isNodeFixed.Add(true);
                }
                //Always add the wp where we are going from forward -> reverse or the opposite
                else if (finalPath[i].isReversing != finalPath[i + 1].isReversing)
                {
                    smoothPath.Add(clonedNode);
                    isNodeFixed.Add(true);
                }
                else
                {
                    smoothPath.Add(clonedNode);
                    isNodeFixed.Add(false);
                }
            }

            //Step 2
            //Fix the waypoints before a change in direction of the car will not smoothly move to that waypoint
            List<bool> isNodeFixedTemp = new List<bool>();

            //Need to init this list 
            for (int i = 0; i < finalPath.Count; i++)
            {
                isNodeFixedTemp.Add(false);
            }

            //First and last node is always fixed because we are ignoring them in the loop
            isNodeFixedTemp[0] = true;
            isNodeFixedTemp[finalPath.Count - 1] = true;

            for (int i = 1; i < finalPath.Count - 1; i++)
            {
                if (isNodeFixed[i - 1] || isNodeFixed[i + 1] || isNodeFixed[i])
                {
                    isNodeFixedTemp[i] = true;
                }
                else
                {
                    isNodeFixedTemp[i] = false;
                }
            }

            //Add the new fixed nodes
            isNodeFixed.Clear();

            isNodeFixed.AddRange(isNodeFixedTemp);

            //Step 3
            //Remove nodes to get a smoother path
            for (int i = finalPath.Count - 1; i >= 0; i--)
            {
                if (i % 2 == 0 && !isNodeFixed[i])
                {
                    isNodeFixed.RemoveAt(i);
                    smoothPath.RemoveAt(i);
                }
            }
        }



        //Add waypoints to the smooth path
        private void AddWayPoints(List<Node> smoothPath, List<bool> isNodeFixed)
        {        
            List<Node> tempSmoothPath = new List<Node>();
            List<bool> tempIsNodeFixed = new List<bool>();


            for (int i = 0; i < smoothPath.Count; i++)
            {
                if (i % 2 == 0)
                {
                    tempSmoothPath.Add(smoothPath[i]);

                    if (isNodeFixed[i])
                    {
                        tempIsNodeFixed.Add(true);
                    }
                    else
                    {
                        tempIsNodeFixed.Add(false);
                    }
                }
                else
                {
                    Node newNode = new Node();

                    newNode.carPos = (smoothPath[i].carPos + smoothPath[i - 1].carPos) * 0.5f;
                    newNode.isReversing = smoothPath[i].isReversing;

                    tempSmoothPath.Add(newNode);
                    tempSmoothPath.Add(smoothPath[i]);

                    tempIsNodeFixed.Add(false);

                    if (isNodeFixed[i])
                    {
                        tempIsNodeFixed.Add(true);
                    }
                    else
                    {
                        tempIsNodeFixed.Add(false);
                    }
                }
            }

            smoothPath.Clear();

            smoothPath.AddRange(tempSmoothPath);

            isNodeFixed.Clear();

            isNodeFixed.AddRange(tempIsNodeFixed);
        }
    }
}
