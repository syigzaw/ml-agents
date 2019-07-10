using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;

namespace PathfindingForCars
{
    public class EgoCarInterface : Agent
    {
        private Vector3 targetPosition;
        private float targetHeading;
        private float targetSpeed;
        private float startingXPos = 15;
        private float currentXPos = 15;
        private float oldActionX = 0;
        private float oldActionZ = 0;
        private Vector3 target;
        private bool finished = false;
        private float wins = 0;
        private float loses = 0;

        //Reference to the car data belonging to this car
        private CarData carData;

        // Map coordinates
        private int mapLength = PathfindingController.mapLength;
        private int mapWidth = PathfindingController.mapWidth;

        // Lidar detection parameters
        private int numLineSegments = 100;
        private float detectionRadius = 20f;
        LineRenderer line;

        private int resetCount = 0;
        private int agentResetCount = 0;
        private GameObject Pathfinding;

        // Use this for initialization
        void Start()
        {
            carData = GetComponent<CarData>();
            Pathfinding = GameObject.Find("Pathfinding");

            //Draw the lidar visualization
            line = gameObject.GetComponent<LineRenderer>();
            line.SetVertexCount(numLineSegments + 1);
            line.useWorldSpace = false;
            DrawLidar();
            Vector3 target = new Vector3(100, 0, 25);
        }

        // Update is called once per frame
        void Update()
        {
            SetTargetPosition(target);
            // AddReward(-0.1f);
            // resetCount = resetCount + 1;
            // if ((transform.position - GetTargetPosition()).sqrMagnitude < 5) {
            //     resetCount = 0;
            // }
            // Debug.Log(resetCount);
                // RequestDecision();
            // } else if (resetCount > 20) { // CHANGE THIS!!!
            //     Debug.Log("Running In Circles");
            //     resetCount = 0;
            //     RequestDecision();
            // }
            if (transform.position.x >= mapLength - 10) {
                Debug.Log("Finished");
                // resetCount = 0;
                SetReward(1.0f);
                finished = true;
                Done();
            } else if (
                GetTargetPosition().x < 10 || 
                GetTargetPosition().z < 10 ||
                GetTargetPosition().z > 40) {
                Debug.Log("Out Of Bounds");
                // resetCount = 0;
                SetReward(-1.0f);
                finished = false;
                Done();
            }
        }

        public Transform GetCurrentTransform()
        {
            return carData.GetCarTransform();
        }

        public Vector3 GetCurrentPosition()
        {
            return carData.GetRearWheelPos();
        }

        public float GetCurrentHeading()
        {
            return carData.GetHeading();
        }

        private float GetCurrentSpeed()
        {
            return carData.GetSpeed();
        }

        public void SetTargetPosition(Vector3 position)
        {
            targetPosition = position;
        }

        public void SetTargetHeading(float heading)
        {
            targetHeading = heading;
        }

        private void SetTargetSpeed(float speed)
        {
            targetSpeed = speed;
        }

        public Vector3 GetTargetPosition()
        {
            return targetPosition;
        }

        public float GetTargetHeading()
        {
            return targetHeading;
        }

        public float GetTargetSpeed()
        {
            return targetSpeed;
        }

        public override void AgentReset()
        {
            agentResetCount = agentResetCount + 1;
            Debug.Log(string.Format("Agent Reset: {0}", agentResetCount));
            Pathfinding.GetComponent<ObstaclesController>().InitObstacles();
            transform.position = new Vector3(startingXPos, 0, 25);
            transform.eulerAngles = new Vector3(0, 90, 0);

            //Reset target position
            targetPosition = transform.position;
            Debug.Log(transform.position);
            if (finished) {
                wins = wins + 1;
            } else if (transform.position.x > 30) {
                loses = loses + 1;
            }
            Debug.Log(wins);
            Debug.Log(loses);
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log("Collision detected");
            SetReward(-1.0f);
            finished = false;
            Done();
        }

