using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class PlacedGridObject
    {
        public string PlacedObjectID;
        public Vector2 TilePosition;
        public GridPlacementObjectSO GridObjectSO;
        public GridPlacementObjectSO.Dir Dir;

        public PlacedGridObject(string PlacedObjectID, GridPlacementObjectSO GridObjectSO, Vector2 TilePosition, GridPlacementObjectSO.Dir Dir)
        {
            this.PlacedObjectID = PlacedObjectID;
            this.GridObjectSO = GridObjectSO;
            this.TilePosition = TilePosition;
            this.Dir = Dir;
        }

        public override string ToString()
        {
            return $"PlacedObjectID {PlacedObjectID} | Tile pos: {TilePosition} | GridObjectSO {GridObjectSO.prefab.name} | Rotation: {Dir}";
        }
    }
}