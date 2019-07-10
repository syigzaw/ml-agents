using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace PathfindingForCars
{
    [System.Serializable]
    public class AxleInfo
    {
        public WheelCollider leftWheel;
        public WheelCollider rightWheel;
        //Is this wheel powered by a motor?
        public bool motor;
        //Can this wheel steer?
        public bool steering;
    }



    //From http://docs.unity3d.com/Manual/WheelColliderTutorial.html
    public class CarController : MonoBehaviour
    {
        public List<AxleInfo> axleInfos;

        //An nimation curve that will deterine the wheel angle as a function of speed
        public AnimationCurve wheelAngleCurve;

        //Speed calculations
        private float currentSpeed = 0f;
        private Vector3 lastPosition = Vector3.zero;

        //Reference to the PID controller
        private PIDController PIDScript;
        //Reference to the script that makes the car follow a path
        private FollowPath followPathScript;
        //Reference to the car data belonging to this car
        private CarData carData;

        //Driving modes
        private enum CarMode { Forward, Reverse, Stop };

        private CarMode carMode = CarMode.Stop;

        //Average the steering angles to simulate the time it takes to turn the wheels
        private float averageSteeringAngle = 0f;



        void Start()
        {
            //Move the center of mass down
            Rigidbody carRB = GetComponent<Rigidbody>();

            carRB.centerOfMass = carRB.centerOfMass - new Vector3(0f, 0.8f, 0f);

            PIDScript = GetComponent<PIDController>();

            carData = GetComponent<CarData>();

            followPathScript = GetComponent<FollowPath>();
        }



        void FixedUpdate()
        {
            AddMotorAndSteering();
            CalculateSpeed();
        }



        void AddMotorAndSteering()
        {
            //Manual control
            //float motorTorque = maxMotorTorque * Input.GetAxis("Vertical");
            //float steeringAngle = CalculateSteerAngle() * Input.GetAxis("Horizontal");

            float steeringAngle = 0f;
            float motorTorque = 0f;
            float brakeTorque = 0f;

            //Self-driving control
            if (carMode == CarMode.Forward && currentSpeed < carData.GetMaxSpeed())
            {
                motorTorque = carData.GetMaxMotorTorque();

                //Get the steering angle for the steering wheels
                //Has to be in either forward or reverse, because we need a path to
                //get the steering angle
                steeringAngle = GetSteeringAngle();
            }
            else if (carMode == CarMode.Reverse)
            {
                //Reversing is slower
                motorTorque = -carData.GetMaxMotorTorque() * 0.5f;

                //Get the steering angle for the steering wheels
                steeringAngle = GetSteeringAngle();
            }
            //Stop
            else
            {
                brakeTorque = carData.GetMaxBrakeTorque();
            }



            //Add everything to the wheels
            foreach (AxleInfo axleInfo in axleInfos)
            {
                if (axleInfo.steering)
                {
                    axleInfo.leftWheel.steerAngle = steeringAngle;
                    axleInfo.rightWheel.steerAngle = steeringAngle;
                }
                if (axleInfo.motor)
                {
                    axleInfo.leftWheel.motorTorque = motorTorque;
                    axleInfo.rightWheel.motorTorque = motorTorque;
                }

                axleInfo.leftWheel.brakeTorque = brakeTorque;
                axleInfo.rightWheel.brakeTorque = brakeTorque;

                //Make to wheel meshes rotate and move from suspension
                ApplyLocalPositionToVisuals(axleInfo.leftWheel);
                ApplyLocalPositionToVisuals(axleInfo.rightWheel);
            }
        }



        //Make the wheel meshes rotate and move from suspension
        public void ApplyLocalPositionToVisuals(WheelCollider collider)
        {
            if (collider.transform.childCount == 0)
            {
                return;
            }

            //Get the wheel mesh which is the only child to the wheel collider
            Transform visualWheel = collider.transform.GetChild(0);

            Vector3 position;
            Quaternion rotation;
            collider.GetWorldPose(out position, out rotation);

            visualWheel.transform.position = position;
            visualWheel.transform.rotation = rotation;
        }



        //Calculate the current speed in km/h
        private void CalculateSpeed()
        {
            //First calculate the distance of the transform between the fixedupdate calls
            //Now you know the m/fixedupdate
            //Divide by Time.deltaTime to get m/s
            //Multiply with 3.6 to get km/h
            currentSpeed = ((transform.position - lastPosition).magnitude / Time.deltaTime) * 3.6f;

            //Save the position for the next update
            lastPosition = transform.position;

            //Debug.Log(currentSpeed);
        }



        //Used if we don't have a PID controller
        //float CalculateAutomaticSteering()
        //{
        //    Vector3 currentWaypoint = pathControllerScript.GetWayPointPos(pathControllerScript.currentWayPointIndex);

        //    //Transforms the waypoint's position from world space to the car's local space
        //    //Like if the waypoint wiuld be the child of the car
        //    //The height of the waypoint doesn't matter
        //    Vector3 steerVector = transform.InverseTransformPoint(new Vector3(
        //        currentWaypoint.x,
        //        transform.position.y,
        //        currentWaypoint.z
        //        ));

        //    //Will define if we should steer left or right to get to the waypoint
        //    //Is 1 if the wp is to the right of the car
        //    //Is -1 if the wp is to the left of the car
        //    //Is 0 if the wp is infront or at the back of the car
        //    float steering = steerVector.x / steerVector.magnitude;

        //    return steering;
        //}



        //Limit steering when going fast because its not realistic
        //to steer with an angle of 45 degrees at 200 km/h
        float GetSteeringAngle()
        {
            float steeringAngle = 0f;

            //Without PID controller
            //steeringAngle = CalculateSteerAngle() * CalculateAutomaticSteering();

            //With PID controller
            steeringAngle = PIDScript.GetSteerAngle();

            //Make sure to clamp the steering angle because it depends on the speed
            float maxSteeringAngle = CalculateSteerAngle();

            steeringAngle = Mathf.Clamp(steeringAngle, -maxSteeringAngle, maxSteeringAngle);

            if (carMode == CarMode.Reverse)
            {
                steeringAngle *= -1f;
            }


            //Average the steering angles to simulate that it takes some time to turn the steering wheels
            //http://www.bennadel.com/blog/1627-create-a-running-average-without-storing-individual-values.htm
            float averageAmount = 15f;

            averageSteeringAngle = ((averageSteeringAngle * averageAmount) + steeringAngle) / (averageAmount + 1f);


            return averageSteeringAngle;
        }



        //A fast car can't turn the wheels as much as it can when driving slower
        float CalculateSteerAngle()
        {
            float lowSpeedSteerAngle = 45f;
            float highSpeedSteerAngle = 2f;

            ////Exponential function y = Math.Pow(ab,x)
            ////y - angle
            ////x - speed of car
            //float b = Mathf.Pow(highSpeedSteerAngle / lowSpeedSteerAngle, 1f / carData.GetMaxSpeed());

            //return lowSpeedSteerAngle * Mathf.Pow(b, Mathf.Abs(currentSpeed));

            float speedFactor = Mathf.Abs(currentSpeed) / carData.GetMaxSpeed();

            float wheelAngle = highSpeedSteerAngle + wheelAngleCurve.Evaluate(1f - speedFactor) * lowSpeedSteerAngle;

            return wheelAngle;

            //return 40f;
        }



        //
        // Set and get methods
        //

        public void StopCar()
        {
            carMode = CarMode.Stop;
        }

        public void MoveCarForward()
        {
            carMode = CarMode.Forward;
        }

        public void MoveCarReverse()
        {
            carMode = CarMode.Reverse;
        }

        public float GetCarSpeed()
        {
            return currentSpeed;
        }

        public CarData GetCarData()
        {
            return carData;
        }

        public void SendPathToCar(List<Node> wayPoints)
        {
            followPathScript.SetPath(wayPoints);
        }
    }
}
