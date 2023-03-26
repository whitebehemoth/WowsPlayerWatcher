using System.Text.Json.Serialization;

namespace WowsPlayerWatcher
{
    public partial class Battle
    {
        [JsonPropertyName("vehicles")]
        public WgPlayer[] WgPlayers { get; set; } = null!;
        //public WgStat Overall { get; set; } = new WgStat();
    }
}
