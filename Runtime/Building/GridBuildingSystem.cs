using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class GridBuildingSystem : MonoBehaviour
    {
        enum BuildMode { BUILD, MOVE, DEMOLISH }

        [SerializeField] BuildMode buildMode = BuildMode.BUILD;
        [SerializeField] bool _showGrid = false;
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
        GridPlacementObjectSO selectedGridObjectSO;
        Grid<GridObject> grid;
        GridPlacementObjectSO.Dir dir = GridPlacementObjectSO.Dir.Down;
        PlacedObjectData lastDemolish = null;
        bool movingObject = false;

        public event EventHandler OnSelectedChanged;
        public event EventHandler OnObjectPlaced;
        public GridPlacementObjectSO SelectedGridObject { get => selectedGridObjectSO; }

        public void DisplayGrid(bool isDisplayed)
        {
            _showGrid = isDisplayed;
            _buildingGhost.SetActive(_showGrid);
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(_showGrid);
            }
            SetBuildMode(BuildMode.BUILD);
        }

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
        public bool CheckSurroundingSpace()
        {
            grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            Vector2Int placedObjectOrigin = new Vector2Int(x, y);
            List<Vector2Int> gridPositionList = selectedGridObjectSO.GetGridPositionList(placedObjectOrigin, dir);
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                //if the surrounding tile is outside grid bounds or can't build
                GridObject gridObj = grid.GetGridObject(gridPosition.x, gridPosition.y);
                if (gridObj == null || !gridObj.CanBuild()) return false;
            }
            return true;
        }

        void Awake()
        {
            selectedGridObjectSO = _gridObjects[0];
            grid = new Grid<GridObject>(
                _gridWidth,
                _gridHeight,
                _cellSize,
                _gridStartingPosition,
                (Grid<GridObject> g, int x, int z) =>
                new GridObject(g, x, z), _isDebug);
            CreateBuildingGhost();
            GenerateWorldTiles();
            DisplayGrid(_showGrid);
        }

        void OnApplicationQuit()
        {
            DisplayGrid(false);
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
                    tile.transform.localPosition = grid.GetWorldPosition(x, y);
                }
            }
        }

        void Update()
        {
            if (!_showGrid) return;

            if (selectedGridObjectSO == null) ResetRotation();

            if (Input.GetKeyDown(KeyCode.B)) SetBuildMode(BuildMode.BUILD);
            if (Input.GetKeyDown(KeyCode.D)) SetBuildMode(BuildMode.DEMOLISH);
            if (Input.GetKeyDown(KeyCode.M)) SetBuildMode(BuildMode.MOVE);

            switch (buildMode)
            {
                case BuildMode.BUILD:
                    if (Input.GetMouseButtonDown(0)) Build();
                    if (Input.GetKeyDown(KeyCode.R)) Rotate();
                    if (Input.GetKeyDown(KeyCode.Alpha1)) SelectGridObject(0);
                    if (Input.GetKeyDown(KeyCode.Alpha2)) SelectGridObject(1);
                    if (Input.GetKeyDown(KeyCode.Alpha0)) DeselectBuildObject();
                    break;
                case BuildMode.MOVE:
                    if (!movingObject && Input.GetMouseButtonDown(0))
                    {
                        SelectMoveObject();
                        break;
                    }
                    if (movingObject && Input.GetMouseButtonDown(0))
                    {
                        Move();
                        break;
                    }
                    if (movingObject && Input.GetKeyDown(KeyCode.R))
                    {
                        Rotate();
                        break;
                    }
                    break;
                case BuildMode.DEMOLISH:
                    if (Input.GetMouseButtonDown(0)) Demolish();
                    break;
                default:
                    Debug.LogError("Build mode doesn't exist");
                    break;
            }
        }

        void SetBuildMode(BuildMode buildMode)
        {
            if (this.buildMode == buildMode) return;
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
            if (movingObject) return;
            grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            GridObject gridObject = grid.GetGridObject(x, y);
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
            movingObject = true;
        }

        //need to call this when the application closes too
        void UndoSelectedMoveObject()
        {
            if (!movingObject) return;
            Debug.Log("Deselecting object to move");
            DeselectBuildObject();
            UndoLastDemolish();
            movingObject = false;
        }

        void Move()
        {
            if (!Build()) return;
            movingObject = false;
            lastDemolish = null;
            DeselectBuildObject();
        }

        bool Build()
        {
            if (selectedGridObjectSO == null) return false;

            if (!CheckSurroundingSpace())
            {
                Debug.Log("Can't build, space already taken");
                return false;
            }

            grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
            Vector2Int placedObjectOrigin = new Vector2Int(x, y);

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

            //populate other tiles that take up the dimensions of the object with info that they are taken
            List<Vector2Int> gridPositionList = selectedGridObjectSO.GetGridPositionList(placedObjectOrigin, dir);
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
            return true;
        }



        void Demolish()
        {
            GridObject gridObject = grid.GetGridObject(GetMouseWorldPosition());
            PlacedObject placedObject = gridObject.PlacedObject;
            if (placedObject == null) return;

            lastDemolish = gridObject.PlacedObject.GetData();
            placedObject.DestroySelf();
            List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
            }
        }

        void UndoLastDemolish()
        {
            if (lastDemolish == null)
            {
                Debug.Log("No last demolish found");
                return;
            }
            Debug.Log("Undoing last demolish");
            Vector2Int placedObjectOrigin = lastDemolish.Origin;
            var selectedGridObjectSO = lastDemolish.GridObjectSO;
            var dir = lastDemolish.Dir;


            Vector2Int rotationOffset = selectedGridObjectSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) +
                new Vector3(rotationOffset.x, rotationOffset.y) * grid.CellSize;


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
                grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
            }

            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
            lastDemolish = null;
        }

        void Rotate()
        {
            dir = GridPlacementObjectSO.GetNextDir(dir);
        }

        void SelectGridObject(int i)
        {
            selectedGridObjectSO = _gridObjects[i];
            RefreshSelectedObjectType();
        }

        void DeselectBuildObject()
        {
            selectedGridObjectSO = null;
            RefreshSelectedObjectType();
        }

        void ResetRotation()
        {
            while (dir != GridPlacementObjectSO.Dir.Down)
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