using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class BuildingAnimation : MonoBehaviour
    {
        [SerializeField] AnimationCurve animationCurve;
        float time;

        private void Update()
        {
            time += Time.deltaTime;

            transform.localScale = new Vector3(1, animationCurve.Evaluate(time), 1);
        }

    }
}