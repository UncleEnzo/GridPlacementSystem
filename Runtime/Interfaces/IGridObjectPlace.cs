namespace Nevelson.GridPlacementSystem
{
    public interface IGridObjectPlace
    {
        BuildMode BuildMode { get; }
        bool DisplayGrid(bool isGridDisplayed);
        PlacedObjectData GetPlaceObjInfoAtMousePos();
        PlacedObject GetPlacedObjectAtMousePos();
        bool SetBuildMode(BuildMode buildMode, out string error);
        bool BuildSelectedObject(out string error);
        bool RotateSelectedObject(out string error);
        bool ChangeSelectedBuildObject(GridPlacementObjectSO selectedGridObject, out string error);
        bool PickAndPlaceMoveObject(out string error);
        bool UndoMove(out string error);
        bool DemolishObject(out string demolishObjectId, out string error);
    }
}