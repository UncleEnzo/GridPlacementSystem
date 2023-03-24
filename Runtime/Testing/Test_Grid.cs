using UnityEngine;
using UnityEngine.Events;

namespace Nevelson.GridPlacementSystem
{
    public class Test_Grid : MonoBehaviour
    {
        [SerializeField] PreInitObject[] preInitObjs;
        [SerializeField] GameObject gbsPrefab;
        Grid<HeatMapGridObject> _heatGrid;
        Grid<StringGridObject> _stringGrid;

        [SerializeField] HeatMapGenericVisual heatmapVisual;

        [SerializeField] UnityEvent _buildButtonDown;
        [SerializeField] UnityEvent _rotateButtonUp;

        GridBuildingSystem gbs;
        GridBuildingSystem GBS
        {
            get
            {
                if (gbs == null)
                {
                    gbs = Instantiate(gbsPrefab, new Vector3(-15, -10, 0), Quaternion.identity).GetComponent<GridBuildingSystem>();
                }
                return gbs;
            }
        }


        void Start()
        {
            GBS.AddPreInitObjects(preInitObjs);

            //testing heat map
            //_heatGrid = new Grid<HeatMapGridObject>(10, 1, 2f, new Vector3(0, 3, 0),
            //    (Grid<HeatMapGridObject> g, int x, int y) => new HeatMapGridObject(g, x, y), true);
            //heatmapVisual.SetGrid(_heatGrid);

            //testing string grid
            //_stringGrid = new Grid<StringGridObject>(10, 1, 2f, new Vector3(0, 0, 0),
            //    (Grid<StringGridObject> g, int x, int y) => new StringGridObject(g, x, y), true);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                GBS.PerformBuildAction(GridBuildingSystem.BuildAction.DISPLAY_GRID);
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                GBS.PerformBuildAction(GridBuildingSystem.BuildAction.HIDE_GRID);
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                GBS.PerformBuildAction(GridBuildingSystem.BuildAction.SET_BUILD_MODE);
            }

            //D is for display so I just changed it
            if (Input.GetKeyDown(KeyCode.G))
            {
                GBS.PerformBuildAction(GridBuildingSystem.BuildAction.SET_DEMOLISH_MODE);
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                GBS.PerformBuildAction(GridBuildingSystem.BuildAction.SET_MOVE_MODE);
            }

            if (Input.GetMouseButtonDown(0))
            {
                GBS.PerformBuildAction(GridBuildingSystem.BuildAction.ACCEPT_BUTTON);
            }

            if (Input.GetMouseButtonDown(1))
            {
                GBS.PerformBuildAction(GridBuildingSystem.BuildAction.UNDO_BUTTON);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                GBS.PerformBuildAction(GridBuildingSystem.BuildAction.ROTATE);
            }

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                GBS.ChangeGridObjectToPlace(-1);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                GBS.ChangeGridObjectToPlace(0);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                GBS.ChangeGridObjectToPlace(1);
            }

            //Vector3 pos = GetMouseWorldPosition();
            //if (Input.GetMouseButtonDown(0))
            //{
            //    HeatMapGridObject heatMapGridObject = _heatGrid.GetGridObject(pos);
            //    if (heatMapGridObject != null) heatMapGridObject.AddValue(5);
            //    heatmapVisual.SetGrid(_heatGrid);
            //}

            //if (Input.GetKeyDown(KeyCode.A)) _stringGrid.GetGridObject(pos).AddLetter("A");
            //if (Input.GetKeyDown(KeyCode.Alpha1)) _stringGrid.GetGridObject(pos).AddNumber("1");
        }

        //Vector3 GetMouseWorldPosition()
        //{
        //    Vector3 vec = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //    vec.z = 0f;
        //    return vec;
        //}
    }
}