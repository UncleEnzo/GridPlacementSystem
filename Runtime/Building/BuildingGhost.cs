using System;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class BuildingGhost : MonoBehaviour
    {
        [SerializeField] Material canPlace;
        [SerializeField] Material cannotPlace;
        Transform visual = null;
        Vector3 lastTargetPosition;

        Func<Vector3> _mouseWorldSnappedPosition;
        Func<Quaternion> _placedObjectRotation;
        Func<GridPlacementObjectSO, bool> _checkSurroundingSpace;
        Func<GridPlacementObjectSO> _selectedGridObject;
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
                    GridBuildingSystem gbs)
        {
            this._mouseWorldSnappedPosition = mouseWorldSnappedPosition;
            this._placedObjectRotation = placedObjectRotation;
            this._checkSurroundingSpace = checkSurroundingSpace;
            this._selectedGridObject = selectedGridObject;
            this._undoPreviousTileColors = undoPreviousTileColors;
            this._updateTileColors = updateTileColors;
            this._undoMoveDemolishTileColors = undoMoveDemolishTileColors;
            this._updateMoveDemolishTileColors = updateMoveDemolishTileColors;
            RefreshVisual();
            gbs.OnSelectedChanged += Instance_OnSelectedChanged;
        }

        void LateUpdate()
        {
            Vector3 targetPosition = _mouseWorldSnappedPosition();
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