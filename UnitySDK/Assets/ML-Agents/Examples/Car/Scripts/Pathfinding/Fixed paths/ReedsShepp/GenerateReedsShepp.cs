using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathfindingForCars;


namespace FixedPathAlgorithms
{
    public class GenerateReedsShepp
    {
        //The position we want to drive from
        Vector3 startPos;
        //Heading in radians
        float startHeading;
        //The position we want to drive to
        Vector3 goalPos;
        //Heading in radians
        float goalHeading;

        //The 4 different circles we have that sits to the left/right of the start/goal
        public Vector3 startLeftCircle;
        public Vector3 startRightCircle;
        public Vector3 goalLeftCircle;
        public Vector3 goalRightCircle;

        //The object that includes the math
        ReedsSheppMath reedsSheppMath;

        //Store the final paths here
        List<OneReedsSheppPath> allReedsSheppPaths = new List<OneReedsSheppPath>();


        public GenerateReedsShepp(float turningRadius)
        {
            reedsSheppMath = new ReedsSheppMath(turningRadius);
        }



        //
        // Reset
        //

        //Reset before we can generate a paths
        void Reset(Vector3 startPos, float startHeading, Vector3 goalPos, float goalHeading)
        {
            //Assign the latest positions and headings of the cars
            this.startPos = startPos;

            this.startHeading = startHeading;

            this.goalPos = goalPos;

            this.goalHeading = goalHeading;

            //Find the positions of the left/right circles of the start/goal
            PositionLeftRightCircles();

            //Remove the old paths
            allReedsSheppPaths.Clear();
        }



        //Position the left and right circles that are to the left/right of the target and the car
        void PositionLeftRightCircles()
        {
            //Goal pos
            goalRightCircle = reedsSheppMath.GetRightCircleCenterPos(goalPos, goalHeading);

            goalLeftCircle = reedsSheppMath.GetLeftCircleCenterPos(goalPos, goalHeading);

            //Start pos
            startRightCircle = reedsSheppMath.GetRightCircleCenterPos(startPos, startHeading);

            startLeftCircle = reedsSheppMath.GetLeftCircleCenterPos(startPos, startHeading);
        }



        //
        // Generate different types of paths
        //

        //Get one Reed Shepp path for debugging
        public OneReedsSheppPath GetOneReedSheppPath(Vector3 startPos, float startHeading, Vector3 goalPos, float goalHeading)
        {
            //Reset before we can begin generating paths
            Reset(startPos, startHeading, goalPos, goalHeading);

            //Generate all paths
            CalculatePathLengths();

            //If we have any paths
            if (allReedsSheppPaths.Count > 0)
            {
                //Sort to find the shortest path
                allReedsSheppPaths.Sort((x, y) => x.totalLength.CompareTo(y.totalLength));

                //Get the final coordinates of the path
                GeneratePathCoordinates(allReedsSheppPaths[0]);

                return allReedsSheppPaths[0];
            }
            else
            {
                return null;
            }
        }



        //Get the coordinate data of the shortest Reed Shepp path
        public List<Node> GetShortestReedSheppPath(Vector3 startPos, float startHeading, Vector3 goalPos, float goalHeading)
        {
            //Reset before we can begin generating paths
            Reset(startPos, startHeading, goalPos, goalHeading);

            //Generate all paths
            CalculatePathLengths();

            //If we have any paths
            if (allReedsSheppPaths.Count > 0)
            {
                //Sort to find the shortest path
                allReedsSheppPaths.Sort((x, y) => x.totalLength.CompareTo(y.totalLength));

                //Get the final coordinates of the path = the waypoints the car will follow
                GeneratePathCoordinates(allReedsSheppPaths[0]);

                //Return the coordinates of the shortest path
                return allReedsSheppPaths[0].pathCoordinates;
            }
            else
            {
                return null;
            }
        }



