using Rpc.Client;

namespace BlazorPWA.Services.Api;

public sealed class PlayerIdentityServiceClient
{
    private const string LoginWithCustomIdMethod = "LoginWithCustomId";

    private readonly IClient _client;

    public PlayerIdentityServiceClient(IClient client)
    {
        _client = client;
    }

    public async Task<PlayerIdentityService.Api.Data.LoginResult> LoginWithCustomIdAsync(
        string customId,
        CancellationToken cancellationToken
    )
    {
        var request = new PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Request
        {
            CustomId = customId,
        };

        var response = await _client.UnaryCall<
            PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Request,
            PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Response
        >(LoginWithCustomIdMethod, request, cancellationToken);

        return response.Result;
    }
}
