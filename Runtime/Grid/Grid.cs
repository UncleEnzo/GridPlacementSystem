using System;
using TMPro;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class Grid<TGridObject>
    {
        public event EventHandler<OnGridValueChangedEventArgs> OnGridValueChanged;
        public class OnGridValueChangedEventArgs : EventArgs
        {
            public int x;
            public int y;
        }

        float _cellSize;
        int _width;
        int _height;
        Transform _transform;

        TGridObject[,] _gridArray;
        TextMeshPro[,] debugTextArray;

        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        public float CellSize { get { return _cellSize; } }

        public Grid(int width, int height, float cellSize,
            Vector2IntRanges[] ignoredTilesRanges,
            Func<Grid<TGridObject>, int, int, TGridObject> createGridObject,
            Transform transform, bool debug = false)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            _gridArray = new TGridObject[width, height];
            _transform = transform;

            for (int x = 0; x < _gridArray.GetLength(0); x++)
            {
                for (int y = 0; y < _gridArray.GetLength(1); y++)
                {
                    if (IsIgnoredTile(new Vector2Int(x, y), ignoredTilesRanges)) continue;
                    _gridArray[x, y] = createGridObject(this, x, y);
                }
            }

            if (debug)
            {
                debugTextArray = new TextMeshPro[width, height];

                for (int x = 0; x < _gridArray.GetLength(0); x++)
                {
                    for (int y = 0; y < _gridArray.GetLength(1); y++)
                    {
                        Vector2Int xy = new Vector2Int(x, y);
                        if (IsIgnoredTile(xy, ignoredTilesRanges)) continue;
                        var pos = new Vector3(x, y) * _cellSize;
                        debugTextArray[x, y] = StaticFactory.CreateWorldText(
                         transform,
                        $"{x},{y}",
                         GetWorldPosition(xy) + (new Vector3(cellSize, cellSize, 0) * .5f), //centers the textmesh
                         4,
                         Color.white);
                        Debug.DrawLine(
                            GetWorldPosition(xy),
                            GetWorldPosition(xy + Vector2Int.up),
                            Color.white, 1000f);
                        Debug.DrawLine(
                            GetWorldPosition(xy),
                            GetWorldPosition(xy + Vector2Int.right),
                            Color.white, 1000f);
                    }
                }

                Debug.DrawLine(
                    GetWorldPosition(new Vector2Int(0, height)),
                    GetWorldPosition(new Vector2Int(width, height)),
                    Color.white, 1000f);
                Debug.DrawLine(
                    GetWorldPosition(new Vector2Int(width, 0)),
                    GetWorldPosition(new Vector2Int(width, height)),
                    Color.white, 1000f);

                OnGridValueChanged += (object sender, OnGridValueChangedEventArgs eventArgs) =>
                {
                    debugTextArray[eventArgs.x, eventArgs.y].text = _gridArray[eventArgs.x, eventArgs.y]?.ToString();
                };
            }

            Debug.Log($"Grid is: {width} x {height}");
        }

        public void SetGridObject(Vector3 worldPosition, TGridObject value)
        {
            Vector2Int v = GetXY(worldPosition);
            SetGridObject(v.x, v.y, value);
        }

        public void SetGridObject(int x, int y, TGridObject value)
        {
            if (x < 0 || y < 0) return;
            if (x >= _width || y >= _height) return;
            _gridArray[x, y] = value;
            debugTextArray[x, y].text = _gridArray[x, y].ToString();
            if (OnGridValueChanged != null) OnGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, y = y });
        }

        public void TriggerGridObjectChanged(int x, int y)
        {
            if (OnGridValueChanged != null) OnGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, y = y });
        }

        public TGridObject GetGridObject(Vector3 worldPosition)
        {
            Vector2Int xy = GetXY(worldPosition);
            return GetGridObject(xy);
        }

        public TGridObject GetGridObject(Vector2Int gridPos)
        {
            if (gridPos.x < 0 || gridPos.y < 0) return default;
            if (gridPos.x >= _width || gridPos.y >= _height) return default;
            return _gridArray[gridPos.x, gridPos.y];
        }

        public Vector2Int GetXY(Vector3 worldPosition)
        {
            int x = Mathf.FloorToInt((worldPosition - _transform.position).x / _cellSize);
            int y = Mathf.FloorToInt((worldPosition - _transform.position).y / _cellSize);
            return new Vector2Int(x, y);
        }

        public Vector3 GetWorldPosition(Vector2Int worldPos)
        {
            return new Vector3(worldPos.x, worldPos.y) * _cellSize + _transform.position;
        }

        bool IsIgnoredTile(Vector2Int pos, Vector2IntRanges[] ignoredTilesRanges)
        {
            foreach (var vectorRange in ignoredTilesRanges)
            {
                if (vectorRange.IsBetweenRange(pos.x, pos.y)) return true;
            }
            return false;
        }
    }
}