        //Generate all paths 
        public List<OneReedsSheppPath> GetAllReedSheppPaths(Vector3 startPos, float startHeading, Vector3 goalPos, float goalHeading)
        {
            //Reset before we can begin generating paths
            Reset(startPos, startHeading, goalPos, goalHeading);

            //Generate all paths
            CalculatePathLengths();

            if (allReedsSheppPaths.Count > 0)
            {
                //Get the final coordinates of the path
                for (int i = 0; i < allReedsSheppPaths.Count; i++)
                {
                    GeneratePathCoordinates(allReedsSheppPaths[i]);
                }

                //Debug.Log(allReedsSheppPaths[0].pathCoordinates.Count);

                return allReedsSheppPaths;
            }
            else
            {
                return null;
            }
        }



        //
        // Get the length of each path
        //

        //Get the path lengths of all paths and store them in a list together with other data
        void CalculatePathLengths()
        {
            CalculatePathLengths_CCC();

            CalculatePathLengths_CSC();

            CalculatePathLength_CC_turn_CC();
        }



        //
        // CCC
        //
        void CalculatePathLengths_CCC()
        {
            //
            // With the CCC paths, the distance between the start and goal have to be less than 4 * r
            //
            float maxDistSqr = 4f * reedsSheppMath.TurningRadius * 4f * reedsSheppMath.TurningRadius;

            //The number of segments is always 3
            int segments = 3;

            //
            // RLR
            //        
            if ((startRightCircle - goalRightCircle).sqrMagnitude < maxDistSqr)
            {
                List<OneReedsSheppPath> tmpPathList = new List<OneReedsSheppPath>();

                //Add all data that's the same for all 6 paths
                for (int i = 0; i < 5; i++)
                {
                    OneReedsSheppPath pathData = new OneReedsSheppPath(segments);

                    pathData.AddIfTurningLeft(false, true, false);

                    pathData.AddIfTurning(true, true, true);

                    tmpPathList.Add(pathData);
                }


                //R+ L- R+
                tmpPathList[0].AddIfReversing(false, true, false);

                //R- L+ R- Can be eliminated
                //tmpPathList[1].AddIfReversing(true, false, true);

                //R+ L+ R-
                tmpPathList[1].AddIfReversing(false, false, true);

                //R- L- R+
                tmpPathList[2].AddIfReversing(true, true, false);

                //R+ L- R-
                tmpPathList[3].AddIfReversing(false, true, true);

                //R- L+ R+
                tmpPathList[4].AddIfReversing(true, false, false);


                //Get all path lengths
                for (int i = 0; i < tmpPathList.Count; i++)
                {
                    //Unsure if all should be true but gives better result because no have the same length if all are true
                    Get_CCC_Length(startRightCircle, goalRightCircle, true, tmpPathList[i]);
                }
            }


            //
            // LRL
            //        
            if ((startLeftCircle - goalLeftCircle).sqrMagnitude < maxDistSqr)
            {
                List<OneReedsSheppPath> tmpPathList = new List<OneReedsSheppPath>();

                //Add all data that's the same for all 6 paths
                for (int i = 0; i < 5; i++)
                {
                    OneReedsSheppPath pathData = new OneReedsSheppPath(segments);

                    pathData.AddIfTurningLeft(true, false, true);

                    pathData.AddIfTurning(true, true, true);

                    tmpPathList.Add(pathData);
                }


                //L+ R- L+
                tmpPathList[0].AddIfReversing(false, true, false);

                //L- R+ L- Can be eliminated
                //tmpPathList[1].AddIfReversing(true, false, true);

                //L+ R+ L-
                tmpPathList[1].AddIfReversing(false, false, true);

                //L- R- L+
                tmpPathList[2].AddIfReversing(true, true, false);

                //L+ R- L-
                tmpPathList[3].AddIfReversing(false, true, true);

                //L- R+ L+
                tmpPathList[4].AddIfReversing(true, false, false);

                for (int i = 0; i < tmpPathList.Count; i++)
                {
                    Get_CCC_Length(startLeftCircle, goalLeftCircle, false, tmpPathList[i]);
                }
            }
        }



