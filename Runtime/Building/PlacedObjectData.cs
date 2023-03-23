using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class PlacedObjectData
    {
        public GridPlacementObjectSO GridObjectSO;
        public Vector2Int Origin;
        public GridPlacementObjectSO.Dir Dir;
        public bool IsMovable;
        public bool IsDestructable;
        public string PlacedObjectID;

        public PlacedObjectData(
            GridPlacementObjectSO GridObjectSO,
            Vector2Int Origin,
            GridPlacementObjectSO.Dir Dir,
            bool IsMovable,
            bool IsDestructable,
            string PlacedObjectID)
        {
            this.Dir = Dir;
            this.GridObjectSO = GridObjectSO;
            this.Origin = Origin;
            this.IsMovable = IsMovable;
            this.IsDestructable = IsDestructable;
            this.PlacedObjectID = PlacedObjectID;
        }
    }
}