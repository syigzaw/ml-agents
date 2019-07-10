using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace PathfindingForCars
{
    //Generate different types of heuristics and combine them
    public class HeuristicsController
    {
        //How many squares the map has, is always square
        //private int mapWidth;

        //Arrays that will store the final heuristics
        private float[,] euclideanHeuristics;
        //Dynamic programming = flow map
        public static float[,] flowFieldHeuristics;
        //Maximum of all heuristics
        public static float[,] heuristics;



        //Init
        public HeuristicsController()
        {
            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            euclideanHeuristics = new float[mapLength, mapWidth];
            heuristics = new float[mapLength, mapWidth];
            flowFieldHeuristics = new float[mapLength, mapWidth];

            //The report is also using the reeds shepp paths has heuristic and is pre-calculating these
            //The report is also pre-calculating the euclidean distance
        }



        //Get the final heuristics from all individual heuristics
        public void GenerateFinalHeuristics()
        {
            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            //Heuristic is the max of the different heuristics
            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    heuristics[x, z] = Mathf.Max(flowFieldHeuristics[x, z], euclideanHeuristics[x, z]);
                    //finalHeuristics[x, z] = euclideanHeuristics[x, z];
                    //finalHeuristics[x, z] = flowFieldHeuristics[x, z];
                }
            }
        }



        //Calculate the euclidean distance from all squares to the target
        public void EuclideanDistance(IntVector2 targetCellPos)
        {
            Vector2 targetPos2D = new Vector2((float)targetCellPos.x, (float)targetCellPos.z);

            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            //Populate the heuristics array
            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    //if (!ObstaclesController.isObstacleInCell[x, z])          (MATT)
                    //{
                        Vector2 currentPos2D = new Vector2((float)x, (float)z);

                        //The distance from the center of the square to the target
                        //Vector2.Distance is not faster
                        float heuristic = (targetPos2D - currentPos2D).magnitude;

                        euclideanHeuristics[x, z] = heuristic;
                    //}
                }
            }
        }



        //Calculate the shortest path with obstacles from each square
        //Is called Dynamic Programming in "Programming self-driving car" but is the same as a flow map
        //Is called holonomic-with-obstacles in the reports
        public void DynamicProgramming(IntVector2 targetPos)
        {
            FlowField flowField = new FlowField();

            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            //The final flow field will be stored here, so init it
            FlowFieldNode[,] gridArray = new FlowFieldNode[mapLength, mapWidth];

            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    //bool isWalkable = ObstaclesController.isObstacleInCell[x, z] ? false : true;      (MATT)
                    bool isWalkable = true;

                    FlowFieldNode node = new FlowFieldNode(isWalkable);

                    node.cellPos = new IntVector2(x, z);

                    gridArray[x, z] = node;
                }
            }

            //A flow field can have several start nodes
            List<FlowFieldNode> startNodes = new List<FlowFieldNode>();

            startNodes.Add(gridArray[targetPos.x, targetPos.z]);

            flowField.FindPath(startNodes, gridArray);


            //Add the values to the other array
            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    flowFieldHeuristics[x, z] = gridArray[x, z].totalCostFlowField;
                }
            }
        }
    }
}
