using System.Text.Json.Serialization;

public class WgPlayer
{
    public long ShipId { get; set; }
    public int Relation { get; set; }
    public int Id { get; set; }

    [JsonPropertyName("account_id")]
    public int AccountId { get; set; } = 0;

    public string Name { get; set; } = null!;

    public string ClanTag { get; set; } = null!;

    [JsonPropertyName("details")]
    public ShipInfo Ship { get; set; } = new ShipInfo();

    public PlayerStat StatInfo { get; set; } = new PlayerStat();
}
