using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathfindingForCars;

namespace FixedPathAlgorithms
{
    //Used when testing the reeds shepp paths
    public class DebugReedsShepp : MonoBehaviour
    {
        //Drags
        //The cars
        public GameObject startCarObj;
        public GameObject goalCarObj;
        //Line renderers
        public LineRenderer lineForward;
        public LineRenderer lineReverse;
        //Debug circles
        public Transform startLeftCircle;
        public Transform startRightCircle;
        public Transform goalLeftCircle;
        public Transform goalRightCircle;
        //Debug middle circles
        public Transform middleCircle1;
        public Transform middleCircle2;
        //Tangent points
        public Transform tangentSphere1;
        public Transform tangentSphere2;
        public Transform tangentSphere3;
        public Transform tangentSphere4;

        //The object that generates new paths
        GenerateReedsShepp generateReedsShepp;

        //Model S says on their website that the turning circle is 37 feet, which is the radius 
        float turningRadius = 11.2776f;

        //Pooled line renderers - we pool line renderers because we sometimes need to display all paths
        //when testing the reeds shepp curves
        List<LineRenderer> pooledLinesForward = new List<LineRenderer>();
        List<LineRenderer> pooledLinesReverse = new List<LineRenderer>();



        void Start()
        {
            generateReedsShepp = new GenerateReedsShepp(turningRadius);

            //Pool line renderers
            PoolLineRenderers(lineForward, pooledLinesForward);
            PoolLineRenderers(lineReverse, pooledLinesReverse);
        }



        void Update()
        {
            GetOnePath();

            //GetAllPaths();

            //GetOnePathDebug();

            //Position the left and right circle objects that are to the left/right of the start/goal
            //PositionLeftRightCircles();
        }



        //Get one Reeds-Shepp path and display it
        private void GetOnePath()
        {
            //Deactivate all line renderers
            DeactivateAllLineRenderers();

            //Get one path
            Vector3 startPos = startCarObj.transform.position;

            float startHeading = startCarObj.transform.eulerAngles.y * Mathf.Deg2Rad;

            Vector3 goalPos = goalCarObj.transform.position;

            float goalHeading = goalCarObj.transform.eulerAngles.y * Mathf.Deg2Rad;

            OneReedsSheppPath oneReedsSheppPath = generateReedsShepp.GetOneReedSheppPath(startPos, startHeading, goalPos, goalHeading);

            if (oneReedsSheppPath != null && oneReedsSheppPath.pathCoordinates.Count > 0)
            {
                //Display the path with a line renderer
                DisplayOneReedsSheppPath(oneReedsSheppPath.pathCoordinates);

                //Display the tangent spheres
                //DisplayTangentSpheres(oneReedsSheppPath);
            }
        }



        private void GetOnePathDebug()
        {
            //Deactivate all line renderers
            DeactivateAllLineRenderers();

            //Get one path
            Vector3 startPos = startCarObj.transform.position;

            float startHeading = startCarObj.transform.eulerAngles.y * Mathf.Deg2Rad;

            Vector3 goalPos = goalCarObj.transform.position;

            float goalHeading = goalCarObj.transform.eulerAngles.y * Mathf.Deg2Rad;

            OneReedsSheppPath oneReedsSheppPath = generateReedsShepp.GetOneReedSheppPath(startPos, startHeading, goalPos, goalHeading);

            if (oneReedsSheppPath != null)
            {
                //Display the path with lines
                //DisplayOneReedsSheppPath(oneReedsSheppPath.pathCoordinates);

                //Display the tangent spheres
                //DisplayTangentSpheres(oneReedsSheppPath);

                middleCircle1.gameObject.SetActive(true);

                middleCircle1.position = oneReedsSheppPath.middleCircleCoordinate;

                middleCircle2.gameObject.SetActive(true);

                middleCircle2.position = oneReedsSheppPath.middleCircleCoordinate2;

                tangentSphere1.gameObject.SetActive(true);
                tangentSphere2.gameObject.SetActive(true);
                tangentSphere3.gameObject.SetActive(true);

                tangentSphere1.position = oneReedsSheppPath.startTangent;
                tangentSphere2.position = oneReedsSheppPath.middleTangent;
                tangentSphere3.position = oneReedsSheppPath.goalTangent;
            }
        }



