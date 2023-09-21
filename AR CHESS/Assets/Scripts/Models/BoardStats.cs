using System;
using UnityEngine;

namespace Models
{
    [Serializable]
    public class BoardStats
    {
        public float tileSize = 1.0f;
        public float yOffset = 0.2f;
        public Vector3 bounds;
        public Vector3 tileCenter;
        public bool isWhiteTurn;
    }
}