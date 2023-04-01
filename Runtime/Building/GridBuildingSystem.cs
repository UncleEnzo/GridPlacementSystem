using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Nevelson.GridPlacementSystem
{
    public enum BuildMode { BUILD, MOVE, DEMOLISH }

    public class GridBuildingSystem : MonoBehaviour, IPreinitGrid, IGridObjectPlace
    {
        [Header("Place grid objects here you want instantiated before game starts")]
        [SerializeField] List<PreInitObject> _preInitGridObjects;

        [Header("Place objects here you want player to be able to instantiate on build")]
        [SerializeField] List<GridPlacementObjectSO> _gridObjects;
        [SerializeField] Color _canBuildTileColor;
        [SerializeField] Color _cannotBuildTileColor;
        [SerializeField] Color _occupiedTileColor;
        [SerializeField] Color _moveOrDestroyColor;
        [SerializeField] bool _displayGridOnStart = false;
        [SerializeField] int _gridWidth = 15;
        [SerializeField] int _gridHeight = 10;
        [SerializeField] float _cellSize = 1;
        [SerializeField] bool _isDebug = true;
        [SerializeField] GameObject _buildingSoundPrefab;
        [SerializeField] GameObject _worldGridSprite;
        [SerializeField] GameObject _buildingGhostPrefab;
        [SerializeField] AudioClip _buildSound;
        [SerializeField] AudioClip _moveSound;
        [SerializeField] AudioClip _demolishSound;
        [SerializeField] UnityEvent<List<PlacedGridObject>> _OnGridUpdate;

        [Header("Tile positions you don't want included in the array.  Use Debug to find positions")]
        [SerializeField] Vector2IntRanges[] ignoredTileRanges;

        BuildMode buildMode = BuildMode.BUILD;
        List<PlacedGridObject> _placedGridObjects = new List<PlacedGridObject>();
        Grid<GridObject> _grid;
        List<GridObject> previousTiles = new List<GridObject>();
        List<GridObject> previousMoveDemolishTiles = new List<GridObject>();
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

        public GridPlacementObjectSO SelectedGridObject() { return _selectedGridObjectSO; }

        public void Test_DefaultInit()
        {
            Debug.Log("Performing default init");
            _gridWidth = 20;
            _gridHeight = 20;
            _cellSize = 1;
            _displayGridOnStart = false;
            _isDebug = false;
            //_buildingSoundPrefab = Resources.Load<>();
            //_worldGridSprite = Resources.Load<>();
            //_buildingGhostPrefab = Resources.Load<>();
            //_buildSound = Resources.Load<>();
            //_demolishSound = Resources.Load<>();
            _OnGridUpdate.AddListener((List<PlacedGridObject> p) => Debug.Log(p.ToString()));
        }

        public void AddPlaceableGridObject(GridPlacementObjectSO[] gridObjects)
        {
            foreach (GridPlacementObjectSO obj in gridObjects)
            {
                _gridObjects.Add(obj);
            }
        }

        #region PreinitOperations
        public void ReplacePreInitObjectsList(List<PreInitObject> preInitObjects)
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
        #endregion

        #region GridOperations
        public BuildMode BuildMode { get => buildMode; }

        public PlacedObjectData GetPlaceObjInfoAtMousePos()
        {
            _grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            GridObject gridObject = _grid.GetGridObject(x, y);

            if (gridObject == null) return null;
            if (gridObject.PlacedObject == null) return null;

            return gridObject.PlacedObject.GetData();
        }

        public bool DisplayGrid(bool isGridDisplayed)
        {
            _isGridDisplayed = isGridDisplayed;
            _buildingGhost.SetActive(_isGridDisplayed);
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(_isGridDisplayed);
            }
            SetBuildMode(BuildMode.BUILD);
            Debug.Log($"Displaying grid: {isGridDisplayed} and Auto setting to build mode");
            return true;
        }

        public bool SetBuildMode(BuildMode buildMode)
        {
            if (!VerifyBuildAction()) return false;
            PerformRotationReset();

            if (this.buildMode == buildMode)
            {
                Debug.Log($"Not setting build mode because mode is already {buildMode}");
                return false;
            }
            this.buildMode = buildMode;
            UndoMoveDemolishTilesColors();
            UndoSelectedTilesColors();
            switch (buildMode)
            {
                case BuildMode.BUILD:
                    Debug.Log("Build Mode Activated");
                    UndoSelectedMoveObject();
                    SelectGridObject(0, _gridObjects);
                    return true;
                case BuildMode.DEMOLISH:
                    Debug.Log("Demolish Mode Activated");
                    UndoSelectedMoveObject();
                    DeselectBuildObject();
                    return true;
                case BuildMode.MOVE:
                    Debug.Log("Move Mode Activated");
                    DeselectBuildObject();
                    return true;
                default:
                    Debug.LogError("Build mode doesn't exist");
                    return false;
            }
        }

        public bool BuildSelectedObject()
        {
            if (!VerifyBuildAction()) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.BUILD)
            {
                Debug.LogWarning($"Attempting to perform build action while build mode set to: {buildMode}. Not allowing");
                return false;
            }

            Debug.Log($"Build Mode is: {buildMode}. Attempting to build: {_selectedGridObjectSO.name}");

            bool ok = Build(_buildSound);
            if (!ok)
            {
                Debug.Log($"Could not build {_selectedGridObjectSO.name} at location.");
                return false;
            }

            _OnGridUpdate?.Invoke(_placedGridObjects);
            Debug.Log($"Build of {_selectedGridObjectSO.name} was successful.");
            return ok;
        }

        public bool RotateSelectedObject()
        {
            if (!VerifyBuildAction()) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.BUILD && buildMode != BuildMode.MOVE)
            {
                Debug.LogWarning($"Attempting to perform rotate action while build mode set to: {buildMode}. Not allowing");
                return false;
            }

            if (_selectedGridObjectSO == null)
            {
                Debug.Log("Not object selected to rotate. Not Performing.");
                return false;
            }

            Debug.Log($"Build Mode is: {buildMode}, performing rotate");
            Rotate();
            return true;
        }

        public bool ChangeSelectedBuildObject(int gridObjectIndex)
        {
            if (!VerifyBuildAction()) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.BUILD)
            {
                Debug.LogWarning($"Attempting to perform change selected object build action while build mode set to: {buildMode}. Not allowing");
                return false;
            }

            if (gridObjectIndex < -1 || gridObjectIndex > _gridObjects.Count - 1)
            {
                Debug.LogError($"{gridObjectIndex} is not out of bounds of the _gridObjects list");
                return false;
            }

            if (gridObjectIndex == -1)
            {
                Debug.Log($"Build Mode is: {buildMode}, Deselecting grid object");
                DeselectBuildObject();
                return true;
            }

            Debug.Log($"Build Mode is: {buildMode}, Selecting grid object: {gridObjectIndex}");
            SelectGridObject(gridObjectIndex, _gridObjects);
            return true;

        }

        //note: this will perform both the move and the placement and just report which it is
        public bool PickAndPlaceMoveObject()
        {
            if (!VerifyBuildAction()) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.MOVE)
            {
                Debug.LogWarning($"Attempting to perform build action while build mode set to: {buildMode}. Not allowing");
                return false;
            }

            if (!_movingObject)
            {
                Debug.Log($"Build Mode is: {buildMode}, Selecting object to move");
                SelectMoveObject();
                return true;
            }

            Debug.Log($"Build Mode is: {buildMode}, moving selected object");
            bool ok = Move();
            if (!ok)
            {
                Debug.Log($"Failed to place {_selectedGridObjectSO.name} at position");
                return false;
            }

            _OnGridUpdate?.Invoke(_placedGridObjects);
            return ok;
        }

        public bool UndoMove()
        {
            if (!VerifyBuildAction()) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.MOVE)
            {
                Debug.LogWarning($"Attempting to perform build action while build mode set to: {buildMode}. Not allowing");
                return false;
            }

            if (!_movingObject)
            {
                Debug.Log($"Attempting to undo move operation but no object selected to move. Not performing.");
                return false;
            }

            Debug.Log($"Build Mode is: {buildMode}, undoing move of {_selectedGridObjectSO.name}");
            UndoSelectedMoveObject();
            DeselectBuildObject();
            return true;
        }

        public bool DemolishObject()
        {
            if (!VerifyBuildAction()) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.DEMOLISH)
            {
                Debug.LogWarning($"Attempting to perform demolish action while build mode set to: {buildMode}. Not allowing");
                return false;
            }

            Debug.Log($"Build Mode is: {buildMode}, performing Demolish");
            bool ok = Demolish(false);
            if (!ok)
            {
                Debug.Log($"Did not demolish object at position");
                return false;
            }

            _OnGridUpdate?.Invoke(_placedGridObjects);
            return ok;
        }
        #endregion

        void Start()
        {
            Debug.Log($"Starting grid:\n" +
                $"Preinit objects {_preInitGridObjects.Count}\n" +
                $"width {_gridWidth} x height {_gridHeight} | Cellsize {_cellSize}\n" +
                $"Debug: {_isDebug}" +
                $"Transform position {transform.position}");
            _selectedGridObjectSO = _gridObjects[0];
            _grid = new Grid<GridObject>(
                _gridWidth,
                _gridHeight,
                _cellSize,
                ignoredTileRanges,
                (Grid<GridObject> g, int x, int y) =>
                {
                    //this sets the world tile and also passes its reference to the grid object
                    //so it can control its color
                    GameObject tile = Instantiate(_worldGridSprite, transform);
                    tile.transform.localPosition = new Vector3(x, y) * _cellSize;
                    Vector3Int gridTransform = Vector3Int.RoundToInt(tile.transform.localPosition);
                    return new GridObject(g,
                        gridTransform.x,
                        gridTransform.y,
                        tile,
                        _occupiedTileColor,
                        _canBuildTileColor,
                        _cannotBuildTileColor,
                        _moveOrDestroyColor);
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
                Vector2Int tilePosWithTransOffset = tilePos + Vector2Int.FloorToInt(transform.position);



                bool CheckSurroundingSpaceAtPos(Vector2Int tilePosWithTransOffset, Vector2 tilePosDebug, GridPlacementObjectSO buildObject)
                {
                    _grid.GetXY((Vector2)tilePosWithTransOffset, out int x, out int y);
                    Vector2Int placedObjectOrigin = new Vector2Int(x, y);
                    List<Vector2Int> gridPositionList = _selectedGridObjectSO.GetGridPositionList(placedObjectOrigin, dir);
                    foreach (Vector2Int gridPosition in gridPositionList)
                    {
                        //if the surrounding tile is outside grid bounds or can't build
                        GridObject gridObj = _grid.GetGridObject(gridPosition.x, gridPosition.y);
                        if (gridObj == null || !gridObj.CanBuild())
                        {
                            Debug.LogError($"Couldn't pre-init {buildObject.prefab.name} at global position: {tilePosWithTransOffset} / Tile position: {tilePosDebug} because something is already occupying that space");
                            return false;
                        }
                    }
                    return true;
                }

                if (!CheckSurroundingSpaceAtPos(tilePosWithTransOffset, tilePos, buildObject)) return false;
                _grid.GetXY((Vector2)tilePosWithTransOffset, out int x, out int y);
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
                    tilePos,
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

        void CreateBuildingGhost()
        {
            _buildingGhost = Instantiate(_buildingGhostPrefab);
            _buildingGhost.GetComponent<BuildingGhost>().Init(
                GetMouseWorldSnappedPosition,
                GetPlacedObjectRotation,
                CheckSurroundingSpace,
                SelectedGridObject,
                UndoSelectedTilesColors,
                UpdateSurroundingTileColors,
                UndoMoveDemolishTilesColors,
                UpdateDestroyOrMovableTileColors,
                this);
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
            if (!Build(_moveSound)) return false;
            _movingObject = false;
            _lastDemolish = null;
            DeselectBuildObject();
            return true;
        }

        bool Build(AudioClip soundEffect)
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

            UndoMoveDemolishTilesColors();
            UndoSelectedTilesColors();

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
            BuildingSound.PlayOneShot(soundEffect);

            _placedGridObjects.Add(new PlacedGridObject(
                placedObject.GetInstanceID().ToString(),
                _selectedGridObjectSO,
                placedObjectOrigin,
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

            UndoMoveDemolishTilesColors();
            UndoSelectedTilesColors();

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
                placedObjectOrigin,
                _dir));
            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
            _lastDemolish = null;
        }

        void Rotate()
        {
            if (_selectedGridObjectSO != null && !_selectedGridObjectSO.IsRotatable)
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

        bool CheckIfMaxOfObjectPlaced(int selectedIndex)
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

        bool VerifyBuildAction()
        {
            if (!_isGridDisplayed)
            {
                Debug.LogWarning($"Not performing action because grid is not displayed");
                return false;
            }
            return true;
        }

        void PerformRotationReset()
        {
            if (_selectedGridObjectSO != null) return;

            Debug.Log("Resetting rotation");
            while (_dir != GridPlacementObjectSO.Dir.Down)
            {
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

        void UndoSelectedTilesColors()
        {
            //unset all of the previous colors
            foreach (var gridObj in previousTiles)
            {
                if (gridObj == null) continue;
                if (!gridObj.CanBuild()) continue;
                gridObj.SetVacantColor();
            }
            previousTiles.Clear();
        }

        void UndoMoveDemolishTilesColors()
        {
            //unset all of the previous colors
            foreach (var gridObj in previousMoveDemolishTiles)
            {
                if (gridObj == null) continue;
                //if (!gridObj.CanBuild()) continue;
                gridObj.SetOccupiedColor();
            }
            previousMoveDemolishTiles.Clear();
        }

        void UpdateDestroyOrMovableTileColors()
        {
            UndoMoveDemolishTilesColors();

            GridObject gridObject = _grid.GetGridObject(GetMouseWorldPosition());
            if (gridObject == null) return;
            PlacedObject placedObject = gridObject.PlacedObject;
            if (placedObject == null) return;

            if (buildMode == BuildMode.MOVE && !placedObject.IsMovable)
            {
                List<Vector2Int> gridPosList = placedObject.GetGridPositionList();
                foreach (Vector2Int gridPosition in gridPosList)
                {
                    GridObject gridObj = _grid.GetGridObject(gridPosition.x, gridPosition.y);
                    gridObj.SetCannotBuildColor();
                    previousMoveDemolishTiles.Add(gridObj);
                }
                return;
            }

            if (buildMode == BuildMode.DEMOLISH && !placedObject.IsDestructable)
            {
                List<Vector2Int> gridPosList = placedObject.GetGridPositionList();
                foreach (Vector2Int gridPosition in gridPosList)
                {
                    GridObject gridObj = _grid.GetGridObject(gridPosition.x, gridPosition.y);
                    gridObj.SetCannotBuildColor();
                    previousMoveDemolishTiles.Add(gridObj);
                }
                return;
            }

            List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                GridObject gridObj = _grid.GetGridObject(gridPosition.x, gridPosition.y);
                gridObj.SetCanMoveOrDestroyColor();
                previousMoveDemolishTiles.Add(gridObj);
            }
        }

        void UpdateSurroundingTileColors()
        {
            //set colors
            _grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            Vector2Int origin = new Vector2Int(x, y);
            List<Vector2Int> gridPositionList = _selectedGridObjectSO.GetGridPositionList(origin, _dir);
            List<GridObject> newTiles = new List<GridObject>();
            if (CheckSurroundingSpace())
            {
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    GridObject gridObj = _grid.GetGridObject(gridPosition.x, gridPosition.y);
                    if (gridObj == null) continue;
                    if (!gridObj.CanBuild()) continue;
                    gridObj.SetCanBuildColor();
                    newTiles.Add(gridObj);
                }
            }
            else
            {
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    GridObject gridObj = _grid.GetGridObject(gridPosition.x, gridPosition.y);
                    if (gridObj == null) continue;
                    if (!gridObj.CanBuild()) continue;
                    gridObj.SetCannotBuildColor();
                    newTiles.Add(gridObj);
                }
            }

            previousTiles = newTiles;
        }

        //functions for ghost. Not crazy about this but whatever. too coupled
        bool CheckSurroundingSpace()
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

        Vector3 GetMouseWorldSnappedPosition()
        {
            Vector3 mousePosition = GetMouseWorldPosition();
            if (_selectedGridObjectSO == null) return mousePosition;

            _grid.GetXY(mousePosition, out int x, out int y);
            Vector2Int rotationOffset = _selectedGridObjectSO.GetRotationOffset(_dir);
            Vector3 placedObjectWorldPosition = _grid.GetWorldPosition(x, y) + new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;
            return placedObjectWorldPosition;
        }
        Quaternion GetPlacedObjectRotation()
        {
            return _selectedGridObjectSO == null ?
                Quaternion.identity :
                Quaternion.Euler(0, 0, -_selectedGridObjectSO.GetRotationAngle(_dir));
        }
    }
}