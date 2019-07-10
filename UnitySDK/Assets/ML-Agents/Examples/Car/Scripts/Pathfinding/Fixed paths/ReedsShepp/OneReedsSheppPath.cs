using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathfindingForCars;

namespace FixedPathAlgorithms
{
    //Help class to sort the different paths
    public class OneReedsSheppPath
    {
        //The total length of this path so we can find the shortest of all paths
        public float totalLength;

        //Data that belongs to each segment of this path
        public List<OneReedsSheppSegment> segmentsList;

        //The final path data, with waypoint coordinates, 
        //headings for collision detection, and if reversing
        public List<Node> pathCoordinates;

        //Save these for debugging
        //Tangents - not always 3
        public Vector3 startTangent;
        public Vector3 middleTangent;
        public Vector3 goalTangent;
        //Circle pos - not always needed
        public Vector3 middleCircleCoordinate;
        public Vector3 middleCircleCoordinate2;


        //Init
        public OneReedsSheppPath(int segments)
        {
            this.segmentsList = new List<OneReedsSheppSegment>();

            //Add all the segments we will need
            for (int i = 0; i < segments; i++)
            {
                this.segmentsList.Add(new OneReedsSheppSegment());
            }
        }



        public void AddIfTurningLeft(params bool[] isTurningLeftArray)
        {
            for (int i = 0; i < isTurningLeftArray.Length; i++)
            {
                this.segmentsList[i].isTurningLeft = isTurningLeftArray[i];
            }
        }



        public void AddIfReversing(params bool[] isReversingArray)
        {
            for (int i = 0; i < isReversingArray.Length; i++)
            {
                this.segmentsList[i].isReversing = isReversingArray[i];
            }
        }



        public void AddIfTurning(params bool[] isTurningArray)
        {
            for (int i = 0; i < isTurningArray.Length; i++)
            {
                this.segmentsList[i].isTurning = isTurningArray[i];
            }
        }



        //Add the lengths of each segment and calculate the length of the entire path
        public void AddPathLengths(params float[] lengthsArray)
        {
            this.totalLength = 0f;

            for (int i = 0; i < segmentsList.Count; i++)
            {
                this.segmentsList[i].pathLength = lengthsArray[i];

                //Calculate the total length of this path
                this.totalLength += lengthsArray[i];
            }
        }
    }
}
