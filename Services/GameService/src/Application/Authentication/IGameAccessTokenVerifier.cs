namespace GameService.Application;

public interface IGameAccessTokenVerifier
{
    public Task<GameAccessTokenClaims?> VerifyAsync(string accessToken, CancellationToken cancellationToken);
}
