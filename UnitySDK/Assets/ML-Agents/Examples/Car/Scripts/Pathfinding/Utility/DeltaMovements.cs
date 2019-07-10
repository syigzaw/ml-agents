using UnityEngine;
using System.Collections;

namespace PathfindingForCars
{
    //The directions we can move when finding surrounding cells to a cell
    public class DeltaMovements
    {
        //No corners
        public static IntVector2[] delta = {
                   new IntVector2(0, 1),
                   new IntVector2(1, 0),
                   new IntVector2(0, -1),
                   new IntVector2(-1, 0)
                   };

        //With corners
        public static IntVector2[] deltaWithCorners = {
                   new IntVector2(0, 1),
                   new IntVector2(1, 0),
                   new IntVector2(0, -1),
                   new IntVector2(-1, 0),
                   new IntVector2(1, 1),
                   new IntVector2(-1, -1),
                   new IntVector2(1, -1),
                   new IntVector2(-1, 1)
                   };
    }
}