        //
        // CSC
        //
        void CalculatePathLengths_CSC()
        {
            bool isOuterTangent = false;
            bool isBottomTangent = false;

            int segments = 3;

            OneReedsSheppPath pathData = null;

            //
            //LSL and RSR is only working if the circles don't have the same position
            //

            //LSL
            if (!startLeftCircle.Equals(goalLeftCircle))
            {
                isOuterTangent = true;


                //L+ S+ L+
                isBottomTangent = true;

                pathData = new OneReedsSheppPath(segments);

                pathData.AddIfTurningLeft(true, false, true);

                pathData.AddIfTurning(true, false, true);

                pathData.AddIfReversing(false, false, false);

                Get_CSC_Length(startLeftCircle, goalLeftCircle, isOuterTangent, isBottomTangent, pathData);


                //L- S- L-
                isBottomTangent = false;

                pathData = new OneReedsSheppPath(segments);

                pathData.AddIfTurningLeft(true, false, true);

                pathData.AddIfTurning(true, false, true);

                pathData.AddIfReversing(true, true, true);

                Get_CSC_Length(startLeftCircle, goalLeftCircle, isOuterTangent, isBottomTangent, pathData);
            }


            //RSR
            if (!startRightCircle.Equals(goalRightCircle))
            {
                isOuterTangent = true;


                //R+ S+ R+
                isBottomTangent = false;

                pathData = new OneReedsSheppPath(segments);

                pathData.AddIfTurningLeft(false, false, false);

                pathData.AddIfTurning(true, false, true);

                pathData.AddIfReversing(false, false, false);

                Get_CSC_Length(startRightCircle, goalRightCircle, isOuterTangent, isBottomTangent, pathData);


                //R- S- R-
                isBottomTangent = true;

                pathData = new OneReedsSheppPath(segments);

                pathData.AddIfTurningLeft(false, false, false);

                pathData.AddIfTurning(true, false, true);

                pathData.AddIfReversing(true, true, true);

                Get_CSC_Length(startRightCircle, goalRightCircle, isOuterTangent, isBottomTangent, pathData);
            }


            //
            // LSR and RSL is only working of the circles don't intersect
            //
            float comparisonSqr = reedsSheppMath.TurningRadius * 2f * reedsSheppMath.TurningRadius * 2f;

            //LSR
            if ((startLeftCircle - goalRightCircle).sqrMagnitude > comparisonSqr)
            {
                isOuterTangent = false;


                //L+ S+ R+
                isBottomTangent = true;

                pathData = new OneReedsSheppPath(segments);

                pathData.AddIfTurningLeft(true, false, false);

                pathData.AddIfTurning(true, false, true);

                pathData.AddIfReversing(false, false, false);

                Get_CSC_Length(startLeftCircle, goalRightCircle, isOuterTangent, isBottomTangent, pathData);


                //L- S- R-
                isBottomTangent = false;

                pathData = new OneReedsSheppPath(segments);

                pathData.AddIfTurningLeft(true, false, false);

                pathData.AddIfTurning(true, false, true);

                pathData.AddIfReversing(true, true, true);

                Get_CSC_Length(startLeftCircle, goalRightCircle, isOuterTangent, isBottomTangent, pathData);
            }


            //RSL
            if ((startRightCircle - goalLeftCircle).sqrMagnitude > comparisonSqr)
            {
                isOuterTangent = false;


                //R+ S+ L+
                isBottomTangent = false;

                pathData = new OneReedsSheppPath(segments);

                pathData.AddIfTurningLeft(false, false, true);

                pathData.AddIfTurning(true, false, true);

                pathData.AddIfReversing(false, false, false);

                Get_CSC_Length(startRightCircle, goalLeftCircle, isOuterTangent, isBottomTangent, pathData);


                //R- S- L-
                isBottomTangent = true;

                pathData = new OneReedsSheppPath(segments);

                pathData.AddIfTurningLeft(false, false, true);

                pathData.AddIfTurning(true, false, true);

                pathData.AddIfReversing(true, true, true);

                Get_CSC_Length(startRightCircle, goalLeftCircle, isOuterTangent, isBottomTangent, pathData);
            }
        }



