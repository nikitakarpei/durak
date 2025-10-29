using MongoDB.Driver;
using PlayerIdentityService.Infrastructure.Persistence.Database.MongoDb;
using Rpc.Server;

namespace PlayerIdentityService.Web.MongoDb;

/// <summary>
/// Interceptor that wraps unary RPC calls in MongoDB transactions.
/// Starts a transaction before the call and commits it on success or aborts on failure.
/// </summary>
public class UnaryMongoDbTransactionInterceptor : IUnaryCallInterceptor
{
    private readonly IMongoClient _mongoClient;
    private readonly ILogger<UnaryMongoDbTransactionInterceptor> _logger;

    public UnaryMongoDbTransactionInterceptor(
        IMongoClient mongoClient,
        ILogger<UnaryMongoDbTransactionInterceptor> logger
    )
    {
        _mongoClient = mongoClient;
        _logger = logger;
    }

    public async Task<Google.Protobuf.IMessage> InterceptUnaryAsync(
        IServiceProvider serviceProvider,
        Google.Protobuf.WellKnownTypes.Any request,
        UnaryCallInterceptorDelegate next,
        CancellationToken cancellationToken
    )
    {
        var sessionAccessor = serviceProvider.GetRequiredService<MongoSessionAccessor>();

        using var session = await _mongoClient.StartSessionAsync(null, cancellationToken);
        sessionAccessor.Current = session;

        try
        {
            _logger.LogDebug("Starting MongoDB transaction for unary call");

            session.StartTransaction();

            var response = await next(request, cancellationToken);

            await session.CommitTransactionAsync(cancellationToken);

            _logger.LogDebug("MongoDB transaction committed successfully");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during unary call, aborting MongoDB transaction");

            try
            {
                await session.AbortTransactionAsync(cancellationToken);
                _logger.LogDebug("MongoDB transaction aborted");
            }
            catch (Exception abortEx)
            {
                _logger.LogError(abortEx, "Failed to abort MongoDB transaction");
            }

            throw;
        }
        finally
        {
            sessionAccessor.Current = null;
        }
    }
}
