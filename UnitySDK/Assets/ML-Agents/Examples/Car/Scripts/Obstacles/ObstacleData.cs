using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathfindingForCars
{
    //A help struct to better control the obstacles
    public class ObstacleData
    {
        //Center pos in 2d space
        public Vector3 centerPos;

        //The coordinates of the corners in 2d space
        public Rectangle cornerPos;


        public ObstacleData(Vector3 centerPos, Rectangle cornerPos)
        {
            this.centerPos = centerPos;

            this.cornerPos = cornerPos;
        }
    }
}
