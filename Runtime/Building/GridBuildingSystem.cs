using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Nevelson.GridPlacementSystem
{
    public class ReloadBuildingData
    {
        //public string ID;
        public ConstructionState ConstructionState;
    }

    public enum BuildMode { BUILD, MOVE, DEMOLISH }

    public class GridBuildingSystem : MonoBehaviour, IPreinitGrid, IGridObjectPlace, ISubscribeGridCallbacks, IUnsubscribeGridCallbacks
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

        public UnityEvent<PlacedGridObject> _OnBuild;
        public UnityEvent<PlacedGridObject> _OnMove;
        public UnityEvent<PlacedGridObject> _OnUndoMove;
        public UnityEvent<string> _OnDestroy;

        [Header("The object that gets auto selected when setting to build mode")]
        [SerializeField] GridPlacementObjectSO _defaultSelectedObject;

        [Header("Place grid objects here you want instantiated before game starts")]
        [SerializeField] List<PreInitObject> _preInitGridObjects;

        [Header("Use this event for callbacks that do stuff with the update grid data (like saving)")]
        [SerializeField] UnityEvent<List<PlacedGridObject>> _OnGridUpdate;

        [Header("Tile positions you don't want included in the array.  Use Debug to find positions")]
        [SerializeField] Vector2IntRanges[] ignoredTileRanges;

        List<PlacedGridObject> _placedGridObjects = new List<PlacedGridObject>();

        public BuildMode GBuildMode { get => buildMode; }

        BuildMode buildMode = BuildMode.BUILD;
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
            Vector2Int xy = _grid.GetXY(GetMouseWorldPosition());
            GridObject gridObject = _grid.GetGridObject(xy);
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
                    SelectGridObject(_defaultSelectedObject);
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

            bool ok = Build(_buildSound,
                _selectedGridObjectSO,
                ConstructionState.CONSTRUCTION,
                out PlacedGridObject placedGridObject,
                out error);

            if (!ok)
            {
                Debug.Log($"Could not build {_selectedGridObjectSO.name} at location.");
                return false;
            }

            _OnGridUpdate?.Invoke(_placedGridObjects);
            _OnBuild?.Invoke(placedGridObject);
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

        public bool ChangeSelectedBuildObject(GridPlacementObjectSO selectedGridObject, out string error)
        {
            if (!VerifyBuildAction(out error)) return false;
            PerformRotationReset();
            if (buildMode != BuildMode.BUILD)
            {
                error = $"Attempting to perform change selected object build action while build mode set to: {buildMode}. Not allowing";
                Debug.LogWarning(error);
                return false;
            }

            if (selectedGridObject == null)
            {
                Debug.Log($"Build Mode is: {buildMode}, Deselecting grid object");
                DeselectBuildObject();
                return true;
            }

            Debug.Log($"Build Mode is: {buildMode}, Selecting grid object: {selectedGridObject}");
            SelectGridObject(selectedGridObject);
            UndoSelectedTilesColors();
            UpdateSurroundingTileColors();
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
            if (!Move(out PlacedGridObject placedGridObject, out error))
            {
                Debug.Log($"Failed to place {_selectedGridObjectSO.name} at position");
                return false;
            }

            _OnGridUpdate?.Invoke(_placedGridObjects);
            _OnMove?.Invoke(placedGridObject);
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
            PlacedGridObject placedGridObject = UndoSelectedMoveObject();
            DeselectBuildObject();
            _OnUndoMove?.Invoke(placedGridObject);
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

            //caching now, if operation succeeds supplying the id
            string cachedDemolishedObjID = "";
            if (gridObject != null && gridObject.PlacedObject != null)
            {
                cachedDemolishedObjID = gridObject.PlacedObject.ID;
            }
            if (!Demolish(false, false, gridObject, out error))
            {
                Debug.Log(error);
                return false;
            }

            _OnDestroy?.Invoke(cachedDemolishedObjID);
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
            _selectedGridObjectSO = _defaultSelectedObject;
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
            bool CheckSurroundingSpaceAtPos(Vector2Int tilePosWithTransOffset, Vector2 tilePosDebug, PreInitObject preInitObject)
            {
                Vector2Int placedObjectOrigin = _grid.GetXY((Vector2)tilePosWithTransOffset);
                List<Vector2Int> gridPositionList = preInitObject.GridObject.GetGridPositionList(placedObjectOrigin, preInitObject.Dir);
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    //if the surrounding tile is outside grid bounds or can't build
                    GridObject gridObj = _grid.GetGridObject(gridPosition);
                    if (gridObj == null || !gridObj.CanBuild())
                    {
                        Debug.LogError($"Couldn't pre-init {preInitObject.GridObject.prefab.name} at global position: {tilePosWithTransOffset} / Tile position: {tilePosDebug} because something is already occupying that space");
                        return false;
                    }
                }
                return true;
            }

            bool PreInitBuild(
                PreInitObject preInitObject,
                out PlacedGridObject preInitedPlacedObject)
            {
                preInitedPlacedObject = null;
                Vector2Int tilePosWithTransOffset = preInitObject.TilePosition + Vector2Int.FloorToInt(transform.position);

                if (!CheckSurroundingSpaceAtPos(tilePosWithTransOffset, preInitObject.TilePosition, preInitObject))
                {
                    return false;
                }

                Vector2Int placedObjectOrigin = _grid.GetXY((Vector2)tilePosWithTransOffset);
                Vector2Int rotationOffset = preInitObject.GridObject.GetRotationOffset(preInitObject.Dir);
                Vector3 placedObjectWorldPosition = _grid.GetWorldPosition(placedObjectOrigin) +
                    new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;

                GridObject gridObject = _grid.GetGridObject(placedObjectOrigin);
                if (gridObject == null)
                {
                    Debug.LogError("Could not find gridobject");
                    return false;
                }

                PlacedObject placedObject = PlacedObject.Create(
                    preInitObject.ID,
                    placedObjectWorldPosition,
                    placedObjectOrigin,
                    preInitObject.Dir,
                    preInitObject.GridObject,
                    gridObject,
                    preInitObject.ConstructionState,
                    ReloadBuilding);

                //this rotates the sprite a bit more for 2D
                placedObject.transform.rotation = Quaternion.Euler(0, 0, -preInitObject.GridObject.GetRotationAngle(preInitObject.Dir));

                //populate other tiles that take up the dimensions of the object with info that they are taken
                List<Vector2Int> gridPositionList = preInitObject.GridObject.GetGridPositionList(placedObjectOrigin, preInitObject.Dir);
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    _grid.GetGridObject(gridPosition).SetPlacedObject(placedObject);
                }

                preInitedPlacedObject = new PlacedGridObject(
                    preInitObject.ID,
                    placedObject,
                    preInitObject.GridObject,
                    preInitObject.TilePosition,
                    preInitObject.Dir,
                    preInitObject.ConstructionState);
                _OnBuild?.Invoke(preInitedPlacedObject);
                OnObjectPlaced?.Invoke(this, EventArgs.Empty);
                return true;
            }

            foreach (var obj in preInitObject)
            {
                if (PreInitBuild(obj, out PlacedGridObject preInitedPlacedObject))
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
                ref _grid,
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
            Vector2Int xy = _grid.GetXY(GetMouseWorldPosition());
            GridObject gridObject = _grid.GetGridObject(xy);
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

            if (gridObject.PlacedObject.GridObjectSO == null)
            {
                Debug.LogError("Could not find grid object so in placed object");
                return false;
            }

            SelectGridObject(gridObject.PlacedObject.GridObjectSO);

            if (!Demolish(true, false, gridObject, out error))
            {
                return false;
            }

            _movingObject = true;
            return true;
        }

        PlacedGridObject UndoSelectedMoveObject()
        {
            if (!_movingObject) return null;
            Debug.Log("Deselecting object to move");
            UndoSelectedTilesColors();
            DeselectBuildObject();
            PlacedGridObject placedGridObject = UndoLastDemolish();
            _movingObject = false;
            return placedGridObject;
        }

        bool Move(out PlacedGridObject placedGridObject, out string error)
        {
            error = "";
            placedGridObject = null;
            if (!Build(
                _lastDemolishPlaceData.ID,
                _moveSound,
                _selectedGridObjectSO,
                _lastDemolishPlaceData.ConstructionState,
                out PlacedGridObject _placedGridObject,
                out error))
            {
                return false;
            }

            placedGridObject = _placedGridObject;
            _movingObject = false;
            _lastDemolishPlaceData = null;
            DeselectBuildObject();
            return true;
        }

        bool Build(
            AudioClip soundEffect,
            GridPlacementObjectSO gridPlacementObject,
            ConstructionState constructionState,
            out PlacedGridObject placedGridObject,
            out string error)
        {
            return Build(Guid.NewGuid().ToString(),
                soundEffect,
                gridPlacementObject,
                constructionState,
                out placedGridObject,
                out error
                );
        }

        bool Build(
            string id,
            AudioClip soundEffect,
            GridPlacementObjectSO gridPlacementObjectSO,
            ConstructionState constructionState,
            out PlacedGridObject placedGridObject,
            out string error)
        {
            error = "";
            placedGridObject = null;
            if (gridPlacementObjectSO == null)
            {
                error = "No object to build";
                return false;
            }

            if (!CheckSurroundingSpace(gridPlacementObjectSO, out error))
            {
                Debug.Log(error);
                return false;
            }

            if (!CheckIfMaxOfObjectPlaced(gridPlacementObjectSO))
            {
                error = "Can't place any more of this object";
                Debug.Log(error);
                return false;
            }

            UndoMoveDemolishTilesColors();
            UndoSelectedTilesColors();

            Vector2Int placedObjectOrigin;
            PlacedObject placedObject;
            if (gridPlacementObjectSO.UpgradeFrom == null ||
                gridPlacementObjectSO.UpgradeFrom != null && buildMode == BuildMode.MOVE)
            {
                placedObjectOrigin = _grid.GetXY(GetMouseWorldPosition());
                Vector2Int rotationOffset = gridPlacementObjectSO.GetRotationOffset(_dir);
                Vector3 placedObjectWorldPosition = _grid.GetWorldPosition(placedObjectOrigin) +
                    new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;

                GridObject gridObject = _grid.GetGridObject(placedObjectOrigin);
                if (gridObject == null)
                {
                    error = "Could not find gridobject";
                    Debug.LogError(error);
                    return false;
                }

                placedObject = PlacedObject.Create(
                    id: id,
                    worldPosition: placedObjectWorldPosition,
                    origin: placedObjectOrigin,
                    dir: _dir,
                    gridObjectSO: gridPlacementObjectSO,
                    gridObject: gridObject,
                    constructionState: constructionState,
                    reloadBuilding: ReloadBuilding);

                //this rotates the sprite a bit more for 2D
                placedObject.transform.rotation = Quaternion.Euler(0, 0, -gridPlacementObjectSO.GetRotationAngle(_dir));

                //populate other tiles that take up the dimensions of the object with info that they are taken
                List<Vector2Int> gridPositionList = gridPlacementObjectSO.GetGridPositionList(placedObjectOrigin, _dir);
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    _grid.GetGridObject(gridPosition).SetPlacedObject(placedObject);
                }
            }
            //upgrade
            else
            {
                placedObjectOrigin = _grid.GetXY(GetMouseWorldPosition());
                GridObject gridObj = _grid.GetGridObject(placedObjectOrigin);
                if (gridObj == null)
                {
                    error = "Could not find gridobject";
                    Debug.LogError(error);
                    return false;
                }

                //need to cache all these because we're about to delete
                var dir = gridObj.PlacedObject.Dir;
                placedObjectOrigin = gridObj.PlacedObject.Origin;//setting this here for later
                Vector2Int rotationOffset = gridPlacementObjectSO.GetRotationOffset(dir);
                Vector2 replacePosition = _grid.GetWorldPosition(placedObjectOrigin) + new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;

                if (!Demolish(false, true, gridObj, out error))
                {
                    Debug.Log(error);
                    return false;
                }

                placedObject = PlacedObject.Create(
                    id,
                    replacePosition,
                    placedObjectOrigin,
                    dir,
                    gridPlacementObjectSO,
                    gridObj,
                    constructionState,
                    ReloadBuilding);

                //this rotates the sprite a bit more for 2D
                placedObject.transform.rotation = Quaternion.Euler(0, 0, -gridPlacementObjectSO.GetRotationAngle(_dir));

                //populate other tiles that take up the dimensions of the object with info that they are taken
                List<Vector2Int> gridPositionList = gridPlacementObjectSO.GetGridPositionList(placedObjectOrigin, dir);
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    _grid.GetGridObject(gridPosition).SetPlacedObject(placedObject);
                }
            }

            //Play build sound
            BuildingSound.transform.position = GetMouseWorldPosition();
            BuildingSound.PlayOneShot(soundEffect);

            placedGridObject = new PlacedGridObject(
                id,
                placedObject,
                gridPlacementObjectSO,
                placedObjectOrigin,
                _dir,
                constructionState);

            _placedGridObjects.Add(placedGridObject);
            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
            return true;
        }

        bool Demolish(bool isMoveDemolish, bool isUpgrade, GridObject gridObject, out string error)
        {
            error = "";
            if (gridObject == null || gridObject.PlacedObject == null)
            {
                error = "Nothing to demolish";
                Debug.Log(error);
                return false;
            }

            PlacedObject placedObject = gridObject.PlacedObject;

            if (!isMoveDemolish && !isUpgrade && !placedObject.IsDestructable)
            {
                error = "Object is indestructable";
                Debug.Log(error);
                return false;
            }

            UndoMoveDemolishTilesColors();
            UndoSelectedTilesColors();

            _lastDemolishPlaceData = gridObject.PlacedObject.GetData();
            Debug.Log($"Placed object rotation is: {_lastDemolishPlaceData.Dir}");
            List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                _grid.GetGridObject(gridPosition).ClearPlacedObject();
            }

            if (!isMoveDemolish)
            {
                //Play demolish sound
                BuildingSound.transform.position = GetMouseWorldPosition();
                BuildingSound.PlayOneShot(_demolishSound);
            }

            //remove it from the placed objects list
            int idx = _placedGridObjects.FindIndex(x => x.ID.Equals(_lastDemolishPlaceData.ID));
            _placedGridObjects.RemoveAt(idx);

            placedObject.DestroySelf();
            return true;
        }

        PlacedGridObject UndoLastDemolish()
        {
            if (_lastDemolishPlaceData == null)
            {
                Debug.Log("No last demolish found");
                return null;
            }

            Debug.Log("Undoing last demolish");
            string placedObjectID = _lastDemolishPlaceData.ID;
            Vector2Int placedObjectOrigin = _lastDemolishPlaceData.Origin;
            GridPlacementObjectSO selectedGridObjectSO = _lastDemolishPlaceData.GridObjectSO;
            GridPlacementObjectSO.Dir dir = _lastDemolishPlaceData.Dir;
            ConstructionState constructionState = _lastDemolishPlaceData.ConstructionState;
            _lastDemolishPlaceData = null;

            Vector2Int rotationOffset = selectedGridObjectSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = _grid.GetWorldPosition(placedObjectOrigin) +
                new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;

            GridObject gridObject = _grid.GetGridObject(placedObjectOrigin);
            if (gridObject == null)
            {
                Debug.LogError("Could not find the grid object");
                return null;
            }

            PlacedObject placedObject = PlacedObject.Create(
                placedObjectID,
                placedObjectWorldPosition,
                placedObjectOrigin,
                dir,
                selectedGridObjectSO,
                gridObject,
                constructionState,
                ReloadBuilding);

            //this rotates the sprite a bit more for 2D
            placedObject.transform.rotation = Quaternion.Euler(0, 0, -selectedGridObjectSO.GetRotationAngle(dir));

            //populate other tiles that take up the dimensions of the object with info that they are taken
            List<Vector2Int> gridPositionList = selectedGridObjectSO.GetGridPositionList(placedObjectOrigin, dir);
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                _grid.GetGridObject(gridPosition).SetPlacedObject(placedObject);
            }

            //update the placed list, don't need to send this info
            PlacedGridObject placedGridObject = new PlacedGridObject(
                placedObjectID,
                placedObject,
                selectedGridObjectSO,
                placedObjectOrigin,
                _dir, constructionState);
            _placedGridObjects.Add(placedGridObject);
            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
            return placedGridObject;
        }

        PlacedGridObject ReloadBuilding(string id, ReloadBuildingData reloadBuildingData)
        {
            PlacedGridObject placedGridObject = _placedGridObjects.Find(x => x.ID.Equals(id));
            if (placedGridObject == null)
            {
                Debug.LogError("Could not find placed grid object to reload");
                return null;
            }

            //not doing verify cause we want this done even on NON displayed grids
            if (!Demolish(true, true, placedGridObject.PlacedObject.GridObject, out string error))
            {
                Debug.Log(error);
                return null;
            }

            _lastDemolishPlaceData.ConstructionState = reloadBuildingData.ConstructionState;
            PlacedGridObject newPlacedGridObject = UndoLastDemolish();
            _OnGridUpdate?.Invoke(_placedGridObjects);

            //Calling this again to HIDE tiles
            ShowOrHideGridTiles();
            return newPlacedGridObject;
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

        void SelectGridObject(GridPlacementObjectSO gridObjectSO)
        {
            _selectedGridObjectSO = gridObjectSO;
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

            //used to recursively find all upgrades and test max object against all upgraded forms
            GridPlacementObjectSO FindUpgradeBase(GridPlacementObjectSO gridObjectSO)
            {
                int retryCounter = 100;
                GridPlacementObjectSO baseObject = gridObjectSO;
                while (baseObject.UpgradeFrom != null)
                {
                    baseObject = baseObject.UpgradeFrom;
                    retryCounter--;
                    if (retryCounter <= 0)
                    {
                        Debug.LogError("Breaking endless loop trying to get upgradeFrom base");
                        break;
                    }
                }
                return baseObject;
            }

            GridPlacementObjectSO baseGridObj = FindUpgradeBase(gridObjectSO);

            //foreach obj in placedGridObjects
            //if the object == base then ++
            //or traverse EACH object's UpgradeFrom stack to base and check EACH layer for if object == base then ++
            int placedObjCount = 0;
            foreach (var placedObj in _placedGridObjects)
            {
                GridPlacementObjectSO basePlacedObj = FindUpgradeBase(placedObj.GridObjectSO);
                if (basePlacedObj == baseGridObj)
                {
                    placedObjCount++;
                }
            }

            //I think these are still correct
            if (gridObjectSO.UpgradeFrom == null && placedObjCount >= gridObjectSO.maxPlaced)
            {
                Debug.Log($"Non-Upgrade: Cannot place more {gridObjectSO.name}. Max count {gridObjectSO.maxPlaced}. Number placed {placedObjCount}");
                return false;
            }

            if (gridObjectSO.UpgradeFrom != null && placedObjCount > gridObjectSO.maxPlaced)
            {
                Debug.Log($"Upgrade: Cannot place more {gridObjectSO.name}. Max count {gridObjectSO.maxPlaced}. Number placed {placedObjCount}");
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
                if (gridObj.CanBuild())
                {
                    gridObj.SetVacantColor();
                }
                //This is to reverse the color if you set an upgrade to GREEN
                //object is not equal to null and you CAN'T build on it
                else if (!gridObj.CanBuild())
                {
                    gridObj.SetOccupiedColor();
                }
                else
                {
                    gridObj.SetVacantColor();
                }
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
                    GridObject gridObj = _grid.GetGridObject(gridPosition);
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
                    GridObject gridObj = _grid.GetGridObject(gridPosition);
                    gridObj.SetCannotBuildColor();
                    previousMoveDemolishTiles.Add(gridObj);
                }
                return;
            }

            List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                GridObject gridObj = _grid.GetGridObject(gridPosition);
                gridObj.SetCanMoveOrDestroyColor();
                previousMoveDemolishTiles.Add(gridObj);
            }
        }

        void UpdateSurroundingTileColors()
        {
            List<GridObject> newTiles = new List<GridObject>();
            if (_selectedGridObjectSO.UpgradeFrom == null ||
                _selectedGridObjectSO.UpgradeFrom != null && buildMode == BuildMode.MOVE)
            {
                Vector2Int origin = _grid.GetXY(GetMouseWorldPosition());
                List<Vector2Int> gridPositionList = _selectedGridObjectSO.GetGridPositionList(origin, _dir);
                if (CheckSurroundingSpace(_selectedGridObjectSO, out string error))
                {
                    foreach (Vector2Int gridPosition in gridPositionList)
                    {
                        GridObject gridObj = _grid.GetGridObject(gridPosition);
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
                        GridObject gridObj = _grid.GetGridObject(gridPosition);
                        if (gridObj == null) continue;
                        if (!gridObj.CanBuild()) continue;
                        gridObj.SetCannotBuildColor();
                        newTiles.Add(gridObj);
                    }
                }
            }
            else
            //Color settings for upgrading
            {
                Vector2Int mouseOrigin = _grid.GetXY(GetMouseWorldPosition());
                GridObject gridObj = _grid.GetGridObject(mouseOrigin);

                //FAIL UPGRADE CHECK
                if (gridObj == null ||
                    !gridObj.CanUpgrade(_selectedGridObjectSO.UpgradeFrom))
                {
                    List<Vector2Int> gridPositionList = _selectedGridObjectSO.GetGridPositionList(mouseOrigin, _dir);
                    foreach (Vector2Int gridPosition in gridPositionList)
                    {
                        GridObject gridObjRelativeToGhost = _grid.GetGridObject(gridPosition);
                        if (gridObjRelativeToGhost == null) continue;
                        if (!gridObjRelativeToGhost.CanBuild()) continue;
                        gridObjRelativeToGhost.SetCannotBuildColor();
                        newTiles.Add(gridObjRelativeToGhost);
                    }
                }
                //CAN UPGRADE > takes the colors for the object it is hovering over and changes them
                else
                {
                    //NOTE: if the dimensions of _selectedGridObjectSO and the upgrading object are not == then we have a problem
                    List<Vector2Int> gridPositionList = _selectedGridObjectSO.GetGridPositionList(gridObj.PlacedObject.Origin, _dir);
                    foreach (Vector2Int gridPosition in gridPositionList)
                    {
                        GridObject gridObjRelativeToOtherBuilding = _grid.GetGridObject(gridPosition);
                        if (gridObjRelativeToOtherBuilding == null) continue;
                        if (gridObjRelativeToOtherBuilding.CanBuild()) continue;
                        gridObjRelativeToOtherBuilding.SetCanBuildColor();
                        newTiles.Add(gridObjRelativeToOtherBuilding);
                    }
                }
            }
            previousTiles = newTiles;
        }

        //functions for ghost. Not crazy about this but whatever. too coupled
        bool CheckSurroundingSpace(GridPlacementObjectSO gridPlacementObjectSO, out string error)
        {
            error = "";
            //uses normal logic if there is no UpgradeFrom field OR if there is but we're moving the building
            if (gridPlacementObjectSO.UpgradeFrom == null ||
                gridPlacementObjectSO.UpgradeFrom != null && buildMode == BuildMode.MOVE)
            {
                Vector2Int placedObjectOrigin = _grid.GetXY(GetMouseWorldPosition());
                List<Vector2Int> gridPositionList = gridPlacementObjectSO.GetGridPositionList(placedObjectOrigin, _dir);
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    //if the surrounding tile is outside grid bounds or can't build
                    GridObject gridObj = _grid.GetGridObject(gridPosition);
                    if (gridObj == null || !gridObj.CanBuild())
                    {
                        error = "Space is occupied";
                        return false;
                    }
                }
                return true;
            }
            //CHECKS IF YOU CAN UPGRADE
            else
            {
                Vector2Int placedObjectOrigin = _grid.GetXY(GetMouseWorldPosition());
                GridObject gridObj = _grid.GetGridObject(placedObjectOrigin);
                if (gridObj == null ||
                    !gridObj.CanUpgrade(gridPlacementObjectSO.UpgradeFrom))
                {
                    error = "Space is not upgradable";
                    return false;
                }

                if (gridObj.PlacedObject.ConstructionState != ConstructionState.BUILT)
                {
                    error = "Cannot upgrade until construction complete";
                    return false;
                }

                return true;
            }
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

            Vector2Int xy = _grid.GetXY(mousePosition);
            Vector2Int rotationOffset = _selectedGridObjectSO.GetRotationOffset(_dir);
            Vector3 placedObjectWorldPosition = _grid.GetWorldPosition(xy) + new Vector3(rotationOffset.x, rotationOffset.y) * _grid.CellSize;
            return placedObjectWorldPosition;
        }

        Quaternion GetPlacedObjectRotation()
        {
            return _selectedGridObjectSO == null ?
                Quaternion.identity :
                Quaternion.Euler(0, 0, -_selectedGridObjectSO.GetRotationAngle(_dir));
        }

        public void SubscribeOnBuildSuccess(UnityAction<PlacedGridObject> action)
        {
            _OnBuild.AddListener(action);
        }

        public void SubscribeOnMoveSuccess(UnityAction<PlacedGridObject> action)
        {
            _OnMove.AddListener(action);
        }

        public void SubscribeUndoMoveSuccess(UnityAction<PlacedGridObject> action)
        {
            _OnUndoMove.AddListener(action);
        }

        public void SubscribeOnDestroySuccess(UnityAction<string> action)
        {
            _OnDestroy.AddListener(action);
        }

        public void UnsubscribeOnBuildSuccess(UnityAction<PlacedGridObject> action)
        {
            _OnBuild.RemoveListener(action);
        }

        public void UnsubscribeOnMoveSuccess(UnityAction<PlacedGridObject> action)
        {
            _OnMove.RemoveListener(action);
        }

        public void UnSubscribeUndoMoveSuccess(UnityAction<PlacedGridObject> action)
        {
            _OnUndoMove.RemoveListener(action);
        }

        public void UnsubscribeOnDestroySuccess(UnityAction<string> action)
        {
            _OnDestroy.RemoveListener(action);
        }
    }
}