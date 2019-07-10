using UnityEngine;
using System.Collections;

namespace PathfindingForCars
{
    //A rectangle with 4 corners
    public struct Rectangle
    {
        public Vector3 FL, FR, BL, BR;

        public Rectangle(Vector3 FL, Vector3 FR, Vector3 BL, Vector3 BR)
        {
            this.FL = FL;
            this.FR = FR;
            this.BL = BL;
            this.BR = BR;
        }
    }
}
