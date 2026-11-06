using Unity.Netcode;

public struct PlayerInputData : INetworkSerializable
{
    public uint Sequence;
    public MovementInput MovementInput;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Sequence);
        serializer.SerializeValue(ref MovementInput.MoveDirection);
    }
}
