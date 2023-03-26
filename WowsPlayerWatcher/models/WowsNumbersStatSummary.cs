using System.Text.Json;

namespace WowsPlayerWatcher.models
{
    public static class WowsNumbersStatSummary
    {
        public static Dictionary<string, AvrgStat> ShipData = new();
        public static AvrgStat ServerTotal = new AvrgStat();

        static WowsNumbersStatSummary()
        {
            if (File.Exists("wows-numbers-expected.json"))
            {
                LoadFromJson("wows-numbers-expected.json");
            }
            //TODO: read from wows-numbers API, handle Kitakami
        }
        public static void LoadFromJson(string filename)
        {
            string json = File.ReadAllText(filename);
            var data = JsonSerializer.Deserialize<WowsNumbersRootObject>(json) ?? new WowsNumbersRootObject();
            ShipData = data.Data;
            ServerTotal.WR = ShipData.Sum(s => s.Value.WR) / ShipData.Count;
            ServerTotal.AvgFrangs = ShipData.Sum(s => s.Value.AvgFrangs) / ShipData.Count;
            ServerTotal.AvgDamage = ShipData.Sum(s => s.Value.AvgDamage) / ShipData.Count;
        }
    }

    /// <summary>
    /// Root object for JSON file from wows numbers with avg damage & WR
    /// </summary>
    public class WowsNumbersRootObject
    {
        public int Time { get; set; }
        public Dictionary<string, AvrgStat> Data { get; set; } = new();
    }
}
