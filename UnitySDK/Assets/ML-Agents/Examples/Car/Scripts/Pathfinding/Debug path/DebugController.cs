using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathfindingForCars
{
    //Takes care of all debugging
    public class DebugController : MonoBehaviour
    {
        public static DebugController current;

        //The object with all debug componets
        public GameObject debugObj;

        //Do we want to debug by displaying lines
        public bool debugOn = true;

        //Display the search tree and final path
        private DisplayPath pathDebug;
        //Display old car positions
        private DisplayOldCarPositions displayOldCarPositions;
        //The grid with quads so we can change color of them
        private DisplayGrid displayGrid;
        //Obstacles
        private ObstacleDebug obstacleDebug;

        //The line material used to display all lines, which can be the same for all
        private Material lineMaterial;



        private void Awake()
        {
            current = this;

            pathDebug = debugObj.GetComponent<DisplayPath>();

            displayOldCarPositions = debugObj.GetComponent<DisplayOldCarPositions>();

            displayGrid = debugObj.GetComponent<DisplayGrid>();

            obstacleDebug = debugObj.GetComponent<ObstacleDebug>();
        }



        //Reset
        public void Reset()
        {
            pathDebug.Reset();

            displayOldCarPositions.Reset();
        }



        //Display the final path and the smooth path
        public void DisplayFinalPath(List<Node> finalPath, List<Node> smoothPath)
        {
            pathDebug.DisplayDebug(finalPath, smoothPath);
        }



        //Display the search tree because it can be useful if we dont find a complete path
        public void DisplaySearchTree(List<Node> expandedNodes)
        {
            pathDebug.DisplaySearchTree(expandedNodes);
        }



        //Change color of one quad
        public void ChangeQuadColor(IntVector2 cellPos, Color color)
        {
            displayGrid.ChangeSquareColor(cellPos, color);
        }



        //Display which cells intersect with an obstacle
        public void DisplayCellObstacleIntersection()
        {
            obstacleDebug.DisplayCellObstacleIntersection();
        }



        //Display the show field show the distance to nearest obstacle
        public void DisplayObstacleFlowField()
        {
            obstacleDebug.DisplayObstacleFlowField();
        }



        //Create a material for the line used to display the grid
        public Material GetLineMaterial()
        {
            if (!lineMaterial)
            {
                //Unity has a built-in shader that is useful for drawing simple colored things
                Shader shader = Shader.Find("Hidden/Internal-Colored");

                lineMaterial = new Material(shader);

                //So the material is not saved anywhere
                lineMaterial.hideFlags = HideFlags.HideAndDontSave;

                //Turn on alpha blending
                //lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                //lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

                //Turn backface culling off
                //lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

                //Turn off depth writes to make it transparent
                //lineMaterial.SetInt("_ZWrite", 0);

                //If you want the lines to render "above" the object
                //lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }

            return lineMaterial;
        }
    }
}
