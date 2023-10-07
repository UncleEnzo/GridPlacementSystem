using UnityEngine.Events;

namespace Nevelson.GridPlacementSystem
{
    public interface IUnsubscribeGridCallbacks
    {
        void UnsubscribeOnBuildSuccess(UnityAction<PlacedGridObject> action);
        void UnsubscribeOnMoveSuccess(UnityAction<PlacedGridObject> action);
        void UnsubscribeOnUndoMoveSuccess(UnityAction<PlacedGridObject> action);
        void UnsubscribeOnDestroySuccess(UnityAction<string> action);
    }
}