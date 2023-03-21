using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class BuildingGhost : MonoBehaviour
    {
        [SerializeField] Material canPlace;
        [SerializeField] Material cannotPlace;
        Transform visual = null;
        GridBuildingSystem gbs;
        Vector3 lastTargetPosition;

        public void Init(GridBuildingSystem gbs)
        {
            this.gbs = gbs;
        }

        void Start()
        {
            RefreshVisual();
            gbs.OnSelectedChanged += Instance_OnSelectedChanged;
        }

        void LateUpdate()
        {
            Vector3 targetPosition = gbs.GetMouseWorldSnappedPosition();
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
            transform.rotation = Quaternion.Lerp(transform.rotation, gbs.GetPlacedObjectRotation(), Time.deltaTime * 15f);
        }

        void SetGhostColor()
        {
            if (visual == null) return;
            var sr = visual.GetComponentInChildren<SpriteRenderer>();
            sr.material = gbs.CheckSurroundingSpace() ?
                canPlace : cannotPlace;
        }

        void Instance_OnSelectedChanged(object sender, System.EventArgs e)
        {
            RefreshVisual();
        }

        void RefreshVisual()
        {
            GridPlacementObjectSO placedObjectTypeSO = gbs.SelectedGridObject;
            if (placedObjectTypeSO == null)
            {
                if (visual != null)
                {
                    Destroy(visual.gameObject);
                    visual = null;
                }
                return;
            }

            if (visual != null)
            {
                Destroy(visual.gameObject);
                visual = null;
            }

            visual = Instantiate(placedObjectTypeSO.ghost, Vector3.zero, Quaternion.identity);
            visual.parent = transform;
            visual.localPosition = Vector3.zero;
            visual.localEulerAngles = Vector3.zero;
            SetGhostColor();
        }
    }
}