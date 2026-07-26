//https://stackoverflow.com/questions/50580232/get-userid-from-jwt-on-all-controller-methods

namespace StockHub.Interfaces;

public interface IUserClaims
{
    string GetUid();
}