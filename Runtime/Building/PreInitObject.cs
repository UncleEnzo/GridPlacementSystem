using System;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    [Serializable]
    public class PreInitObject
    {
        [Min(0), SerializeField] int x;
        [Min(0), SerializeField] int y;
        public string ID;
        public Vector2Int TilePosition { get => new Vector2Int(x, y); }
        public GridPlacementObjectSO GridObject;
        public GridPlacementObjectSO.Dir Dir = GridPlacementObjectSO.Dir.Down;
        public ConstructionState ConstructionState;

        public PreInitObject(
            string id,
            Vector2Int tilePosition,
            GridPlacementObjectSO gridObject,
            GridPlacementObjectSO.Dir dir,
            ConstructionState constructionState)
        {
            ID = id;
            x = tilePosition.x;
            y = tilePosition.y;
            GridObject = gridObject;
            Dir = dir;
            ConstructionState = constructionState;
        }
    }
}