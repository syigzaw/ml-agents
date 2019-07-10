using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathfindingForCars
{
    public class ObstacleDebug : MonoBehaviour
    {
        //Its better to debug by using a single quad with a large texture than one quad for each cell
        //The quad on which we will display the potential field = dynamic programming = flow field
        public Transform quadPotentialField;
        //The quad on which we will display which cells intersects with obstacles
        public Transform quadObstacleCellIntersection;

        //Should be above the floor but below the lines
        private float quadHeight = 0.001f;



        private void Start()
        {
            //Resize the quads to fit the grid
            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            Vector3 centerPos = new Vector3(mapLength * 0.5f, quadHeight, mapWidth * 0.5f);

            Vector3 scale = new Vector3(mapLength, mapWidth, 1f);

            //Add the data to the quads
            quadPotentialField.position = centerPos;

            quadPotentialField.localScale = scale;

            quadObstacleCellIntersection.position = centerPos;

            quadObstacleCellIntersection.localScale = scale;

            //Deactivate all
            quadPotentialField.gameObject.SetActive(false);
            quadObstacleCellIntersection.gameObject.SetActive(false);
        }



        //Display which cells intersect with an obstacle
        public void DisplayCellObstacleIntersection()
        {
            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            bool[,] obstaclesArray = ObstaclesController.isObstacleInCell;

            //Create a texture on which we will display the information
            Texture2D debugTexture = GenerateNewDebugTexture();

            //Add colors to the texture
            //More efficient to generate the colors once and then add the array to the texture
            Color[] colors = new Color[mapLength * mapWidth];

            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    Color thisColor = Color.white;

                    //Obstacle
                    if (obstaclesArray[x, z])
                    {
                        thisColor = Color.red;
                    }

                    colors[z * mapWidth + x] = thisColor;
                }
            }

            debugTexture.SetPixels(colors);

            debugTexture.Apply();

            //Add the texture to the quad
            quadObstacleCellIntersection.GetComponent<MeshRenderer>().material.mainTexture = debugTexture;
        }



        //Create a texture on which we will display the information
        private Texture2D GenerateNewDebugTexture()
        {
            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            //Create a texture on which we will display the information
            Texture2D debugTexture = new Texture2D(mapLength, mapWidth);

            //Change texture settings to make it look better
            debugTexture.filterMode = FilterMode.Point;

            debugTexture.wrapMode = TextureWrapMode.Clamp;

            return debugTexture;
        }



        //Display the flow field showing the distance to the closest obstacle
        public void DisplayObstacleFlowField()
        {
            int mapLength = PathfindingController.mapLength;
            int mapWidth = PathfindingController.mapWidth;

            bool[,] obstaclesArray = ObstaclesController.isObstacleInCell;

            //Create a texture on which we will display the information
            Texture2D debugTexture = GenerateNewDebugTexture();

            //Debug flow field by changing the color of the cells 
            //To display the grid with a grayscale, we need the max distance to the node furthest away
            int maxDistance = 0;
            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    int distance = ObstaclesController.distanceToClosestObstacle[x, z];

                    if (distance > maxDistance && distance < System.Int32.MaxValue)
                    {
                        maxDistance = distance;
                    }
                }
            }

            //Debug.Log("Max distance flow field: " + maxDistance);


            //Generate the colors
            //More efficient to generate the colors once and then add the array to the texture
            Color[] colors = new Color[mapLength * mapWidth];

            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    Color thisColor = Color.black;

                    //Obstacle
                    if (obstaclesArray[x, z])
                    {
                        thisColor = Color.red;
                    }
                    //Grayscale
                    else
                    {
                        int distance = ObstaclesController.distanceToClosestObstacle[x, z];

                        //If this is not an obstacle or a blocked node
                        if (distance < System.Int32.MaxValue)
                        {
                            float rgb = 1f - ((float)distance / (float)maxDistance);

                            thisColor = new Vector4(rgb, rgb, rgb, 1.0f);
                        }
                        //Inaccessible
                        else
                        {
                            thisColor = Color.blue;
                        }
                    }


                    colors[z * mapWidth + x] = thisColor;
                }
            }

            debugTexture.SetPixels(colors);

            debugTexture.Apply();



            quadPotentialField.GetComponent<MeshRenderer>().material.mainTexture = debugTexture;



            //for (int x = 0; x < mapLength; x++)
            //{
            //    for (int z = 0; z < mapWidth; z++)
            //    {
            //        int distance = ObstaclesController.distanceToObstacles[x, z];

            //        //If this is not an obstacle or a blocked node
            //        if (distance < System.Int32.MaxValue)
            //        {
            //            float rgb = 1f - ((float)distance / (float)maxDistance);

            //            Color grayScale = new Vector4(rgb, rgb, rgb, 1.0f);

            //            DebugController.current.ChangeQuadColor(new IntVector2(x, z), grayScale);
            //        }
            //    }
            //}
        }



        //Active/deactivate the flow field
        public void ActivateDeactivateFlowField()
        {
            if (quadPotentialField.gameObject.activeInHierarchy)
            {
                quadPotentialField.gameObject.SetActive(false);
            }
            else
            {
                quadPotentialField.gameObject.SetActive(true);
            }
        }
    }
}
