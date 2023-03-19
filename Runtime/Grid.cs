using System;
using TMPro;
using UnityEngine;

public class Grid
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
    int[,] _gridArray;
    Vector3 _originPosition;
    TextMeshPro[,] debugTextArray;

    public Grid(int width, int height, float cellSize, Vector3 originPosition)
    {
        _width = width;
        _height = height;
        _cellSize = cellSize;
        _gridArray = new int[width, height];
        debugTextArray = new TextMeshPro[width, height];
        _originPosition = originPosition;
        Debug.Log($"Grid is: {width} x {height}");

        for (int x = 0; x < _gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < _gridArray.GetLength(1); y++)
            {
                //Debug.Log($"Position: {x}, {y}");
                debugTextArray[x, y] = GOFactory.CreateWorldText(
                    null,
                    _gridArray[x, y].ToString(),
                    GetWorldPosition(x, y) + (Vector3.one * cellSize * .5f), //centers the textmesh
                    5,
                    Color.white);
                Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, 1000f);
                Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, 1000f);
            }
        }

        Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 1000f);
        Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 1000f);
        SetValue(2, 1, 56);
    }

    public void SetValue(Vector3 worldPosition, int value)
    {
        int x, y;
        GetXY(worldPosition, out x, out y);
        SetValue(x, y, value);
    }

    public void SetValue(int x, int y, int value)
    {
        if (x < 0 || y < 0) return;
        if (x >= _width || y >= _height) return;
        _gridArray[x, y] = value;
        debugTextArray[x, y].text = _gridArray[x, y].ToString();
        if (OnGridValueChanged != null) OnGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, y = y });
    }

    public int GetValue(Vector3 worldPosition)
    {
        int x, y;
        GetXY(worldPosition, out x, out y);
        return GetValue(x, y);
    }

    public int GetValue(int x, int y)
    {
        if (x < 0 || y < 0) return -1;
        if (x >= _width || y >= _height) return -1;
        return _gridArray[x, y];
    }

    void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition - _originPosition).x / _cellSize);
        y = Mathf.FloorToInt((worldPosition - _originPosition).y / _cellSize);
    }

    Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, y) * _cellSize + _originPosition;
    }
}
