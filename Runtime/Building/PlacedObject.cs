using System.Collections.Generic;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class PlacedObject : MonoBehaviour
    {
        public GridPlacementObjectSO _gridObjectSO;
        public Vector2Int _origin;
        public GridPlacementObjectSO.Dir _dir;

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
        public PlacedObjectData GetData()
        {
            return new PlacedObjectData(_gridObjectSO, _origin, _dir);
        }

        void Setup(GridPlacementObjectSO placedObjectTypeSO, Vector2Int origin, GridPlacementObjectSO.Dir dir)
        {
            _gridObjectSO = placedObjectTypeSO;
            _origin = origin;
            _dir = dir;
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