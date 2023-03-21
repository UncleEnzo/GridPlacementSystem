using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class GridBuildingSystem : MonoBehaviour
    {
        enum BuildMode { BUILD, MOVE, DEMOLISH }

        [SerializeField] BuildMode buildMode = BuildMode.BUILD;
        [SerializeField] bool _displayGridOnStart = false;
        [SerializeField] List<GridPlacementObjectSO> _gridObjects;
        [SerializeField] int _gridWidth = 15;
        [SerializeField] int _gridHeight = 10;
        [SerializeField] float _cellSize = 1;
        [SerializeField] Vector3 _gridStartingPosition = Vector3.zero;
        [SerializeField] bool _isDebug = true;
        [SerializeField] GameObject _buildingSoundPrefab;
        [SerializeField] GameObject _worldGridSprite;
        [SerializeField] GameObject _buildingGhostPrefab;

        GameObject _buildingGhost;
        GameObject _buildingSoundGO = null;
        GridPlacementObjectSO _selectedGridObjectSO;
        Grid<GridObject> _grid;
        GridPlacementObjectSO.Dir _dir = GridPlacementObjectSO.Dir.Down;
        PlacedObjectData _lastDemolish = null;
        bool _isGridDisplayed = false;
        bool _movingObject = false;

        public event EventHandler OnSelectedChanged;
        public event EventHandler OnObjectPlaced;
        public GridPlacementObjectSO SelectedGridObject { get => _selectedGridObjectSO; }

        public enum BuildAction
        {
            DISPLAY_GRID,
            HIDE_GRID,
            SET_BUILD_MODE,
            SET_DEMOLISH_MODE,
            SET_MOVE_MODE,
            ACCEPT_BUTTON,
            UNDO_BUTTON,
            ROTATE,
            SELECT_GRID_OBJECT,
        }

        public void ChangeGridObjectToPlace(int gridObjectIndex)
        {
            PerformBuildAction(BuildAction.SELECT_GRID_OBJECT, gridObjectIndex);
        }

        public void PerformBuildAction(BuildAction buildAction)
        {
            if (buildAction == BuildAction.SELECT_GRID_OBJECT)
            {
                Debug.LogError("To perform Select Grid Object, call ChangeGridObjectToPlace Function instead");
                return;
            }
            PerformBuildAction(buildAction, -2);
        }

        void PerformBuildAction(BuildAction buildAction, int gridObjectIndex)
        {
            //-1 == Deselect Grid object
            //-2 == Performing an action other than SelectGridObject and don't need index
            if (gridObjectIndex <= -3 || gridObjectIndex > _gridObjects.Count - 1)
            {
                Debug.LogError($"{gridObjectIndex} is not out of bounds of the _gridObjects list");
                return;
            }

            if (buildAction == BuildAction.DISPLAY_GRID)
            {
                Debug.Log("Displaying grid");
                DisplayGrid(true);
                return;
            }
            if (buildAction == BuildAction.HIDE_GRID)
            {
                Debug.Log("Hiding grid");
                DisplayGrid(false);
                return;
            }

            if (!_isGridDisplayed)
            {
                Debug.LogWarning($"Not performing action {buildAction} because grid is not displayed");
                return;
            }

            if (_selectedGridObjectSO == null)
            {
                Debug.Log("Resetting rotation");
                ResetRotation();
            }

            if (buildAction == BuildAction.SET_BUILD_MODE)
            {
                Debug.Log("Setting mode to Build Mode");
                SetBuildMode(BuildMode.BUILD);
                return;
            }

            if (buildAction == BuildAction.SET_DEMOLISH_MODE)
            {
                Debug.Log("Setting mode to Demolish Mode");
                SetBuildMode(BuildMode.DEMOLISH);
                return;
            }

            if (buildAction == BuildAction.SET_MOVE_MODE)
            {
                Debug.Log("Setting mode to Move Mode");
                SetBuildMode(BuildMode.MOVE);
                return;
            }

            switch (buildMode)
            {
                case BuildMode.BUILD:
                    if (buildAction == BuildAction.ACCEPT_BUTTON)
                    {
                        Debug.Log($"Build Mode is: {buildMode}, performing build action");
                        Build();
                        break;
                    }
                    if (buildAction == BuildAction.ROTATE)
                    {
                        Debug.Log($"Build Mode is: {buildMode}, performing rotate");
                        Rotate();
                        break;
                    }
                    if (buildAction == BuildAction.SELECT_GRID_OBJECT)
                    {
                        if (gridObjectIndex == -1)
                        {
                            Debug.Log($"Build Mode is: {buildMode}, Deselecting grid object");
                            DeselectBuildObject();
                            break;
                        }

                        Debug.Log($"Build Mode is: {buildMode}, Selecting grid object: {gridObjectIndex}");
                        SelectGridObject(gridObjectIndex);
                        break;
                    }
                    break;
                case BuildMode.MOVE:
                    if (!_movingObject && buildAction == BuildAction.ACCEPT_BUTTON)
                    {
                        Debug.Log($"Build Mode is: {buildMode}, selecting object to move");
                        SelectMoveObject();
                        break;
                    }
                    if (_movingObject && buildAction == BuildAction.ACCEPT_BUTTON)
                    {
                        Debug.Log($"Build Mode is: {buildMode}, moving selected object");
                        Move();
                        break;
                    }
                    if (_movingObject && buildAction == BuildAction.UNDO_BUTTON)
                    {
                        Debug.Log($"Build Mode is: {buildMode}, cancelling move");
                        UndoSelectedMoveObject();
                        DeselectBuildObject();
                        break;
                    }
                    if (_movingObject && buildAction == BuildAction.ROTATE)
                    {
                        Debug.Log($"Build Mode is: {buildMode}, rotating object");
                        Rotate();
                        break;
                    }
                    break;
                case BuildMode.DEMOLISH:
                    if (buildAction == BuildAction.ACCEPT_BUTTON)
                    {
                        Debug.Log($"Build Mode is: {buildMode}, performing Demolish");
                        Demolish();
                        break;
                    }
                    break;
                default:
                    Debug.LogError("Build mode doesn't exist");
                    break;
            }
        }
        public Vector3 GetMouseWorldSnappedPosition()
        {
            Vector3 mousePosition = GetMouseWorldPosition();
            if (_selectedGridObjectSO == null) return mousePosition;

            _grid.GetXY(mousePosition, out int x, out int y);
            Vector2Int rotationOffset = _selectedGridObjectSO.GetRotationOffset(_dir);
            Vector3 placedObjectWorldPosition = _grid.GetWorldPosition(x, y) + new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;
            return placedObjectWorldPosition;
        }

        public Quaternion GetPlacedObjectRotation()
        {
            return _selectedGridObjectSO == null ?
                Quaternion.identity :
                Quaternion.Euler(0, 0, -_selectedGridObjectSO.GetRotationAngle(_dir));
        }
        public bool CheckSurroundingSpace()
        {
            _grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            Vector2Int placedObjectOrigin = new Vector2Int(x, y);
            List<Vector2Int> gridPositionList = _selectedGridObjectSO.GetGridPositionList(placedObjectOrigin, _dir);
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                //if the surrounding tile is outside grid bounds or can't build
                GridObject gridObj = _grid.GetGridObject(gridPosition.x, gridPosition.y);
                if (gridObj == null || !gridObj.CanBuild()) return false;
            }
            return true;
        }

        void Awake()
        {
            _selectedGridObjectSO = _gridObjects[0];
            _grid = new Grid<GridObject>(
                _gridWidth,
                _gridHeight,
                _cellSize,
                _gridStartingPosition,
                (Grid<GridObject> g, int x, int z) =>
                new GridObject(g, x, z), _isDebug);
            CreateBuildingGhost();
            GenerateWorldTiles();
            DisplayGrid(_displayGridOnStart);
        }

        void OnApplicationQuit()
        {
            DisplayGrid(false);
        }

        void DisplayGrid(bool isGridDisplayed)
        {
            _isGridDisplayed = isGridDisplayed;
            _buildingGhost.SetActive(_isGridDisplayed);
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(_isGridDisplayed);
            }
            SetBuildMode(BuildMode.BUILD);
        }

        void CreateBuildingGhost()
        {
            _buildingGhost = Instantiate(_buildingGhostPrefab);
            _buildingGhost.GetComponent<BuildingGhost>().Init(this);
        }

        void GenerateWorldTiles()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    GameObject tile = Instantiate(_worldGridSprite, transform);
                    tile.transform.localPosition = _grid.GetWorldPosition(x, y);
                }
            }
        }

        void SetBuildMode(BuildMode buildMode)
        {
            if (this.buildMode == buildMode)
            {
                Debug.Log($"Not setting build mode because mode is already {buildMode}");
                return;
            }
            this.buildMode = buildMode;
            switch (buildMode)
            {
                case BuildMode.BUILD:
                    Debug.Log("Build Mode Activated");
                    UndoSelectedMoveObject();
                    SelectGridObject(0);
                    break;
                case BuildMode.DEMOLISH:
                    Debug.Log("Demolish Mode Activated");
                    UndoSelectedMoveObject();
                    DeselectBuildObject();
                    break;
                case BuildMode.MOVE:
                    Debug.Log("Move Mode Activated");
                    DeselectBuildObject();
                    break;
                default:
                    Debug.LogError("Build mode doesn't exist");
                    break;
            }
        }

        void SelectMoveObject()
        {
            if (_movingObject) return;
            _grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            GridObject gridObject = _grid.GetGridObject(x, y);
            if (gridObject.PlacedObject == null)
            {
                Debug.Log("Did not find object to move");
                return;
            }
            Debug.Log($"Moving {gridObject.PlacedObject.gameObject.name}");
            int index = _gridObjects.FindIndex((x) =>
            {
                Debug.Log($"prefab Name {x.prefab.name}");
                Debug.Log($"gridObject Name {gridObject.PlacedObject.gameObject.name}");
                bool ok = gridObject.PlacedObject.gameObject.name.Contains(x.prefab.name);
                return ok;
            });
            SelectGridObject(index);
            Demolish();
            _movingObject = true;
        }

        void UndoSelectedMoveObject()
        {
            if (!_movingObject) return;
            Debug.Log("Deselecting object to move");
            DeselectBuildObject();
            UndoLastDemolish();
            _movingObject = false;
        }

        void Move()
        {
            if (!Build()) return;
            _movingObject = false;
            _lastDemolish = null;
            DeselectBuildObject();
        }

        bool Build()
        {
            if (_selectedGridObjectSO == null) return false;

            if (!CheckSurroundingSpace())
            {
                Debug.Log("Can't build, space already taken");
                return false;
            }

            _grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            Vector2Int placedObjectOrigin = new Vector2Int(x, y);

            Vector2Int rotationOffset = _selectedGridObjectSO.GetRotationOffset(_dir);
            Vector3 placedObjectWorldPosition = _grid.GetWorldPosition(x, y) +
                new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;

            PlacedObject placedObject = PlacedObject.Create(
                placedObjectWorldPosition,
                placedObjectOrigin,
                _dir,
                _selectedGridObjectSO);

            //this rotates the sprite a bit more for 2D
            placedObject.transform.rotation = Quaternion.Euler(0, 0, -_selectedGridObjectSO.GetRotationAngle(_dir));

            //populate other tiles that take up the dimensions of the object with info that they are taken
            List<Vector2Int> gridPositionList = _selectedGridObjectSO.GetGridPositionList(placedObjectOrigin, _dir);
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                _grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
            }

            if (_buildingSoundGO == null)
            {
                _buildingSoundGO = Instantiate(_buildingSoundPrefab, GetMouseWorldPosition(), Quaternion.identity);
            }

            //place the sound effect at mouse location and play it
            _buildingSoundGO.transform.position = GetMouseWorldPosition();
            _buildingSoundGO.GetComponent<AudioSource>().Play();

            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
            return true;
        }

        void Demolish()
        {
            GridObject gridObject = _grid.GetGridObject(GetMouseWorldPosition());
            PlacedObject placedObject = gridObject.PlacedObject;
            if (placedObject == null) return;

            _lastDemolish = gridObject.PlacedObject.GetData();
            placedObject.DestroySelf();
            List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                _grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
            }
        }

        void UndoLastDemolish()
        {
            if (_lastDemolish == null)
            {
                Debug.Log("No last demolish found");
                return;
            }
            Debug.Log("Undoing last demolish");
            Vector2Int placedObjectOrigin = _lastDemolish.Origin;
            var selectedGridObjectSO = _lastDemolish.GridObjectSO;
            var dir = _lastDemolish.Dir;


            Vector2Int rotationOffset = selectedGridObjectSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = _grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) +
                new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;


            PlacedObject placedObject = PlacedObject.Create(
                placedObjectWorldPosition,
                placedObjectOrigin,
                dir,
                selectedGridObjectSO);

            //this rotates the sprite a bit more for 2D
            placedObject.transform.rotation = Quaternion.Euler(0, 0, -selectedGridObjectSO.GetRotationAngle(dir));

            //populate other tiles that take up the dimensions of the object with info that they are taken
            List<Vector2Int> gridPositionList = selectedGridObjectSO.GetGridPositionList(placedObjectOrigin, dir);
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                _grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
            }

            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
            _lastDemolish = null;
        }

        void Rotate()
        {
            _dir = GridPlacementObjectSO.GetNextDir(_dir);
        }

        void SelectGridObject(int i)
        {
            _selectedGridObjectSO = _gridObjects[i];
            RefreshSelectedObjectType();
        }

        void DeselectBuildObject()
        {
            _selectedGridObjectSO = null;
            RefreshSelectedObjectType();
        }

        void ResetRotation()
        {
            while (_dir != GridPlacementObjectSO.Dir.Down)
            {
                Debug.Log("Resetting rotate position");
                Rotate();
            }
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