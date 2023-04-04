using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class PlacedObjectData
    {
        public string ID;
        public GridPlacementObjectSO GridObjectSO;
        public Vector2Int Origin;
        public GridPlacementObjectSO.Dir Dir;
        public bool IsMovable;
        public bool IsDestructable;
        public bool UseConstructionState;
        public ConstructionState ConstructionState;

        public PlacedObjectData(
            string _id,
            GridPlacementObjectSO GridObjectSO,
            Vector2Int Origin,
            GridPlacementObjectSO.Dir Dir,
            bool IsMovable,
            bool IsDestructable,
            bool UseConstructionState,
            ConstructionState ConstructionState,
            string PlacedObjectID)
        {
            this.ID = _id;
            this.Dir = Dir;
            this.GridObjectSO = GridObjectSO;
            this.Origin = Origin;
            this.IsMovable = IsMovable;
            this.IsDestructable = IsDestructable;
            this.UseConstructionState = UseConstructionState;
            this.ConstructionState = ConstructionState;
        }

        public override string ToString()
        {
            return $"ID: {ID} \n" +
                $"Object: {GridObjectSO.name} \n" +
                $"IsMovable: {IsMovable}\n" +
                $"IsDestructable: {IsDestructable}\n" +
                $"IsRotatable: {GridObjectSO.IsRotatable}\n" +
                $"MaxPlacable: {GridObjectSO.maxPlaced}\n" +
                $"UseConstructionState: {UseConstructionState}\n" +
                $"ConstructionState: {ConstructionState}";
        }
    }
}