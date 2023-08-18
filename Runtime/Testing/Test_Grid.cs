using UnityEngine;
using UnityEngine.Events;

namespace Nevelson.GridPlacementSystem
{
    public class Test_Grid : MonoBehaviour
    {
        [SerializeField] PreInitObject[] preInitObjs;
        [SerializeField] GridPlacementObjectSO house;
        [SerializeField] GridPlacementObjectSO market;
        [SerializeField] GridPlacementObjectSO houseII;
        [SerializeField] GridPlacementObjectSO houseIII;
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
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                var placedObj = GBS.GetPlacedObjectAtMousePos();
                if (placedObj != null)
                {
                    if (placedObj.ConstructionState == ConstructionState.CONSTRUCTION)
                    {
                        Debug.Log("Setting Construction state to built");
                        placedObj.ReloadBuilding(new ReloadBuildingData()
                        {
                            ConstructionState = ConstructionState.BUILT
                        });

                    }
                    else
                    {
                        Debug.Log("Setting Construction state to CONSTRUCTION");
                        placedObj.ReloadBuilding(new ReloadBuildingData()
                        {
                            ConstructionState = ConstructionState.CONSTRUCTION
                        });
                    }

                }
                else
                {
                    Debug.Log("Did NOT set construction state");
                }
            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                var placedObj = GBS.GetPlaceObjInfoAtMousePos();
                if (placedObj != null)
                {
                    Debug.Log(placedObj.ToString());
                }
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
                GBS.SetBuildMode(BuildMode.BUILD, out string error);
            }

            //D is for display so I just changed it
            if (Input.GetKeyDown(KeyCode.G))
            {
                GBS.SetBuildMode(BuildMode.DEMOLISH, out string error);
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                GBS.SetBuildMode(BuildMode.MOVE, out string error);
            }

            if (Input.GetMouseButtonDown(0))
            {
                switch (GBS.BuildMode)
                {
                    case BuildMode.BUILD:
                        GBS.BuildSelectedObject(out string error);
                        if (error != null && !error.Equals(""))
                        {
                            Debug.Log("Error is: " + error);
                        }
                        break;
                    case BuildMode.MOVE:
                        GBS.PickAndPlaceMoveObject(out error);
                        if (error != null && !error.Equals(""))
                        {
                            Debug.Log("Error is: " + error);
                        }
                        break;
                    case BuildMode.DEMOLISH:
                        GBS.DemolishObject(out error);
                        if (error != null && !error.Equals(""))
                        {
                            Debug.Log("Error is: " + error);
                        }
                        break;
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                GBS.UndoMove(out string error);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                GBS.RotateSelectedObject(out string error);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                GBS.ChangeSelectedBuildObject(house, out string error);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                GBS.ChangeSelectedBuildObject(market, out string error);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                GBS.ChangeSelectedBuildObject(houseII, out string error);
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                GBS.ChangeSelectedBuildObject(houseIII, out string error);
            }

            //not in the list currently
            //if (Input.GetKeyDown(KeyCode.Alpha2))
            //{
            //    GBS.ChangeSelectedBuildObject(1);
            //}

        }
    }
}