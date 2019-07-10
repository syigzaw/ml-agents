using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using FixedPathAlgorithms;

namespace PathfindingForCars
{
    //Hybrid A* pathfinding algorithm
    //This version is allowing an expansion to a cell it has visited before if the cost so far is lower than it was before
    //It is also stopping when its finding a Reeds-sheep path and not just add a node from the Reeds-shepp path
    public class HybridAStar
    {
        //Car data needed for Hybrid A*
        //The distance between each waypoint
        //Is finding more targets if a little bit more than the resolution
        public static float driveDistance = 1.05f;
        //Used in the loop so we can also include reversing
        private float[] driveDistances = new float[] { driveDistance, -driveDistance };
        //The steering angles we are going to test
        private List<float> steeringAngles = new List<float>();

        //Arrays
        private float[,] lowestCostForward;
        private float[,] lowestCostReverse;
        //Array with open nodes - the parameter is how many items can fit in the heap
        private Heap<Node> openNodes = new Heap<Node>(400000);
        //Save which cells are expanded so we can add the lowest cost after we have expanded in all driving directions
        private List<ExpandedCellsStorage> expandedCellsForward = new List<ExpandedCellsStorage>();
        private List<ExpandedCellsStorage> expandedCellsReverse = new List<ExpandedCellsStorage>();
        private bool[,] closedCellsForward;
        private bool[,] closedCellsReverse;

        //Costs to make the car behave in differ�nt ways
        //We dont want to turn back and forth and drive straight, so turningCost will prevent that
        private float turningCost = 0.5f;
        private float obstacleCost = 5f;
        private float reverseCost = 1f;
        private float switchingDirectionOfMovementCost = 1f;

        //This is the final node which is the goal node
        private Node finalNode;
        //This is a node if we cant find our way to the goal, then it's better to drive as far as possible
        //and maybe find a better path
        private Node badNode;

        //The object that generates Reeds Shepp paths
        private GenerateReedsShepp reedsSheppPathGenerator;

        //Needed so we dont need to send the data to all methods
        private CarData carData;



        public HybridAStar()
        {
            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            //Init the arrays
            lowestCostForward = new float[mapLength, mapWidth];
            lowestCostReverse = new float[mapLength, mapWidth];

            closedCellsForward = new bool[mapLength, mapWidth];
            closedCellsReverse = new bool[mapLength, mapWidth];

            //Init steering angles
            CalculateSteeringAngles();
        }



        //The car's steering angles we are going to test when expanding each node
        void CalculateSteeringAngles()
        {
            //The max steering angle the car can have
            //Model S's max angle is 45 degrees, but this is also a function of speed
            //Higher speed = lower angle
            float maxAngle = 20f;

            steeringAngles.Add(-maxAngle * Mathf.Deg2Rad);
            steeringAngles.Add(0f);
            steeringAngles.Add(maxAngle * Mathf.Deg2Rad);
        }



        //
        // Main Hybrid A* algorithm
        //

        //What we need to reset before generating a new path
        void Reset()
        {
            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            //Reset the closed array
            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    lowestCostForward[x, z] = Mathf.Infinity;
                    lowestCostReverse[x, z] = Mathf.Infinity;

                    closedCellsForward[x, z] = false;
                    closedCellsReverse[x, z] = false;
                }
            }

            //Reset the list with open nodes
            openNodes.Clear();


