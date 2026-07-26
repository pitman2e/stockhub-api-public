using StockHub.Interfaces;
namespace UnitTestsDev;

public class TestUserClaim : IUserClaims
{
    public string GetUid()
    {
        return "<uid>";
    }

    public static TestUserClaim Get()
    {
        return new TestUserClaim();
    }
}