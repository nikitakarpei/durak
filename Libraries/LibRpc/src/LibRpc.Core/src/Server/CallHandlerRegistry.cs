using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Rpc.Server;

public sealed class CallHandlerRegistry
{
    private readonly ReadOnlyDictionary<string, CallHandlerDescriptor> _handlers;

    internal CallHandlerRegistry(ReadOnlyDictionary<string, CallHandlerDescriptor> handlers)
    {
        _handlers = handlers;
    }

    internal bool TryGetHandler(
        string method,
        IServiceProvider serviceProvider,
        [NotNullWhen(true)] out ICallHandler? handler
    )
    {
        if (!_handlers.TryGetValue(method, out var descriptor))
        {
            handler = null;
            return false;
        }

        handler = descriptor.CreateHandler(serviceProvider);
        return true;
    }

    public static CallHandlerRegistryBuilder CreateBuilder() => new();
}

internal abstract class CallHandlerDescriptor
{
    public abstract ICallHandler CreateHandler(IServiceProvider serviceProvider);
}

internal sealed class UnaryCallHandlerDescriptor<TRequest, TResponse, THandler> : CallHandlerDescriptor
    where TRequest : class, Google.Protobuf.IMessage, new()
    where TResponse : Google.Protobuf.IMessage
    where THandler : class, IUnaryCallHandler<TRequest, TResponse>
{
    public override ICallHandler CreateHandler(IServiceProvider serviceProvider)
    {
        var handler = serviceProvider.GetRequiredService<THandler>();
        return new UnaryCallHandlerAdapter<TRequest, TResponse>(handler);
    }
}

internal sealed class StreamingCallHandlerDescriptor<TRequest, TResponse, THandler> : CallHandlerDescriptor
    where TRequest : class, Google.Protobuf.IMessage, new()
    where TResponse : Google.Protobuf.IMessage
    where THandler : class, IStreamingCallHandler<TRequest, TResponse>
{
    public override ICallHandler CreateHandler(IServiceProvider serviceProvider)
    {
        var handler = serviceProvider.GetRequiredService<THandler>();
        return new StreamingCallHandlerAdapter<TRequest, TResponse>(handler);
    }
}

public sealed class CallHandlerRegistryBuilder
{
    private readonly Dictionary<string, CallHandlerDescriptor> _handlers = [];

    public CallHandlerRegistryBuilder AddUnaryCall<TRequest, TResponse, THandler>(string method)
        where TRequest : class, Google.Protobuf.IMessage, new()
        where TResponse : Google.Protobuf.IMessage
        where THandler : class, IUnaryCallHandler<TRequest, TResponse>
    {
        var descriptor = new UnaryCallHandlerDescriptor<TRequest, TResponse, THandler>();

        if (!_handlers.TryAdd(method, descriptor))
        {
            throw new InvalidOperationException($"Method '{method}' is already registered");
        }

        return this;
    }

    public CallHandlerRegistryBuilder AddStreamingCall<TRequest, TResponse, THandler>(string method)
        where TRequest : class, Google.Protobuf.IMessage, new()
        where TResponse : Google.Protobuf.IMessage
        where THandler : class, IStreamingCallHandler<TRequest, TResponse>
    {
        var descriptor = new StreamingCallHandlerDescriptor<TRequest, TResponse, THandler>();

        if (!_handlers.TryAdd(method, descriptor))
        {
            throw new InvalidOperationException($"Method '{method}' is already registered");
        }

        return this;
    }

    public CallHandlerRegistry Build()
    {
        return new CallHandlerRegistry(new ReadOnlyDictionary<string, CallHandlerDescriptor>(_handlers));
    }
}
