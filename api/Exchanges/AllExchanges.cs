using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace StockHub.Exchanges;

public class AllExchanges : IReadOnlyDictionary<string, IExchange>
{
    private readonly Dictionary<string, IExchange> _exchanges;

    public AllExchanges(IEnumerable<IExchange> exchanges) 
    {
        _exchanges = new Dictionary<string, IExchange>();
        
        foreach (var exchange in exchanges)
        {
            _exchanges.Add(exchange.MarketId, exchange);
        }
    }

    // --- IReadOnlyDictionary Implementation ---
    
    public IExchange this[string key] => _exchanges[key];
    
    public IEnumerable<string> Keys => _exchanges.Keys;
    
    public IEnumerable<IExchange> Values => _exchanges.Values;
    
    public int Count => _exchanges.Count;
    
    public bool ContainsKey(string key) => _exchanges.ContainsKey(key);
    
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out IExchange value) 
    {
        return _exchanges.TryGetValue(key, out value);
    }
    
    public IEnumerator<KeyValuePair<string, IExchange>> GetEnumerator()
    {
        return _exchanges.GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}