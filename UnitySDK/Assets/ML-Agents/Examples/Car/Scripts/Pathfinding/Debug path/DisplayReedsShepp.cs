using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathfindingForCars;

namespace FixedPathAlgorithms
{
    //Display the shortest reeds shepp path between the car and the target
    public class DisplayReedsShepp : MonoBehaviour
    {
        //The object that generates new paths
        private GenerateReedsShepp generateReedsShepp;

        //Line renderers - need maximum of 3 to display the path
        public LineRenderer[] lineArray;
        //The target we want to reach
        public Transform goalCarTrans;
        //Materials
        public Material lineForwardMaterial;
        public Material lineReverseMaterial;



        private void Update()
        {
            //Deactivate all line renderers
            for (int i = 0; i < lineArray.Length; i++)
            {
                lineArray[i].positionCount = 0;
            }

            if (generateReedsShepp == null)
            {
                float turningRadius = SimController.current.GetActiveCarData().GetTurningRadius();

                generateReedsShepp = new GenerateReedsShepp(turningRadius);
            }

            Transform startCarTrans = SimController.current.selfDrivingCar;

            DisplayShortestPath(startCarTrans, goalCarTrans);
        }



        //Get the shortest Reeds-Shepp path and display it
        private void DisplayShortestPath(Transform startCarTrans, Transform goalCarTrans)
        {
            Vector3 startPos = startCarTrans.position;

            float startHeading = startCarTrans.eulerAngles.y * Mathf.Deg2Rad;

            Vector3 goalPos = goalCarTrans.position;

            float goalHeading = goalCarTrans.eulerAngles.y * Mathf.Deg2Rad;

            //Get the path
            OneReedsSheppPath oneReedsSheppPath = generateReedsShepp.GetOneReedSheppPath(startPos, startHeading, goalPos, goalHeading);

            //If we found a path
            if (oneReedsSheppPath != null && oneReedsSheppPath.pathCoordinates.Count > 0)
            {
                //Display the path with line renderers
                //DisplayPath(oneReedsSheppPath.pathCoordinates);
            }
        }



        //Display the Reed Shepp path with line renderers
        private void DisplayPath(List<Node> oneReedsSheppPath)
        {
            List<Node> nodes = new List<Node>();

            //A path needs between 1 and 3 line renderers
            int lineArrayPos = 0;

            bool isReversing = oneReedsSheppPath[0].isReversing;

            for (int i = 0; i < oneReedsSheppPath.Count; i++)
            {
                nodes.Add(oneReedsSheppPath[i]);

                //This means we have finished this path
                if (oneReedsSheppPath[i].isReversing != isReversing)
                {
                    AddPositionsToLineRenderer(nodes, lineArray[lineArrayPos], isReversing);

                    lineArrayPos += 1;

                    nodes.Clear();

                    isReversing = oneReedsSheppPath[i].isReversing;

                    //So the lines connect
                    nodes.Add(oneReedsSheppPath[i]);
                }
            }
            //The last segment of the line
            AddPositionsToLineRenderer(nodes, lineArray[lineArrayPos], isReversing);
        }




        //Display path positions with a line renderer
        private void AddPositionsToLineRenderer(List<Node> nodes, LineRenderer lineRenderer, bool isReversing)
        {
            if (nodes.Count > 0)
            {
                List<Vector3> linePositions = new List<Vector3>();

                for (int i = 0; i < nodes.Count; i++)
                {
                    linePositions.Add(nodes[i].carPos);
                }

                Vector3[] linePositionsArray = linePositions.ToArray();

                lineRenderer.positionCount = linePositionsArray.Length;

                lineRenderer.SetPositions(linePositionsArray);

                if (isReversing)
                {
                    lineRenderer.material = lineReverseMaterial;
                }
                else
                {
                    lineRenderer.material = lineForwardMaterial;
                }
            }
        }
    }
}
