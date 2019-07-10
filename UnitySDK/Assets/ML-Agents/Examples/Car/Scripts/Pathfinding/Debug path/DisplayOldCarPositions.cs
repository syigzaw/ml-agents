using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PathfindingForCars
{
    //Show green rectangles so we know where the car has driven and how well it follows the path
    public class DisplayOldCarPositions : MonoBehaviour
    {
        //The material used to display the lines
        public Material lineMaterial;

        //Store the car's position here so we know if it has moved
        Vector3 lastPos;

        //All old car positions
        private List<Rectangle> oldCarPositions = new List<Rectangle>();



        private void Update()
        {
            AddSquare();
        }



        //Reset the list of old postions
        public void Reset()
        {
            oldCarPositions.Clear();
        }



        //Add a square showing the car's position if it has moved
        private void AddSquare()
        {
            Transform carTrans = SimController.current.selfDrivingCar;

            //How far has the car driven since last saved position?
            float distSqr = (lastPos - carTrans.position).sqrMagnitude;

            float dist = 2f;

            if (distSqr > dist * dist)
            {
                //Find the coordinates of the car at this position
                float carWidth = 2.2f;
                float carLength = 4.7f;

                Vector3 forward = carTrans.forward * carLength * 0.5f;
                Vector3 right = carTrans.right * carWidth * 0.5f;

                Vector3 FL = carTrans.position + forward - right;
                Vector3 FR = carTrans.position + forward + right;
                Vector3 BL = carTrans.position - forward - right;
                Vector3 BR = carTrans.position - forward + right;

                //Save this position
                oldCarPositions.Add(new Rectangle(FL, FR, BL, BR));

                lastPos = carTrans.position;
            }
        }



        //Display all old positions with lines
        private void OnRenderObject()
        {
            //Apply the line material
            //If you dont call SetPass, then you'll get basically a random material (whatever was used before) which is not good
            lineMaterial.SetPass(0);

            GL.PushMatrix();

            //Set transformation matrix for drawing to match the transform
            //This will make us draw everything in local space
            //but not needed because the transform is already at 0
            //GL.MultMatrix(transform.localToWorldMatrix);

            //Draw the rectangles
            GL.Begin(GL.LINES);

            for (int i = 0; i < oldCarPositions.Count; i++)
            {
                //The rectangle 
                Rectangle rect = oldCarPositions[i];

                //Draw the rectangle
                GL.Vertex(rect.FL);
                GL.Vertex(rect.FR);

                GL.Vertex(rect.FR);
                GL.Vertex(rect.BR);

                GL.Vertex(rect.BR);
                GL.Vertex(rect.BL);

                GL.Vertex(rect.BL);
                GL.Vertex(rect.FL);
            }

            GL.End();

            GL.PopMatrix();
        }



        //Help struct to make it easier to store old rectangle positons
        //private struct Rectangle
        //{
        //    public Vector3 FL;
        //    public Vector3 FR;
        //    public Vector3 BL;
        //    public Vector3 BR;

        //    public Rectangle(Transform carTrans)
        //    {
        //        //Find the coordinates of the car at this position
        //        float carWidth = 2.2f;
        //        float carLength = 4.7f;

        //        Vector3 forward = carTrans.forward * carLength * 0.5f;
        //        Vector3 right = carTrans.right * carWidth * 0.5f;

        //        this.FL = carTrans.position + forward - right;
        //        this.FR = carTrans.position + forward + right;
        //        this.BL = carTrans.position - forward - right;
        //        this.BR = carTrans.position - forward + right;
        //    }
        //}
    }
}