        //
        // CC turn CC
        //
        void CalculatePathLength_CC_turn_CC()
        {
            //Is only valid if the two circles intersect?
            float comparisonSqr = reedsSheppMath.TurningRadius * 2f * reedsSheppMath.TurningRadius * 2f;

            //Always 4 segments
            int segments = 4;

            bool isBottom = false;

            OneReedsSheppPath pathData = null;

            //RLRL
            if ((startRightCircle - goalLeftCircle).sqrMagnitude < comparisonSqr)
            {
                //R+ L+ R- L-
                pathData = new OneReedsSheppPath(segments);

                pathData.AddIfTurningLeft(false, true, false, true);

                pathData.AddIfTurning(true, true, true, true);

                pathData.AddIfReversing(false, false, true, true);

                isBottom = false;

                Get_CC_turn_CC_Length(startRightCircle, goalLeftCircle, isBottom, pathData);


                //R- L- R+ L+
                pathData = new OneReedsSheppPath(segments);

                pathData.AddIfTurningLeft(false, true, false, true);

                pathData.AddIfTurning(true, true, true, true);

                pathData.AddIfReversing(true, true, false, false);

                isBottom = false;

                Get_CC_turn_CC_Length(startRightCircle, goalLeftCircle, isBottom, pathData);
            }


            //LRLR
            if ((startLeftCircle - goalRightCircle).sqrMagnitude < comparisonSqr)
            {
                //L+ R+ L- R-
                pathData = new OneReedsSheppPath(segments);

                pathData.AddIfTurningLeft(true, false, true, false);

                pathData.AddIfTurning(true, true, true, true);

                pathData.AddIfReversing(false, false, true, true);

                isBottom = true;

                Get_CC_turn_CC_Length(startLeftCircle, goalRightCircle, isBottom, pathData);


                //L- R- L+ R+
                pathData = new OneReedsSheppPath(segments);

                pathData.AddIfTurningLeft(true, false, true, false);

                pathData.AddIfTurning(true, true, true, true);

                pathData.AddIfReversing(true, true, false, false);

                //Should maybe be false?
                isBottom = true;

                Get_CC_turn_CC_Length(startLeftCircle, goalRightCircle, isBottom, pathData);
            }
        }


        //One path CC turn CC
        void Get_CC_turn_CC_Length(Vector3 startCircle, Vector3 goalCircle, bool isBottom, OneReedsSheppPath pathData)
        {
            //Find the 3 tangent points and the 2 middle circle positions
            reedsSheppMath.CC_turn_CC(
                startCircle,
                goalCircle,
                isBottom,
                pathData);

            //Calculate the total length of each path
            float length1 = reedsSheppMath.GetArcLength(
                startCircle,
                startPos,
                pathData.startTangent,
                pathData.segmentsList[0]);

            float length2 = reedsSheppMath.GetArcLength(
                pathData.middleCircleCoordinate,
                pathData.startTangent,
                pathData.middleTangent,
                pathData.segmentsList[1]);

            float length3 = reedsSheppMath.GetArcLength(
                pathData.middleCircleCoordinate2,
                pathData.middleTangent,
                pathData.goalTangent,
                pathData.segmentsList[2]);

            float length4 = reedsSheppMath.GetArcLength(
                goalCircle,
                pathData.goalTangent,
                goalPos,
                pathData.segmentsList[3]);

            //Save the lengths
            pathData.AddPathLengths(length1, length2, length3, length4);

            //Add the final path
            allReedsSheppPaths.Add(pathData);
        }



