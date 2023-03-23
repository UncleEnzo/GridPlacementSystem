using Nevelson.GridPlacementSystem;
using System.Collections.Generic;
using UnityEngine;

public class Test_GPSCallback : MonoBehaviour
{
    public void On_GridUpdate(List<PlacedGridObject> pgo)
    {
        foreach (var obj in pgo)
        {
            Debug.Log(obj.ToString());
        }
    }
}
