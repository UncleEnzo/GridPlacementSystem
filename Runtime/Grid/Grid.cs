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

        TGridObject[,] _gridArray;
        TextMeshPro[,] debugTextArray;

        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        public float CellSize { get { return _cellSize; } }

        public Grid(int width, int height, float cellSize, Func<Grid<TGridObject>, int, int, TGridObject> createGridObject, bool debug = false)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            _gridArray = new TGridObject[width, height];

            for (int x = 0; x < _gridArray.GetLength(0); x++)
            {
                for (int y = 0; y < _gridArray.GetLength(1); y++)
                {
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
                        var pos = new Vector3(x, y) * _cellSize;
                        debugTextArray[x, y] = StaticFactory.CreateWorldText(
                         null,

                        $"{pos.x},{pos.y}",
                         GetWorldPosition(x, y) + (new Vector3(cellSize, cellSize, 0) * .5f), //centers the textmesh
                         4,
                         Color.white);
                        Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, 1000f);
                        Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, 1000f);
                    }
                }
                Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 1000f);
                Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 1000f);

                OnGridValueChanged += (object sender, OnGridValueChangedEventArgs eventArgs) =>
                {
                    debugTextArray[eventArgs.x, eventArgs.y].text = _gridArray[eventArgs.x, eventArgs.y]?.ToString();
                };
            }

            Debug.Log($"Grid is: {width} x {height}");
        }

        public void SetGridObject(Vector3 worldPosition, TGridObject value)
        {
            int x, y;
            GetXY(worldPosition, out x, out y);
            SetGridObject(x, y, value);
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
            int x, y;
            GetXY(worldPosition, out x, out y);
            return GetGridObject(x, y);
        }

        public TGridObject GetGridObject(int x, int y)
        {
            if (x < 0 || y < 0) return default;
            if (x >= _width || y >= _height) return default;
            return _gridArray[x, y];
        }

        public void GetXY(Vector3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt((worldPosition).x / _cellSize);
            y = Mathf.FloorToInt((worldPosition).y / _cellSize);
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x, y) * _cellSize;
        }
    }
}