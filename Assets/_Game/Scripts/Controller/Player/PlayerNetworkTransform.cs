using System;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerNetworkTransform : NetworkTransform
{
    public event Action<Vector3, int, bool> OnPositionChanged;

    protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
    {
        if(!newState.HasPositionChange) { return; }

        OnPositionChanged?.Invoke(newState.GetPosition(), newState.GetNetworkTick(), newState.IsTeleportingNextFrame);
    }
}
