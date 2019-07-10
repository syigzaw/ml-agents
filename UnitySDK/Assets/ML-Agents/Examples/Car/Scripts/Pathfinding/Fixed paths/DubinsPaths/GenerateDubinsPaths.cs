using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathfindingForCars;

namespace FixedPathAlgorithms
{
    //Generates 1 or as many as possible Dubin paths
    public class GenerateDubinsPaths
    {
        //The start object we want to drive from
        //We cant use the car because if we add this to Hybrid A* then we have to use the 
        //position and rotation of the car in a future position and not the car's start position
        Vector3 startPos;
        float startHeading;
        //The goal object we want to drive to, which is always the same
        Transform goalObj;

        //The 4 different circles we have that sits to the left/right of the start/goal
        public Vector3 startLeftCircle;
        public Vector3 startRightCircle;
        public Vector3 goalLeftCircle;
        public Vector3 goalRightCircle;

        //Where we store all path data so we can sort and find the shortest path
        List<OneDubinsPath> pathDataList = new List<OneDubinsPath>();
        //And when we reverse
        List<OneDubinsPath> pathDataListReverse = new List<OneDubinsPath>();

        //The object with all math
        DubinsPathEquations dubinsMath;


        public GenerateDubinsPaths(Transform goalObj, float turningRadius)
        {
            //These are always the same no matter what
            this.goalObj = goalObj;

            dubinsMath = new DubinsPathEquations(turningRadius);
        }



        //Reset before we can generate a path/paths
        void Reset(Vector3 startCarPosition, float startCarHeading)
        {
            this.startPos = startCarPosition;

            this.startHeading = startCarHeading;

            //First we need to find the positions of the left/right circles of the car/target
            PositionLeftRightCircles();
        }



        //Position the left and right circles that are to the left/right of the target and the car
        void PositionLeftRightCircles()
        {
            //Goal pos
            Vector3 goalPos = goalObj.position;

            float goalHeading = goalObj.eulerAngles.y;

            goalRightCircle = dubinsMath.GetRightCircleCenterPos(goalPos, goalHeading);

            goalLeftCircle = dubinsMath.GetLeftCircleCenterPos(goalPos, goalHeading);


            //Start pos
            startRightCircle = dubinsMath.GetRightCircleCenterPos(startPos, startHeading);

            startLeftCircle = dubinsMath.GetLeftCircleCenterPos(startPos, startHeading);
        }



        //
        // Get path/paths forward
        //

        //Returns the shortest of the dubin paths with waypoints from start to finish
        public OneDubinsPath GetShortestDubinPath(Vector3 startCarPosition, float startCarHeading)
        {
            Reset(startCarPosition, startCarHeading);

            //Reset the list with all paths
            pathDataList.Clear();

            //Find the length of each path
            CalculatePathLengths(false);

            //If we have paths
            if (pathDataList.Count > 0)
            {
                //Sort the list with paths
                pathDataList.Sort((x, y) => x.totalLength.CompareTo(y.totalLength));

                //Generate the shortest path
                GetTotalPath(pathDataList[0], false);

                return pathDataList[0];
            }

            return null;
        }



        //Generate all valid Dubin paths
        public List<OneDubinsPath> GetAllDubinPaths(Vector3 startCarPosition, float startCarHeading)
        {
            Reset(startCarPosition, startCarHeading);

            //Reset the list with all paths
            pathDataList.Clear();

            //Find the length of each path
            CalculatePathLengths(false);

            //If we have paths
            if (pathDataList.Count > 0)
            {
                //Sort the list with paths
                pathDataList.Sort((x, y) => x.totalLength.CompareTo(y.totalLength));

                //Generate the final paths
                GeneratePaths(pathDataList, false);

                return pathDataList;
            }

            return null;
        }



        //
        // Get path/paths reverse
        //

        //Returns the shortest of the dubin paths with waypoints from start to finish
        public OneDubinsPath GetShortestDubinPathReverse(Vector3 startCarPosition, float startCarHeading)
        {
            Reset(startCarPosition, startCarHeading);

            //Reset the list with all paths
            pathDataListReverse.Clear();

            //Find the length of each path
            CalculatePathLengths(true);

            //If we have paths
            if (pathDataListReverse.Count > 0)
            {
                //Sort the list with paths
                pathDataListReverse.Sort((x, y) => x.totalLength.CompareTo(y.totalLength));

                //Generate the shortest path
                GetTotalPath(pathDataListReverse[0], true);

                return pathDataListReverse[0];
            }

            return null;
        }



