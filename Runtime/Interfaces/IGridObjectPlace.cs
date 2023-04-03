namespace Nevelson.GridPlacementSystem
{
    public interface IGridObjectPlace
    {
        public BuildMode BuildMode { get; }
        public bool DisplayGrid(bool isGridDisplayed);

        public PlacedObjectData GetPlaceObjInfoAtMousePos();

        public PlacedObject GetPlacedObjectAtMousePos();

        public bool SetBuildMode(BuildMode buildMode, out string error);

        public bool BuildSelectedObject(out string error);

        public bool RotateSelectedObject(out string error);

        public bool ChangeSelectedBuildObject(int gridObjectIndex, out string error);

        public bool PickAndPlaceMoveObject(out string error);

        public bool UndoMove(out string error);

        public bool DemolishObject(out string error);
    }
}