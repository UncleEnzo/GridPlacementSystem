using System;
using System.Collections.Generic;
using UnityEngine;
namespace Nevelson.GridPlacementSystem
{
    public class GridBuildingSystem : MonoBehaviour
    {
        public static GridBuildingSystem Instance { get; private set; }
        public event EventHandler OnSelectedChanged;
        public event EventHandler OnObjectPlaced;

        [SerializeField] List<GridPlacementObjectSO> gridObjects;
        GridPlacementObjectSO selectedGridObjectSO;
        Grid<GridObject> grid;
        GridPlacementObjectSO.Dir dir = GridPlacementObjectSO.Dir.Down;

        void Awake()
        {
            Instance = this;
            int gridWidth = 15;
            int gridHeight = 10;
            float cellSize = 2;
            grid = new Grid<GridObject>(gridWidth, gridHeight, cellSize, Vector3.zero,
                (Grid<GridObject> g, int x, int z) => new GridObject(g, x, z), true);
            selectedGridObjectSO = gridObjects[0];
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0) && selectedGridObjectSO != null)
            {
                grid.GetXY(GetMouseWorldPosition(), out int x, out int y);
                Vector2Int placedObjectOrigin = new Vector2Int(x, y);

                List<Vector2Int> gridPositionList = selectedGridObjectSO.GetGridPositionList(placedObjectOrigin, dir);

                //checks if can build in space
                bool canBuild = true;
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    GridObject gridObj = grid.GetGridObject(gridPosition.x, gridPosition.y);
                    if (gridObj == null || !gridObj.CanBuild())
                    {
                        canBuild = false;
                        break;
                    }
                }

                if (canBuild)
                {
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

                    foreach (Vector2Int gridPosition in gridPositionList)
                    {
                        grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
                    }
                    OnObjectPlaced?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Debug.Log("Can't build, space already taken");
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                dir = GridPlacementObjectSO.GetNextDir(dir);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                //this changes the selected object
                selectedGridObjectSO = gridObjects[0];
                //this refresh the building Ghost
                RefreshSelectedObjectType();
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                selectedGridObjectSO = gridObjects[1];
                RefreshSelectedObjectType();
            }

            if (Input.GetKeyDown(KeyCode.Alpha0)) { DeselectObjectType(); }

            //demolish function
            if (Input.GetMouseButtonDown(1))
            {
                GridObject gridObject = grid.GetGridObject(GetMouseWorldPosition());
                PlacedObject placedObject = gridObject.PlacedObject;
                if (placedObject != null)
                {
                    placedObject.DestroySelf();
                    List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
                    foreach (Vector2Int gridPosition in gridPositionList)
                    {
                        grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
                    }
                }
            }
        }

        public Vector3 GetMouseWorldSnappedPosition()
        {
            Vector3 mousePosition = GetMouseWorldPosition();
            grid.GetXY(mousePosition, out int x, out int y);

            if (selectedGridObjectSO != null)
            {
                Vector2Int rotationOffset = selectedGridObjectSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, y) + new Vector3(rotationOffset.x, rotationOffset.y) * grid.CellSize;
                return placedObjectWorldPosition;
            }
            else
            {
                return mousePosition;
            }
        }

        public Quaternion GetPlacedObjectRotation()
        {
            if (selectedGridObjectSO != null)
            {
                return Quaternion.Euler(0, 0, -selectedGridObjectSO.GetRotationAngle(dir));
            }
            else
            {
                return Quaternion.identity;
            }
        }

        public GridPlacementObjectSO GetPlacedObjectTypeSO()
        {
            return selectedGridObjectSO;
        }

        Vector3 GetMouseWorldPosition()
        {
            Vector3 vec = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            vec.z = 0f;
            return vec;
        }

        void DeselectObjectType()
        {
            selectedGridObjectSO = null;
            RefreshSelectedObjectType();
        }

        void RefreshSelectedObjectType()
        {
            OnSelectedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}