        //Generate all valid Dubins paths if we reverse
        public List<OneDubinsPath> GetAllDubinPathsReverse(Vector3 startCarPosition, float startCarHeading)
        {
            Reset(startCarPosition, startCarHeading);

            //Reset the list with all paths
            pathDataListReverse.Clear();

            //Find the length of each path
            CalculatePathLengths(true);

            //If we have paths
            if (pathDataListReverse.Count > 0)
            {
                //Sort the list with paths
                pathDataListReverse.Sort((x, y) => x.totalLength.CompareTo(y.totalLength));

                //Generate the final paths
                GeneratePaths(pathDataListReverse, true);

                return pathDataListReverse;
            }

            return null;
        }



        //
        // Calculate the path lengths of all paths
        //

        //Calculate the path lengths of all paths by using tangent points
        //Store the paths in a list together with other useful data
        void CalculatePathLengths(bool isReversing)
        {
            //
            // RSR and LSL is only working if the circles don't have the same position
            //
            //RSR
            if (!startRightCircle.Equals(goalRightCircle))
            {
                if (!isReversing)
                {
                    Get_RSR_Length(startRightCircle, goalRightCircle, pathDataList);
                }
                else
                {
                    Get_LSL_Length(startRightCircle, goalRightCircle, pathDataListReverse);
                }
            }
            //LSL
            if (!startLeftCircle.Equals(goalLeftCircle))
            {
                if (!isReversing)
                {
                    Get_LSL_Length(startLeftCircle, goalLeftCircle, pathDataList);
                }
                else
                {
                    Get_RSR_Length(startLeftCircle, goalLeftCircle, pathDataListReverse);
                }
            }


            //
            // RSL and LSR is only working of the circles don't intersect
            //
            float comparisonSqr = dubinsMath.TurningRadius * 2f * dubinsMath.TurningRadius * 2f;

            //RSL
            float distanceRSLSqr = (startRightCircle - goalLeftCircle).sqrMagnitude;

            if (distanceRSLSqr > comparisonSqr)
            {
                if (!isReversing)
                {
                    Get_RSL_Length(startRightCircle, goalLeftCircle, pathDataList);
                }
                else
                {
                    Get_LSR_Length(startRightCircle, goalLeftCircle, pathDataListReverse);
                }

            }

            //LSR
            float distanceLSRSqr = (startLeftCircle - goalRightCircle).sqrMagnitude;

            if (distanceLSRSqr > comparisonSqr)
            {
                if (!isReversing)
                {
                    Get_LSR_Length(startLeftCircle, goalRightCircle, pathDataList);
                }
                else
                {
                    Get_RSL_Length(startLeftCircle, goalRightCircle, pathDataListReverse);
                }

            }


            //
            // With the 2 CCC paths, the distance between the start and goal have to be less than 4 * r
            //
            comparisonSqr = 4f * dubinsMath.TurningRadius * 4f * dubinsMath.TurningRadius;

            //RLR        
            if ((startRightCircle - goalRightCircle).sqrMagnitude < comparisonSqr)
            {
                if (!isReversing)
                {
                    Get_RLR_Length(startRightCircle, goalRightCircle, false, pathDataList);
                }
                else
                {
                    Get_LRL_Length(startRightCircle, goalRightCircle, false, pathDataListReverse);
                }
            }

            //LRL
            if ((startLeftCircle - goalLeftCircle).sqrMagnitude < comparisonSqr)
            {
                if (!isReversing)
                {
                    Get_LRL_Length(startLeftCircle, goalLeftCircle, true, pathDataList);
                }
                else
                {
                    Get_RLR_Length(startLeftCircle, goalLeftCircle, true, pathDataListReverse);
                }
            }
        }



