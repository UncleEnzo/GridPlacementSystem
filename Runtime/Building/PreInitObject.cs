using System;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    [Serializable]
    public class PreInitObject
    {
        [Min(0), SerializeField] int x;
        [Min(0), SerializeField] int y;
        public Vector2Int TilePosition { get => new Vector2Int(x, y); }
        public GridPlacementObjectSO GridObject;
        public GridPlacementObjectSO.Dir Dir = GridPlacementObjectSO.Dir.Down;

        public PreInitObject(Vector2Int tilePosition, GridPlacementObjectSO gridObject, GridPlacementObjectSO.Dir dir)
        {
            x = tilePosition.x;
            y = tilePosition.y;
            GridObject = gridObject;
            Dir = dir;
        }
    }
}