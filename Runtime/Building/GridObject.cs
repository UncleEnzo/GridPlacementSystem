using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class GridObject
    {
        Grid<GridObject> grid;
        int x;
        int y;
        PlacedObject placedObject;
        SpriteRenderer worldTile;
        Color vacantColor;
        Color filledColor;


        //note: Display and hide functions are responsible for activating and deactivating the tiles
        //this class just sets it's color to it's original color or transparent

        public GridObject(Grid<GridObject> grid, int x, int y, GameObject worldTile)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
            this.worldTile = worldTile.GetComponentInChildren<SpriteRenderer>();
            placedObject = null;
            vacantColor = this.worldTile.color;
            filledColor = Color.blue;
            filledColor.a = vacantColor.a;
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
            worldTile.color = filledColor;
        }

        public void ClearPlacedObject()
        {
            placedObject = null;
            grid.TriggerGridObjectChanged(x, y);
            worldTile.color = vacantColor;
        }

        public bool CanBuild()
        {
            return placedObject == null;
        }
    }
}