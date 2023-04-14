using UnityEngine.Events;

namespace Nevelson.GridPlacementSystem
{
    public interface ISubscribeGridCallbacks
    {
        void SubscribeOnBuildSuccess(UnityAction<PlacedGridObject> action);
        void SubscribeOnMoveSuccess(UnityAction<PlacedGridObject> action);
        void SubscribeOnDestroySuccess(UnityAction<string> action);
    }
}