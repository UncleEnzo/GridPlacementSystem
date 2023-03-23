using System;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    [Serializable]
    public class PreInitObject
    {
        public Vector2Int TilePosition;
        public GridPlacementObjectSO GridObject;
    }
}