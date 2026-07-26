using System;
using System.Text.Json.Serialization;
using StockHub.Interfaces;

namespace StockHub.Database;

public class StockUser : IColIden
{
    [JsonIgnore]
    public int iden { get; private set; }

    public required string Uid { get; set; }

    public required DateTimeOffset? LastBeat { get; set; }

    //FK
}