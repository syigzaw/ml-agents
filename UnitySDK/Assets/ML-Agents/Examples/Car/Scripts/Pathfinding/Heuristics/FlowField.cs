using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PathfindingForCars
{
    //The flow field algorithm, which finds the shortest path from one cell (or more cells)to all other cells
    public class FlowField
    {
        public void FindPath(List<FlowFieldNode> startNodes, FlowFieldNode[,] gridArray)
        {
            //Reset such as costs and parent nodes, etc
            //Will set set costs to max value

            //Debug.Log("Length 0: " + gridArray.GetLength(0));
            //Debug.Log("Length 1: " + gridArray.GetLength(1));
            for (int x = 0; x < gridArray.GetLength(0); x++)
            {
                for (int z = 0; z < gridArray.GetLength(1); z++)
                {
                    gridArray[x, z].ResetNodeFlowField();
                }
            }

            //The list with the open nodes
            List<FlowFieldNode> openSet = new List<FlowFieldNode>();

            //Add the start nodes to the list with open nodes
            for (int i = 0; i < startNodes.Count; i++)
            {
                FlowFieldNode startNode = startNodes[i];

                openSet.Add(startNode);

                //Set the cost of the start node to 0
                startNode.totalCostFlowField = 0;
                startNode.isInOpenSet = true;

                //Can have different start costs if we want one node to be better than another node
                //if (i == 1)
                //{
                //    startNode.costFlowField = 10;
                //}
            }


            //To avoid infinite loop
            int safety = 0;
            //Stop the algorithm if open list is empty
            while (openSet.Count > 0 && safety < 50000)
            {
                safety += 1;

                //Pick the first node in the open set as the current node, no sorting is needed
                FlowFieldNode currentNode = openSet[0];

                //Remove it from the list of open nodes
                openSet.RemoveAt(0);

                currentNode.isInOpenSet = false;

                //Explore the neighboring nodes
                List<FlowFieldNode> neighbors = FindNeighboringNodes(currentNode, gridArray);

                //Loop through all neighbors, which is 4 (up-down-left-right)
                for (int i = 0; i < neighbors.Count; i++)
                {
                    FlowFieldNode neighbor = neighbors[i];

                    //Ignore the neighbor if it's an obstacle
                    if (!neighbor.isWalkable)
                    {
                        continue;
                    }

                    //Cost calculations - The cost added can be different depending on the terrain
                    int newCost = currentNode.totalCostFlowField + neighbor.movementCostFlowField;
                    
                    //Update the the cost if it's is less than the old cost
                    if (newCost < neighbor.totalCostFlowField)
                    {
                        neighbor.totalCostFlowField = newCost;

                        //Add it if it isnt already in the list of open nodes
                        if (!neighbor.isInOpenSet)
                        {
                            openSet.Add(neighbor);

                            neighbor.isInOpenSet = true;
                        }
                    }

                    //Dont need to add the current node back to the open set. If we find a shorter path to it from 
                    //another node, it will be added
                }
            }



            //IS NOT NEEDED HERE
            //Now we have the integration field, and we are now going to create the flow field
            //which is the direction from a node to the node with the smallest cost
            //for (int x = 0; x < gridArray.GetLength(0); x++)
            //{
            //    for (int z = 0; z < gridArray.GetLength(0); z++)
            //    {
            //        FlowFieldNode thisNode = gridArray[x, z];

            //        if (thisNode.isWalkable && thisNode.totalCostFlowField < System.Int32.MaxValue && thisNode.totalCostFlowField != 0)
            //        {
            //            thisNode.parent = FindLowestCostNeighbor(thisNode, gridArray, true);

            //            //Find the direction between the nodes
            //            if (thisNode.parent != null)
            //            {
            //                thisNode.flowDirection = (thisNode.parent.worldPos - thisNode.worldPos).normalized;
            //            }
            //        }
            //    }
            //}
        }



        //Find the neighboring node with the least cost
        private FlowFieldNode FindLowestCostNeighbor(FlowFieldNode node, FlowFieldNode[,] gridArray, bool includeCorners)
        {
            int lowestCost = System.Int32.MaxValue;

            FlowFieldNode lowestCostNode = null;

            //Get the directions we can move in
            IntVector2[] delta = DeltaMovements.delta;

            if (includeCorners)
            {
                delta = DeltaMovements.deltaWithCorners;
            }

            for (int i = 0; i < delta.Length; i++)
            {
                IntVector2 cellPos = new IntVector2(node.cellPos.x + delta[i].x, node.cellPos.z + delta[i].z);

                //Is this cell position within the grid?
                if (IsCellPosWithinGrid(cellPos, gridArray))
                {
                    ////Make sure we are not crossing obstacles diagonally
                    //bool topNode = gridArray[node.cellPos.x + 0, node.cellPos.z + 1].isWalkable;
                    //bool bottomNode = gridArray[node.cellPos.x + 0, node.cellPos.z - 1].isWalkable;
                    //bool leftNode = gridArray[node.cellPos.x - 1, node.cellPos.z + 0].isWalkable;
                    //bool rightNode = gridArray[node.cellPos.x + 1, node.cellPos.z + 0].isWalkable;

                    ////TR
                    //if (delta[i].x == 1 && delta[i].z == 1)
                    //{
                    //    if (!topNode || !rightNode)
                    //    {
                    //        continue;
                    //    }
                    //}
                    ////BL
                    //else if (delta[i].x == -1 && delta[i].z == -1)
                    //{
                    //    if (!leftNode || !bottomNode)
                    //    {
                    //        continue;
                    //    }
                    //}
                    ////TL
                    //else if (delta[i].x == -1 && delta[i].z == 1)
                    //{
                    //    if (!topNode || !leftNode)
                    //    {
                    //        continue;
                    //    }
                    //}
                    ////BR
                    //else if (delta[i].x == 1 && delta[i].z == -1)
                    //{
                    //    if (!rightNode || !bottomNode)
                    //    {
                    //        continue;
                    //    }
                    //}

                    int neighborCost = gridArray[cellPos.x, cellPos.z].totalCostFlowField;

                    if (neighborCost < lowestCost)
                    {
                        lowestCost = neighborCost;

                        lowestCostNode = gridArray[cellPos.x, cellPos.z];
                    }
                }
            }

            if (lowestCost < System.Int32.MaxValue)
            {
                return lowestCostNode;
            }
            else
            {
                return null;
            }
        }



        //Find the neighboring nodes to a node by checking all 4 nodes around it
        private List<FlowFieldNode> FindNeighboringNodes(FlowFieldNode node, FlowFieldNode[,] gridArray)
        {
            List<FlowFieldNode> neighboringNodes = new List<FlowFieldNode>();

            //Get the directions we can move in, which are up, left, right, down
            IntVector2[] delta = DeltaMovements.delta;

            for (int i = 0; i < delta.Length; i++)
            {
                IntVector2 cellPos = new IntVector2(node.cellPos.x + delta[i].x, node.cellPos.z + delta[i].z);

                //Is this cell position within the grid?
                if (IsCellPosWithinGrid(cellPos, gridArray))
                {
                    neighboringNodes.Add(gridArray[cellPos.x, cellPos.z]);
                }          
            }

            return neighboringNodes;
        }



        //Is a cell position within the grid?
        private bool IsCellPosWithinGrid(IntVector2 cellPos, FlowFieldNode[,] gridArray)
        {
            bool isWithin = false;
            int gridLength = gridArray.GetLength(0);
            int gridWidth = gridArray.GetLength(1);

            if (cellPos.x >= 0 && cellPos.x < gridLength && cellPos.z >= 0 && cellPos.z < gridWidth)
            {
                isWithin = true;
            }

            return isWithin;
        }
    }
}
