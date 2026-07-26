using StockHub.Interfaces;
namespace UnitTests;

public class TestUserClaim : IUserClaims
{
    public string GetUid()
    {
        return "TEST_UID";
    }

    public static TestUserClaim Get()
    {
        return new TestUserClaim();
    }
}