            //Save the final node so we can get the final path when the A* search is done
            this.finalNode = null;
            this.badNode = null;
        }



        //Generate a path with Hybrid A*
        public void GenerateHybridAStarPath(Transform targetTrans, List<Node> finalPath, List<Node> allExpandedNodes)
        {
            //The data belonging to the car we want to reach
            CarData targetCarData = targetTrans.GetComponent<CarData>();
            //Get the data belonging to the current active car
            this.carData = SimController.current.GetActiveCarData();
            //Each car may have a different turning radius
            reedsSheppPathGenerator = new GenerateReedsShepp(carData.GetTurningRadius());

            //Everything we need to reset before we begin
            Reset();

            //Run the main loop
            RunHybridAStar(allExpandedNodes, targetCarData);

            //Generate the final path when Hybrid A* has found the goal
            GenerateFinalPath(finalPath);
        }



        //Run the main loop
        private void RunHybridAStar(List<Node> allExpandedNodes, CarData targetCarData)
        {
            //Why rear wheel? Because we need that position when simulating the "skeleton" car
            //and then it's easier if everything is done from the rear wheel positions
            Vector3 startPos = carData.GetRearWheelPos();

            IntVector2 startCellPos = PathfindingController.ConvertCoordinateToCellPos(startPos);

            lowestCostForward[startCellPos.x, startCellPos.z] = 0f;
            lowestCostReverse[startCellPos.x, startCellPos.z] = 0f;

            //Create a new node
            Node node = new Node();

            //Add the initial car data to the start node
            node.g = 0f;
            node.h = HeuristicsController.heuristics[startCellPos.x, startCellPos.z];
            node.cellPos = startCellPos;
            node.carPos = startPos;
            node.heading = carData.GetHeading() * Mathf.Deg2Rad;
            node.steeringAngle = 0f;
            node.isReversing = false;

            openNodes.Add(node);

            //Init the bad node
            this.badNode = node;

            //Bools so we can break out of the main loop
            //Set when search is complete
            bool found = false;
            //Set if we can't find a node to expand  
            bool resign = false;
            //To identify the best of the bad nodes
            //bestDistance = Mathf.Infinity;
            //To break out of the loop if it takes too long time
            int iterations = 0;

            while (!found && !resign)
            {
                if (iterations > 100000)
                {
                    Debug.Log("Stuck in infinite loop");

                    break;
                }

                iterations += 1;

                //If we don't have any nodes to expand
                if (openNodes.Count == 0)
                {
                    resign = true;

                    Debug.Log("Failed to find a path");
                }
                //We have nodes to expand
                else
                {
                    //Get the node with the lowest cost
                    Node nextNode = openNodes.RemoveFirst();

                    //Save it in case we can find an entire path if it has a lower cost
                    //Use heuristics to determine if this node is close to the goal than a previous node
                    if (nextNode.h < badNode.h)
                    {
                        this.badNode = nextNode;
                    }


                    //Close this cell
                    IntVector2 cellPos = nextNode.cellPos;

                    if (nextNode.isReversing)
                    {
                        closedCellsReverse[cellPos.x, cellPos.z] = true;
                    }
                    else
                    {
                        closedCellsForward[cellPos.x, cellPos.z] = true;
                    }



                    //Check if this is a goal node
                    //Use an accuracy of 1 m because we will not hit the exact target coordinate
                    float distanceSqrToGoal = (nextNode.carPos - targetCarData.GetRearWheelPos()).sqrMagnitude;

                    //But we also need to make sure the car has correct heading
                    float headingDifference = Mathf.Abs(targetCarData.GetHeading() - nextNode.heading * Mathf.Rad2Deg);

                    if (distanceSqrToGoal < 1f && headingDifference < 20f)
                    {
                        found = true;

                        //Debug.Log("Found a path");

                        finalNode = nextNode;

                        //Make sure the end node has the same position as the target
                        finalNode.carPos.x = targetCarData.GetRearWheelPos().x;
                        finalNode.carPos.z = targetCarData.GetRearWheelPos().z;
                    }
                    //If we havent found the goal, then expand this node
                    else
                    {
                        float distSqr = (nextNode.carPos - targetCarData.GetRearWheelPos()).sqrMagnitude;

                        //Test if we can find the goal with a fixed path algorithm such as Dubins or Reeds-Shepp
                        List<Node> fixedPath = null;

                        //Don't try to find a fixed path each expansion, but try to find more fixed paths the close to the goal we are
                        if (
                            (allExpandedNodes.Count % 300 == 0) ||
                            (allExpandedNodes.Count % 20 == 0 && distSqr < 40f * 40f) ||
                            (distSqr < 20f * 20f))
                        {
                            fixedPath = GetShortestReedsSheppPath(nextNode, targetCarData.GetCarTransform(), carData);
                        }

                        //If a fixed path is possible
                        if (fixedPath != null)
                        {
                            //Stop looping - real Hybrid A* continues looping and just add this node as a node in the tree 
                            found = true;

                            //Debug.Log("Found a path with a fixed path algorithm");

                            //Generate nodes along this path until we reach the goal
                            Node previousNode = nextNode;

                            //Don't need the first coordinate because it is the same as the position from the tree (nextNode)
                            for (int i = 1; i < fixedPath.Count; i++)
                            {
                                fixedPath[i].previousNode = previousNode;

                                previousNode = fixedPath[i];
                            }

                            finalNode = previousNode;

                            //Make sure the end node has the same position as the target
                            finalNode.carPos.x = targetCarData.GetRearWheelPos().x;
                            finalNode.carPos.z = targetCarData.GetRearWheelPos().z;
                        }
                        else
                        {
                            ExpandNode(nextNode);

                            //For debugging
                            allExpandedNodes.Add(nextNode);
                        }
                    }
                }
            }
        }



        //Expand one node
        private void ExpandNode(Node currentNode)
        {
            //To be able to expand we need the simulated car's heading and position
            float heading = currentNode.heading;

            //Save which cells are expanded so we can close them after we have expanded all directions
            expandedCellsForward.Clear();
            expandedCellsReverse.Clear();

            //Expand both forward and reverse
            for (int j = 0; j < driveDistances.Length; j++)
            {
                float driveDistance = driveDistances[j];

                //Expand by looping through all steering angles
                for (int i = 0; i < steeringAngles.Count; i++)
                {
                    //Steering angle
                    float alpha = steeringAngles[i];

                    //Turning angle
                    float beta = (driveDistance / carData.GetWheelBase()) * Mathf.Tan(alpha);

                    //Simulate the skeleton car
                    Vector3 newCarPos = SkeletonCar.CalculateNewPosition(heading, beta, driveDistance, currentNode.carPos);

                    float newHeading = SkeletonCar.CalculateNewHeading(heading, beta);

                    //Get the cell pos of the new position
                    IntVector2 cellPos = PathfindingController.ConvertCoordinateToCellPos(newCarPos);

                    //Detect if the car is colliding with obstacle or is outside of map
                    if (!ObstaclesDetection.TargetPositionWithinTrack(newCarPos, newHeading, carData))
                    //if (ObstaclesDetection.HasCarInvalidPosition(newCarPos, newHeading, carData)) (MATT)
                    {
                        continue;
                    }
                    //Is this node closed? Important this is after obstacle/outside of map detection or may be out of range
                    else if (
                        (driveDistance < 0f && closedCellsReverse[cellPos.x, cellPos.z]) ||
                        (driveDistance > 0f && closedCellsForward[cellPos.x, cellPos.z]))
                    {
                        continue;
                    }
                    //We can create a new node because this is a valid position           
                    else
                    {
                        //
                        //Calculate costs
                        //

                        //The cost it took to get to this node
                        float cost = Mathf.Abs(driveDistance);

                        //Add cost for turning if we are not having the same steering angle as previously
                        if (alpha != currentNode.steeringAngle)
                        {
                            cost += turningCost;
                        }

                        //Add a cost if we are close to an obstacle, its better to drive around them than close to them
                        //We can use the flow map to check this
                        /*  (MATT)
                        if (ObstaclesController.distanceToClosestObstacle[cellPos.x, cellPos.z] < 6)
                        {
                            cost += obstacleCost;
                        }
                        */

                        //Add cost for reversing
                        if (driveDistance < 0f)
                        {
                            cost += reverseCost;
                        }

                        //Add a cost if we are switching from reverse -> forward or the opposite
                        bool isReversing = driveDistance < 0f ? true : false;

                        if ((isReversing && !currentNode.isReversing) || (!isReversing && currentNode.isReversing))
                        {
                            cost += switchingDirectionOfMovementCost;
                        }

                        //The cost to reach this node
                        float g2 = currentNode.g + cost;

                        //Is this cost lower than it was?
                        if (
                            (driveDistance > 0f && g2 < lowestCostForward[cellPos.x, cellPos.z]) ||
                            (driveDistance < 0f && g2 < lowestCostReverse[cellPos.x, cellPos.z]))
                        {
                            //We have found a better path 
                            if (driveDistance > 0f)
                            {
                                //lowestCostForward[cellPos.x, cellPos.z] = g2;
                                expandedCellsForward.Add(new ExpandedCellsStorage(cellPos, g2));
                            }
                            if (driveDistance < 0f)
                            {
                                //lowestCostReverse[cellPos.x, cellPos.z] = g2;
                                expandedCellsReverse.Add(new ExpandedCellsStorage(cellPos, g2));
                            }

                            //
                            //Create a new node
                            //
                            Node nextNode = new Node();

                            nextNode.g = g2;
                            nextNode.h = HeuristicsController.heuristics[cellPos.x, cellPos.z];
                            nextNode.cellPos = cellPos;
                            nextNode.previousNode = currentNode;

                            //Add the car data to the node
                            nextNode.carPos = newCarPos;
                            nextNode.heading = newHeading;
                            nextNode.steeringAngle = steeringAngles[i];
                            //Are we reversing?
                            nextNode.isReversing = driveDistance < 0f ? true : false;

                            //Add the node to the list with open nodes
                            openNodes.Add(nextNode);
                        }
                    }
                }
            }


            //Close all cells we expanded to from this node so we cant reach them again from another node
            if (expandedCellsForward.Count > 0)
            {
                for (int k = 0; k < expandedCellsForward.Count; k++)
                {
                    //closedArrayForward[expandedCellsForward[k].x, expandedCellsForward[k].z] = true;

                    IntVector2 cellPos = expandedCellsForward[k].cellPos;

                    if (expandedCellsForward[k].cost < lowestCostForward[cellPos.x, cellPos.z])
                    {
                        lowestCostForward[cellPos.x, cellPos.z] = expandedCellsForward[k].cost;
                    }
                }
            }
            if (expandedCellsReverse.Count > 0)
            {
                for (int k = 0; k < expandedCellsReverse.Count; k++)
                {
                    //closedArrayReverse[expandedCellsReverse[k].x, expandedCellsReverse[k].z] = true;

                    IntVector2 cellPos = expandedCellsReverse[k].cellPos;

                    if (expandedCellsReverse[k].cost < lowestCostReverse[cellPos.x, cellPos.z])
                    {
                        lowestCostReverse[cellPos.x, cellPos.z] = expandedCellsReverse[k].cost;
                    }
                }
            }
        }



        //Generate the final path when Hybrid A* has found the goal
        private void GenerateFinalPath(List<Node> finalPath)
        {
            //If we haven't reached the final goal, then we have to use a less optimal
            //node which is set to the first node so it will never be null
            if (finalNode == null)
            {
                finalNode = badNode;
            }

            //Generate the path
            Node currentNode = finalNode;

            //Loop from the end of the path until we reach the start node
            while (currentNode != null)
            {
                finalPath.Add(currentNode);

                //Get the next node
                currentNode = currentNode.previousNode;
            }

            //If we have found a path 
            if (finalPath.Count > 1)
            {
                //Reverse
                finalPath.Reverse();
            }
        }



        //Find the shortest Reeds-Shepp path
        List<Node> GetShortestReedsSheppPath(Node nextNode, Transform goalTrans, CarData carData)
        {
            //Get the current position and heading of the car
            //In Hybrid A* we can't use the car, but the current position and heading in search tree
            Vector3 treeCarPos = nextNode.carPos;
            //We store heading in radians but dubins generator is currently using degrees as input
            float startHeading = nextNode.heading;

            Vector3 goalPos = goalTrans.position;

            float goalHeading = goalTrans.eulerAngles.y * Mathf.Deg2Rad;

            List<Node> shortestPath = reedsSheppPathGenerator.GetShortestReedSheppPath(
                treeCarPos,
                startHeading,
                goalPos,
                goalHeading);

            //If we have a path and it is not blocked by obstacle
            //if (shortestPath != null && ObstaclesDetection.IsFixedPathDrivable(shortestPath, carData)) (MATT)
            if (shortestPath != null)

            {
                return shortestPath;
            }

            return null;
        }



        public struct ExpandedCellsStorage
        {
            public IntVector2 cellPos;

            public float cost;

            public ExpandedCellsStorage(IntVector2 cellPos, float cost)
            {
                this.cellPos = cellPos;

                this.cost = cost;
            }
        }
    }
}
