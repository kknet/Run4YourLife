﻿using System;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Run4YourLife.CameraUtils
{
    public class CameraConverter {
        
        public static Vector3 NormalizedViewportToGamePlaneWorldPosition(Camera camera, Vector2 normalizedViewport)
        {
            Vector3 position = new Vector3()
            {
                x = normalizedViewport.x,
                y = normalizedViewport.y,
                z = Math.Abs(camera.transform.position.z)
            };

            return NormalizedScreenPositionToWorldPosition(camera, position);
        }
        
        /// <summary>
        /// Converts position (x,y) in range [0,1], z is the distance from the camera in which to take the measurement.
        /// for more information look at Camera.ScreenToWorldPoint
        /// </summary>
        /// <returns>World position at the specified normalized viewport position</returns>
        public static Vector3 NormalizedScreenPositionToWorldPosition(Camera camera, Vector3 position)
        {
            float deltaX = Screen.width - camera.pixelWidth;
            float deltaY = Screen.height - camera.pixelHeight;

            float renderedScreenX = Screen.width - deltaX;
            float renderedScreenY = Screen.height - deltaY;

            position.x = (deltaX /2.0f) + position.x * renderedScreenX;
            position.y = (deltaY /2.0f) + position.y * renderedScreenY;
            return camera.ScreenToWorldPoint(position);
        }
    }
}