        //One path CCC
        void Get_CCC_Length(Vector3 startCircle, Vector3 goalCircle, bool isRightToRight, OneReedsSheppPath pathData)
        {
            //Find both tangent positions and the position of the 3rd circles
            reedsSheppMath.Get_CCC_Tangents(
                startCircle,
                goalCircle,
                isRightToRight,
                pathData);

            //Calculate the total length of this path
            float length1 = reedsSheppMath.GetArcLength(
                startCircle,
                startPos,
                pathData.startTangent,
                pathData.segmentsList[0]);

            float length2 = reedsSheppMath.GetArcLength(
                pathData.middleCircleCoordinate,
                pathData.startTangent,
                pathData.goalTangent,
                pathData.segmentsList[1]);

            float length3 = reedsSheppMath.GetArcLength(
                goalCircle,
                pathData.goalTangent,
                goalPos,
                pathData.segmentsList[2]);

            //Save the data
            pathData.AddPathLengths(length1, length2, length3);

            //Add the path to the collection of all paths
            allReedsSheppPaths.Add(pathData);
        }



        //One path CSC
        void Get_CSC_Length(
            Vector3 startCircle,
            Vector3 goalCircle,
            bool isOuterTangent,
            bool isBottomTangent,
            OneReedsSheppPath pathData)
        {
            //Find both tangent positions
            if (isOuterTangent)
            {
                reedsSheppMath.LSLorRSR(startCircle, goalCircle, isBottomTangent, pathData);
            }
            else
            {
                reedsSheppMath.RSLorLSR(startCircle, goalCircle, isBottomTangent, pathData);
            }


            //Calculate the total length of this path
            float length1 = reedsSheppMath.GetArcLength(
                startCircle,
                startPos,
                pathData.startTangent,
                pathData.segmentsList[0]);

            float length2 = (pathData.startTangent - pathData.goalTangent).magnitude;

            float length3 = reedsSheppMath.GetArcLength(
                goalCircle,
                pathData.goalTangent,
                goalPos,
                pathData.segmentsList[2]);

            //Save the data
            pathData.AddPathLengths(length1, length2, length3);

            //Add the path to the collection of all paths
            allReedsSheppPaths.Add(pathData);
        }



        //
        // Generate the final path from the tangent points
        //

        //When we have found the shortest path we need to get the individual coordinates
        //so we can travel along the path
        void GeneratePaths(List<OneReedsSheppPath> pathDataList)
        {
            for (int i = 0; i < pathDataList.Count; i++)
            {
                GeneratePathCoordinates(pathDataList[i]);
            }
        }



        //Find the coordinates of the entire path from the 2 tangents
        void GeneratePathCoordinates(OneReedsSheppPath pathData)
        {
            //Store the waypoints of the final path here
            List<Node> finalPath = new List<Node>();

            //Start position of the car
            Vector3 currentPos = startPos;
            //Start heading of the car
            float theta = startHeading;

            //Loop through all segments and generate the waypoints
            for (int i = 0; i < pathData.segmentsList.Count; i++)
            {
                reedsSheppMath.AddCoordinatesToPath(
                    ref currentPos,
                    ref theta,
                    finalPath,
                    pathData.segmentsList[i]);
            }

            //Add the final goal coordinate
            Vector3 finalPos = new Vector3(goalPos.x, currentPos.y, goalPos.z);

            Node newNode = new Node();

            newNode.carPos = finalPos;
            newNode.heading = goalHeading;

            if (pathData.segmentsList[pathData.segmentsList.Count - 1].isReversing)
            {
                newNode.isReversing = true;
            }

            finalPath.Add(newNode);

            //Save the final path in the path data
            pathData.pathCoordinates = finalPath;
        }
    }
}
