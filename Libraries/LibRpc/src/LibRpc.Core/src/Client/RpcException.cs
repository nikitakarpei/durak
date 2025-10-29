namespace Rpc.Client;

public class RpcException : Exception
{
    public RpcException(string message)
        : base(message) { }

    public RpcException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class MethodNotImplementedException : RpcException
{
    public MethodNotImplementedException()
        : base("Method not implemented on server") { }
}

public class ServerInternalException : RpcException
{
    public ServerInternalException()
        : base("Server internal error") { }
}

public class MalformedRequestException : RpcException
{
    public MalformedRequestException()
        : base("Malformed request") { }
}

public class ConnectionClosedException : RpcException
{
    public ConnectionClosedException()
        : base("Connection closed") { }
}

public class ClientDisposedException : RpcException
{
    public ClientDisposedException()
        : base("Client disposed") { }
}

public class ProtocolViolationException : RpcException
{
    public ProtocolViolationException()
        : base("Protocol violation") { }
}
