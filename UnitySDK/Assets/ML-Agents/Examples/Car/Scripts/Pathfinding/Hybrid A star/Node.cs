using UnityEngine;
using System.Collections;
using System;

namespace PathfindingForCars
{
    //One node in the A star algorithm
    public class Node : IHeapItem<Node>
    {
        //The cost to this node
        public float g;
        //The estimated cost to the goal from this node = the heuristics
        public float h;
        //The coordinates of this node in the grid
        public IntVector2 cellPos;

        //The data we need for the car
        public Vector3 carPos;
        //The heading in radians
        public float heading;
        //Other data so we can calculate costs
        //E.g. higher cost if reversing because moving forward is better
        //The steering angle in radians
        public float steeringAngle;
        //Is the car reversing?
        public bool isReversing = false;

        //The node we took to get here so we can get the final path
        public Node previousNode = null;

        //The index this node has in the heap, to make sorting nodes faster
        private int heapIndex;



        public Node()
        {
            this.cellPos = new IntVector2();
            this.carPos = new Vector3();
        }



        //The total cost including heuristic (f = g + h)
        public float GetFCost()
        {
            return g + h;
        }



        //The heap requires that we implement this
        public int HeapIndex
        {
            get
            {
                return heapIndex;
            }
            set
            {
                heapIndex = value;
            }
        }



        //To compare nodes when sorting the heap
        public int CompareTo(Node nodeToCompare)
        {
            int compare = GetFCost().CompareTo(nodeToCompare.GetFCost());

            //If they are equal, use the one that is the closest
            //Will return 1, 0 or -1, so 0 means the f costs are the same
            if (compare == 0)
            {
                compare = h.CompareTo(nodeToCompare.h);
            }

            return -compare;
        }
    }
}
