namespace Nevelson.GridPlacementSystem
{
    public interface IGridObjectPlace
    {
        public BuildMode BuildMode { get; }
        public bool DisplayGrid(bool isGridDisplayed);

        public PlacedObjectData GetPlaceObjInfoAtMousePos();

        public bool SetBuildMode(BuildMode buildMode);

        public bool BuildSelectedObject();

        public bool RotateSelectedObject();

        public bool ChangeSelectedBuildObject(int gridObjectIndex);

        public bool PickAndPlaceMoveObject();

        public bool UndoMove();

        public bool DemolishObject();
    }
}