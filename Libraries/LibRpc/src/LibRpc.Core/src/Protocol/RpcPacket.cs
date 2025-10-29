using Google.Protobuf;

namespace Rpc.Protocol;

public record struct RpcPacket
{
    private readonly byte[] _data;

    private RpcPacket(byte[] data)
    {
        _data = data;
    }

    public static RpcPacket FromRawBytes(byte[] data)
    {
        return new RpcPacket(data);
    }

    internal static RpcPacket FromProtoMessage(IMessage message)
    {
        return new RpcPacket(message.ToByteArray());
    }

    public readonly byte[] AsRawBytes()
    {
        return _data;
    }
}
