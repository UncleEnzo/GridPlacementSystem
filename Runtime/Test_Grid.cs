using UnityEngine;

public class Test_Grid : MonoBehaviour
{
    float mouseMoveTimer = 3;
    float mouseMoveTimerMax = 3;
    Grid _grid;
    void Start()
    {
        _grid = new Grid(4, 2, 1f, new Vector3(2, 0, 0));
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _grid.SetValue(GetMouseWorldPosition(), 56);
        }
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log(_grid.GetValue(GetMouseWorldPosition()));
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 vec = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        vec.z = 0f;
        return vec;
    }

    //void HandleHeatMapMouseMove()
    //{
    //    mouseMoveTimer -= Time.deltaTime;
    //    if (mouseMoveTimer < 0)
    //    {
    //        mouseMoveTimer += mouseMoveTimerMax;
    //    }
    //}
}
