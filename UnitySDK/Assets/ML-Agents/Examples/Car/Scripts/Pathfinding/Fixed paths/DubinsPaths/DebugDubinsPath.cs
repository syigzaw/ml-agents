using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathfindingForCars;

namespace FixedPathAlgorithms
{
    //Debug Dubin paths to see if they are working
    public class DebugDubinsPath : MonoBehaviour
    {
        //Drags

        //The car
        public Transform startPosObj;
        //The target we want to reach
        public Transform endPosObj;

        //Circles for debugging
        public Transform rightCircleTarget;
        public Transform leftCircleTarget;

        public Transform rightCircleCar;
        public Transform leftCircleCar;

        public Transform CCCCircleObj;

        //Line renderers
        public LineRenderer lineRSR;
        public LineRenderer lineRSL;
        public LineRenderer lineLSL;
        public LineRenderer lineLSR;
        public LineRenderer lineLRL;
        public LineRenderer lineRLR;
        public LineRenderer lineFinalPath;
        public LineRenderer lineFinalPathReverse;

        //Spheres to display tangent line positions
        public Transform[] debugSpheres;

        //The object that generates dubin paths
        GenerateDubinsPaths dubinsPathGenerator;

        //Same as in Hybrid A star but need it here as well, maybe remove later?
        float turningRadius = 11.2776f;


        void Start()
        {
            dubinsPathGenerator = new GenerateDubinsPaths(endPosObj, turningRadius);
        }



        void Update()
        {
            //Find the shortest path
            FindShortestDubinPathForward();

            //Find all dubin paths
            //FindAllDubinPathsForward();

            //Find the shortest path reverse
            FindShortestDubinPathReverse();

            //Find all dubin paths reverse
            //FindAllDubinPathsReverse();

            //Position the physical left and right circles for debugging
            //PositionLeftRightCircles();
        }



        //Find the shortest path from the set of curves and display it - Forward
        void FindShortestDubinPathForward()
        {
            //Get the current position and heading of the car
            //In Hybrid A* we can't use the car, but the current position and heading in search tree
            Vector3 startPos = startPosObj.position;

            float startHeading = startPosObj.eulerAngles.y;

            OneDubinsPath shortestPath = dubinsPathGenerator.GetShortestDubinPath(startPos, startHeading);

            //If we found a path
            if (shortestPath != null)
            {
                //Display the shortest path with a special line
                DisplayPath(shortestPath, lineFinalPath);
            }
        }



        //Find all valid paths and display them - Forward
        void FindAllDubinPathsForward()
        {
            //Get the current position and heading of the car
            //In Hybrid A* we can't use the car, but the current position and heading in search tree
            Vector3 startPos = startPosObj.position;

            float startHeading = startPosObj.eulerAngles.y;

            List<OneDubinsPath> allDubinPaths = dubinsPathGenerator.GetAllDubinPaths(startPos, startHeading);

            //If we have paths
            if (allDubinPaths != null)
            {
                //Display all paths
                DebugAllPaths(allDubinPaths);
            }
        }



        //Find the shortest path from the set of curves and display it - Forward
        void FindShortestDubinPathReverse()
        {
            //Get the current position and heading of the car
            //In Hybrid A* we can't use the car, but the current position and heading in search tree
            Vector3 startPos = startPosObj.position;

            float startHeading = startPosObj.eulerAngles.y;

            OneDubinsPath shortestPath = dubinsPathGenerator.GetShortestDubinPathReverse(startPos, startHeading);

            //If we found a path
            if (shortestPath != null)
            {
                //Display the shortest path with a special line
                DisplayPath(shortestPath, lineFinalPathReverse);

                //DisplayTangentBalls
                //DisplayTangentBalls(shortestPath);
            }
        }



        //Find all valid paths and display them - Reverse
        void FindAllDubinPathsReverse()
        {
            //Get the current position and heading of the car
            //In Hybrid A* we can't use the car, but the current position and heading in search tree
            Vector3 startPos = startPosObj.position;

            float startHeading = startPosObj.eulerAngles.y;

            List<OneDubinsPath> allDubinPaths = dubinsPathGenerator.GetAllDubinPathsReverse(startPos, startHeading);

            //If we have paths
            if (allDubinPaths != null)
            {
                //Display all paths
                DebugAllPaths(allDubinPaths);
            }
        }



        //
        // Show debug lines and objects
        //

        //Debug all paths by displaying their paths with lines
        void DebugAllPaths(List<OneDubinsPath> pathDataList)
        {
            //Deactivate all line renderers (we activate them if a path is available)
            DeactivateLineRenderers();

            for (int i = 0; i < pathDataList.Count; i++)
            {
                PathType currentPathType = pathDataList[i].pathType;

                switch (currentPathType)
                {
                    case PathType.LRL:
                        DisplayPath(pathDataList[i], lineLRL);
                        break;
                    case PathType.RLR:
                        DisplayPath(pathDataList[i], lineRLR);
                        break;
                    case PathType.LSR:
                        DisplayPath(pathDataList[i], lineLSR);
                        break;
                    case PathType.RSL:
                        DisplayPath(pathDataList[i], lineRSL);
                        break;
                    case PathType.RSR:
                        DisplayPath(pathDataList[i], lineRSR);
                        break;
                    case PathType.LSL:
                        DisplayPath(pathDataList[i], lineLSL);
                        break;
                }
            }
        }



        //Display a path with a line renderer
        void DisplayPath(OneDubinsPath pathData, LineRenderer lineRenderer)
        {
            //Activate the line renderer
            lineRenderer.gameObject.SetActive(true);

            //The coordinates of the final path
            List<Node> finalPath = pathData.pathCoordinates;

            //Display the final line
            //lineRenderer.SetVertexCount(finalPath.Count);
            lineRenderer.positionCount = finalPath.Count;

            for (int i = 0; i < finalPath.Count; i++)
            {
                lineRenderer.SetPosition(i, finalPath[i].carPos);
            }
        }



        //Deactivate all line renderers in case a circle is not possible
        //Then we dont want to show the old circle
        void DeactivateLineRenderers()
        {
            lineLRL.gameObject.SetActive(false);
            lineRLR.gameObject.SetActive(false);

            lineLSL.gameObject.SetActive(false);
            lineRSR.gameObject.SetActive(false);

            lineLSR.gameObject.SetActive(false);
            lineRSL.gameObject.SetActive(false);
        }



        //Position the left and right circle objects that are to the left/right of the start/goal
        void PositionLeftRightCircles()
        {
            //Goal
            rightCircleTarget.position = dubinsPathGenerator.goalRightCircle;

            leftCircleTarget.position = dubinsPathGenerator.goalLeftCircle;


            //Start
            rightCircleCar.position = dubinsPathGenerator.startRightCircle;

            leftCircleCar.position = dubinsPathGenerator.startLeftCircle;
        }



        //Display balls where the tangent coordinates are
        void DisplayTangentBalls(OneDubinsPath shortestPath)
        {
            debugSpheres[0].transform.position = shortestPath.tangent1;
            debugSpheres[1].transform.position = shortestPath.tangent2;
        }
    }
}
