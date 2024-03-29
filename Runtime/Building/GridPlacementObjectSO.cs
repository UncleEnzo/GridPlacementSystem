using System.Collections.Generic;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    [CreateAssetMenu(fileName = "PlacementObject", menuName = "Building")]
    public class GridPlacementObjectSO : ScriptableObject
    {
        public static Dir GetNextDir(Dir dir)
        {
            switch (dir)
            {
                default:
                case Dir.Down: return Dir.Left;
                case Dir.Left: return Dir.Up;
                case Dir.Up: return Dir.Right;
                case Dir.Right: return Dir.Down;
            }
        }

        public enum Dir
        {
            Down,
            Left,
            Up,
            Right,
        }

        public string nameString;
        public Transform prefab;
        public Transform ghost;
        public int width;
        public int height;
        [Header("If you add an Grid Placement Object SO, this will ONLY upgrade an existing version of the UpgradeFrom Building")]
        public GridPlacementObjectSO UpgradeFrom;

        public bool IsRotatable = true;

        [Header("Maximum copies of this object that can be placed. 0 == infinite")]
        [Range(0, 1000)] public int maxPlaced = 0;


        public int GetRotationAngle(Dir dir)
        {
            switch (dir)
            {
                default:
                case Dir.Down: return 0;
                case Dir.Left: return 90;
                case Dir.Up: return 180;
                case Dir.Right: return 270;
            }
        }

        public Vector2Int GetRotationOffset(Dir dir)
        {
            switch (dir)
            {
                default:
                case Dir.Down: return new Vector2Int(0, 0);
                case Dir.Left: return new Vector2Int(0, width);
                case Dir.Up: return new Vector2Int(width, height);
                case Dir.Right: return new Vector2Int(height, 0);
            }
        }

        //Constructs a list of tile positions for this object Width/Height
        //Then takes in the supplied offset to offset each tile by the coordinates supplied
        //Ex: mouse pos + width/height of object
        public List<Vector2Int> GetGridPositionList(Vector2Int offset, Dir dir)
        {
            List<Vector2Int> VerticalGridPositionList(Vector2Int offset)
            {
                List<Vector2Int> gridPositionList = new List<Vector2Int>();
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        gridPositionList.Add(offset + new Vector2Int(x, y));
                    }
                }
                return gridPositionList;
            }

            List<Vector2Int> HorizontalGridPositionList(Vector2Int offset)
            {
                List<Vector2Int> gridPositionList = new List<Vector2Int>();
                for (int x = 0; x < height; x++)
                {
                    for (int y = 0; y < width; y++)
                    {
                        gridPositionList.Add(offset + new Vector2Int(x, y));
                    }
                }
                return gridPositionList;
            }

            switch (dir)
            {
                case Dir.Down:
                    return VerticalGridPositionList(offset);
                case Dir.Up:
                    return VerticalGridPositionList(offset);
                case Dir.Left:
                    return HorizontalGridPositionList(offset);
                case Dir.Right:
                    return HorizontalGridPositionList(offset);
                default:
                    Debug.LogError($"Direction does not exist: {dir}");
                    return new List<Vector2Int>();
            }
        }
    }
}