using System;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class BuildingGhost : MonoBehaviour
    {
        [SerializeField] Material canPlace;
        [SerializeField] Material cannotPlace;
        Grid<GridObject> _grid;
        Transform visual = null;
        Vector3 lastTargetPosition;

        Func<Vector3> _mouseWorldSnappedPosition;
        Func<Quaternion> _placedObjectRotation;
        Func<GridPlacementObjectSO, bool> _checkSurroundingSpace;
        Func<GridPlacementObjectSO> _selectedGridObject;
        Func<Vector3, Vector2Int> _getXY;
        Func<Vector2Int, GridObject> _getGridObject;
        Action _undoPreviousTileColors;
        Action _updateTileColors;
        Action _undoMoveDemolishTileColors;
        Action _updateMoveDemolishTileColors;


        public void Init(Func<Vector3> mouseWorldSnappedPosition,
                    Func<Quaternion> placedObjectRotation,
                    Func<GridPlacementObjectSO, bool> checkSurroundingSpace,
                    Func<GridPlacementObjectSO> selectedGridObject,
                    Action undoPreviousTileColors,
                    Action updateTileColors,
                    Action undoMoveDemolishTileColors,
                    Action updateMoveDemolishTileColors,
                    Func<Vector3, Vector2Int> getXY,
                    Func<Vector2Int, GridObject> getGridObject,
                    ref Grid<GridObject> grid,
                    GridBuildingSystem gbs)
        {
            this._mouseWorldSnappedPosition = mouseWorldSnappedPosition;
            this._placedObjectRotation = placedObjectRotation;
            this._checkSurroundingSpace = checkSurroundingSpace;
            this._selectedGridObject = selectedGridObject;
            this._undoPreviousTileColors = undoPreviousTileColors;
            this._updateTileColors = updateTileColors;
            this._undoMoveDemolishTileColors = undoMoveDemolishTileColors;
            this._getXY = getXY;
            this._getGridObject = getGridObject;
            this._grid = grid;
            this._updateMoveDemolishTileColors = updateMoveDemolishTileColors;
            RefreshVisual();
            gbs.OnSelectedChanged += Instance_OnSelectedChanged;
        }

        Vector3 GetMouseWorldPosition()
        {
            Vector3 vec = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            vec.z = 0f;
            return vec;
        }

        void LateUpdate()
        {
            Vector3 targetPosition;
            GridPlacementObjectSO selectedGridPlacementSO = _selectedGridObject();
            if (selectedGridPlacementSO == null)
            {
                targetPosition = _mouseWorldSnappedPosition();
            }
            //IF IT"S UPGRADEABLE SNAPS TO THIS LOCATION!
            //LAST BUG >> IT'S SETTING TO THE CENTER OF THE ENTIRE MAP
            else if (selectedGridPlacementSO.UpgradeFrom != null)
            {
                //Vector2Int mouseOrigin = _getXY(GetMouseWorldPosition());
                //GridObject gridObj = _getGridObject(mouseOrigin);
                Vector2Int mouseOrigin = _grid.GetXY(GetMouseWorldPosition());
                GridObject gridObj = _grid.GetGridObject(mouseOrigin);
                if (gridObj != null &&
                    gridObj.CanUpgrade(selectedGridPlacementSO.UpgradeFrom))
                {


                    //I THINK IT'S ALL GOOD, BUT THE BUG IS WE'RE NOT GETTING THE RIGHT ONE

                    Debug.Log($"ORIGIN IS: {gridObj.PlacedObject.Origin}");

                    targetPosition = new Vector3(gridObj.PlacedObject.Origin.x, gridObj.PlacedObject.Origin.y);
                }
                else
                {
                    Debug.Log("HITTING THIS");
                    Debug.Log("Grid obj == null? " + gridObj == null);
                    targetPosition = _mouseWorldSnappedPosition();
                }
            }
            else
            {
                targetPosition = _mouseWorldSnappedPosition();
            }


            if (lastTargetPosition == null || targetPosition != lastTargetPosition)
            {
                SetGhostColor();
                if (_selectedGridObject())
                {
                    _undoPreviousTileColors();
                    _updateTileColors();
                }
                else
                {
                    _undoMoveDemolishTileColors();
                    _updateMoveDemolishTileColors();
                }
                lastTargetPosition = targetPosition;
            }

            Animate(targetPosition);
        }

        void SetGhostColor()
        {
            if (visual == null) return;
            var sr = visual.GetComponentInChildren<SpriteRenderer>();
            sr.material = _checkSurroundingSpace(_selectedGridObject()) ?
                canPlace : cannotPlace;
        }

        void Animate(Vector3 targetPosition)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, _placedObjectRotation(), Time.deltaTime * 15f);
        }

        void Instance_OnSelectedChanged(object sender, EventArgs e)
        {
            RefreshVisual();
        }

        void RefreshVisual()
        {
            GridPlacementObjectSO placedObjectTypeSO = _selectedGridObject();
            if (placedObjectTypeSO == null)
            {
                if (visual != null)
                {
                    Destroy(visual.gameObject);
                    visual = null;
                }
                return;
            }

            if (visual != null)
            {
                Destroy(visual.gameObject);
                visual = null;
            }

            visual = Instantiate(placedObjectTypeSO.ghost, Vector3.zero, Quaternion.identity);
            visual.parent = transform;
            visual.localPosition = Vector3.zero;
            visual.localEulerAngles = Vector3.zero;
            SetGhostColor();
        }
    }
}