        //RSR
        void Get_RSR_Length(Vector3 startCircle, Vector3 goalCircle, List<OneDubinsPath> finalPathList)
        {
            //Find both tangent positons
            Vector3 startTangent = Vector3.zero;
            Vector3 goalTangent = Vector3.zero;

            dubinsMath.LSLorRSR(startCircle, goalCircle, false, out startTangent, out goalTangent);

            //Calculate lengths
            float length1 = dubinsMath.GetArcLength(startCircle, startPos, startTangent, false);

            float length2 = (startTangent - goalTangent).magnitude;

            float length3 = dubinsMath.GetArcLength(goalCircle, goalTangent, goalObj.position, false);

            //Save the data
            OneDubinsPath pathData = new OneDubinsPath(length1, length2, length3, startTangent, goalTangent, PathType.RSR);

            //We also need this data to simplify when generating the final path
            pathData.path2Turning = false;

            //RSR
            pathData.SetIfTurningRight(true, false, true);

            //Add the path to the collection of all paths
            finalPathList.Add(pathData);
        }



        //LSL
        void Get_LSL_Length(Vector3 startCircle, Vector3 goalCircle, List<OneDubinsPath> finalPathList)
        {
            //Find both tangent positions
            Vector3 startTangent = Vector3.zero;
            Vector3 goalTangent = Vector3.zero;

            dubinsMath.LSLorRSR(startCircle, goalCircle, true, out startTangent, out goalTangent);

            //Calculate lengths
            float length1 = dubinsMath.GetArcLength(startCircle, startPos, startTangent, true);

            float length2 = (startTangent - goalTangent).magnitude;

            float length3 = dubinsMath.GetArcLength(goalCircle, goalTangent, goalObj.position, true);

            //Save the data
            OneDubinsPath pathData = new OneDubinsPath(length1, length2, length3, startTangent, goalTangent, PathType.LSL);

            //We also need this data to simplify when generating the final path
            pathData.path2Turning = false;

            //LSL
            pathData.SetIfTurningRight(false, false, false);

            //Add the path to the collection of all paths
            finalPathList.Add(pathData);
        }



        //RSL
        void Get_RSL_Length(Vector3 startCircle, Vector3 goalCircle, List<OneDubinsPath> finalPathList)
        {
            //Find both tangent positions
            Vector3 startTangent = Vector3.zero;
            Vector3 goalTangent = Vector3.zero;

            dubinsMath.RSLorLSR(startCircle, goalCircle, false, out startTangent, out goalTangent);

            //Calculate lengths
            float length1 = dubinsMath.GetArcLength(startCircle, startPos, startTangent, false);

            float length2 = (startTangent - goalTangent).magnitude;

            float length3 = dubinsMath.GetArcLength(goalCircle, goalTangent, goalObj.position, true);

            //Save the data
            OneDubinsPath pathData = new OneDubinsPath(length1, length2, length3, startTangent, goalTangent, PathType.RSL);

            //We also need this data to simplify when generating the final path
            pathData.path2Turning = false;

            //RSL
            pathData.SetIfTurningRight(true, false, false);

            //Add the path to the collection of all paths
            finalPathList.Add(pathData);
        }



        //LSR
        void Get_LSR_Length(Vector3 startCircle, Vector3 goalCircle, List<OneDubinsPath> finalPathList)
        {
            //Find both tangent positions
            Vector3 startTangent = Vector3.zero;
            Vector3 goalTangent = Vector3.zero;

            dubinsMath.RSLorLSR(startCircle, goalCircle, true, out startTangent, out goalTangent);

            //Calculate lengths
            float length1 = dubinsMath.GetArcLength(startCircle, startPos, startTangent, true);

            float length2 = (startTangent - goalTangent).magnitude;

            float length3 = dubinsMath.GetArcLength(goalCircle, goalTangent, goalObj.position, false);

            //Save the data
            OneDubinsPath pathData = new OneDubinsPath(length1, length2, length3, startTangent, goalTangent, PathType.LSR);

            //We also need this data to simplify when generating the final path
            pathData.path2Turning = false;

            //LSR
            pathData.SetIfTurningRight(false, false, true);

            //Add the path to the collection of all paths
            finalPathList.Add(pathData);
        }



