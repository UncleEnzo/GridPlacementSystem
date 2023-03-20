namespace Nevelson.GridPlacementSystem
{
    public class GridObject
    {
        Grid<GridObject> grid;
        int x;
        int y;
        PlacedObject placedObject;

        public GridObject(Grid<GridObject> grid, int x, int y)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
            placedObject = null;
        }

        public override string ToString()
        {
            return x + ", " + y + "\n" + placedObject;
        }

        public PlacedObject PlacedObject { get { return placedObject; } }

        public void SetPlacedObject(PlacedObject placedObject)
        {
            this.placedObject = placedObject;
            grid.TriggerGridObejctChanged(x, y);
        }

        public void ClearPlacedObject()
        {
            placedObject = null;
            grid.TriggerGridObejctChanged(x, y);
        }

        public bool CanBuild()
        {
            return placedObject == null;
        }
    }

}

