using System;
using UnityEngine.Events;

namespace IEdgeGames {

    [Serializable] public class UnitySingleEvent : UnityEvent<float> { }
    [Serializable] public class UnityInt32Event : UnityEvent<int> { }
    [Serializable] public class UnityStringEvent : UnityEvent<string> { }
    [Serializable] public class UnityBoolEvent : UnityEvent<bool> { }
}