        //RLR
        void Get_RLR_Length(Vector3 startCircle, Vector3 goalCircle, bool isLRL, List<OneDubinsPath> finalPathList)
        {
            //Find both tangent positions
            Vector3 startTangent = Vector3.zero;
            Vector3 goalTangent = Vector3.zero;

            //Center of the 3rd circle
            Vector3 middleCircle = Vector3.zero;

            //Calculate the positions of the 3 circles
            dubinsMath.GetRLRorLRLTangents(
                startCircle,
                goalCircle,
                isLRL,
                out startTangent,
                out goalTangent,
                out middleCircle);

            //Calculate lengths
            float length1 = dubinsMath.GetArcLength(startCircle, startPos, startTangent, false);

            float length2 = dubinsMath.GetArcLength(middleCircle, startTangent, goalTangent, true);

            float length3 = dubinsMath.GetArcLength(goalCircle, goalTangent, goalObj.position, false);

            //Save the data
            OneDubinsPath pathData = new OneDubinsPath(length1, length2, length3, startTangent, goalTangent, PathType.RLR);

            //We also need this data to simplify when generating the final path
            pathData.path2Turning = true;

            //RLR
            pathData.SetIfTurningRight(true, false, true);

            //Add the path to the collection of all paths
            finalPathList.Add(pathData);
        }



        //LRL
        void Get_LRL_Length(Vector3 startCircle, Vector3 goalCircle, bool isLRL, List<OneDubinsPath> finalPathList)
        {
            //Find both tangent positions
            Vector3 startTangent = Vector3.zero;
            Vector3 goalTangent = Vector3.zero;

            //Center of the 3rd circle
            Vector3 middleCircle = Vector3.zero;

            //Calculate the positions of the 3 circles
            dubinsMath.GetRLRorLRLTangents(
                startCircle,
                goalCircle,
                isLRL,
                out startTangent,
                out goalTangent,
                out middleCircle);

            //Calculate the total length of this path
            float length1 = dubinsMath.GetArcLength(startCircle, startPos, startTangent, true);

            float length2 = dubinsMath.GetArcLength(middleCircle, startTangent, goalTangent, false);

            float length3 = dubinsMath.GetArcLength(goalCircle, goalTangent, goalObj.position, true);

            //Save the data
            OneDubinsPath pathData = new OneDubinsPath(length1, length2, length3, startTangent, goalTangent, PathType.LRL);

            //We also need this data to simplify when generating the final path
            pathData.path2Turning = true;

            //LRL
            pathData.SetIfTurningRight(false, true, false);

            //Add the path to the collection of all paths
            finalPathList.Add(pathData);
        }



        //
        // Generate the final path from the tangent points
        //

        //When we have found the shortest path we need to get the individual coordinates
        //so we can travel along the path
        void GeneratePaths(List<OneDubinsPath> pathDataList, bool isReversing)
        {
            for (int i = 0; i < pathDataList.Count; i++)
            {
                GetTotalPath(pathDataList[i], isReversing);
            }
        }



        //Find the coordinates of the entire path from the 2 tangents
        void GetTotalPath(OneDubinsPath pathData, bool isReversing)
        {
            //Store the waypoints of the final path here
            List<Node> finalPath = new List<Node>();

            //Start position of the car
            Vector3 currentPos = startPos;
            //Start heading of the car
            float theta = startHeading * Mathf.Deg2Rad;

            //First
            dubinsMath.AddCoordinatesToPath(
                ref currentPos,
                ref theta,
                finalPath,
                pathData.length1,
                true,
                pathData.path1TurningRight,
                isReversing);

            //Second
            dubinsMath.AddCoordinatesToPath(
                ref currentPos,
                ref theta,
                finalPath,
                pathData.length2,
                pathData.path2Turning,
                pathData.path2TurningRight,
                isReversing);

            //Third
            dubinsMath.AddCoordinatesToPath(
                ref currentPos,
                ref theta,
                finalPath,
                pathData.length3,
                true,
                pathData.path3TurningRight,
                isReversing);


            //Add the final goal coordinate of the real car and not the coordinateb we ended up with
            //Because of numerical methods they may be different
            Node newNode = new Node();

            newNode.carPos = new Vector3(goalObj.position.x, currentPos.y, goalObj.position.z);
            newNode.heading = theta;

            if (isReversing)
            {
                newNode.isReversing = true;
            }

            finalPath.Add(newNode);

            //Save the final path in the path data
            pathData.pathCoordinates = finalPath;
        }
    }
}
