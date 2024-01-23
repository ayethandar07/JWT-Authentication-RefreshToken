namespace CustomerAPI.Repositories
{
    public interface IRefreshTokenGenerator
    {
        string GenerateToken(string username);
    }
}
