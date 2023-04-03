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
        [SerializeField] bool _displayGridOnStart = false;
        [SerializeField] bool _isDebug = true;
        [SerializeField] bool _showContructionTilesOnGridHide = true;
        [SerializeField] int _gridWidth = 15;
        [SerializeField] int _gridHeight = 10;
        [SerializeField] float _cellSize = 1;
        [SerializeField] GameObject _buildingSoundPrefab;
        [SerializeField] GameObject _worldGridSprite;
        [SerializeField] GameObject _buildingGhostPrefab;
        [SerializeField] AudioClip _buildSound;
        [SerializeField] AudioClip _moveSound;
        [SerializeField] AudioClip _demolishSound;
        [SerializeField] Color _canBuildTileColor;
        [SerializeField] Color _cannotBuildTileColor;
        [SerializeField] Color _occupiedTileColor;
        [SerializeField] Color _moveOrDestroyColor;

        [Header("Place grid objects here you want instantiated before game starts")]
        [SerializeField] List<PreInitObject> _preInitGridObjects;

        [Header("Place objects here you want player to be able to instantiate on build")]
        [SerializeField] List<GridPlacementObjectSO> _gridObjects;

        [Header("Use this event for callbacks that do stuff with the update grid data (like saving)")]
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
        PlacedObjectData _lastDemolishPlaceData = null;
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

        //IDEA: LOAD THE PREFAB
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

        public PlacedObject GetPlacedObjectAtMousePos()
        {
            _grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            GridObject gridObject = _grid.GetGridObject(x, y);
            if (gridObject == null) return null;
            if (gridObject.PlacedObject == null) return null;
            return gridObject.PlacedObject;
        }

        public PlacedObjectData GetPlaceObjInfoAtMousePos()
        {
            return GetPlacedObjectAtMousePos().GetData();
        }

        public bool DisplayGrid(bool isGridDisplayed)
        {
            SetBuildMode(BuildMode.BUILD, out string error);
            _isGridDisplayed = isGridDisplayed; //do this set build mode or won't work
            _buildingGhost.SetActive(_isGridDisplayed);
            ShowOrHideGridTiles();
            Debug.Log($"Displaying grid: {isGridDisplayed} and Auto setting to build mode");
            return true;
        }

        public bool SetBuildMode(BuildMode buildMode, out string error)
        {
            if (!VerifyBuildAction(out error)) return false;
            PerformRotationReset();

            if (this.buildMode == buildMode)
            {
                error = $"Not setting build mode because mode is already {buildMode}";
                Debug.Log(error);
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
                    error = $"Build mode doesn't exist";
                    Debug.LogError(error);
                    return false;
            }
        }

        public bool BuildSelectedObject(out string error)
        {
            if (!VerifyBuildAction(out error)) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.BUILD)
            {
                error = $"Attempting to perform build action while build mode set to: {buildMode}. Not allowing";
                Debug.LogWarning(error);
                return false;
            }

            Debug.Log($"Build Mode is: {buildMode}. Attempting to build: {_selectedGridObjectSO.name}");

            bool ok = Build(_buildSound, _selectedGridObjectSO, ConstructionState.CONSTRUCTION, out error);
            if (!ok)
            {
                Debug.Log($"Could not build {_selectedGridObjectSO.name} at location.");
                return false;
            }

            _OnGridUpdate?.Invoke(_placedGridObjects);
            Debug.Log($"Build of {_selectedGridObjectSO.name} was successful.");
            return ok;
        }

        public bool RotateSelectedObject(out string error)
        {
            if (!VerifyBuildAction(out error)) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.BUILD && buildMode != BuildMode.MOVE)
            {
                error = $"Attempting to perform rotate action while build mode set to: {buildMode}. Not allowing";
                Debug.LogWarning(error);
                return false;
            }

            if (_selectedGridObjectSO == null)
            {
                error = $"No object selected to rotate";
                Debug.Log(error);
                return false;
            }

            Debug.Log($"Build Mode is: {buildMode}, performing rotate");
            if (!Rotate(out error))
            {
                return false;
            }

            return true;
        }

        public bool ChangeSelectedBuildObject(int gridObjectIndex, out string error)
        {
            if (!VerifyBuildAction(out error)) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.BUILD)
            {
                error = $"Attempting to perform change selected object build action while build mode set to: {buildMode}. Not allowing";
                Debug.LogWarning(error);
                return false;
            }

            if (gridObjectIndex < -1 || gridObjectIndex > _gridObjects.Count - 1)
            {
                error = $"{gridObjectIndex} is out of bounds of the _gridObjects list";
                Debug.LogError(error);
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
        public bool PickAndPlaceMoveObject(out string error)
        {
            if (!VerifyBuildAction(out error)) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.MOVE)
            {
                error = $"Attempting to perform build action while build mode set to: {buildMode}. Not allowing";
                Debug.LogWarning(error);
                return false;
            }

            if (!_movingObject)
            {
                Debug.Log($"Build Mode is: {buildMode}, Selecting object to move");
                if (!SelectMoveObject(out error))
                {
                    return false;
                }
                return true;
            }

            Debug.Log($"Build Mode is: {buildMode}, moving selected object");
            if (!Move(out error))
            {
                Debug.Log($"Failed to place {_selectedGridObjectSO.name} at position");
                return false;
            }

            _OnGridUpdate?.Invoke(_placedGridObjects);
            return true;
        }

        public bool UndoMove(out string error)
        {
            if (!VerifyBuildAction(out error)) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.MOVE)
            {
                error = $"Attempting to perform build action while build mode set to: {buildMode}. Not allowing";
                Debug.LogWarning(error);
                return false;
            }

            if (!_movingObject)
            {
                error = $"No object selected to move";
                Debug.Log(error);
                return false;
            }

            Debug.Log($"Build Mode is: {buildMode}, undoing move of {_selectedGridObjectSO.name}");
            UndoSelectedMoveObject();
            DeselectBuildObject();
            return true;
        }

        public bool DemolishObject(out string error)
        {
            if (!VerifyBuildAction(out error)) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.DEMOLISH)
            {
                error = $"Attempting to perform demolish action while build mode set to: {buildMode}. Not allowing";
                Debug.LogWarning(error);
                return false;
            }

            Debug.Log($"Build Mode is: {buildMode}, performing Demolish");
            GridObject gridObject = _grid.GetGridObject(GetMouseWorldPosition());
            if (!Demolish(false, gridObject, out error))
            {
                Debug.Log(error);
                return false;
            }

            _OnGridUpdate?.Invoke(_placedGridObjects);
            return true;
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

        bool SetNewBuildingState(ConstructionState constructionState, GridObject gridObject)
        {
            //not doing verify cause we want this done even on NON displayed grids
            if (!Demolish(true, gridObject, out string error))
            {
                Debug.Log(error);
                return false;
            }

            _lastDemolishPlaceData.ConstructionState = constructionState;
            UndoLastDemolish();
            _OnGridUpdate?.Invoke(_placedGridObjects);

            //Calling this again to HIDE tiles
            ShowOrHideGridTiles();
            return true;
        }

        void PreInstantiateGridObjects(List<PreInitObject> preInitObject)
        {
            bool PreInitBuild(
                Vector2Int tilePos,
                GridPlacementObjectSO buildObject,
                GridPlacementObjectSO.Dir dir,
                ConstructionState constructionState,
                out PlacedGridObject preInitedPlacedObject)
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

                if (!CheckSurroundingSpaceAtPos(tilePosWithTransOffset, tilePos, buildObject))
                {
                    return false;
                }

                _grid.GetXY((Vector2)tilePosWithTransOffset, out int x, out int y);
                Vector2Int placedObjectOrigin = new Vector2Int(x, y);
                Vector2Int rotationOffset = buildObject.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = _grid.GetWorldPosition(x, y) +
                    new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;

                GridObject gridObject = _grid.GetGridObject(x, y);
                if (gridObject == null)
                {
                    Debug.LogError("Could not find gridobject");
                    return false;
                }

                PlacedObject placedObject = PlacedObject.Create(
                    placedObjectWorldPosition,
                    placedObjectOrigin,
                    dir,
                    buildObject,
                    gridObject,
                    constructionState,
                    SetNewBuildingState);

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
                    dir,
                    constructionState);

                OnObjectPlaced?.Invoke(this, EventArgs.Empty);
                return true;
            }

            foreach (var obj in preInitObject)
            {
                if (PreInitBuild(obj.TilePosition, obj.GridObject, obj.Dir, obj.ConstructionState, out PlacedGridObject preInitedPlacedObject))
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

        bool SelectMoveObject(out string error)
        {
            error = "";
            if (_movingObject)
            {
                error = "Already moving an object";
                return false;
            }
            _grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            GridObject gridObject = _grid.GetGridObject(x, y);
            if (gridObject == null || gridObject.PlacedObject == null)
            {
                error = "No object to move";
                Debug.Log(error);
                return false;
            }

            if (!gridObject.PlacedObject.IsMovable)
            {
                error = "Can't move this object";
                Debug.Log(error);
                return false;
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

            if (!Demolish(true, gridObject, out error))
            {
                return false;
            }

            _movingObject = true;
            return true;
        }

        void UndoSelectedMoveObject()
        {
            if (!_movingObject) return;
            Debug.Log("Deselecting object to move");
            DeselectBuildObject();
            UndoLastDemolish();
            _movingObject = false;
        }

        bool Move(out string error)
        {
            error = "";
            if (!Build(
                _moveSound,
                _selectedGridObjectSO,
                _lastDemolishPlaceData.ConstructionState,
                out error))
            {
                return false;
            }
            _movingObject = false;
            _lastDemolishPlaceData = null;
            DeselectBuildObject();
            return true;
        }

        bool Build(AudioClip soundEffect,
            GridPlacementObjectSO gridPlacementObject,
            ConstructionState constructionState,
            out string error)
        {
            error = "";
            if (gridPlacementObject == null)
            {
                error = "No object to build";
                return false;
            }

            if (!CheckSurroundingSpace(gridPlacementObject))
            {
                error = "Space is occupied";
                Debug.Log(error);
                return false;
            }

            int idx = _gridObjects.IndexOf(gridPlacementObject);
            if (!CheckIfMaxOfObjectPlaced(gridPlacementObject))
            {
                error = "Can't place any more of this object";
                Debug.Log(error);
                return false;
            }

            UndoMoveDemolishTilesColors();
            UndoSelectedTilesColors();

            _grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            Vector2Int placedObjectOrigin = new Vector2Int(x, y);
            Vector2Int rotationOffset = gridPlacementObject.GetRotationOffset(_dir);
            Vector3 placedObjectWorldPosition = _grid.GetWorldPosition(x, y) +
                new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;

            GridObject gridObject = _grid.GetGridObject(x, y);
            if (gridObject == null)
            {
                error = "Could not find gridobject";
                Debug.LogError(error);
                return false;
            }

            PlacedObject placedObject = PlacedObject.Create(
                placedObjectWorldPosition,
                placedObjectOrigin,
                _dir,
                gridPlacementObject,
                gridObject,
                constructionState,
                SetNewBuildingState);

            //this rotates the sprite a bit more for 2D
            placedObject.transform.rotation = Quaternion.Euler(0, 0, -gridPlacementObject.GetRotationAngle(_dir));

            //populate other tiles that take up the dimensions of the object with info that they are taken
            List<Vector2Int> gridPositionList = gridPlacementObject.GetGridPositionList(placedObjectOrigin, _dir);
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                _grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
            }

            //Play build sound
            BuildingSound.transform.position = GetMouseWorldPosition();
            BuildingSound.PlayOneShot(soundEffect);

            _placedGridObjects.Add(new PlacedGridObject(
                placedObject.GetInstanceID().ToString(),
                gridPlacementObject,
                placedObjectOrigin,
                _dir,
                constructionState));
            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
            return true;
        }

        bool Demolish(bool isMoveDemolish, GridObject gridObject, out string error)
        {
            error = "";
            if (gridObject == null || gridObject.PlacedObject == null)
            {
                error = "Nothing to demolish";
                Debug.Log(error);
                return false;
            }

            PlacedObject placedObject = gridObject.PlacedObject;

            if (!isMoveDemolish && !placedObject.IsDestructable)
            {
                error = "Object is indestructable";
                Debug.Log(error);
                return false;
            }

            UndoMoveDemolishTilesColors();
            UndoSelectedTilesColors();

            _lastDemolishPlaceData = gridObject.PlacedObject.GetData();
            Debug.Log($"Placed object rotation is: {_lastDemolishPlaceData.Dir}");
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
            int idx = _placedGridObjects.FindIndex(x => x.PlacedObjectID.Equals(_lastDemolishPlaceData.PlacedObjectID));
            _placedGridObjects.RemoveAt(idx);
            return true;
        }

        void UndoLastDemolish()
        {
            if (_lastDemolishPlaceData == null)
            {
                Debug.Log("No last demolish found");
                return;
            }

            Debug.Log("Undoing last demolish");
            Vector2Int placedObjectOrigin = _lastDemolishPlaceData.Origin;
            GridPlacementObjectSO selectedGridObjectSO = _lastDemolishPlaceData.GridObjectSO;
            GridPlacementObjectSO.Dir dir = _lastDemolishPlaceData.Dir;
            ConstructionState constructionState = _lastDemolishPlaceData.ConstructionState;

            Vector2Int rotationOffset = selectedGridObjectSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = _grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) +
                new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;

            GridObject gridObject = _grid.GetGridObject(placedObjectOrigin.x, placedObjectOrigin.y);
            if (gridObject == null)
            {
                Debug.LogError("Could nto find the grid object");
                return;
            }


            PlacedObject placedObject = PlacedObject.Create(
                placedObjectWorldPosition,
                placedObjectOrigin,
                dir,
                selectedGridObjectSO,
                gridObject,
                constructionState,
                SetNewBuildingState);

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
                _dir, constructionState));
            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
            _lastDemolishPlaceData = null;
        }

        bool Rotate(out string error)
        {
            error = "";
            if (_selectedGridObjectSO != null && !_selectedGridObjectSO.IsRotatable)
            {
                error = "This object can't be rotated";
                Debug.Log(error);
                return false;
            }
            _dir = GridPlacementObjectSO.GetNextDir(_dir);
            return true;
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

        bool CheckIfMaxOfObjectPlaced(GridPlacementObjectSO gridObjectSO)
        {
            //if the max placed is 0, the limit is infinite
            if (gridObjectSO.maxPlaced == 0) return true;

            int placedObjCount = _placedGridObjects.Where(x => x.GridObjectSO == gridObjectSO).Count();
            if (placedObjCount >= gridObjectSO.maxPlaced)
            {
                Debug.Log($"Cannot place more {gridObjectSO.name}. Max count {gridObjectSO.maxPlaced}. Number placed {placedObjCount}");
                return false;
            }
            return true;
        }

        bool VerifyBuildAction(out string error)
        {
            error = "";
            if (!_isGridDisplayed)
            {
                error = $"Not performing action because grid is not displayed";
                Debug.LogWarning(error);
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
                Rotate(out string error);
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
            if (CheckSurroundingSpace(_selectedGridObjectSO))
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
        bool CheckSurroundingSpace(GridPlacementObjectSO gridPlacementObjectSO)
        {
            _grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            Vector2Int placedObjectOrigin = new Vector2Int(x, y);
            List<Vector2Int> gridPositionList = gridPlacementObjectSO.GetGridPositionList(placedObjectOrigin, _dir);
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                //if the surrounding tile is outside grid bounds or can't build
                GridObject gridObj = _grid.GetGridObject(gridPosition.x, gridPosition.y);
                if (gridObj == null || !gridObj.CanBuild()) return false;
            }
            return true;
        }

        void ShowOrHideGridTiles()
        {
            foreach (Transform child in transform)
            {
                GridObject gridObj = _grid.GetGridObject(child.position);
                if (_showContructionTilesOnGridHide)
                {
                    if (gridObj == null || gridObj.PlacedObject == null)
                    {
                        child.gameObject.SetActive(_isGridDisplayed);
                        continue;
                    }
                    if (gridObj.PlacedObject.ConstructionState == ConstructionState.CONSTRUCTION)
                    {
                        child.gameObject.SetActive(true);
                        continue;
                    }
                }
                child.gameObject.SetActive(_isGridDisplayed);
            }
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