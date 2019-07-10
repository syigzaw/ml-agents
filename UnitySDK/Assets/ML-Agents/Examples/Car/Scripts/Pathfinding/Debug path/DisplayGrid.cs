using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathfindingForCars
{
    //Display the search grid with lines and quads
    public class DisplayGrid : MonoBehaviour
    {
        //The flat square mesh to debug the grid
        public GameObject squareObj;

        //Should we display the grid
        public bool shouldDisplayGrid;

        //The parent to get a clean workspace
        public Transform quadParent;
        //Store all flat squares
        private GameObject[,] squaresArray;
        private MeshRenderer[,] meshRenderersArray;

        //The color of the grid - black is too dark
        private Color gridColor = new Color(0.4f, 0.4f, 0.4f);



        //Add flat squares where each square is a square in the A* algorithm
        private void Start()
        {
            int width = PathfindingController.mapWidth;
            int length = PathfindingController.mapLength;

            //Add all square meshes for debugging
            squaresArray = new GameObject[length, width];

            meshRenderersArray = new MeshRenderer[length, width];

            float halfCellWidth = PathfindingController.cellWidth / 2f;

            //Fill the array with quads for debugging
            for (int x = 0; x < length; x++)
            {
                for (int z = 0; z < width; z++)
                {
                    Vector3 pos = new Vector3(x + halfCellWidth, 0.05f, z + halfCellWidth);

                    GameObject newSquare = Instantiate(squareObj, pos, squareObj.transform.rotation) as GameObject;

                    newSquare.transform.parent = quadParent;

                    newSquare.SetActive(shouldDisplayGrid);

                    squaresArray[x, z] = newSquare;

                    meshRenderersArray[x, z] = newSquare.GetComponent<MeshRenderer>();
                }
            }
        }



        //Set the color of one square
        public void ChangeSquareColor(IntVector2 cellPos, Color color)
        {
            meshRenderersArray[cellPos.x, cellPos.z].material.color = color;
        }



        //Lines have to be drawn in OnRenderObject and not in Update
        private void OnRenderObject()
        {
            if (shouldDisplayGrid)
            {
                // TODO: Refactor to include track length / width before using
                //DisplayGridWithLines();
            }
        }



        //Display the grid with lines
        private void DisplayGridWithLines()
        {
            Material lineMaterial = DebugController.current.GetLineMaterial();

            //Use this material
            //If you dont call SetPass, then you'll get basically a random material (whatever was used before) which is not good
            lineMaterial.SetPass(0);

            GL.PushMatrix();

            //Set transformation matrix for drawing to match the transform
            GL.MultMatrix(transform.localToWorldMatrix);

            //Begin drawing 3D primitives
            GL.Begin(GL.LINES);

            GL.Color(gridColor);

            float xCoord = 0f;
            float zCoord = 0f;

            //The height is actually in local coordinates
            float lineHeight = 0.02f;

            int gridSize = PathfindingController.mapWidth;

            float cellSize = PathfindingController.cellWidth;

            for (int x = 0; x <= gridSize; x++)
            {
                //x
                Vector3 lineStartX = new Vector3(xCoord, lineHeight, zCoord);

                Vector3 lineEndX = new Vector3(xCoord, lineHeight, zCoord + (gridSize * cellSize));

                //Draw the line
                GL.Vertex(lineStartX);
                GL.Vertex(lineEndX);


                //z
                Vector3 lineStartZ = new Vector3(zCoord, lineHeight, xCoord);

                Vector3 lineEndZ = new Vector3(zCoord + (gridSize * cellSize), lineHeight, xCoord);

                //Draw the line
                GL.Vertex(lineStartZ);
                GL.Vertex(lineEndZ);

                xCoord += cellSize;
            }

            GL.End();

            GL.PopMatrix();
        }
    }
}
