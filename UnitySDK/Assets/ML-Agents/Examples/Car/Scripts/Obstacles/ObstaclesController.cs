using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PathfindingForCars
{
    //Takes care of all obstacles, including adding them and 
    //generating an array with bools showing which squares are obstacles 
    public class ObstaclesController : MonoBehaviour
    {
        //Drags
        //The parent of the obstacle to get a cleaner workspace
        public Transform obstaclesParent;
        //Obstacle cube we add to the scene
        public GameObject obstacleObj;

        //The obstacles data are stored here 
        public static List<ObstacleData> obstaclesPosList = new List<ObstacleData>();
        //The obstacles that intersects with a cell are stored here
        public static List<ObstacleData>[,] obstaclesInCell;
        //If there is an obstacle in a cell
        public static bool[,] isObstacleInCell;
        //The flow field (potential field), which tells the number of cells to the closest obstacle from a cell
        public static int[,] distanceToClosestObstacle;



        public void InitObstacles()
        {
            //Init the arrays
            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            isObstacleInCell = new bool[mapLength, mapWidth];

            obstaclesInCell = new List<ObstacleData>[mapLength, mapWidth];

            distanceToClosestObstacle = new int[mapLength, mapWidth];

            //Add init values to the obstacles array
            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    isObstacleInCell[x, z] = false;
                }
            }

            //Remove any previous obstacles
            Reset();

            //Generate obstacles (= center coordinates of cubes)
            AddObstacles();

            //Figure out which cells the obstacle touch
            CellObstacleDetection();

            //Generate the flow field showing how far to the closest obstacle from each cell
            GenerateObstacleFlowField();
        }

        public void Reset()
        {
            obstaclesPosList.Clear();
            Object[] allObjects = FindObjectsOfType(typeof(GameObject));
            foreach (GameObject obj in allObjects)
            {
                if (obj.transform.name == "Obstacle(Clone)")
                {
                    Destroy(obj);
                }
            }
        }

        //Figure out which cells the obstacle touch
        private void CellObstacleDetection()
        {
            //Now we need to check which cells the obstacles intersect with
            //Find if corners of the obstacles intersect with a cell
            Vector3[] corners = new Vector3[4];

            //Loop through all obstacles
            for (int i = 0; i < obstaclesPosList.Count; i++)
            {
                //Loop through all corners of the obstacle cubes, and not just the center
                Vector3 centerPos = obstaclesPosList[i].centerPos;

                //To easier loop through the corners
                corners[0] = obstaclesPosList[i].cornerPos.BL;
                corners[1] = obstaclesPosList[i].cornerPos.FL;
                corners[2] = obstaclesPosList[i].cornerPos.FR;
                corners[3] = obstaclesPosList[i].cornerPos.BR;

                //Loop through all corners
                for (int j = 0; j < corners.Length; j++)
                {
                    //In which cell is this position?
                    IntVector2 cellPos = PathfindingController.ConvertCoordinateToCellPos(corners[j]);

                    //It's an obstacle in this square
                    isObstacleInCell[cellPos.x, cellPos.z] = true;



                    //Populate the other arrays - need to do this for every corner

                    //Get the list
                    List<ObstacleData> obstaclesList = obstaclesInCell[cellPos.x, cellPos.z];

                    //Create a new list if needed
                    if (obstaclesList == null)
                    {
                        obstaclesList = new List<ObstacleData>();

                        obstaclesInCell[cellPos.x, cellPos.z] = obstaclesList;
                    }

                    //Check if the center of the obstacle already is in the list
                    bool isInList = false;

                    for (int k = 0; k < obstaclesList.Count; k++)
                    {
                        if (Vector3.SqrMagnitude(obstaclesList[k].centerPos - centerPos) < 0.001f)
                        {
                            isInList = true;

                            break;
                        }
                    }

                    if (!isInList)
                    {
                        obstaclesList.Add(obstaclesPosList[i]);
                    }
                }
            }
        }



        //Generate obstacles and return the center coordinates of them in a list 
        public void AddObstacles()
        {
            float mapLength = (float)PathfindingController.mapLength;
            float mapWidth = (float)PathfindingController.mapWidth;

            //How many cubes are we going to add?
            int numberOfObstacles = 10;

            for (int i = 0; i < numberOfObstacles; i++)
            {
                //Generate random coordinates in the map
                float randomX = Random.Range(1f, mapLength - 1f);
                float randomZ = Random.Range(1f, mapWidth - 1f);

                //Car starts at 30, 30, so avoid that area
                if ((randomX < 25f || randomX > 35f) && (randomZ < 25f || randomZ > 35f))
                {
                    //The center of the cube
                    Vector3 center = new Vector3(randomX, 0.5f, randomZ);

                    //First create a random square which consists of several cubes
                    int sqrSize = Random.Range(1, 8);

                    //Create the square of several cubes
                    CreateSquare(sqrSize, center);


                    //Also add obstacles to all directions from the first cube
                    int amount = 1;
                    AddObstacles(center, Random.Range(0, amount), 1f, 0f);
                    AddObstacles(center, Random.Range(0, amount), -1f, 0f);
                    AddObstacles(center, Random.Range(0, amount), 0f, 1f);
                    AddObstacles(center, Random.Range(0, amount), 0f, -1f);

                    if (Random.Range(0f, 1f) < 0.3f)
                    {
                        AddObstacles(center, Random.Range(0, amount), 1f, 1f);
                        AddObstacles(center, Random.Range(0, amount), -1f, -1f);
                        AddObstacles(center, Random.Range(0, amount), 1f, -1f);
                        AddObstacles(center, Random.Range(0, amount), -1f, 1f);
                    }
                }
                else
                {
                    i -= 1;
                }


            }
        }



        //Generate the flow field showing how far to the closest obstacle from each cell
        private void GenerateObstacleFlowField()
        {
            FlowField flowField = new FlowField();

            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            //The flow field will be stored in this array
            FlowFieldNode[,] gridArray = new FlowFieldNode[mapLength, mapWidth];

            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    bool isWalkable = true;

                    FlowFieldNode node = new FlowFieldNode(isWalkable);

                    node.cellPos = new IntVector2(x, z);

                    gridArray[x, z] = node;
                }
            }

            //A flow field can have several start nodes
            List<FlowFieldNode> startNodes = new List<FlowFieldNode>();

            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    //If this is an obstacle
                    if (isObstacleInCell[x, z])
                    {
                        startNodes.Add(gridArray[x, z]);
                    }
                }
            }

            //Generate the flow field
            flowField.FindPath(startNodes, gridArray);


            //Add the values to the other array
            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    distanceToClosestObstacle[x, z] = gridArray[x, z].totalCostFlowField;
                }
            }
        }



        //Creates a square consisting of several cubes
        void CreateSquare(int squareSize, Vector3 center)
        {
            float mapLength = (float)PathfindingController.mapLength;
            float mapWidth = (float)PathfindingController.mapWidth;

            float startX = center.x;
            float startZ = center.z;

            for (int x = 0; x < squareSize; x++)
            {
                for (int z = 0; z < squareSize; z++)
                {
                    Vector3 pos = new Vector3(startX + (1f * x), 0.5f, startZ + (1f * z));

                    //If we are within the map
                    if (pos.x > 0.5f && pos.x < mapLength - 0.5f && pos.z > 0.5f && pos.z < mapWidth - 0.5f)
                    {
                        //If we are not hitting the car
                        if ((pos.x < 25f || pos.x > 35f) && (pos.z < 25f || pos.z > 35f))
                        {
                            AddObstacle(pos);
                        }
                    }
                }
            }
        }



        //Add several cubes in a direction
        void AddObstacles(Vector3 center, int amount, float deltaX, float deltaZ)
        {
            float mapLength = (float)PathfindingController.mapLength;
            float mapWidth = (float)PathfindingController.mapWidth;

            float startX = center.x;
            float startZ = center.z;

            for (int i = 0; i < amount; i++)
            {
                startX += deltaX;
                startZ += deltaZ;

                Vector3 pos = new Vector3(startX, 0.5f, startZ);

                //If we are within the map
                if (startX > 0.5f && startX < mapLength - 0.5f && startZ > 0.5f && startZ < mapWidth - 0.5f)
                {
                    //If we are not hitting the car
                    if ((startX < 25f || startX > 35f) && (startZ < 25f || startZ > 35f))
                    {
                        AddObstacle(pos);
                    }
                }

            }
        }



        //Instantiate one cube and add its position to the array
        void AddObstacle(Vector3 pos)
        {
            //Add a new cube at this position
            Instantiate(obstacleObj, pos, Quaternion.identity, obstaclesParent);

            //Add it to the list of all obstacles
            //But make sure to be in 2d space, because easier to check if the car intersects with an obstacle
            pos.y = 0f;

            //Get the corners
            Vector3 centerPos = pos;

            float halfSquare = 1f * 0.5f;

            //This might be confusing but imagine that x is sideways and z is up
            Vector3 BL = new Vector3(centerPos.x - halfSquare, 0f, centerPos.z - halfSquare);
            Vector3 FL = new Vector3(centerPos.x - halfSquare, 0f, centerPos.z + halfSquare);
            Vector3 FR = new Vector3(centerPos.x + halfSquare, 0f, centerPos.z + halfSquare);
            Vector3 BR = new Vector3(centerPos.x + halfSquare, 0f, centerPos.z - halfSquare);

            Rectangle cornerPos = new Rectangle(FL, FR, BL, BR);

            obstaclesPosList.Add(new ObstacleData(centerPos, cornerPos));
        }
    }
}
