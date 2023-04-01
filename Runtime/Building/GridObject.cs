using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class GridObject
    {
        int x, y;
        Grid<GridObject> grid;
        PlacedObject placedObject;
        SpriteRenderer worldTile;
        Color vacant;
        Color occupied;
        Color canBuild;
        Color cannotBuild;
        Color movableOrDestroyable;

        //note: Display and hide functions are responsible for activating and deactivating the tiles
        //this class just sets it's color to it's original color or transparent

        public GridObject(
            Grid<GridObject> grid,
            int x, int y,
            GameObject worldTile,
            Color occupied,
            Color canBuild,
            Color cannotBuild,
            Color movableOrDestroyable)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
            this.worldTile = worldTile.GetComponentInChildren<SpriteRenderer>();
            placedObject = null;
            vacant = this.worldTile.color;
            this.occupied = occupied;
            this.canBuild = canBuild;
            this.cannotBuild = cannotBuild;
            this.movableOrDestroyable = movableOrDestroyable;
            this.occupied.a =
                this.canBuild.a =
                this.cannotBuild.a =
                this.movableOrDestroyable.a
                = vacant.a;
        }

        public void SetVacantColor()
        {
            worldTile.color = vacant;
        }

        public void SetOccupiedColor()
        {
            worldTile.color = occupied;
        }

        public void SetCanBuildColor()
        {
            worldTile.color = canBuild;
        }

        public void SetCannotBuildColor()
        {
            worldTile.color = cannotBuild;
        }

        public void SetCanMoveOrDestroyColor()
        {
            worldTile.color = movableOrDestroyable;
        }

        public override string ToString()
        {
            return x + ", " + y + "\n" + placedObject;
        }

        public PlacedObject PlacedObject { get { return placedObject; } }

        public void SetPlacedObject(PlacedObject placedObject)
        {
            this.placedObject = placedObject;
            grid.TriggerGridObjectChanged(x, y);
            worldTile.color = occupied;
        }

        public void ClearPlacedObject()
        {
            placedObject = null;
            grid.TriggerGridObjectChanged(x, y);
            worldTile.color = vacant;
        }

        public bool CanBuild()
        {
            return placedObject == null;
        }
    }
}