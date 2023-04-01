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
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {

            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                var placedObj = GBS.GetPlaceObjInfoAtMousePos();
                if (placedObj != null) Debug.Log(placedObj.ToString());
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                GBS.DisplayGrid(true);
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                GBS.DisplayGrid(false);
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                GBS.SetBuildMode(BuildMode.BUILD);
            }

            //D is for display so I just changed it
            if (Input.GetKeyDown(KeyCode.G))
            {
                GBS.SetBuildMode(BuildMode.DEMOLISH);
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                GBS.SetBuildMode(BuildMode.MOVE);
            }

            if (Input.GetMouseButtonDown(0))
            {
                switch (GBS.BuildMode)
                {
                    case BuildMode.BUILD:
                        GBS.BuildSelectedObject();
                        break;
                    case BuildMode.DEMOLISH:
                        GBS.DemolishObject();
                        break;
                    case BuildMode.MOVE:
                        GBS.PickAndPlaceMoveObject();
                        break;
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                GBS.UndoMove();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                GBS.RotateSelectedObject();
            }

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                GBS.ChangeSelectedBuildObject(-1);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                GBS.ChangeSelectedBuildObject(0);
            }

            //not in the list currently
            //if (Input.GetKeyDown(KeyCode.Alpha2))
            //{
            //    GBS.ChangeSelectedBuildObject(1);
            //}
        }
    }
}