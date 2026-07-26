using StockHub.Interfaces;

namespace UnitTests.Mocks;

public class UserClaimMock : IUserClaims
{
    public string GetUid()
    {
        return "TEST_UID";
    }

    public static UserClaimMock Get()
    {
        return new UserClaimMock();
    }
}