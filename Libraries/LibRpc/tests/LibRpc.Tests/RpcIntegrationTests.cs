using System.Runtime.CompilerServices;
using FluentAssertions;
using Meziantou.Extensions.Logging.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rpc.Client;
using Rpc.Server;
using Rpc.Tests.TestTransport;
using Xunit.Abstractions;

namespace Rpc.Tests;

public class RpcIntegrationTests : IDisposable
{
    private const int DefaultTimeoutMs = 5000;
    private readonly ILogger<RpcClient> _clientLogger;
    private readonly ILogger<RpcRequestHandler> _serverLogger;

    private InMemoryTransportPipe? _clientPipe;
    private InMemoryTransportPipe? _serverPipe;
    private InMemoryTransportConnection? _clientConnection;
    private InMemoryTransportConnection? _serverConnection;
    private IClient? _client;
    private RpcRequestHandler? _server;

    public RpcIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddProvider(new XUnitLoggerProvider(testOutputHelper))
        );
        _clientLogger = loggerFactory.CreateLogger<RpcClient>();
        _serverLogger = loggerFactory.CreateLogger<RpcRequestHandler>();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _clientConnection?.Dispose();
        _serverConnection?.Dispose();
    }

    private void SetupClientServer(
        IServiceProvider serviceProvider,
        CallHandlerRegistry registry,
        IEnumerable<IUnaryCallInterceptor>? unaryInterceptors = null,
        IEnumerable<IStreamingCallInterceptor>? streamingInterceptors = null
    )
    {
        unaryInterceptors ??= [];
        streamingInterceptors ??= [];

        // Create bidirectional pipes for communication
        _clientPipe = new InMemoryTransportPipe();
        _serverPipe = new InMemoryTransportPipe();
        _clientPipe.ConnectTo(_serverPipe);
        _serverPipe.ConnectTo(_clientPipe);

        // Create connections
        _clientConnection = new InMemoryTransportConnection(Guid.NewGuid(), _clientPipe);
        _serverConnection = new InMemoryTransportConnection(Guid.NewGuid(), _serverPipe);

        // Create client and server
        _client = new RpcClient(_clientConnection, _clientLogger);
        _server = new RpcRequestHandler(
            serviceProvider,
            _serverLogger,
            registry,
            unaryInterceptors,
            streamingInterceptors
        );

        // Wire up server to handle messages from connection
        _serverConnection.PacketReceived += async packet =>
        {
            try
            {
                await foreach (var response in _server.HandleAsync(packet, CancellationToken.None))
                {
                    await _serverConnection.SendAsync(response, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _serverLogger.LogError(ex, "Error handling message");
            }
        };
    }

    private static CancellationToken CreateTimeoutToken(int timeoutMs = DefaultTimeoutMs)
    {
        return new CancellationTokenSource(timeoutMs).Token;
    }

    #region Unary Call Tests

    [Fact]
    public async Task UnaryCall_SimpleRequest_ReturnsExpectedResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<EchoHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddUnaryCall<TestRequest, TestResponse, EchoHandler>("test.echo")
            .Build();

        SetupClientServer(serviceProvider, registry);

        var request = new TestRequest { Message = "Hello", Value = 42 };

        // Act
        var response = await _client!.UnaryCall<TestRequest, TestResponse>("test.echo", request, CreateTimeoutToken());

        // Assert
        response.Should().NotBeNull();
        response.Result.Should().Be("Echo: Hello");
        response.ComputedValue.Should().Be(84); // 42 * 2
    }

    [Fact]
    public async Task UnaryCall_MultipleSequentialCalls_AllSucceed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<EchoHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddUnaryCall<TestRequest, TestResponse, EchoHandler>("test.echo")
            .Build();

        SetupClientServer(serviceProvider, registry);

        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var request = new TestRequest { Message = $"Message {i}", Value = i };

            var response = await _client!.UnaryCall<TestRequest, TestResponse>(
                "test.echo",
                request,
                CreateTimeoutToken()
            );

            response.Result.Should().Be($"Echo: Message {i}");
            response.ComputedValue.Should().Be(i * 2);
        }
    }

    [Fact]
    public async Task UnaryCall_ConcurrentCalls_AllSucceed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<EchoHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddUnaryCall<TestRequest, TestResponse, EchoHandler>("test.echo")
            .Build();

        SetupClientServer(serviceProvider, registry);

        // Act
        var tasks = Enumerable
            .Range(0, 10)
            .Select(async i =>
            {
                var request = new TestRequest { Message = $"Message {i}", Value = i };

                var response = await _client!.UnaryCall<TestRequest, TestResponse>(
                    "test.echo",
                    request,
                    CreateTimeoutToken()
                );

                return (i, response);
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        foreach (var (index, response) in results)
        {
            response.Result.Should().Be($"Echo: Message {index}");
            response.ComputedValue.Should().Be(index * 2);
        }
    }

    [Fact]
    public async Task UnaryCall_WithTimeout_CompletesBeforeTimeout()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<EchoHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddUnaryCall<TestRequest, TestResponse, EchoHandler>("test.echo")
            .Build();

        SetupClientServer(serviceProvider, registry);

        var request = new TestRequest { Message = "Quick", Value = 1 };

        // Act
        var response = await _client!.UnaryCall<TestRequest, TestResponse>(
            "test.echo",
            request,
            CreateTimeoutToken(1000)
        );

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task UnaryCall_CancelledBeforeResponse_ThrowsOperationCancelledException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient(sp => new DelayedEchoHandler(500));
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddUnaryCall<TestRequest, TestResponse, DelayedEchoHandler>("test.delayed")
            .Build();

        SetupClientServer(serviceProvider, registry);

        var request = new TestRequest { Message = "Delayed", Value = 1 };

        var cts = new CancellationTokenSource();

        // Act
        var callTask = _client!.UnaryCall<TestRequest, TestResponse>("test.delayed", request, cts.Token);

        // Cancel after 50ms (before the 500ms delay completes)
        await Task.Delay(50);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await callTask);
    }

    [Fact]
    public async Task UnaryCall_MethodNotImplemented_ThrowsMethodNotFoundException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry.CreateBuilder().Build();

        SetupClientServer(serviceProvider, registry);

        var request = new TestRequest { Message = "Test", Value = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<Client.MethodNotImplementedException>(async () =>
        {
            await _client!.UnaryCall<TestRequest, TestResponse>("test.nonexistent", request, CreateTimeoutToken());
        });
    }

    [Fact]
    public async Task UnaryCall_HandlerThrowsException_ThrowsServerInternalException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ThrowingHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddUnaryCall<TestRequest, TestResponse, ThrowingHandler>("test.throwing")
            .Build();

        SetupClientServer(serviceProvider, registry);

        var request = new TestRequest { Message = "Test", Value = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<Client.ServerInternalException>(async () =>
        {
            await _client!.UnaryCall<TestRequest, TestResponse>("test.throwing", request, CreateTimeoutToken());
        });
    }

    [Fact]
    public async Task UnaryCall_ConnectionClosedDuringCall_ThrowsConnectionClosedException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient(sp => new DelayedEchoHandler(500));
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddUnaryCall<TestRequest, TestResponse, DelayedEchoHandler>("test.delayed")
            .Build();

        SetupClientServer(serviceProvider, registry);

        var request = new TestRequest { Message = "Test", Value = 1 };

        // Act
        var callTask = _client!.UnaryCall<TestRequest, TestResponse>("test.delayed", request, CreateTimeoutToken());

        // Close connection while call is in progress
        await Task.Delay(50);
        _clientConnection!.Close();

        // Assert
        await Assert.ThrowsAsync<Client.ConnectionClosedException>(async () => await callTask);
    }

    [Fact]
    public async Task UnaryCall_ClientDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry.CreateBuilder().Build();

        SetupClientServer(serviceProvider, registry);

        var request = new TestRequest { Message = "Test", Value = 1 };

        _client!.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await _client.UnaryCall<TestRequest, TestResponse>("test.echo", request, CreateTimeoutToken());
        });
    }

    #endregion

    #region Streaming Call Tests

    [Fact]
    public async Task StreamingCall_SimpleRequest_ReturnsExpectedResponses()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StreamingHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddStreamingCall<StreamingTestRequest, StreamingTestResponse, StreamingHandler>("test.stream")
            .Build();

        SetupClientServer(serviceProvider, registry);

        var request = new StreamingTestRequest { Count = 5, DelayMs = 0 };

        // Act
        var responses = new List<StreamingTestResponse>();
        await foreach (
            var response in _client!.StreamingCall<StreamingTestRequest, StreamingTestResponse>(
                "test.stream",
                request,
                CreateTimeoutToken()
            )
        )
        {
            responses.Add(response);
        }

        // Assert
        responses.Should().HaveCount(5);
        for (int i = 0; i < 5; i++)
        {
            responses[i].Index.Should().Be(i);
            responses[i].Message.Should().Be($"Item {i}");
        }
    }

    [Fact]
    public async Task StreamingCall_EmptyStream_ReturnsNoResponses()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StreamingHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddStreamingCall<StreamingTestRequest, StreamingTestResponse, StreamingHandler>("test.stream")
            .Build();

        SetupClientServer(serviceProvider, registry);

        var request = new StreamingTestRequest { Count = 0, DelayMs = 0 };

        // Act
        var responses = new List<StreamingTestResponse>();
        await foreach (
            var response in _client!.StreamingCall<StreamingTestRequest, StreamingTestResponse>(
                "test.stream",
                request,
                CreateTimeoutToken()
            )
        )
        {
            responses.Add(response);
        }

        // Assert
        responses.Should().BeEmpty();
    }

    [Fact]
    public async Task StreamingCall_CancelledMidStream_StopsEnumeration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StreamingHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddStreamingCall<StreamingTestRequest, StreamingTestResponse, StreamingHandler>("test.stream")
            .Build();

        SetupClientServer(serviceProvider, registry);

        var request = new StreamingTestRequest { Count = 100, DelayMs = 50 };

        var cts = new CancellationTokenSource();
        var responses = new List<StreamingTestResponse>();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (
                var response in _client!.StreamingCall<StreamingTestRequest, StreamingTestResponse>(
                    "test.stream",
                    request,
                    cts.Token
                )
            )
            {
                responses.Add(response);

                if (responses.Count == 3)
                {
                    cts.Cancel();
                }
            }
        });

        // Should have received only the first few items before cancellation
        responses.Should().HaveCountLessThan(10);
    }

    [Fact]
    public async Task StreamingCall_MultipleStreamsInParallel_AllSucceed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StreamingHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddStreamingCall<StreamingTestRequest, StreamingTestResponse, StreamingHandler>("test.stream")
            .Build();

        SetupClientServer(serviceProvider, registry);

        // Act
        var tasks = Enumerable
            .Range(0, 5)
            .Select(async streamIndex =>
            {
                var request = new StreamingTestRequest { Count = 3, DelayMs = 0 };

                var responses = new List<StreamingTestResponse>();
                await foreach (
                    var response in _client!.StreamingCall<StreamingTestRequest, StreamingTestResponse>(
                        "test.stream",
                        request,
                        CreateTimeoutToken()
                    )
                )
                {
                    responses.Add(response);
                }

                return (streamIndex, responses);
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(5);
        foreach (var (_, responses) in results)
        {
            responses.Should().HaveCount(3);
        }
    }

    [Fact]
    public async Task StreamingCall_ConnectionClosedDuringStream_ThrowsConnectionClosedException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StreamingHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddStreamingCall<StreamingTestRequest, StreamingTestResponse, StreamingHandler>("test.stream")
            .Build();

        SetupClientServer(serviceProvider, registry);

        var request = new StreamingTestRequest { Count = 100, DelayMs = 50 };

        var responses = new List<StreamingTestResponse>();

        // Act & Assert
        await Assert.ThrowsAsync<Client.ConnectionClosedException>(async () =>
        {
            await foreach (
                var response in _client!.StreamingCall<StreamingTestRequest, StreamingTestResponse>(
                    "test.stream",
                    request,
                    CreateTimeoutToken()
                )
            )
            {
                responses.Add(response);

                if (responses.Count == 3)
                {
                    _clientConnection!.Close();
                }
            }
        });
    }

    [Fact]
    public async Task StreamingCall_MethodNotImplemented_ThrowsMethodNotFoundException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StreamingHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddStreamingCall<StreamingTestRequest, StreamingTestResponse, StreamingHandler>("test.stream")
            .Build();

        SetupClientServer(serviceProvider, registry);

        var request = new StreamingTestRequest { Count = 5, DelayMs = 0 };

        // Act & Assert
        await Assert.ThrowsAsync<Client.MethodNotImplementedException>(async () =>
        {
            await foreach (
                var _ in _client!.StreamingCall<StreamingTestRequest, StreamingTestResponse>(
                    "test.nonexistent",
                    request,
                    CreateTimeoutToken()
                )
            )
            {
                // Should throw before getting here
            }
        });
    }

    #endregion

    #region Unary Call Interceptor Tests

    [Fact]
    public async Task UnaryCall_WithSingleInterceptor_InterceptsRequestAndResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<EchoHandler>();
        services.AddTransient<LoggingUnaryInterceptor>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddUnaryCall<TestRequest, TestResponse, EchoHandler>("test.echo")
            .Build();

        var interceptor = serviceProvider.GetRequiredService<LoggingUnaryInterceptor>();
        SetupClientServer(serviceProvider, registry, unaryInterceptors: new[] { interceptor });

        var request = new TestRequest { Message = "Hello", Value = 42 };

        // Act
        var response = await _client!.UnaryCall<TestRequest, TestResponse>("test.echo", request, CreateTimeoutToken());

        // Assert
        response.Should().NotBeNull();
        response.Result.Should().Be("Echo: Hello");
        response.ComputedValue.Should().Be(84);

        interceptor.CallCount.Should().Be(1);
        interceptor.LastRequest.Should().NotBeNull();
        interceptor.LastResponse.Should().NotBeNull();
    }

    [Fact]
    public async Task UnaryCall_WithMultipleInterceptors_ExecutesInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<EchoHandler>();
        services.AddTransient<LoggingUnaryInterceptor>();
        services.AddTransient<ModifyingUnaryInterceptor>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddUnaryCall<TestRequest, TestResponse, EchoHandler>("test.echo")
            .Build();

        var loggingInterceptor = serviceProvider.GetRequiredService<LoggingUnaryInterceptor>();
        var modifyingInterceptor = serviceProvider.GetRequiredService<ModifyingUnaryInterceptor>();

        SetupClientServer(
            serviceProvider,
            registry,
            unaryInterceptors: new IUnaryCallInterceptor[] { loggingInterceptor, modifyingInterceptor }
        );

        var request = new TestRequest { Message = "Hello", Value = 42 };

        // Act
        var response = await _client!.UnaryCall<TestRequest, TestResponse>("test.echo", request, CreateTimeoutToken());

        // Assert
        response.Should().NotBeNull();
        response.Result.Should().Be("Modified: Echo: Hello"); // Modified by interceptor
        response.ComputedValue.Should().Be(84);

        loggingInterceptor.CallCount.Should().Be(1);
        modifyingInterceptor.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task UnaryCall_InterceptorThrowsException_PropagatesException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<EchoHandler>();
        services.AddTransient<ThrowingUnaryInterceptor>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddUnaryCall<TestRequest, TestResponse, EchoHandler>("test.echo")
            .Build();

        var interceptor = serviceProvider.GetRequiredService<ThrowingUnaryInterceptor>();
        SetupClientServer(serviceProvider, registry, unaryInterceptors: new[] { interceptor });

        var request = new TestRequest { Message = "Hello", Value = 42 };

        // Act & Assert
        await Assert.ThrowsAsync<Client.ServerInternalException>(async () =>
        {
            await _client!.UnaryCall<TestRequest, TestResponse>("test.echo", request, CreateTimeoutToken());
        });
    }

    [Fact]
    public async Task UnaryCall_InterceptorModifiesRequest_ModifiedRequestReachesHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<EchoHandler>();
        services.AddTransient<RequestModifyingUnaryInterceptor>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddUnaryCall<TestRequest, TestResponse, EchoHandler>("test.echo")
            .Build();

        var interceptor = serviceProvider.GetRequiredService<RequestModifyingUnaryInterceptor>();
        SetupClientServer(serviceProvider, registry, unaryInterceptors: new[] { interceptor });

        var request = new TestRequest { Message = "Hello", Value = 42 };

        // Act
        var response = await _client!.UnaryCall<TestRequest, TestResponse>("test.echo", request, CreateTimeoutToken());

        // Assert
        response.Should().NotBeNull();
        response.Result.Should().Be("Echo: Modified: Hello"); // Handler received modified request
        response.ComputedValue.Should().Be(84);
    }

    #endregion

    #region Streaming Call Interceptor Tests

    [Fact]
    public async Task StreamingCall_WithSingleInterceptor_InterceptsRequestAndResponses()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StreamingHandler>();
        services.AddTransient<LoggingStreamingInterceptor>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddStreamingCall<StreamingTestRequest, StreamingTestResponse, StreamingHandler>("test.stream")
            .Build();

        var interceptor = serviceProvider.GetRequiredService<LoggingStreamingInterceptor>();
        SetupClientServer(serviceProvider, registry, streamingInterceptors: new[] { interceptor });

        var request = new StreamingTestRequest { Count = 3, DelayMs = 0 };

        // Act
        var responses = new List<StreamingTestResponse>();
        await foreach (
            var response in _client!.StreamingCall<StreamingTestRequest, StreamingTestResponse>(
                "test.stream",
                request,
                CreateTimeoutToken()
            )
        )
        {
            responses.Add(response);
        }

        // Assert
        responses.Should().HaveCount(3);
        interceptor.CallCount.Should().Be(1);
        interceptor.LastRequest.Should().NotBeNull();
    }

    [Fact]
    public async Task StreamingCall_WithMultipleInterceptors_ExecutesInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StreamingHandler>();
        services.AddTransient<LoggingStreamingInterceptor>();
        services.AddTransient<ModifyingStreamingInterceptor>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddStreamingCall<StreamingTestRequest, StreamingTestResponse, StreamingHandler>("test.stream")
            .Build();

        var loggingInterceptor = serviceProvider.GetRequiredService<LoggingStreamingInterceptor>();
        var modifyingInterceptor = serviceProvider.GetRequiredService<ModifyingStreamingInterceptor>();

        SetupClientServer(
            serviceProvider,
            registry,
            streamingInterceptors: new IStreamingCallInterceptor[] { loggingInterceptor, modifyingInterceptor }
        );

        var request = new StreamingTestRequest { Count = 2, DelayMs = 0 };

        // Act
        var responses = new List<StreamingTestResponse>();
        await foreach (
            var response in _client!.StreamingCall<StreamingTestRequest, StreamingTestResponse>(
                "test.stream",
                request,
                CreateTimeoutToken()
            )
        )
        {
            responses.Add(response);
        }

        // Assert
        responses.Should().HaveCount(2);
        responses[0].Message.Should().Be("Modified: Item 0"); // Modified by interceptor
        responses[1].Message.Should().Be("Modified: Item 1");

        loggingInterceptor.CallCount.Should().Be(1);
        modifyingInterceptor.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task StreamingCall_InterceptorThrowsException_PropagatesException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StreamingHandler>();
        services.AddTransient<ThrowingStreamingInterceptor>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddStreamingCall<StreamingTestRequest, StreamingTestResponse, StreamingHandler>("test.stream")
            .Build();

        var interceptor = serviceProvider.GetRequiredService<ThrowingStreamingInterceptor>();
        SetupClientServer(serviceProvider, registry, streamingInterceptors: new[] { interceptor });

        var request = new StreamingTestRequest { Count = 3, DelayMs = 0 };

        // Act & Assert
        await Assert.ThrowsAsync<Client.ServerInternalException>(async () =>
        {
            await foreach (
                var _ in _client!.StreamingCall<StreamingTestRequest, StreamingTestResponse>(
                    "test.stream",
                    request,
                    CreateTimeoutToken()
                )
            )
            {
                // Should throw before getting here
            }
        });
    }

    [Fact]
    public async Task StreamingCall_InterceptorModifiesRequest_ModifiedRequestReachesHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StreamingHandler>();
        services.AddTransient<RequestModifyingStreamingInterceptor>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = CallHandlerRegistry
            .CreateBuilder()
            .AddStreamingCall<StreamingTestRequest, StreamingTestResponse, StreamingHandler>("test.stream")
            .Build();

        var interceptor = serviceProvider.GetRequiredService<RequestModifyingStreamingInterceptor>();
        SetupClientServer(serviceProvider, registry, streamingInterceptors: new[] { interceptor });

        var request = new StreamingTestRequest { Count = 2, DelayMs = 0 };

        // Act
        var responses = new List<StreamingTestResponse>();
        await foreach (
            var response in _client!.StreamingCall<StreamingTestRequest, StreamingTestResponse>(
                "test.stream",
                request,
                CreateTimeoutToken()
            )
        )
        {
            responses.Add(response);
        }

        // Assert
        responses.Should().HaveCount(4); // Handler received modified request (count was doubled from 2 to 4)
        responses[0].Message.Should().Be("Item 0");
        responses[1].Message.Should().Be("Item 1");
        responses[2].Message.Should().Be("Item 2");
        responses[3].Message.Should().Be("Item 3");
    }

    #endregion

    #region Test Handlers

    private class EchoHandler : IUnaryCallHandler<TestRequest, TestResponse>
    {
        public Task<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            var response = new TestResponse { Result = $"Echo: {request.Message}", ComputedValue = request.Value * 2 };
            return Task.FromResult(response);
        }
    }

    private class DelayedEchoHandler : IUnaryCallHandler<TestRequest, TestResponse>
    {
        private readonly int _delayMs;

        public DelayedEchoHandler(int delayMs)
        {
            _delayMs = delayMs;
        }

        public async Task<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(_delayMs, cancellationToken);

            var response = new TestResponse { Result = $"Echo: {request.Message}", ComputedValue = request.Value * 2 };
            return response;
        }
    }

    private class ThrowingHandler : IUnaryCallHandler<TestRequest, TestResponse>
    {
        public Task<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Handler threw an exception");
        }
    }

    private class StreamingHandler : IStreamingCallHandler<StreamingTestRequest, StreamingTestResponse>
    {
        public async IAsyncEnumerable<StreamingTestResponse> HandleAsync(
            StreamingTestRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            for (int i = 0; i < request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (request.DelayMs > 0)
                {
                    await Task.Delay(request.DelayMs, cancellationToken);
                }

                yield return new StreamingTestResponse { Index = i, Message = $"Item {i}" };
            }
        }
    }

    #endregion

    #region Test Interceptors

    private class LoggingUnaryInterceptor : IUnaryCallInterceptor
    {
        public int CallCount { get; private set; }
        public Google.Protobuf.WellKnownTypes.Any? LastRequest { get; private set; }
        public Google.Protobuf.IMessage? LastResponse { get; private set; }

        public async Task<Google.Protobuf.IMessage> InterceptUnaryAsync(
            IServiceProvider serviceProvider,
            Google.Protobuf.WellKnownTypes.Any request,
            UnaryCallInterceptorDelegate next,
            CancellationToken cancellationToken
        )
        {
            CallCount++;
            LastRequest = request;

            var response = await next(request, cancellationToken);
            LastResponse = response;

            return response;
        }
    }

    private class ModifyingUnaryInterceptor : IUnaryCallInterceptor
    {
        public int CallCount { get; private set; }

        public async Task<Google.Protobuf.IMessage> InterceptUnaryAsync(
            IServiceProvider serviceProvider,
            Google.Protobuf.WellKnownTypes.Any request,
            UnaryCallInterceptorDelegate next,
            CancellationToken cancellationToken
        )
        {
            CallCount++;

            var response = await next(request, cancellationToken);

            // Modify the response
            if (response is TestResponse testResponse)
            {
                testResponse.Result = "Modified: " + testResponse.Result;
            }

            return response;
        }
    }

    private class ThrowingUnaryInterceptor : IUnaryCallInterceptor
    {
        public Task<Google.Protobuf.IMessage> InterceptUnaryAsync(
            IServiceProvider serviceProvider,
            Google.Protobuf.WellKnownTypes.Any request,
            UnaryCallInterceptorDelegate next,
            CancellationToken cancellationToken
        )
        {
            return Task.FromException<Google.Protobuf.IMessage>(
                new InvalidOperationException("Interceptor threw an exception")
            );
        }
    }

    private class RequestModifyingUnaryInterceptor : IUnaryCallInterceptor
    {
        public async Task<Google.Protobuf.IMessage> InterceptUnaryAsync(
            IServiceProvider serviceProvider,
            Google.Protobuf.WellKnownTypes.Any request,
            UnaryCallInterceptorDelegate next,
            CancellationToken cancellationToken
        )
        {
            // Modify the request
            if (request.TryUnpack<TestRequest>(out var testRequest))
            {
                testRequest.Message = "Modified: " + testRequest.Message;
                request = Google.Protobuf.WellKnownTypes.Any.Pack(testRequest);
            }

            return await next(request, cancellationToken);
        }
    }

    private class LoggingStreamingInterceptor : IStreamingCallInterceptor
    {
        public int CallCount { get; private set; }
        public Google.Protobuf.WellKnownTypes.Any? LastRequest { get; private set; }

        public IAsyncEnumerable<Google.Protobuf.IMessage> InterceptStreamingAsync(
            IServiceProvider serviceProvider,
            Google.Protobuf.WellKnownTypes.Any request,
            StreamingCallInterceptorDelegate next,
            CancellationToken cancellationToken
        )
        {
            CallCount++;
            LastRequest = request;

            return next(request, cancellationToken);
        }
    }

    private class ModifyingStreamingInterceptor : IStreamingCallInterceptor
    {
        public int CallCount { get; private set; }

        public async IAsyncEnumerable<Google.Protobuf.IMessage> InterceptStreamingAsync(
            IServiceProvider serviceProvider,
            Google.Protobuf.WellKnownTypes.Any request,
            StreamingCallInterceptorDelegate next,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            CallCount++;

            await foreach (var response in next(request, cancellationToken))
            {
                // Modify each response
                if (response is StreamingTestResponse streamingResponse)
                {
                    streamingResponse.Message = "Modified: " + streamingResponse.Message;
                }

                yield return response;
            }
        }
    }

    private class ThrowingStreamingInterceptor : IStreamingCallInterceptor
    {
        public IAsyncEnumerable<Google.Protobuf.IMessage> InterceptStreamingAsync(
            IServiceProvider serviceProvider,
            Google.Protobuf.WellKnownTypes.Any request,
            StreamingCallInterceptorDelegate next,
            CancellationToken cancellationToken
        )
        {
            throw new InvalidOperationException("Streaming interceptor threw an exception");
        }
    }

    private class RequestModifyingStreamingInterceptor : IStreamingCallInterceptor
    {
        public async IAsyncEnumerable<Google.Protobuf.IMessage> InterceptStreamingAsync(
            IServiceProvider serviceProvider,
            Google.Protobuf.WellKnownTypes.Any request,
            StreamingCallInterceptorDelegate next,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            // Modify the request
            if (request.TryUnpack<StreamingTestRequest>(out var streamingRequest))
            {
                streamingRequest.Count *= 2; // Double the count
                request = Google.Protobuf.WellKnownTypes.Any.Pack(streamingRequest);
            }

            await foreach (var response in next(request, cancellationToken))
            {
                yield return response;
            }
        }
    }

    #endregion
}