        //Search through all obstacles to find which fall within a radius of the car
        public List<ObstacleData> DetectObstaclesWithinRadiusOfCar()
        {
            //The list with close obstacles
            List<ObstacleData> closeObstacles = new List<ObstacleData>();

            //The list with all obstacles in the map
            List<ObstacleData> allObstacles = ObstaclesController.obstaclesPosList;

            //Find close obstacles
            for (int i = 0; i < allObstacles.Count; i++)
            {
                float distSqr = (transform.position - allObstacles[i].centerPos).sqrMagnitude;

                //Add to the list of close obstacles if close enough
                if (distSqr < detectionRadius * detectionRadius)
                {
                    closeObstacles.Add(allObstacles[i]);
                }
            }

            return closeObstacles;
        }

        //Search through all obstacles to find which fall within a radius of the car
        public List<ObstacleData> DetectNClosestObstacles(int N)
        {
            //The list with close obstacles
            List<ObstacleData> closeObstacles = new List<ObstacleData>();

            //The list with all obstacles in the map
            List<ObstacleData> allObstacles = ObstaclesController.obstaclesPosList;

            //Find close obstacles
            for (int i = 0; i < allObstacles.Count; i++)
            {
                closeObstacles.Add(allObstacles[i]);
            }
            List<ObstacleData> firstNClosestObstacles = closeObstacles
                .OrderBy(obs => (transform.position - obs.centerPos).magnitude)
                .Take(N)
                .ToList();

            return firstNClosestObstacles;
        }

        // Draw circle around car representing detection range of Lidar sensor
        private void DrawLidar()
        {
            float x;
            float y;
            float z;

            float angle = 20f;

            for (int i = 0; i < (numLineSegments + 1); i++)
            {
                x = Mathf.Sin(Mathf.Deg2Rad * angle) * detectionRadius;
                z = Mathf.Cos(Mathf.Deg2Rad * angle) * detectionRadius;

                line.SetPosition(i, new Vector3(x, 0, z));

                angle += (360f / numLineSegments);
            }
        }

        public override void CollectObservations() {
            Debug.Log("Collecting Observations");
            AddVectorObs(transform.position.x/mapLength);
            AddVectorObs(transform.position.z/mapWidth);

            int numOfObstaclesDetected = 5;
            List<ObstacleData> obstacleList = DetectNClosestObstacles(numOfObstaclesDetected);
            for (int i = 0; i < obstacleList.Count; i++) {
                AddVectorObs((obstacleList[i].centerPos.x - transform.position.x)/mapLength);
                AddVectorObs((obstacleList[i].centerPos.z - transform.position.z)/mapWidth);
                AddVectorObs((obstacleList[i].cornerPos.BL.x - transform.position.x)/mapLength);
                AddVectorObs((obstacleList[i].cornerPos.FR.x - transform.position.x)/mapLength);
                AddVectorObs((obstacleList[i].cornerPos.BL.z - transform.position.z)/mapWidth);
                AddVectorObs((obstacleList[i].cornerPos.FR.z - transform.position.z)/mapWidth);
            }
            if (numOfObstaclesDetected > obstacleList.Count) {
                for (int i = 0; i < numOfObstaclesDetected - obstacleList.Count; i++) {
                    AddVectorObs(0);
                    AddVectorObs(0);
                }
            }
        }

        public override void AgentAction(float[] vectorAction, string textAction) {
            if (vectorAction[0] != oldActionX || vectorAction[1] != oldActionZ) {
                Debug.Log("Acting");
                float norm = Mathf.Sqrt(Mathf.Pow(vectorAction[0], 2) + Mathf.Pow(vectorAction[1], 2));
                target = new Vector3(
                    Mathf.Clamp(transform.position.x + vectorAction[0]*20/norm, 0, mapLength), 
                    0, 
                    transform.position.z + vectorAction[1]*20/norm);
                // SetTargetPosition(waypoint);
                oldActionX = vectorAction[0];
                oldActionZ = vectorAction[1];
            }
            SetReward(transform.position.x - currentXPos);
            // Debug.Log(string.Format("Reward: {0}", transform.position.x - currentXPos));
            currentXPos = transform.position.x;
            // Debug.Log(transform.position);
            // Debug.Log(GetTargetPosition());
        }
    }
}
