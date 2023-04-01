using System.Collections.Generic;
using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public enum ConstructionState { CONSTRUCTION, BUILT, NONE }

    public class PlacedObject : MonoBehaviour
    {
        [SerializeField] bool _isMovable = true;
        [SerializeField] bool _isDestructable = true;
        [SerializeField] bool _useContructionState = true;
        GridPlacementObjectSO _gridObjectSO;
        Vector2Int _origin;
        GridPlacementObjectSO.Dir _dir;
        ConstructionState _constructionState = ConstructionState.CONSTRUCTION;

        public bool IsMovable { get => _isMovable; }
        public bool IsDestructable { get => _isDestructable; }
        public ConstructionState ConstructionState
        {
            get => _useContructionState ? _constructionState : ConstructionState.NONE;
        }

        public static PlacedObject Create(
            Vector3 worldPosition,
            Vector2Int origin,
            GridPlacementObjectSO.Dir dir,
            GridPlacementObjectSO gridObjectSO,
            ConstructionState constructionState)
        {
            Transform placedObjectTransform = Instantiate(
                gridObjectSO.prefab,
                worldPosition,
                Quaternion.Euler(0, gridObjectSO.GetRotationAngle(dir), 0)
                );
            PlacedObject placedObject = placedObjectTransform.GetComponent<PlacedObject>();
            placedObject.Setup(gridObjectSO, origin, dir, constructionState);
            return placedObject;
        }
        public PlacedObjectData GetData()
        {
            return new PlacedObjectData(
                _gridObjectSO,
                _origin,
                _dir,
                _isMovable,
                _isDestructable,
                _useContructionState,
                _constructionState,
                GetInstanceID().ToString());
        }

        void Setup(
            GridPlacementObjectSO placedObjectTypeSO,
            Vector2Int origin,
            GridPlacementObjectSO.Dir dir,
            ConstructionState constructionState)
        {
            _gridObjectSO = placedObjectTypeSO;
            _origin = origin;
            _dir = dir;
            _constructionState = _useContructionState ? constructionState : ConstructionState.NONE;
            GetComponentInChildren<SpriteRenderer>().enabled = constructionState == ConstructionState.NONE || constructionState == ConstructionState.BUILT;
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