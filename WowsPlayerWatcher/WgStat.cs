using System.Text.Json.Serialization;

public class WgStat
{
    [JsonPropertyName("wins")]
    public double WR { get; set; }

    [JsonPropertyName("damage_dealt")]
    public double Damage { get; set; }

    [JsonPropertyName("battles")]
    public double Battels { get; set; }

    [JsonPropertyName("frags")]
    public double Frags { get; set; }

    public string WrFormatted => string.Format("{0:N2}", WR);
    public string DamageFormatted => string.Format("{0:N0}", Damage);
    public string BattelsFormatted => string.Format("{0:N0}", Battels);
    public Color WrColor => WR switch
    {
        < 40 => Color.FromArgb(254, 14, 0), //bad
        >= 40 and < 42 => Color.FromArgb(254, 121, 3), //bellow avg
        >= 42 and < 48 => Color.FromArgb(255, 199, 31), //avg
        >= 48 and < 50 => Color.FromArgb(68, 179, 0), //good
        >= 52 and < 54 => Color.FromArgb(49, 128, 0), //very good
        >= 54 and < 56 => Color.FromArgb(2, 201, 179), //great
        >= 56 => Color.FromArgb(208, 66, 243), //unique
        _ => Color.Black
    };

    public Color DamageColor(double avgDmg) => (avgDmg - Damage) switch
    {
        var d when d > 0 && d > avgDmg * 0.30 => Color.FromArgb(254, 14, 0),
        var d when d > 0 && d <= avgDmg * 0.30 && d > avgDmg * 0.15 => Color.FromArgb(254, 121, 3), //bellow avg
        var d when d > 0 && d <= avgDmg * 0.15 && d > avgDmg * 0.05 => Color.FromArgb(255, 199, 31), //avg
        var d when d >= 0 && d <= avgDmg * 0.05 && d >= 0 => Color.FromArgb(68, 179, 0), //good

        var d when d < 0 && -d <= avgDmg * 0.05 && -d > 0 => Color.FromArgb(68, 179, 0), //good
        var d when d < 0 && -d <= avgDmg * 0.15 && -d > avgDmg * 0.05 => Color.FromArgb(49, 128, 0), //very good
        var d when d < 0 && -d <= avgDmg * 0.30 && -d > avgDmg * 0.15 => Color.FromArgb(2, 201, 179), //great
        var d when d < 0 && -d > avgDmg * 0.30 => Color.FromArgb(208, 66, 243), //unique
        _ => Color.Black
    };
}

public class PersStatRoot
{
    public Statistics statistics { get; set; } = new Statistics();
    public bool hidden_profile { get; set; }
}

public class Statistics
{
    public WgStat pvp { get; set; } = new WgStat();
    public WgStat ranks { get; set; } = new WgStat();
}

