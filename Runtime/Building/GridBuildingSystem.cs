using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Nevelson.GridPlacementSystem
{
    public class GridBuildingSystem : MonoBehaviour
    {
        enum BuildMode { BUILD, MOVE, DEMOLISH }

        [Header("Place grid objects here you want instantiated before game starts")]
        [SerializeField] List<PreInitObject> _preInitGridObjects;

        [Header("Place objects here you want player to be able to instantiate on build")]
        [SerializeField] List<GridPlacementObjectSO> _gridObjects;

        [SerializeField] BuildMode buildMode = BuildMode.BUILD;
        [SerializeField] bool _displayGridOnStart = false;
        [SerializeField] int _gridWidth = 15;
        [SerializeField] int _gridHeight = 10;
        [SerializeField] float _cellSize = 1;
        [SerializeField] bool _isDebug = true;
        [SerializeField] GameObject _buildingSoundPrefab;
        [SerializeField] GameObject _worldGridSprite;
        [SerializeField] GameObject _buildingGhostPrefab;
        [SerializeField] AudioClip _buildSound;
        [SerializeField] AudioClip _demolishSound;
        [SerializeField] UnityEvent<List<PlacedGridObject>> _OnGridUpdate;

        List<PlacedGridObject> _placedGridObjects = new List<PlacedGridObject>();

        Grid<GridObject> _grid;
        GameObject _buildingGhost;
        GameObject _buildingSoundGO;
        GridPlacementObjectSO _selectedGridObjectSO;
        GridPlacementObjectSO.Dir _dir = GridPlacementObjectSO.Dir.Down;
        PlacedObjectData _lastDemolish = null;
        bool _isGridDisplayed = false;
        bool _movingObject = false;

        AudioSource BuildingSound
        {
            get
            {
                if (_buildingSoundGO == null)
                {
                    _buildingSoundGO = Instantiate(_buildingSoundPrefab, GetMouseWorldPosition(), Quaternion.identity);
                }
                return _buildingSoundGO.GetComponent<AudioSource>();
            }
        }

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

        public void PerformBuildAction(BuildAction buildAction)
        {
            if (buildAction == BuildAction.SELECT_GRID_OBJECT)
            {
                Debug.LogError("To perform Select Grid Object, call ChangeGridObjectToPlace Function instead");
                return;
            }
            PerformBuildAction(buildAction, -2);
        }

        public bool CheckIfMaxOfObjectPlaced(int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= _gridObjects.Count)
            {
                Debug.LogError("Selected building index out of range");
                return false;
            }

            //if the max placed is 0, the limit is infinite
            GridPlacementObjectSO gridSO = _gridObjects[selectedIndex];
            if (gridSO.maxPlaced == 0) return true;

            int placedObjCount = _placedGridObjects.Where(x => x.GridObjectSO == gridSO).Count();
            if (placedObjCount >= gridSO.maxPlaced)
            {
                Debug.Log($"Cannot place more {gridSO.name}. Max count {gridSO.maxPlaced}. Number placed {placedObjCount}");
                return false;
            }
            return true;
        }

        public void ChangeGridObjectToPlace(int gridObjectIndex)
        {
            PerformBuildAction(BuildAction.SELECT_GRID_OBJECT, gridObjectIndex);
        }

        public void SetAllPreInitObjects(List<PreInitObject> preInitObjects)
        {
            _preInitGridObjects.Clear();
            _preInitGridObjects = preInitObjects;
        }

        public void AddPreInitObjects(PreInitObject[] preInitObject)
        {
            foreach (var preInit in preInitObject)
            {
                _preInitGridObjects.Add(preInit);
            }
        }

        public void AddPlaceableGridObject(GridPlacementObjectSO[] gridObjects)
        {
            foreach (GridPlacementObjectSO obj in gridObjects)
            {
                _gridObjects.Add(obj);
            }
        }

        //-- above functions are used outside of the package, everything else is kind of internal

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

        void Start()
        {
            _selectedGridObjectSO = _gridObjects[0];
            _grid = new Grid<GridObject>(
                _gridWidth,
                _gridHeight,
                _cellSize,
                (Grid<GridObject> g, int x, int y) =>
                {
                    //this sets the world tile and also passes its reference to the grid object
                    //so it can control its color
                    GameObject tile = Instantiate(_worldGridSprite, transform);
                    tile.transform.localPosition = new Vector3(x, y) * _cellSize;
                    Vector3Int gridTransform = Vector3Int.RoundToInt(tile.transform.localPosition);
                    return new GridObject(g, gridTransform.x, gridTransform.y, tile);
                },
                transform,
                _isDebug);
            CreateBuildingGhost();
            PreInstantiateGridObjects(_preInitGridObjects);
            DisplayGrid(_displayGridOnStart);
        }

        void OnApplicationQuit()
        {
            DisplayGrid(false);
        }

        void PreInstantiateGridObjects(List<PreInitObject> preInitObject)
        {
            bool PreInitBuild(Vector2Int tilePos, GridPlacementObjectSO buildObject, GridPlacementObjectSO.Dir dir, out PlacedGridObject preInitedPlacedObject)
            {
                preInitedPlacedObject = null;
                tilePos += Vector2Int.FloorToInt(transform.position);
                bool CheckSurroundingSpaceAtPos(Vector2Int tilePos, GridPlacementObjectSO buildObject)
                {
                    _grid.GetXY((Vector2)tilePos, out int x, out int y);
                    Vector2Int placedObjectOrigin = new Vector2Int(x, y);
                    List<Vector2Int> gridPositionList = _selectedGridObjectSO.GetGridPositionList(placedObjectOrigin, _dir);
                    foreach (Vector2Int gridPosition in gridPositionList)
                    {
                        //if the surrounding tile is outside grid bounds or can't build
                        GridObject gridObj = _grid.GetGridObject(gridPosition.x, gridPosition.y);
                        if (gridObj == null || !gridObj.CanBuild())
                        {
                            Debug.LogError($"Couldn't pre-init {buildObject.prefab.name} at tile position: {tilePos} because something is already occupying that space");
                            return false;
                        }
                    }
                    return true;
                }

                if (!CheckSurroundingSpaceAtPos(tilePos, buildObject)) return false;
                _grid.GetXY((Vector2)tilePos, out int x, out int y);
                Vector2Int placedObjectOrigin = new Vector2Int(x, y);

                Vector2Int rotationOffset = buildObject.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = _grid.GetWorldPosition(x, y) +
                    new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;

                PlacedObject placedObject = PlacedObject.Create(
                    placedObjectWorldPosition,
                    placedObjectOrigin,
                    dir,
                    buildObject);

                //this rotates the sprite a bit more for 2D
                placedObject.transform.rotation = Quaternion.Euler(0, 0, -buildObject.GetRotationAngle(dir));

                //populate other tiles that take up the dimensions of the object with info that they are taken
                List<Vector2Int> gridPositionList = buildObject.GetGridPositionList(placedObjectOrigin, dir);
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    _grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
                }

                preInitedPlacedObject = new PlacedGridObject(
                    placedObject.GetInstanceID().ToString(),
                    buildObject,
                    placedObjectWorldPosition,
                    dir);

                OnObjectPlaced?.Invoke(this, EventArgs.Empty);
                return true;
            }

            foreach (var obj in preInitObject)
            {
                if (PreInitBuild(obj.TilePosition, obj.GridObject, obj.Dir, out PlacedGridObject preInitedPlacedObject))
                {
                    _placedGridObjects.Add(preInitedPlacedObject);
                }
            }
            _OnGridUpdate?.Invoke(_placedGridObjects);
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
                        if (Build())
                        {
                            _OnGridUpdate?.Invoke(_placedGridObjects);
                        }
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
                        SelectGridObject(gridObjectIndex, _gridObjects);
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
                        if (Move())
                        {
                            _OnGridUpdate?.Invoke(_placedGridObjects);
                        }
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
                        if (Demolish(false))
                        {
                            _OnGridUpdate?.Invoke(_placedGridObjects);
                        }
                        break;
                    }
                    break;
                default:
                    Debug.LogError("Build mode doesn't exist");
                    break;
            }
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
                    SelectGridObject(0, _gridObjects);
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

            if (!gridObject.PlacedObject.IsMovable)
            {
                Debug.Log("Grid object is marked as Immovable. Not moving");
                return;
            }

            Debug.Log($"Moving {gridObject.PlacedObject.gameObject.name}");


            //check if the object exists in _gridObjects array
            int index = _gridObjects.FindIndex((x) =>
            {
                bool foundObject = gridObject.PlacedObject.gameObject.name.Contains(x.prefab.name);
                if (foundObject) Debug.Log($"Found object for move: {x.prefab.name}");
                return foundObject;
            });

            //if not, check the preInit array as well
            if (index == -1)
            {
                index = _preInitGridObjects.FindIndex((x) =>
                {
                    bool foundObject = gridObject.PlacedObject.gameObject.name.Contains(x.GridObject.prefab.name);
                    if (foundObject) Debug.Log($"Found object for move: {x.GridObject.prefab.name}");
                    return foundObject;
                });


                List<GridPlacementObjectSO> preInitObjs = new List<GridPlacementObjectSO>();
                foreach (var gridObj in _preInitGridObjects)
                {
                    preInitObjs.Add(gridObj.GridObject);
                }

                SelectGridObject(index, preInitObjs);
            }
            else
            {
                //if it's from the preinit array, select the grid object from there
                SelectGridObject(index, _gridObjects);
            }


            Demolish(true);
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

        bool Move()
        {
            if (!Build()) return false;
            _movingObject = false;
            _lastDemolish = null;
            DeselectBuildObject();
            return true;
        }

        bool Build()
        {
            if (_selectedGridObjectSO == null) return false;
            if (!CheckSurroundingSpace())
            {
                Debug.Log("Can't build, space already taken");
                return false;
            }

            int idx = _gridObjects.IndexOf(_selectedGridObjectSO);
            if (!CheckIfMaxOfObjectPlaced(idx))
            {
                Debug.Log("Can't perform build because max count reached");
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

            //Play build sound
            BuildingSound.transform.position = GetMouseWorldPosition();
            BuildingSound.PlayOneShot(_buildSound);

            _placedGridObjects.Add(new PlacedGridObject(
                placedObject.GetInstanceID().ToString(),
                _selectedGridObjectSO,
                placedObjectWorldPosition,
                _dir));
            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
            return true;
        }

        bool Demolish(bool isMoveDemolish)
        {
            GridObject gridObject = _grid.GetGridObject(GetMouseWorldPosition());
            PlacedObject placedObject = gridObject.PlacedObject;
            if (placedObject == null) return false;
            if (!isMoveDemolish && !placedObject.IsDestructable)
            {
                Debug.Log("Object is not marked as destructible, not destroying");
                return false;
            }

            _lastDemolish = gridObject.PlacedObject.GetData();
            placedObject.DestroySelf();
            List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                _grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
            }

            if (!isMoveDemolish)
            {
                //Play demolish sound
                BuildingSound.transform.position = GetMouseWorldPosition();
                BuildingSound.PlayOneShot(_demolishSound);
            }

            //remove it from the placed objects list
            int idx = _placedGridObjects.FindIndex(x => x.PlacedObjectID.Equals(_lastDemolish.PlacedObjectID));
            _placedGridObjects.RemoveAt(idx);
            return true;
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

            //update the placed list, don't need to send this info
            _placedGridObjects.Add(new PlacedGridObject(
                placedObject.GetInstanceID().ToString(),
                selectedGridObjectSO,
                placedObjectWorldPosition,
                _dir));
            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
            _lastDemolish = null;
        }

        void Rotate()
        {
            if (_selectedGridObjectSO != null && !_selectedGridObjectSO.canRotate)
            {
                Debug.Log("This grid object SO is marked as not able to rotate");
                return;
            }
            _dir = GridPlacementObjectSO.GetNextDir(_dir);
        }

        void SelectGridObject(int i, List<GridPlacementObjectSO> gridList)
        {
            _selectedGridObjectSO = gridList[i];
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