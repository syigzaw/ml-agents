using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PathfindingForCars
{
    //Implementation of the PID controller from "Artificial Intelligence for Robotics"
    public class PIDController : MonoBehaviour
    {
        FollowPath pathControllerScript;

        //What the PID controller needs to save each frame
        float CTE_old = 0f;
        float CTE_sum = 0f;

        //A list with old errors to remove old errors from the sum
        List<float> oldCTE = new List<float>();

        //Twiddle
        //The target parameters
        float[] p = new float[] { 0f, 0f, 0f };

        //The potential changes to the target parameter
        float[] dp = new float[] { 5f, 0.001f, 15f };

        //Keep track of the best error
        float best_error;



        void Start()
        {
            pathControllerScript = GetComponent<FollowPath>();

            //best_error = Mathf.Abs(pathControllerScript.CalculateCTE());

            //StartCoroutine(UpdateTwiddle());
        }



        //Calculate then tau's with a Twiddle algorithm as the car is moving
        IEnumerator UpdateTwiddle()
        {
            float tol = 0.002f;

            WaitForSeconds waitUpdateTime = new WaitForSeconds(0.05f);
            WaitForSeconds waitBeforeTime = new WaitForSeconds(0.5f);

            //Calculate the average CTE over several updates
            int updates = 5;

            while (true)
            {
                float sum_dp = dp[0] + dp[1] + dp[2];

                if (sum_dp > tol)
                {
                    //Debug.Log("Trying to optimize taus!");

                    //Loop through all taus
                    for (int i = 0; i < p.Length; i++)
                    {
                        p[i] += dp[i];

                        //Wait some time before we start to measure
                        yield return waitBeforeTime;

                        //Measure the average error
                        float err = 0f;
                        for (int j = 0; j < updates; j++)
                        {
                            //Now we need to wait so we can measure a new error because
                            //the car has to move first
                            yield return waitUpdateTime;

                            err += Mathf.Abs(pathControllerScript.CalculateCTE());
                        }

                        err /= updates;

                        if (err < best_error)
                        {
                            best_error = err;
                            dp[i] *= 1.1f;
                        }
                        else
                        {
                            //Do it twice because we added it before
                            p[i] -= 2f * dp[i];

                            yield return waitBeforeTime;

                            //Measure the error
                            err = 0f;
                            for (int j = 0; j < updates; j++)
                            {
                                yield return waitUpdateTime;

                                err += Mathf.Abs(pathControllerScript.CalculateCTE());
                            }

                            err /= updates;

                            if (err < best_error)
                            {
                                best_error = err;
                                dp[i] *= 1.1f;
                            }
                            else
                            {
                                p[i] += dp[i];
                                dp[i] *= 0.9f;
                            }
                        }
                    }
                }

                //Display the optimized tau from Twiddle
                //string tau_P = p[0].ToString() + " ";
                //string tau_I = p[1].ToString() + " ";
                //string tau_D = p[2].ToString() + " ";

                //Debug.Log(tau_P + tau_I + tau_D);

                //Restart the loop
                yield return null;
            }
        }



        //Calculate the steer angle alpha by using a PDI controller
        public float GetSteerAngle()
        {
            //Get the cross track error, which is the distance to the position the car should be at
            float CTE = pathControllerScript.CalculateCTE();

            //Figure out the PID values on your own
            float tau_P = 70f;
            float tau_I = 0.01f;
            float tau_D = 50f;

            //Get the optimized tau from Twiddle
            //float tau_P = p[0];
            //float tau_I = p[1];
            //float tau_D = p[2];

            //The steering angle
            //Will be limited before it reaches the wheels so the angle is not too big
            float alpha = 0f;


            //P
            alpha = -tau_P * CTE;


            //I
            CTE_sum += Time.deltaTime * CTE;

            //It is sometimes that you just add the last errors and not all errors since the beginning of time
            oldCTE.Add(Time.deltaTime * CTE);

            //Why 1000? Why not?
            if (oldCTE.Count > 1000)
            {
                //Remove the first
                CTE_sum -= oldCTE[0];

                //Remove it from the list
                oldCTE.RemoveAt(0);
            }

            alpha -= tau_I * CTE_sum;


            //D
            float d_dt_CTE = (CTE - CTE_old) / Time.deltaTime;

            alpha -= tau_D * d_dt_CTE;

            CTE_old = CTE;


            return alpha;
        }
    }
}
