using System.Collections.Generic;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class PlacedObject : MonoBehaviour
    {
        GridPlacementObjectSO _gridObjectSO;
        Vector2Int _origin;
        GridPlacementObjectSO.Dir _dir;

        public static PlacedObject Create(
            Vector3 worldPosition,
            Vector2Int origin,
            GridPlacementObjectSO.Dir dir,
            GridPlacementObjectSO gridObjectSO)
        {
            Transform placedObjectTransform = Instantiate(
                gridObjectSO.prefab,
                worldPosition,
                Quaternion.Euler(0, gridObjectSO.GetRotationAngle(dir), 0)
                );
            PlacedObject placedObject = placedObjectTransform.GetComponent<PlacedObject>();
            placedObject.Setup(gridObjectSO, origin, dir);
            return placedObject;
        }

        void Setup(GridPlacementObjectSO placedObjectTypeSO, Vector2Int origin, GridPlacementObjectSO.Dir dir)
        {
            this._gridObjectSO = placedObjectTypeSO;
            this._origin = origin;
            this._dir = dir;
        }

        public List<Vector2Int> GetGridPositionList()
        {
            return _gridObjectSO.GetGridPositionList(_origin, _dir);
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }

        public override string ToString()
        {
            return _gridObjectSO.nameString;
        }
    }
}