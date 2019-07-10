using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathfindingForCars
{
    //Will make it easier if we have several self-driving cars than to contact the self-driving car directly
    public class SimController : MonoBehaviour
    {
        public static SimController current;

        //The self-driving car
        public Transform selfDrivingCar;

        private CarController carController;



        void Start()
        {
            current = this;

            carController = selfDrivingCar.GetComponent<CarController>();
        }



        void Update()
        {

        }



        //
        // Set and get
        //
        public void SendPathToActiveCar(List<Node> wayPoints)
        {
            carController.SendPathToCar(wayPoints);
        }

        //Get data such as speed, length, etc
        public CarData GetActiveCarData()
        {
            return carController.GetCarData();
        }

        //Stop the active car from driving
        public void StopCar()
        {
            SendPathToActiveCar(null);

            carController.StopCar();
        }
    }
}
