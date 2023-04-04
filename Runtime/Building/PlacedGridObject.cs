using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class PlacedGridObject
    {
        public string ID;
        public PlacedObject PlacedObject;
        public Vector2 TilePosition;
        public GridPlacementObjectSO GridObjectSO;
        public GridPlacementObjectSO.Dir Dir;
        public ConstructionState ConstructionState;

        public PlacedGridObject(
            string ID,
            PlacedObject PlacedObject,
            GridPlacementObjectSO GridObjectSO,
            Vector2 TilePosition,
            GridPlacementObjectSO.Dir Dir,
            ConstructionState ConstructionState)
        {
            this.ID = ID;
            this.PlacedObject = PlacedObject;
            this.GridObjectSO = GridObjectSO;
            this.TilePosition = TilePosition;
            this.Dir = Dir;
            this.ConstructionState = ConstructionState;
        }

        public override string ToString()
        {
            return $"BuildingID {ID} | Tile pos: {TilePosition} | GridObjectSO {GridObjectSO.prefab.name} | Rotation: {Dir} | Construction State: {ConstructionState}";
        }
    }
}