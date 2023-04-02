using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class PlacedGridObject
    {
        public string PlacedObjectID;
        public Vector2 TilePosition;
        public GridPlacementObjectSO GridObjectSO;
        public GridPlacementObjectSO.Dir Dir;
        public ConstructionState ConstructionState;

        public PlacedGridObject(
            string PlacedObjectID,
            GridPlacementObjectSO GridObjectSO,
            Vector2 TilePosition,
            GridPlacementObjectSO.Dir Dir,
            ConstructionState ConstructionState)
        {
            this.PlacedObjectID = PlacedObjectID;
            this.GridObjectSO = GridObjectSO;
            this.TilePosition = TilePosition;
            this.Dir = Dir;
            this.ConstructionState = ConstructionState;
        }

        public override string ToString()
        {
            return $"PlacedObjectID {PlacedObjectID} | Tile pos: {TilePosition} | GridObjectSO {GridObjectSO.prefab.name} | Rotation: {Dir} | Construction State: {ConstructionState}";
        }
    }
}