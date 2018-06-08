using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class BoneContact : MonoBehaviour
{
    // [HideInInspector]
    public DogAgent agent;


    /// <summary>
    /// Check for collision with ground, and optionally penalize agent.
    /// </summary>
    void OnCollisionEnter(Collision other)
    {
        if(other.transform.CompareTag("bone")) //isa dog
		{
            if(!agent.hasBone)
            {
                agent.shouldPickUpBone = true;
                agent.TouchedTarget();
                // agent.PickUpBone();
            }
		}

        // if(agent.shouldReturnTheBone && other.transform.CompareTag("bone")) //isa dog
		// {
        //     if(!agent.hasBone)
        //     {
        //         agent.shouldPickUpBone = true;
        //         agent.TouchedTarget();
        //         // agent.PickUpBone();
        //     }
		// }
    }

}
