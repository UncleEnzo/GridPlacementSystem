using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class BuildingGhost : MonoBehaviour
    {
        [SerializeField] Material canPlace;
        [SerializeField] Material cannotPlace;
        Transform visual = null;
        Vector3 lastTargetPosition;

        void Start()
        {
            RefreshVisual();
            GridBuildingSystem.Instance.OnSelectedChanged += Instance_OnSelectedChanged;
        }

        void LateUpdate()
        {
            Vector3 targetPosition = GridBuildingSystem.Instance.GetMouseWorldSnappedPosition();
            if (lastTargetPosition == null || targetPosition != lastTargetPosition)
            {
                SetGhostColor();
                lastTargetPosition = targetPosition;
            }

            Animate(targetPosition);
        }

        void Animate(Vector3 targetPosition)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, GridBuildingSystem.Instance.GetPlacedObjectRotation(), Time.deltaTime * 15f);
        }

        void SetGhostColor()
        {
            var sr = visual.GetComponentInChildren<SpriteRenderer>();
            sr.material = GridBuildingSystem.Instance.CheckSurroundingSpace() ?
                canPlace : cannotPlace;
        }

        void Instance_OnSelectedChanged(object sender, System.EventArgs e)
        {
            RefreshVisual();
        }

        void RefreshVisual()
        {
            if (visual != null)
            {
                Destroy(visual.gameObject);
                visual = null;
            }

            GridPlacementObjectSO placedObjectTypeSO = GridBuildingSystem.Instance.SelectedGridObject;

            if (placedObjectTypeSO != null)
            {
                visual = Instantiate(placedObjectTypeSO.ghost, Vector3.zero, Quaternion.identity);
                visual.parent = transform;
                visual.localPosition = Vector3.zero;
                visual.localEulerAngles = Vector3.zero;
                SetGhostColor();
            }
        }
    }
}