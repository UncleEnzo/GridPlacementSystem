using System;
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
        string _id;
        GridPlacementObjectSO _gridObjectSO;
        Vector2Int _origin;
        GridPlacementObjectSO.Dir _dir;
        GridObject _gridObject;
        ConstructionState _constructionState = ConstructionState.CONSTRUCTION;
        Func<ConstructionState, GridObject, bool> _setConstructionState;

        public bool IsMovable { get => _isMovable; }
        public bool IsDestructable { get => _isDestructable; }
        public ConstructionState ConstructionState
        {
            get
            {
                if (_useContructionState)
                {
                    Debug.Log($"INSTANCE ID IS: {gameObject.GetInstanceID()}: RETURNING CONSTRUCTION STATE: {_constructionState}");
                    return _constructionState;
                }
                else
                {
                    return ConstructionState.NONE;
                }
            }
        }

        public string ID
        {
            get => _id;
        }

        public GridPlacementObjectSO GridObjectSO
        {
            get => _gridObjectSO;
        }

        public bool SetConstructionState(ConstructionState constructionState)
        {
            if (!_useContructionState)
            {
                Debug.LogWarning("Can't update construction state because this placed object does not use construction state");
                return false;
            }
            if (constructionState == ConstructionState)
            {
                Debug.LogWarning("Not updating construction state because Set state is equal to current state");
                return false;
            }
            if (constructionState == ConstructionState.NONE)
            {
                Debug.LogWarning("Not updating construction state because cannot set to NONE");
                return false;
            }
            return _setConstructionState(constructionState, _gridObject);
        }

        public static PlacedObject Create(
            string id,
            Vector3 worldPosition,
            Vector2Int origin,
            GridPlacementObjectSO.Dir dir,
            GridPlacementObjectSO gridObjectSO,
            GridObject gridObject,
            ConstructionState constructionState,
            Func<ConstructionState, GridObject, bool> setConstructionState)
        {
            Transform placedObjectTransform = Instantiate(
                gridObjectSO.prefab,
                worldPosition,
                Quaternion.Euler(0, gridObjectSO.GetRotationAngle(dir), 0)
                );
            PlacedObject placedObject = placedObjectTransform.GetComponent<PlacedObject>();

            Debug.Log($"SET CONSTRUCTION STATE TO: {constructionState}");
            placedObject.Setup(id, gridObjectSO, origin, dir, gridObject, constructionState, setConstructionState);
            return placedObject;
        }

        public PlacedObjectData GetData()
        {
            return new PlacedObjectData(
                _id,
                _gridObjectSO,
                _origin,
                _dir,
                _isMovable,
                _isDestructable,
                _useContructionState,
                _constructionState);
        }

        void Setup(
            string id,
            GridPlacementObjectSO placedObjectTypeSO,
            Vector2Int origin,
            GridPlacementObjectSO.Dir dir,
            GridObject gridObject,
            ConstructionState constructionState,
            Func<ConstructionState, GridObject, bool> setConstructionState)
        {
            _id = id;
            _gridObject = gridObject;
            _gridObjectSO = placedObjectTypeSO;
            _origin = origin;
            _dir = dir;

            Debug.Log($"Construction state is {constructionState}");
            _constructionState = _useContructionState ? constructionState : ConstructionState.NONE;

            Debug.Log($"INSTANCE ID IS: {gameObject.GetInstanceID()} Internal construction state is {_constructionState}");

            _setConstructionState = setConstructionState;
            //determine the placed object's transparency based on construction state (Should probably be a callback handled by outside items
            if (ConstructionState == ConstructionState.CONSTRUCTION)
            {
                Debug.Log("TRIGGERED THIS");
                SpriteRenderer sp = GetComponentInChildren<SpriteRenderer>();
                Color newColor = sp.color;
                newColor.a = .6f;
                sp.color = newColor;
            }
        }

        public List<Vector2Int> GetGridPositionList()
        {
            return _gridObjectSO.GetGridPositionList(_origin, _dir);
        }

        public void DestroySelf()
        {
            DestroyImmediate(gameObject);
        }

        public override string ToString()
        {
            return _gridObjectSO.nameString;
        }
    }
}