using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathfindingForCars
{
    public class FlowFieldNode
    {
        //If this cell is not an obstacle
        public bool isWalkable;
        //The position this cell has in world space
        public Vector3 worldPos;
        //The position this cell has in cell space
        public IntVector2 cellPos;
        //g-cost = distance from the start to this node with obstacles
        public float gCost;
        //Heuristic = approximate distance to the goal (without any obstacles) from this node
        public float hCost;
        //The node to get to this node
        public FlowFieldNode parent;
        //Is this node closed (used in pathfidning)
        public bool isClosed;
        //Is this node in the open set (used in pathfidning)
        public bool isInOpenSet;

        //All neighbors to this node
        public List<FlowFieldNode> neighborNodes;

        //Is this a center of the cell node
        public bool isCenter;

        //When flow field, the cost should be an int
        //This is the total cost to this node
        public int totalCostFlowField;
        //This is the movement cost from another node to this node
        public int movementCostFlowField;

        //Get the direction once between the nodes to make it easier for a lot of ai units to follow it
        public Vector3 flowDirection;



        public FlowFieldNode(bool isWalkable, Vector3 worldPos, IntVector2 cellPos)
        {
            this.isWalkable = isWalkable;

            this.worldPos = worldPos;

            this.cellPos = cellPos;
        }



        public FlowFieldNode(bool isWalkable, Vector3 worldPos)
        {
            this.isWalkable = isWalkable;

            this.worldPos = worldPos;
        }



        public FlowFieldNode(bool isWalkable)
        {
            this.isWalkable = isWalkable;
        }



        public float FCost
        {
            get
            {
                float fCost = gCost + hCost;

                return fCost;
            }
        }



        //Reset
        public void ResetNode()
        {
            gCost = 0f;
            hCost = 0f;

            parent = null;

            isClosed = false;

            isInOpenSet = false;
        }



        //Reset node flow field
        public void ResetNodeFlowField()
        {
            ResetNode();

            //Reset cost of movement (total cost) to this node to something large, like 65535
            totalCostFlowField = System.Int32.MaxValue;

            //The cost to move to this node
            movementCostFlowField = 1;

            //Obstacles should have a cost of 255, which is not the same cost as above
            //this is movement cost, which is between 1 and 254, and obstacles is 255
            //Typical movement cost is 1 and higher on more difficult terrain
            //if (!isWalkable)
            //{
            //    costFlowField = 255;
            //}
        }



        //Add a neighbor to this node
        public void AddNeighbor(FlowFieldNode neighbor)
        {
            if (neighborNodes == null)
            {
                neighborNodes = new List<FlowFieldNode>();
            }

            neighborNodes.Add(neighbor);
        }
    }
}
