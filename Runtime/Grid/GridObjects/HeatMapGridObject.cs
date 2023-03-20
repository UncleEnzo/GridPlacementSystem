using UnityEngine;
namespace Nevelson.GridPlacementSystem
{
    public class HeatMapGridObject
    {
        const int MIN = 0;
        const int MAX = 100;
        int _x;
        int _y;
        int value;
        Grid<HeatMapGridObject> _grid;

        public HeatMapGridObject(Grid<HeatMapGridObject> grid, int x, int y)
        {
            _grid = grid;
            _x = x;
            _y = y;
        }

        public void AddValue(int addValue)
        {
            value += addValue;
            Mathf.Clamp(value, MIN, MAX);
            _grid.TriggerGridObejctChanged(_x, _y);
        }

        public float GetValueNormalized()
        {
            return (float)value / MAX;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}