        //Get all Reeds-Shepp paths and display them
        void GetAllPaths()
        {
            //Deactivate all line renderers
            DeactivateAllLineRenderers();

            //Will not match Hybrid A* 100 percent because there the car begins at rear axle
            Vector3 startPos = startCarObj.transform.position;

            float startHeading = startCarObj.transform.eulerAngles.y * Mathf.Deg2Rad;

            Vector3 goalPos = goalCarObj.transform.position;

            float goalHeading = goalCarObj.transform.eulerAngles.y * Mathf.Deg2Rad;

            List<OneReedsSheppPath> allPaths = generateReedsShepp.GetAllReedSheppPaths(startPos, startHeading, goalPos, goalHeading);

            if (allPaths != null && allPaths.Count > 0)
            {
                for (int i = 0; i < allPaths.Count; i++)
                {
                    //Display the path with lines
                    DisplayOneReedsSheppPath(allPaths[i].pathCoordinates);
                }
            }
        }



        //
        // Line renderer pooling
        //

        //Pool line renderers when we start
        void PoolLineRenderers(LineRenderer lineRenderer, List<LineRenderer> lineList)
        {
            int pooledAmount = 50;

            for (int i = 0; i < pooledAmount; i++)
            {
                GameObject newLine = Instantiate(lineRenderer.gameObject);

                newLine.transform.parent = transform;

                lineList.Add(newLine.GetComponent<LineRenderer>());
            }
        }



        //Deactivate all line
        void DeactivateAllLineRenderers()
        {
            for (int i = 0; i < pooledLinesForward.Count; i++)
            {
                GameObject currentLine = pooledLinesForward[i].gameObject;

                if (currentLine.activeInHierarchy)
                {
                    currentLine.SetActive(false);
                }
            }

            for (int i = 0; i < pooledLinesReverse.Count; i++)
            {
                GameObject currentLine = pooledLinesReverse[i].gameObject;

                if (currentLine.activeInHierarchy)
                {
                    currentLine.SetActive(false);
                }
            }
        }



        //Get a pooled line renderer
        LineRenderer GetPooledLineRenderer(LineRenderer lineRenderer, List<LineRenderer> lineList)
        {
            //Loop through the list and see if we have an available line
            for (int i = 0; i < lineList.Count; i++)
            {
                if (!lineList[i].gameObject.activeInHierarchy)
                {
                    //Activate the line
                    lineList[i].gameObject.SetActive(true);

                    return lineList[i];
                }
            }

            //No available line so we have to create a new one
            GameObject newLine = Instantiate(lineRenderer.gameObject);

            newLine.transform.parent = transform;

            lineList.Add(newLine.GetComponent<LineRenderer>());

            //Activate the line
            newLine.SetActive(true);

            return newLine.GetComponent<LineRenderer>();
        }



        //
        // Debug
        //

        //Display the Reed Shepp path with line renderers
        void DisplayOneReedsSheppPath(List<Node> oneReedSheppPath)
        {
            for (int i = 1; i < oneReedSheppPath.Count; i++)
            {
                //Node currentNode = oneReedSheppPath[i];

                LineRenderer line = null;

                //Better result if we use the previous node to determine which line to display
                if (oneReedSheppPath[i - 1].isReversing)
                {
                    line = GetPooledLineRenderer(lineReverse, pooledLinesReverse);
                }
                else
                {
                    line = GetPooledLineRenderer(lineForward, pooledLinesForward);
                }

                line.SetPosition(0, oneReedSheppPath[i - 1].carPos);

                line.SetPosition(1, oneReedSheppPath[i].carPos);
            }
        }



        //Position the left and right circle objects that are to the left/right of the start/goal
        void PositionLeftRightCircles()
        {
            //Activate the circles
            startLeftCircle.gameObject.SetActive(true);
            startRightCircle.gameObject.SetActive(true);
            goalRightCircle.gameObject.SetActive(true);
            goalLeftCircle.gameObject.SetActive(true);

            //Start
            startLeftCircle.position = generateReedsShepp.startLeftCircle;

            //startRightCircle.position = generateReedsShepp.startRightCircle;

            //Goal
            //goalLeftCircle.position = generateReedsShepp.goalLeftCircle;

            goalRightCircle.position = generateReedsShepp.goalRightCircle;
        }



        //Display the tangent spheres
        void DisplayTangentSpheres(OneReedsSheppPath oneReedsSheppPath)
        {
            tangentSphere1.gameObject.SetActive(true);
            tangentSphere2.gameObject.SetActive(true);

            tangentSphere1.position = oneReedsSheppPath.startTangent;
            tangentSphere2.position = oneReedsSheppPath.goalTangent;
        }
    }
}
