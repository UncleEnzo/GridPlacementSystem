using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class PlacedGridObject
    {
        public string PlacedObjectID;
        public Vector2Int TilePosition;
        public GridPlacementObjectSO GridObjectSO;

        public PlacedGridObject(string PlacedObjectID, GridPlacementObjectSO GridObjectSO, Vector2Int TilePosition)
        {
            this.PlacedObjectID = PlacedObjectID;
            this.GridObjectSO = GridObjectSO;
            this.TilePosition = TilePosition;
        }

        public override string ToString()
        {
            return $"PlacedObjectID {PlacedObjectID} | Tile pos: {TilePosition} | GridObjectSO {GridObjectSO.prefab.name}";
        }
    }
}