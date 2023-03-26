using System.Text.Json.Serialization;

namespace WowsPlayerWatcher
{
    public class AvrgStat
    {
        [JsonPropertyName("average_damage_dealt")]
        public float AvgDamage { get; set; }

        [JsonPropertyName("average_frags")]
        public float AvgFrangs { get; set; }

        [JsonPropertyName("win_rate")]
        public float WR { get; set; }

    }

}
