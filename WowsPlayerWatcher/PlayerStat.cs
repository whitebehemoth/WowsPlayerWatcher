using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using WowsPlayerWatcher;
using WowsPlayerWatcher.models;

public class PlayerStat
{
    public bool HiddenProfile { get; set; }
    public WgStat Overall { get; set; } = new WgStat();
    public WgStat CurrentShipStat { get; set; } = new WgStat();

    public Color ShipColor(long shipId)
    {
        if (!HiddenProfile && WowsNumbersStatSummary.ShipData.TryGetValue(shipId.ToString(), out var shipData))
        {
            var PR = GetPr(shipData);
            return GetPrCollor(PR);
        }
        else
        {
            return Color.Black;
        }
    }
    public Color PlayerColor()
    {
        if (HiddenProfile) return Color.Black;
        var PR = GetPr(WowsNumbersStatSummary.ServerTotal);
        return GetPrCollor(PR);
    }
    private double GetPr(AvrgStat stat)
    {
        var expectedDmg = stat.AvgDamage;
        var expectedWins = stat.AvgDamage;
        var expectedFrags = stat.AvgFrangs;

        var actualDmg = CurrentShipStat.Damage;
        var actualWins = CurrentShipStat.WR;
        var actualFrags = CurrentShipStat.Frags;

        var rDmg = actualDmg / expectedDmg;
        var rWins = actualWins / expectedWins;
        var rFrags = actualFrags / expectedFrags;

        var nDmg = Math.Max(0, (rDmg - 0.4) / (1 - 0.4));
        var nFrags = Math.Max(0, (rFrags - 0.1) / (1 - 0.1));
        var nWins = Math.Max(0, (rWins - 0.7) / (1 - 0.7));

        var PR = 700 * nDmg + 300 * nFrags + 150 * nWins;
        return PR;
    }

    private Color GetPrCollor(double pR)
    {
        return pR switch
        {
            < 750 => Color.FromArgb(254, 14, 0),
            >= 750 and < 1100 => Color.FromArgb(254, 121, 3),
            >= 1100 and 1350 => Color.FromArgb(255, 199, 31),
            >= 1350 and < 1550 => Color.FromArgb(68, 179, 0),
            >= 1550 and < 1750 => Color.FromArgb(49, 128, 0),
            >= 1750 and < 2100 => Color.FromArgb(2, 201, 179),
            >= 2100 => Color.FromArgb(208, 66, 243),
            _ => Color.Black
        };
    }

    public async Task LoadFromWgAPI(string apiAddress, string appId, string account_id, string ship_id)
    {
        UriBuilder uri = new UriBuilder(apiAddress);
        uri.Path = "wows/ships/stats/";
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["application_id"] = appId;
        query["fields"] = "pvp.battles,pvp.damage_dealt,pvp.wins,pvp.frags";
        query["account_id"] = account_id;
        query["ship_id"] = ship_id;
        uri.Query = query.ToString();

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add(HeaderNames.Connection, "keep-alive");
        client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");

        var responce = await client.GetAsync(uri.Uri);
        WgResponce<Dictionary<string, Statistics[]>>? responceJson = await responce.Content.ReadFromJsonAsync<WgResponce<Dictionary<string, Statistics[]>>>();

        if (responceJson?.Data.TryGetValue(account_id, out var persStat) ?? false)
        {
            if ((persStat?.Count() ?? 0) > 0)
            {
                CurrentShipStat = persStat[0].pvp;
                CurrentShipStat.WR = persStat[0].pvp.WR / persStat[0].pvp.Battels * 100;
                CurrentShipStat.Damage = persStat[0].pvp.Damage / persStat[0].pvp.Battels;
                CurrentShipStat.Frags = persStat[0].pvp.Frags / persStat[0].pvp.Battels;
                CurrentShipStat.Battels = persStat[0].pvp.Battels;
            }
        }
    }
}