using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class PlacedObjectData
    {
        public GridPlacementObjectSO GridObjectSO;
        public Vector2Int Origin;
        public GridPlacementObjectSO.Dir Dir;

        public PlacedObjectData(
            GridPlacementObjectSO GridObjectSO,
            Vector2Int Origin,
            GridPlacementObjectSO.Dir Dir)
        {
            this.Dir = Dir;
            this.GridObjectSO = GridObjectSO;
            this.Origin = Origin;
        }
    }
}