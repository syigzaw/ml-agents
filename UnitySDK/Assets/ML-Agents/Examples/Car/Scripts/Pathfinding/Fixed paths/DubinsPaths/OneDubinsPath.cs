using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathfindingForCars;

namespace FixedPathAlgorithms
{
    //Help class to sort the different paths
    public class OneDubinsPath
    {
        //The total length of this path
        public float totalLength;

        //Need the individual path lengths for debugging and to find the final path
        public float length1;
        public float length2;
        public float length3;

        //The 2 tangent points we need to connect the lines and curves
        public Vector3 tangent1;
        public Vector3 tangent2;

        //The type, such as RSL
        public PathType pathType;

        //The final path data, like coordinates, headings, if reversing
        public List<Node> pathCoordinates;

        //To simplify when we add the final path
        //Are we turning or driving straight in segment 2?
        public bool path2Turning;

        //Are we turning right in the particular segment?
        public bool path1TurningRight;
        public bool path2TurningRight;
        public bool path3TurningRight;



        public OneDubinsPath(float length1, float length2, float length3, Vector3 tangent1, Vector3 tangent2, PathType pathType)
        {
            //Calculate the total length of this path
            this.totalLength = length1 + length2 + length3;

            this.length1 = length1;
            this.length2 = length2;
            this.length3 = length3;

            this.tangent1 = tangent1;
            this.tangent2 = tangent2;

            this.pathType = pathType;
        }


        //Are we turning right in any of the segments?
        public void SetIfTurningRight(bool path1TurningRight, bool path2TurningRight, bool path3TurningRight)
        {
            this.path1TurningRight = path1TurningRight;
            this.path2TurningRight = path2TurningRight;
            this.path3TurningRight = path3TurningRight;
        }
    }
}
