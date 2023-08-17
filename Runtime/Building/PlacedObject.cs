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
        Func<string, ReloadBuildingData, PlacedGridObject> _reloadBuilding;

        public GridObject GridObject { get => _gridObject; }

        public bool IsMovable { get => _isMovable; }
        public bool IsDestructable { get => _isDestructable; }
        public ConstructionState ConstructionState
        {
            get => _useContructionState ? _constructionState : ConstructionState.NONE;
        }

        public string ID
        {
            get => _id;
        }

        public Vector2Int Origin { get => _origin; }

        public GridPlacementObjectSO.Dir Dir { get => _dir; }

        public GridPlacementObjectSO GridObjectSO
        {
            get => _gridObjectSO;
        }

        public PlacedGridObject ReloadBuilding(ReloadBuildingData reloadBuildingData)
        {
            return _reloadBuilding(ID, reloadBuildingData);
        }

        public static PlacedObject Create(
            string id,
            Vector3 worldPosition,
            Vector2Int origin,
            GridPlacementObjectSO.Dir dir,
            GridPlacementObjectSO gridObjectSO,
            GridObject gridObject,
            ConstructionState constructionState,
            Func<string, ReloadBuildingData, PlacedGridObject> reloadBuilding)
        {
            Transform placedObjectTransform = Instantiate(
                gridObjectSO.prefab,
                worldPosition,
                Quaternion.Euler(0, gridObjectSO.GetRotationAngle(dir), 0)
                );
            PlacedObject placedObject = placedObjectTransform.GetComponent<PlacedObject>();
            placedObject.Setup(id, gridObjectSO, origin, dir, gridObject, constructionState, reloadBuilding);
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
            Func<string, ReloadBuildingData, PlacedGridObject> reloadBuilding)
        {
            _id = id;
            _gridObject = gridObject;
            _gridObjectSO = placedObjectTypeSO;
            _origin = origin;
            _dir = dir;
            _constructionState = _useContructionState ? constructionState : ConstructionState.NONE;
            _reloadBuilding = reloadBuilding;

            //determine the placed object's transparency based on construction state (Should probably be a callback handled by outside items
            if (ConstructionState == ConstructionState.CONSTRUCTION)
            {
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
            Destroy(gameObject);
        }

        public override string ToString()
        {
            return _gridObjectSO.nameString;
        }
    }
}