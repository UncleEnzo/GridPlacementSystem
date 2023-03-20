using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class GridBuildingSystem : MonoBehaviour
    {
        [SerializeField] List<GridPlacementObjectSO> _gridObjects;
        [SerializeField] int _gridWidth = 15;
        [SerializeField] int _gridHeight = 10;
        [SerializeField] float _cellSize = 1;
        [SerializeField] Vector3 _gridStartingPosition = Vector3.zero;
        [SerializeField] bool isDebug = true;
        [SerializeField] GameObject _buildingSoundPrefab;

        GameObject _buildingSoundGO = null;
        GridPlacementObjectSO selectedGridObjectSO;
        Grid<GridObject> grid;
        GridPlacementObjectSO.Dir dir = GridPlacementObjectSO.Dir.Down;

        public static GridBuildingSystem Instance { get; private set; }
        public event EventHandler OnSelectedChanged;
        public event EventHandler OnObjectPlaced;

        public GridPlacementObjectSO SelectedGridObject { get => selectedGridObjectSO; }

        public Vector3 GetMouseWorldSnappedPosition()
        {
            Vector3 mousePosition = GetMouseWorldPosition();
            if (selectedGridObjectSO == null) return mousePosition;

            grid.GetXY(mousePosition, out int x, out int y);
            Vector2Int rotationOffset = selectedGridObjectSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, y) + new Vector3(rotationOffset.x, rotationOffset.y) * grid.CellSize;
            return placedObjectWorldPosition;
        }

        public Quaternion GetPlacedObjectRotation()
        {
            return selectedGridObjectSO == null ?
                Quaternion.identity :
                Quaternion.Euler(0, 0, -selectedGridObjectSO.GetRotationAngle(dir));
        }

        void Awake()
        {
            Instance = this;
            selectedGridObjectSO = _gridObjects[0];
            grid = new Grid<GridObject>(
                _gridWidth,
                _gridHeight,
                _cellSize,
                _gridStartingPosition,
                (Grid<GridObject> g, int x, int z) =>
                new GridObject(g, x, z), isDebug);
        }

        void Update()
        {
            Build();
            Rotate();
            Demolish();
            DeselectBuildObject();
            SelectObjectOne();
            SelectObjectTwo();
        }

        void Build()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (selectedGridObjectSO == null) return;

            grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            Vector2Int placedObjectOrigin = new Vector2Int(x, y);
            List<Vector2Int> gridPositionList = selectedGridObjectSO.GetGridPositionList(placedObjectOrigin, dir);

            if (!CheckSurroundingSpace(gridPositionList))
            {
                Debug.Log("Can't build, space already taken");
                return;
            }

            Vector2Int rotationOffset = selectedGridObjectSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, y) +
                new Vector3(rotationOffset.x, rotationOffset.y) * grid.CellSize;

            PlacedObject placedObject = PlacedObject.Create(
                placedObjectWorldPosition,
                placedObjectOrigin,
                dir,
                selectedGridObjectSO);

            //this rotates the sprite a bit more for 2D
            placedObject.transform.rotation = Quaternion.Euler(0, 0, -selectedGridObjectSO.GetRotationAngle(dir));

            //populate other tiles that take up the dimensions of the obkect with info that they are taken
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
            }

            if (_buildingSoundGO == null)
            {
                _buildingSoundGO = Instantiate(_buildingSoundPrefab, GetMouseWorldPosition(), Quaternion.identity);
            }

            //place the sound effect at mouse location and play it
            _buildingSoundGO.transform.position = GetMouseWorldPosition();
            _buildingSoundGO.GetComponent<AudioSource>().Play();

            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
        }

        void Rotate()
        {
            if (!Input.GetKeyDown(KeyCode.R)) return;

            dir = GridPlacementObjectSO.GetNextDir(dir);
        }

        void Demolish()
        {
            if (!Input.GetMouseButtonDown(1)) return;

            GridObject gridObject = grid.GetGridObject(GetMouseWorldPosition());
            PlacedObject placedObject = gridObject.PlacedObject;
            if (placedObject == null) return;

            placedObject.DestroySelf();
            List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
            }
        }

        void DeselectBuildObject()
        {
            if (!Input.GetKeyDown(KeyCode.Alpha0)) return;

            selectedGridObjectSO = null;
            RefreshSelectedObjectType();
        }

        void SelectObjectOne()
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1)) return;

            selectedGridObjectSO = _gridObjects[0];
            RefreshSelectedObjectType();
        }

        void SelectObjectTwo()
        {
            if (!Input.GetKeyDown(KeyCode.Alpha2)) return;

            selectedGridObjectSO = _gridObjects[1];
            RefreshSelectedObjectType();
        }

        bool CheckSurroundingSpace(List<Vector2Int> gridPositionList)
        {
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                //if the surrounding tile is outside grid bounds or can't build
                GridObject gridObj = grid.GetGridObject(gridPosition.x, gridPosition.y);
                if (gridObj == null || !gridObj.CanBuild()) return false;
            }
            return true;
        }

        Vector3 GetMouseWorldPosition()
        {
            Vector3 vec = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            vec.z = 0f;
            return vec;
        }

        void RefreshSelectedObjectType()
        {
            OnSelectedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}