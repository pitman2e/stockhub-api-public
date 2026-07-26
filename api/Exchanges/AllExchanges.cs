using System.Collections.Generic;
using StockHub.Exchanges.ConcreteExchanges;

namespace StockHub.Exchanges;

public class AllExchanges : Dictionary<string, IExchange>
{
    public AllExchanges(
        US us, 
        LSE lse, 
        HK hk, 
        HSBC hsbc, 
        PCP pcp, 
        MANU manu, 
        CASH cash, 
        USBND usbnd, 
        HKBND hkbnd) 
    {
        Add(US.MARKET_ID, us);
        Add(LSE.MARKET_ID, lse);
        Add(HK.MARKET_ID, hk);
        Add(HSBC.MARKET_ID, hsbc);
        Add(PCP.MARKET_ID, pcp);
        Add(MANU.MARKET_ID, manu);
        Add(CASH.MARKET_ID, cash);
        Add(USBND.MARKET_ID, usbnd);
        Add(HKBND.MARKET_ID, hkbnd);
    }
}