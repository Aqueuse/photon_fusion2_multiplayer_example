using Fusion;
using UnityEngine;

public enum Actions {
    MOVE = 0,
    SHOOT = 1
}

public struct NetworkInputData : INetworkInput {
    public NetworkButtons buttons;
    public Vector2 direction;
}