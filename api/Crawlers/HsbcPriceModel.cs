using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class HsbsMpfData
{
    [JsonPropertyName("data")]
    public DataContainer Data { get; set; }
}

public class DataContainer
{
    [JsonPropertyName("schemeInfos")]
    public List<SchemeInfo> SchemeInfos { get; set; }
}

public class SchemeInfo
{
    [JsonPropertyName("schemeIdentifier")]
    public string SchemeIdentifier { get; set; }

    [JsonPropertyName("schemeCode")]
    public string SchemeCode { get; set; }

    [JsonPropertyName("schemeNames")]
    public List<LocalizedValue> SchemeNames { get; set; }

    [JsonPropertyName("fundInfos")]
    public List<FundInfo> FundInfos { get; set; }

    [JsonPropertyName("fundPriceInfos")]
    public List<FundPriceInfo> FundPriceInfos { get; set; }
}

public class LocalizedValue
{
    [JsonPropertyName("languageCode")]
    public string LanguageCode { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class FundInfo
{
    [JsonPropertyName("fundIdentifier")]
    public string FundIdentifier { get; set; }

    [JsonPropertyName("fundCode")]
    public string FundCode { get; set; }

    [JsonPropertyName("fundNames")]
    public List<LocalizedValue> FundNames { get; set; }

    [JsonPropertyName("fundNameAlias")]
    public List<LocalizedValue> FundNameAlias { get; set; }

    [JsonPropertyName("riskLevelValue")]
    public string RiskLevelValue { get; set; }

    [JsonPropertyName("disFundIndicator")]
    public bool DisFundIndicator { get; set; }

    [JsonPropertyName("disRelatedFundCode")]
    public string DisRelatedFundCode { get; set; }

    // Arrays initialized as empty in JSON
    [JsonPropertyName("objectiveNames")]
    public List<object> ObjectiveNames { get; set; }

    [JsonPropertyName("fundAllocations")]
    public List<object> FundAllocations { get; set; }

    [JsonPropertyName("fundHoldings")]
    public List<object> FundHoldings { get; set; }
}

public class FundPriceInfo
{
    [JsonPropertyName("fundCode")]
    public string FundCode { get; set; }

    [JsonPropertyName("fundPrices")]
    public List<FundPriceDetails> FundPrices { get; set; }
}

public class FundPriceDetails
{
    [JsonPropertyName("fundBuyPrice")]
    public PriceAmount FundBuyPrice { get; set; }

    [JsonPropertyName("fundSellPrice")]
    public PriceAmount FundSellPrice { get; set; }

    [JsonPropertyName("priceDate")]
    public DateTimeOffset PriceDate { get; set; }
}

public class PriceAmount
{
    [JsonPropertyName("amount")]
    public string Amount { get; set; } // Kept as string to preserve precision from JSON

    [JsonPropertyName("fundCurrencyCode")]
    public string FundCurrencyCode { get; set; }
}