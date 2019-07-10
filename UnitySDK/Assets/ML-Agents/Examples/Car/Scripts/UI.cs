using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PathfindingForCars
{
    //Takes care of all user input
    public class UI : MonoBehaviour
    {
        //Drags
        public GameObject debugObj;
        public GameObject menuObj;
        public Text speedText;

        private DisplayGrid displayGrid;

        private ObstacleDebug obstacleDebug;

        private DisplayPath displayPath;



        void Start()
        {
            displayGrid = debugObj.GetComponent<DisplayGrid>();

            obstacleDebug = debugObj.GetComponent<ObstacleDebug>();

            displayPath = debugObj.GetComponent<DisplayPath>();

            menuObj.SetActive(false);

            StartCoroutine(UpdateSpeedText());
        }



        void Update()
        {
            //Quit the program when pressing escape key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }

            //Grid on/off
            if (Input.GetKeyDown(KeyCode.G))
            {
                displayGrid.shouldDisplayGrid = !displayGrid.shouldDisplayGrid;
            }

            //Flow field on/off
            if (Input.GetKeyDown(KeyCode.F))
            {
                obstacleDebug.ActivateDeactivateFlowField();
            }

            //Search tree on/off
            if (Input.GetKeyDown(KeyCode.T))
            {
                displayPath.ActivateDeactivateSearchTree();
            }

            //Show hide menu
            if (Input.GetKeyDown(KeyCode.K))
            {
                if (menuObj.activeInHierarchy)
                {
                    menuObj.SetActive(false);
                }
                else
                {
                    menuObj.SetActive(true);
                }
            }
        }



        //Dont need to update speed every frame
        private IEnumerator UpdateSpeedText()
        {
            while (true)
            {
                if (SimController.current.GetActiveCarData() != null)
                {
                    //Get the speed in km/h
                    float speed = SimController.current.GetActiveCarData().GetSpeed();

                    //Round
                    int speedInt = Mathf.RoundToInt(speed);

                    //Display the speed
                    speedText.text = speedInt + " km/h";
                }
            
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
