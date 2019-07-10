using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using FixedPathAlgorithms;

namespace PathfindingForCars
{
    //Hybrid A* pathfinding algorithm
    //This version is closing a cell by using the heading. So the car can enter the same cell again if it has another heading
    //This is slower but more accurate
    //It is also adding a node from the reeds-shepp path and not the entire path
    public class HybridAStarAngle
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
        //Array with open nodes - the parameter is how many items can fit in the heap
        private Heap<Node> openNodes = new Heap<Node>(400000);
        //int in the dictionaries below is the rounded heading used to enter a cell
        private Dictionary<int, bool>[,] closedCells;
        private Dictionary<int, float>[,] lowestCostCells;
        //Round the angle to nearest roundValue
        float roundValue = 20f;

        //Costs to make the car behave in differ�nt ways
        //We dont want to turn back and forth and drive straight, so turningCost will prevent that
        private float turningCost = 1f;
        //Obstacle cost will slow down the algorithm
        private float obstacleCost = 0f;
        private float reverseCost = 1f;
        private float switchingDirectionOfMovementCost = 1f;

        //This is the final node which is the goal node
        private Node finalNode;
        //This is a node if we cant find our way to the goal, then it's better to drive as far as possible
        //and maybe find a better path
        private Node badNode;

        //The object that generates Reeds Shepp paths
        private GenerateReedsShepp reedsSheppPathGenerator;

        //Needed so we dont need to send it as parameters to different methods
        private CarData carData;



        public HybridAStarAngle()
        {
            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            //Init the arrays
            lowestCostCells = new Dictionary<int, float>[mapLength, mapWidth];

            closedCells = new Dictionary<int, bool>[mapLength, mapWidth];

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

            //Reset the arrays
            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    lowestCostCells[x, z] = new Dictionary<int, float>();

                    closedCells[x, z] = new Dictionary<int, bool>();
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
            CarData targetCarData = targetTrans.GetComponent<CarData>();
            //Get the data belonging to the current active car
            this.carData = SimController.current.GetActiveCarData();
            //Each car may have a different turning radius
            reedsSheppPathGenerator = new GenerateReedsShepp(carData.GetTurningRadius());

            //Everything we need to reset before we begin
            Reset();

            //Run the main loop
            RunHybridAStar(targetCarData, allExpandedNodes);

            //Generate the final path when Hybrid A* has found the goal
            GenerateFinalPath(finalPath);
        }



