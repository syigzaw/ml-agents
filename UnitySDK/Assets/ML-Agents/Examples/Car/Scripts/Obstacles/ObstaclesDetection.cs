using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace PathfindingForCars
{
    //Takes care of all obstacle detection related to Hybrid A*
    public static class ObstaclesDetection
    {
        //Test if one path is drivable
        public static bool IsFixedPathDrivable(List<Node> path, CarData carData)
        {
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 carPos = path[i].carPos;

                float carHeading = path[i].heading;

                if (HasCarInvalidPosition(carPos, carHeading, carData))
                {
                    //This path is not drivable
                    return false;
                }

            }

            //This path is drivable
            return true;
        }



        //Check if the car is colliding with an obstacle or is outside of map
        public static bool HasCarInvalidPosition(Vector3 carRearWheelPos, float heading, CarData carData)
        {
            bool hasInvalidPosition = false;

            //Make the car bigger than it is to be on the safe side
            float marginOfSafety = 0.5f;

            float carLength = carData.GetLength() + marginOfSafety;
            float carWidth = carData.GetWidth() + marginOfSafety;


            //Find the center pos of the car (carPos is at the rearWheels)
            float distCenterToRearWheels = carData.GetDistanceToRearWheels();

            float xCenter = carRearWheelPos.x + distCenterToRearWheels * Mathf.Sin(heading);
            float zCenter = carRearWheelPos.z + distCenterToRearWheels * Mathf.Cos(heading);

            Vector3 carPos = new Vector3(xCenter, carRearWheelPos.y, zCenter);


            //
            // Step 1. Check if the car's corners is inside of the map
            //

            //Find all corners of the car
            Rectangle carCornerPos = SkeletonCar.GetCornerPositions(carPos, heading, carWidth, carLength);

            //Detect if any of the corners is outside of the map
            if (
                !PathfindingController.IsPositionWithinGrid(carCornerPos.FL) ||
                !PathfindingController.IsPositionWithinGrid(carCornerPos.FR) ||
                !PathfindingController.IsPositionWithinGrid(carCornerPos.BL) ||
                !PathfindingController.IsPositionWithinGrid(carCornerPos.BR))
            {
                //At least one of the corners is outside of the map
                hasInvalidPosition = true;

                return hasInvalidPosition;
            }



            //
            // Step 2. Check if the car's center position is far away from an obstacle
            //
            //We dont need to check if the car is colliding with an obstacle if the distance to an obstacle is great
            IntVector2 cellPos = PathfindingController.ConvertCoordinateToCellPos(carPos);

            //The car is not colliding with anything if the steps to an obstacle is greater than the length of the car
            if (ObstaclesController.distanceToClosestObstacle[cellPos.x, cellPos.z] > carData.GetLength())
            {
                //This is a valid position
                hasInvalidPosition = false;

                return hasInvalidPosition;
            }



            //
            // Step 3. Check if the car is hitting an obstacle
            //

            //Find all corners of the car
            Rectangle carCornerPosFat = SkeletonCar.GetCornerPositions(carPos, heading, carWidth, carLength);

            //Method 1 - Use the car's corners and then rectangle-rectangle-intersection with the obstacles
            hasInvalidPosition = ObstacleDetectionCorners(carPos, carCornerPosFat);

            //Method 2 - Approximate the car with circles
            //hasInvalidPosition = ObstacleDetectionCircles(carCenterPos, heading, carData, carCornerPosFat);       


            return hasInvalidPosition;
        }


        //Check if the car outside of map (MATT)
        public static bool TargetPositionWithinTrack(Vector3 carRearWheelPos, float heading, CarData carData)
        {
            bool withinTrack = true;

            //Make the car bigger than it is to be on the safe side
            float marginOfSafety = 0.5f;

            float carLength = carData.GetLength() + marginOfSafety;
            float carWidth = carData.GetWidth() + marginOfSafety;


            //Find the center pos of the car (carPos is at the rearWheels)
            float distCenterToRearWheels = carData.GetDistanceToRearWheels();

            float xCenter = carRearWheelPos.x + distCenterToRearWheels * Mathf.Sin(heading);
            float zCenter = carRearWheelPos.z + distCenterToRearWheels * Mathf.Cos(heading);

            Vector3 carPos = new Vector3(xCenter, carRearWheelPos.y, zCenter);

            //Find all corners of the car
            Rectangle carCornerPos = SkeletonCar.GetCornerPositions(carPos, heading, carWidth, carLength);

            //Detect if any of the corners is outside of the map
            if (
                !PathfindingController.IsPositionWithinGrid(carCornerPos.FL) ||
                !PathfindingController.IsPositionWithinGrid(carCornerPos.FR) ||
                !PathfindingController.IsPositionWithinGrid(carCornerPos.BL) ||
                !PathfindingController.IsPositionWithinGrid(carCornerPos.BR))
            {
                //At least one of the corners is outside of the map
                withinTrack = false;
            }

            return withinTrack;
        }



        //Use the car's corners and then rectangle-rectangle-intersection with the obstacles to check if the car is intersecting with an obstacle
        private static bool ObstacleDetectionCorners(Vector3 carPos, Rectangle carCornerPos)
        {
            bool hasInvalidPosition = false;

            //Find all obstacles that are close to the car
            List<ObstacleData> obstaclesThatAreClose = FindCloseObstaclesCell(carPos, carCornerPos);

            //If there are no obstacle close, then return false
            if (obstaclesThatAreClose.Count == 0)
            {
                return hasInvalidPosition;
            }


            Rectangle r1 = carCornerPos;

            for (int i = 0; i < obstaclesThatAreClose.Count; i++)
            {
                Rectangle r2 = obstaclesThatAreClose[i].cornerPos;

                //Rectangle-rectangle intersection, which is here multiple triangle-triangle intersection
                if (RectangleRectangleIntersection.IsTriangleTriangleIntersecting(r1.FL, r1.BR, r1.BL, r2.FL, r2.BR, r2.BL))
                {
                    hasInvalidPosition = true;

                    return hasInvalidPosition;
                }
                if (RectangleRectangleIntersection.IsTriangleTriangleIntersecting(r1.FL, r1.BR, r1.BL, r2.FL, r2.FR, r2.BR))
                {
                    hasInvalidPosition = true;

                    return hasInvalidPosition;
                }
                if (RectangleRectangleIntersection.IsTriangleTriangleIntersecting(r1.FL, r1.FR, r1.BR, r2.FL, r2.BR, r2.BL))
                {
                    hasInvalidPosition = true;

                    return hasInvalidPosition;
                }
                if (RectangleRectangleIntersection.IsTriangleTriangleIntersecting(r1.FL, r1.FR, r1.BR, r2.FL, r2.FR, r2.BR))
                {
                    hasInvalidPosition = true;

                    return hasInvalidPosition;
                }
            }

            return hasInvalidPosition;
        }



        //Approximate the car's area with circles to detect if there's an obstacle within the circle
        private static bool ObstacleDetectionCircles(Vector3 carPos, float heading, CarData carData, Rectangle carCornerPos)
        {
            bool hasInvalidPosition = false;

            Vector3[] circlePositions = new Vector3[3];

            //Get the position of the 3 circles that approximates the size of the car
            circlePositions = SkeletonCar.GetCirclePositions(carPos, heading, circlePositions);

            //
            //Find all obstacles close to the car
            //
            //The car's length is 5 m and each obstacle is 1 m, so add a little to be on the safe side
            //float searchRadius = carData.GetLength() * 0.5f + 1.5f;

            //List<ObstacleData> obstaclesThatAreClose = FindCloseObstaclesWithinRadius(carPos, searchRadius * searchRadius);

            List<ObstacleData> obstaclesThatAreClose = FindCloseObstaclesCell(carPos, carCornerPos);

            //If there are no obstacle close, then return
            if (obstaclesThatAreClose.Count == 0)
            {
                return hasInvalidPosition;
            }


            //
            //If there are obstacles around the car, then we have to see if some of them intersect
            //
            //The radius of one circle that approximates the area of the car 
            //The width of the car is 2 m but the circle has to be larger to provide a margin of safety
            float circleRadius = 1.40f;

            //But we also need to add the radius of the box obstacle which has a width of 1 m
            float criticalRadius = circleRadius + 0.5f;

            //And square it to speed up
            float criticalRadiusSqr = criticalRadius * criticalRadius;

            //Loop through all circles and detect if there's an obstacle within the circle
            for (int i = 0; i < circlePositions.Length; i++)
            {
                Vector3 currentCirclePos = circlePositions[i];

                //Is there an obstacle within the radius of this circle
                for (int j = 0; j < obstaclesThatAreClose.Count; j++)
                {
                    float distSqr = (currentCirclePos - obstaclesThatAreClose[j].centerPos).sqrMagnitude;

                    if (distSqr < criticalRadiusSqr)
                    {
                        hasInvalidPosition = true;

                        return hasInvalidPosition;
                    }
                }
            }


            return hasInvalidPosition;
        }



        //Method 1 - Search through all obstacles to find which are close within a radius
        private static List<ObstacleData> FindCloseObstaclesWithinRadius(Vector3 pos, float radiusSqr)
        {
            //The list with close obstacles
            List<ObstacleData> closeObstacles = new List<ObstacleData>();

            //Method 1 - Search through all obstacles to find which are close
            //The list with all obstacles in the map
            List<ObstacleData> allObstacles = ObstaclesController.obstaclesPosList;

            //Find close obstacles
            for (int i = 0; i < allObstacles.Count; i++)
            {
                float distSqr = (pos - allObstacles[i].centerPos).sqrMagnitude;

                //Add to the list of close obstacles if close enough
                if (distSqr < radiusSqr)
                {
                    closeObstacles.Add(allObstacles[i]);
                }
            }

            return closeObstacles;
        }



        //Method 2 - Find which cells the car is intersecting with and return the obstacles that intersect with those cells
        private static List<ObstacleData> FindCloseObstaclesCell(Vector3 carPos, Rectangle carCorners)
        {
            //The list with close obstacles
            List<ObstacleData> closeObstacles = new List<ObstacleData>();

            IntVector2 carCellPos = PathfindingController.ConvertCoordinateToCellPos(carPos);

            //Check an area of cells around the car's cell for obstacles
            //The car is 5 m long so search 3 m to each side?
            int searchArea = 3;

            for (int x = -searchArea; x <= searchArea; x++)
            {
                for (int z = -searchArea; z <= searchArea; z++)
                {
                    IntVector2 cellPos = new IntVector2(carCellPos.x + x, carCellPos.z + z);

                    //Is this cell within the map?
                    if (PathfindingController.IsCellWithinGrid(cellPos))
                    {
                        //Add all obstacles from this list to the list of close obstacles
                        List<ObstacleData> obstaclesInCell = ObstaclesController.obstaclesInCell[cellPos.x, cellPos.z];

                        if (obstaclesInCell != null)
                        {
                            for (int i = 0; i < obstaclesInCell.Count; i++)
                            {
                                //Might add the same obstacle more than one time, but maybe that's not a big problem?
                                closeObstacles.Add(obstaclesInCell[i]);
                            }
                        }
                    }
                }
            }


            return closeObstacles;
        }



        //Find the position of the closest obstacle to a position
        public static Vector3 FindClosestObstaclePosition(Vector3 pos)
        {
            List<ObstacleData> obstaclesPos = ObstaclesController.obstaclesPosList;

            Vector3 closestObstacle = Vector3.zero;

            float closest = Mathf.Infinity;

            for (int i = 0; i < obstaclesPos.Count; i++)
            {
                float distSqr = (pos - obstaclesPos[i].centerPos).sqrMagnitude;

                if (distSqr < closest)
                {
                    closestObstacle = obstaclesPos[i].centerPos;
                    closest = distSqr;
                }
            }

            return closestObstacle;
        }



        //Find the average position of obstacles close to a position
        public static Vector3 FindAverageObstaclePosition(Vector3 pos, float searchRadius)
        {
            //The search radius is number of cells to the closest obstacle, so maybe increase it?
            searchRadius += 1f;

            List<ObstacleData> closeObstacles = FindCloseObstaclesWithinRadius(pos, searchRadius * searchRadius);

            if (closeObstacles.Count > 0)
            {
                Vector3 averagePos = Vector3.zero;

                for (int i = 0; i < closeObstacles.Count; i++)
                {
                    averagePos += closeObstacles[i].centerPos;
                }

                averagePos /= closeObstacles.Count;

                return averagePos;
            }
            //If no obstacles were found we just return the position we got
            else
            {
                return pos;
            }
        }
    }
}
