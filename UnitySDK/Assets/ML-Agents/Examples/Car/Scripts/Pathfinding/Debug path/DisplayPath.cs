using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace PathfindingForCars
{
    //Show the Hybrid A* and the final smooth path with line renderers
    //Show the search tree with lines 
    //Show the waypoints with lines
    public class DisplayPath : MonoBehaviour
    {
        //Line renderers
        public LineRenderer lineRendererHybridAStar;
        public LineRenderer lineRendererSmoothPath;
        //Materials - we need the colors but the parameters will be reset if we change the script
        //so easier to use materials to get colors
        public Material lineForwardColor;
        public Material lineReverseColor;

        //The y position of the debug lines - easier to see if they are not on the same height
        private float lineHeightForward = 0.02f;
        private float lineHeightReverse = 0.01f;
        private float lineHeightHybridAStarPath = 0.03f;
        private float lineHeightSmoothPath = 0.04f;

        //Save all expanded nodes so we can display them with lines
        private List<Node> expandedNodes;

        //To display the nodes
        private List<Node> smoothPath;

        //Should we display the search tree
        private bool shouldDisplaySearchTree = false;



        //Should we display the search tree
        public void ActivateDeactivateSearchTree()
        {
            shouldDisplaySearchTree = !shouldDisplaySearchTree;
        }



        //Called before we create a new path
        public void Reset()
        {
            //Set these to 0 so we dont display the old lines if we fail to find a path
            lineRendererHybridAStar.positionCount = 0;
            lineRendererSmoothPath.positionCount = 0;

            smoothPath = null;
        }



        //Display the paths from the start to the goal
        public void DisplayDebug(List<Node> finalPath, List<Node> smoothPath)
        {
            //Display the final path with a line
            DisplayOnePath(lineRendererHybridAStar, finalPath, lineHeightHybridAStarPath);

            //Display the smooth path with a line
            DisplayOnePath(lineRendererSmoothPath, smoothPath, lineHeightSmoothPath);

            this.smoothPath = smoothPath;
        }



        //Display the search tree
        public void DisplaySearchTree(List<Node> expandedNodes)
        {
            //How many nodes did we expand?
            //Debug.Log("Expanded nodes: " + expandedNodes.Count);

            //Display the Hybrid A* search tree with GL lines
            //Cant do that with a custom method because it has to be done in OnRenderObject()
            this.expandedNodes = expandedNodes;
        }



        //Display one path with a line renderer
        private void DisplayOnePath(LineRenderer lineRenderer, List<Node> path, float height)
        {
            lineRenderer.positionCount = path.Count;

            for (int index = 0; index < path.Count; index++)
            {
                Vector3 pos = path[index].carPos;
                //Need to have different heights to make the lines easier to see
                pos.y = height;

                lineRenderer.SetPosition(index, pos);
            }
        }



        //Lines have to be drawn in OnRenderObject and not in Update
        private void OnRenderObject()
        {
            //Display the waypoints with lines going straight up
            DisplayWaypoints();

            if (shouldDisplaySearchTree)
            {
                //Display the search tree with lines which is faster than using a lot of line renderers
                DisplaySearchTree();
            }
        }



        //Display the Hybrid A* search tree with lines
        private void DisplaySearchTree()
        {
            if (expandedNodes != null && expandedNodes.Count > 0)
            {
                Material lineMaterial = DebugController.current.GetLineMaterial();

                //Apply the line material
                lineMaterial.SetPass(0);

                GL.PushMatrix();

                //Set transformation matrix for drawing to match our transform
                GL.MultMatrix(transform.localToWorldMatrix);

                //Use quad to get a thicker line
                GL.Begin(GL.LINES);

                for (int i = 0; i < expandedNodes.Count; i++)
                {
                    Node thisNode = expandedNodes[i];

                    //If the previous node is not null then we cant add a line
                    //Is just the first node we add in Hybrid A*
                    if (thisNode.previousNode == null)
                    {
                        continue;
                    }

                    //Also need to change the height of the line depending on if we are going forward or reversing
                    Vector3 startPos = thisNode.carPos;
                    Vector3 endPos = thisNode.previousNode.carPos;

                    //Set color depending on if we are driving forward or reversing
                    if (thisNode.isReversing)
                    {
                        GL.Color(lineReverseColor.color);

                        startPos.y = lineHeightReverse;
                        endPos.y = lineHeightReverse;
                    }
                    else
                    {
                        GL.Color(lineForwardColor.color);

                        startPos.y = lineHeightForward;
                        endPos.y = lineHeightForward;
                    }

                    //Draw the line
                    GL.Vertex(startPos);
                    GL.Vertex(endPos);
                }

                GL.End();
                GL.PopMatrix();
            }
        }



        //Display the waypoints belonging to the smooth path with lines
        private void DisplayWaypoints()
        {
            if (smoothPath != null && smoothPath.Count > 0)
            {
                Material lineMaterial = DebugController.current.GetLineMaterial();

                //Apply the line material
                lineMaterial.SetPass(0);

                GL.PushMatrix();

                //Set transformation matrix for drawing to match our transform
                GL.MultMatrix(transform.localToWorldMatrix);

                //Use quad to get a thicker line
                GL.Begin(GL.LINES);

                //GL.Color(Color.white);

                for (int i = 0; i < smoothPath.Count; i++)
                {
                    //The line is going straight up
                    Vector3 startPos = smoothPath[i].carPos;

                    startPos.y = 0f;

                    Vector3 endPos = startPos + Vector3.up * 0.5f;

                    if (smoothPath[i].isReversing)
                    {
                        GL.Color(Color.white);
                    }
                    else
                    {
                        GL.Color(Color.black);
                    }

                    //Draw the line
                    GL.Vertex(startPos);
                    GL.Vertex(endPos);
                }

                GL.End();
                GL.PopMatrix();
            }
        }
    }
}
