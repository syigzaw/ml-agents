using UnityEngine;
using System.Collections;

namespace FixedPathAlgorithms
{
    public class OneReedsSheppSegment
    {
        //Are we turning?
        public bool isTurning;
        //Are we turning left?
        public bool isTurningLeft;
        //Are we reversing?
        public bool isReversing;
        //The length of this segment
        public float pathLength;
    }
}
