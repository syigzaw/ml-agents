using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathfindingForCars
{
    //Stores specific car's data, so we can have several different cars
    public class CarData : MonoBehaviour
    {
        //An empty game object located where the rear wheels are
        //Needed when reversing, to make it easier for the car to follow the path
        public Transform rearWheelTrans;

        //Engine power
        private float maxMotorTorque = 5000f;
        //Top speed in km/h
        private float maxSpeed = 200f;
        //Brakes
        private float maxBrakeTorque = 800f;
        //Distance between the front and rear wheels
        private float wheelBase = 2.959f;
        //Length of the entire car
        private float carLength = 4.976f;
        //Real width is 1.963 but we need to add a little safety and include rear view mirrors
        private float carWidth = 2.4f;
        //Model S says on their website that the turning circle is 37 feet, which is the radius 
        //But this is when dricing slow, so should maybe be higher here?
        private float turningRadius = 11.2776f;
        //The distance to the rear wheels from the center
        private float distCenterToRearWheels;

        //Needed so we can get the car's current speed because we are calculating the speed in that script
        private CarController carController;



        private void Start()
        {
            carController = GetComponent<CarController>();

            Vector3 carCenter = transform.position;

            //Make sure they have the same y
            carCenter.y = rearWheelTrans.position.y;

            distCenterToRearWheels = Vector3.Magnitude(carCenter - rearWheelTrans.position);

            //print(distCenterToRearWheels);
        }



        //
        // Get methods
        //
        public Vector3 GetRearWheelPos()
        {
            return rearWheelTrans.position;
        }

        public float GetHeading()
        {
            return transform.eulerAngles.y;
        }

        public float GetWheelBase()
        {
            return wheelBase;
        }

        public float GetLength()
        {
            return carLength;
        }

        public float GetWidth()
        {
            return carWidth;
        }

        public float GetTurningRadius()
        {
            return turningRadius;
        }

        public float GetSpeed()
        {
            return carController.GetCarSpeed();
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public float GetDistanceToRearWheels()
        {
            return distCenterToRearWheels;
        }

        public float GetMaxMotorTorque()
        {
            return maxMotorTorque;
        }

        public float GetMaxSpeed()
        {
            return maxSpeed;
        }

        public float GetMaxBrakeTorque()
        {
            return maxBrakeTorque;
        }

        public Transform GetCarTransform()
        {
            return transform;
        }
    }
}
