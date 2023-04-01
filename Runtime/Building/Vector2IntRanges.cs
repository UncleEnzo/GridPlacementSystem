using System;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    [Serializable]
    public class Vector2IntRanges
    {
        public Vector2Int start;
        public Vector2Int end;

        public bool IsBetweenRange(Vector2Int vector2Int)
        {
            return IsBetweenRange(vector2Int.x, vector2Int.y);
        }

        public bool IsBetweenRange(int x, int y)
        {
            if (start.x < end.x)
            {
                if (x < start.x || x > end.x)
                {
                    return false;
                }
            }
            else
            {
                if (x > start.x || x < end.x)
                {
                    return false;
                }
            }

            if (start.y < end.y)
            {
                if (y < start.y || y > end.y)
                {
                    return false;
                }
            }
            else
            {
                if (y > start.y || y < end.y)
                {
                    return false;
                }
            }
            return true;
        }
    }
}