        //Run the main loop
        private void RunHybridAStar(CarData targetCarData, List<Node> allExpandedNodes)
        {
            //Why rear wheel? Because we need that position when simulating the "skeleton" car
            //and then it's easier if everything is done from the rear wheel positions
            Vector3 startPos = carData.GetRearWheelPos();

            IntVector2 startCellPos = PathfindingController.ConvertCoordinateToCellPos(startPos);

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
                    //Use heuristics to determine if this node is vlose to the goal than a previous node
                    if (nextNode.h < badNode.h)
                    {
                        this.badNode = nextNode;
                    }


                    //Close this cell
                    IntVector2 cellPos = nextNode.cellPos;

                    int roundedAngle = RoundAngle(nextNode.heading * Mathf.Rad2Deg);

                    Dictionary<int, bool> currentAngles = closedCells[cellPos.x, cellPos.z];

                    //Close the cell with this angle
                    if (!currentAngles.ContainsKey(roundedAngle))
                    {
                        currentAngles.Add(roundedAngle, true);
                    }
                    else
                    {
                        //This is not costly so it souldnt be counted as an iteration
                        //Is needed because we are not removing nodes with higher cost but the same angle from the heap
                        iterations -= 1;
                    
                        continue;
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
                        //Test if we can find the goal with a fixed path algorithm such as Dubins or Reeds-Shepp
                        List<Node> fixedPath = null;

                        //Don't try to find a fixed path each expansion, but try to find more fixed paths the close to the goal we are
                        if (
                            (allExpandedNodes.Count % 300 == 0) ||
                            (allExpandedNodes.Count % 20 == 0 && distanceSqrToGoal < 40f * 40f)
                            )
                        {
                            fixedPath = GetShortestReedsSheppPath(nextNode, targetCarData.GetCarTransform(), carData);

                            //If a fixed path is possible
                            if (fixedPath != null)
                            {
                                //Add this node to the open list
                                //Not 0 because that's the node we are expanding from
                                Node fixedPathNode = fixedPath[1];

                                fixedPathNode.cellPos = PathfindingController.ConvertCoordinateToCellPos(fixedPathNode.carPos);
                                fixedPathNode.h = HeuristicsController.heuristics[fixedPathNode.cellPos.x, fixedPathNode.cellPos.z];
                                fixedPathNode.previousNode = nextNode;
                                //Add the other car data to the node
                                //This is not exactly true but almost true because this node does almost have the same steering angle as the last node
                                fixedPathNode.steeringAngle = 0f;

                                //Now we can calculate the cost to reach this node
                                fixedPathNode.g = CalculateCosts(fixedPathNode);

                                //Add the node to the list with open nodes
                                openNodes.Add(fixedPathNode);
                            }
                        }


                        ExpandNode(nextNode);

                        //For debugging
                        allExpandedNodes.Add(nextNode);
                    }
                }
            }
        }



        //Expand one node
        private void ExpandNode(Node currentNode)
        {
            //To be able to expand we need the simulated car's heading and position
            float heading = currentNode.heading;

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


                    //
                    //Check if the car is colliding with obstacle or is outside of map
                    //
                    if (!ObstaclesDetection.TargetPositionWithinTrack(newCarPos, newHeading, carData))
                    //if (ObstaclesDetection.HasCarInvalidPosition(newCarPos, newHeading, carData)) (MATT)
                    {
                        continue;
                    }


                    //
                    //Check if this node is closed
                    //
                    //Important this is after obstacle/outside of map detection or may be out of range
                    int roundedAngle = RoundAngle(newHeading * Mathf.Rad2Deg);

                    if (closedCells[cellPos.x, cellPos.z].ContainsKey(roundedAngle))
                    {
                        continue;
                    }


                    //
                    //Check if this node is cheaper than any other node by calculating costs
                    //
                    //First create a new node with all data we need to calculate costs
                    Node nextNode = new Node();

                    nextNode.cellPos = cellPos;
                    nextNode.carPos = newCarPos;
                    nextNode.heading = newHeading;
                    nextNode.steeringAngle = steeringAngles[i];
                    nextNode.isReversing = driveDistance < 0f ? true : false;
                    nextNode.h = HeuristicsController.heuristics[cellPos.x, cellPos.z];
                    nextNode.previousNode = currentNode;

                    //Now we can calculate the cost to reach this node
                    nextNode.g = CalculateCosts(nextNode);


                    //Is this cost lower than it was or have we not expanded to this this cell with this angle?
                    if (
                        ((lowestCostCells[cellPos.x, cellPos.z].ContainsKey(roundedAngle) && nextNode.g < lowestCostCells[cellPos.x, cellPos.z][roundedAngle])) || 
                        !lowestCostCells[cellPos.x, cellPos.z].ContainsKey(roundedAngle))
                        
                    {
                        //We havent expanded to this node before with this angle
                        if (!lowestCostCells[cellPos.x, cellPos.z].ContainsKey(roundedAngle))
                        {
                            lowestCostCells[cellPos.x, cellPos.z].Add(roundedAngle, nextNode.g);
                        }
                        //The costs is lower than a previous expansion
                        else
                        {
                            lowestCostCells[cellPos.x, cellPos.z][roundedAngle] = nextNode.g;

                            //Now we should remove the old node from the heap
                            //Actually not needed because it's costly to remove nodes from the heap
                            //So its better to just skip the node when finding nodes with lowest cost
                        }

                        //Add the node to the list with open nodes
                        openNodes.Add(nextNode);
                    }
                }
            }
        }



        //Calculate the costs for going from one node to another
        private float CalculateCosts(Node node)
        {
            //The cost it took to get to this node
            float cost = driveDistance;

            //Cost for changing heading direction = turning
            //float headingDiff = Mathf.Abs(goingFrom.heading - goingTo.heading) * Mathf.Rad2Deg;

            //if (headingDiff > 1f)
            //{
            //    cost += turningCost;
            //}

            if (node.previousNode.steeringAngle != node.steeringAngle)
            {
                cost += turningCost;
            }

            
            //Add a cost if we are close to an obstacle, its better to drive around them than close to them
            //We can use the flow map to check this
            //But this is also making the search tree bigger, so it is most likely unnecessary
            /*  (MATT)
            if (ObstaclesController.distanceToClosestObstacle[node.cellPos.x, node.cellPos.z] < 6)
            {
                cost += obstacleCost;
            }
            */

            //Add cost for reversing
            if (node.isReversing)
            {
                cost += reverseCost;
            }

            //Add a cost if we are switching from reverse -> forward or the opposite
            if ((node.isReversing && !node.previousNode.isReversing) || (!node.isReversing && node.previousNode.isReversing))
            {
                cost += switchingDirectionOfMovementCost;
            }

            //The cost to reach this node
            float costToReachNode = node.previousNode.g + cost;

            return costToReachNode;
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
            Vector3 carPos = nextNode.carPos;
            //We store heading in radians but dubins generator is currently using degrees as input
            float carHeading = nextNode.heading;

            Vector3 goalPos = goalTrans.position;

            float goalHeading = goalTrans.eulerAngles.y * Mathf.Deg2Rad;

            List<Node> shortestPath = reedsSheppPathGenerator.GetShortestReedSheppPath(
                carPos,
                carHeading,
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



        //Round a value to nearest int
        private int RoundAngle(float angle)
        {
            int roundedAngle = (int)(Mathf.Round(angle / roundValue) * roundValue);

            return roundedAngle;
        }
    }
}
