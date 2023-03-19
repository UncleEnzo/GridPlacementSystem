using UnityEngine;

public class Test_Grid : MonoBehaviour
{
    Grid<HeatMapGridObject> _heatGrid;
    Grid<StringGridObject> _stringGrid;

    [SerializeField] HeatMapGenericVisual heatmapVisual;


    void Start()
    {
        //testing heat map
        _heatGrid = new Grid<HeatMapGridObject>(10, 1, 2f, new Vector3(0, 3, 0),
            (Grid<HeatMapGridObject> g, int x, int y) => new HeatMapGridObject(g, x, y), true);
        heatmapVisual.SetGrid(_heatGrid);

        //testing string grid
        _stringGrid = new Grid<StringGridObject>(10, 1, 2f, new Vector3(0, 0, 0),
            (Grid<StringGridObject> g, int x, int y) => new StringGridObject(g, x, y), true);
    }

    void Update()
    {
        Vector3 pos = GetMouseWorldPosition();
        if (Input.GetMouseButtonDown(0))
        {
            HeatMapGridObject heatMapGridObject = _heatGrid.GetGridObject(pos);
            if (heatMapGridObject != null) heatMapGridObject.AddValue(5);
            heatmapVisual.SetGrid(_heatGrid);
        }

        if (Input.GetKeyDown(KeyCode.A)) _stringGrid.GetGridObject(pos).AddLetter("A");
        if (Input.GetKeyDown(KeyCode.Alpha1)) _stringGrid.GetGridObject(pos).AddNumber("1");
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 vec = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        vec.z = 0f;
        return vec;
    }
}
