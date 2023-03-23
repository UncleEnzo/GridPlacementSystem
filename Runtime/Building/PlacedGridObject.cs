using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class PlacedGridObject
    {
        public string PlacedObjectID;
        public Vector2 TilePosition;
        public GridPlacementObjectSO GridObjectSO;

        public PlacedGridObject(string PlacedObjectID, GridPlacementObjectSO GridObjectSO, Vector2 TilePosition)
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