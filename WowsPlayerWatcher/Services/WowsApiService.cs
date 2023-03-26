using System.Web;
using WowsPlayerWatcher.models;
using Microsoft.Extensions.Logging;

namespace WowsPlayerWatcher.Services
{
    public class WowsApiService
    {
        private string m_apiUrl;
        private string m_appId;
        private ILogger<WowsApiService> m_logger = LoggerHelper.Factory.CreateLogger<WowsApiService>();

        private Dictionary<string, ShipInfo> m_ShipInfo = new();// JsonHelper.LoadJson<Dictionary<string, ShipInfo>>(m_Settings.WatcherSection.ShipInfoList);

        private WgPlayer[] m_wgPlayers = null!;

        public WgPlayer[] WgPlayers => m_wgPlayers;

        public ShipInfo GetShipInfo(long shipId)
        {
            if (m_ShipInfo.TryGetValue(shipId.ToString(), out var playerdetails))
            {
                return playerdetails;
            }
            else
            {
                m_logger.LogWarning($"shipId:{shipId} was not returned by WG API (new ship)");
                return new ShipInfo() { name = "Unknown", type = "Sub" };
            }
        }

        public WowsApiService(string apiAddress, string appId)
        {
            m_appId = appId;
            m_apiUrl = apiAddress;
        }

        public async Task LoadDataForBattle(WgPlayer[] WgPlayers)
        {
            Task[] tasks = new Task[3];
            m_wgPlayers = WgPlayers;
            await LoadAccountIdsFromWgAPI();
            tasks[0] = LoadShipInfoFromWgAPI();
            tasks[1] = LoadAccountDetailsFromWgAPI();
            tasks[2] = LoadClanFromWgAPI();
            await Task.WhenAll(tasks);
        }


        private async Task LoadShipInfoFromWgAPI()
        {
            UriBuilder uri = new UriBuilder(m_apiUrl);
            uri.Path = "wows/account/list/";
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["application_id"] = m_appId;
            query["fields"] = "name,tier,ship_id,nation,is_premium,type";
            query["ship_id"] = m_wgPlayers.Select(p => p.ShipId.ToString()).Aggregate((a, b) => a + "," + b);
            uri.Query = query.ToString();

            using WgHttpClient<Dictionary<string, ShipInfo>> client = new();
            var responceJson = await client.GetData(uri.Uri);
            if (responceJson == null)
            {
                m_logger.LogError("responceJson is null for ShipInfo");
            }
            else
            {
                m_ShipInfo = responceJson.Data;
            }

        }

        private async Task LoadAccountIdsFromWgAPI()
        {
            UriBuilder uri = new UriBuilder(m_apiUrl);
            uri.Path = "wows/account/list/";
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["application_id"] = m_appId;
            query["type"] = "exact";
            query["search"] = m_wgPlayers.Select(p => p.Name).Aggregate((a, b) => a + "," + b);
            uri.Query = query.ToString();

            using WgHttpClient<List<WgUserAccount>> client = new();
            var responceJson = await client.GetData(uri.Uri);
            if (responceJson == null)
            {
                m_logger.LogError("responceJson is null for WgUserAccount");
            }
            else
            {
                foreach (var p in m_wgPlayers)
                {
                    p.AccountId = responceJson.Data.First(a => a.nickname == p.Name).account_id;
                }
            }

        }
        private async Task LoadAccountDetailsFromWgAPI()
        {
            UriBuilder uri = new UriBuilder(m_apiUrl);
            uri.Path = "wows/account/info/";
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["application_id"] = m_appId;
            query["fields"] = "hidden_profile,statistics.pvp.battles,statistics.pvp.damage_dealt,statistics.pvp.wins,statistics.pvp.frags";
            query["account_id"] = m_wgPlayers.Select(p => p.AccountId.ToString()).Aggregate((a, b) => a + "," + b);
            uri.Query = query.ToString();

            using WgHttpClient<Dictionary<string, PersStatRoot>> client = new();
            var responceJson = await client.GetData(uri.Uri);
            if (responceJson == null)
            {
                m_logger.LogError("responceJson is null for PersStatRoot");
            }
            else
            {
                foreach (var p in m_wgPlayers)
                {
                    if (responceJson.Data.TryGetValue(p.AccountId.ToString() ?? "", out var persStat))
                    {
                        p.StatInfo.HiddenProfile = persStat.hidden_profile;
                        if (!persStat.hidden_profile)
                        {
                            p.StatInfo.Overall.WR = persStat.statistics.pvp.WR / persStat.statistics.pvp.Battels * 100;
                            p.StatInfo.Overall.Damage = persStat.statistics.pvp.Damage / persStat.statistics.pvp.Battels;
                            p.StatInfo.Overall.Frags = persStat.statistics.pvp.Frags / persStat.statistics.pvp.Battels;
                            p.StatInfo.Overall.Battels = persStat.statistics.pvp.Battels;
                        }
                    }
                }
            }
        }

        private async Task LoadClanFromWgAPI()
        {
            UriBuilder uri = new UriBuilder(m_apiUrl);
            uri.Path = "wows/clans/accountinfo/";
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["application_id"] = m_appId;
            query["account_id"] = m_wgPlayers.Select(p => p.AccountId.ToString()).Aggregate((a, b) => a + "," + b);
            query["extra"] = "clan";
            query["fields"] = "clan.tag";
            uri.Query = query.ToString();

            using WgHttpClient<Dictionary<string, RootClanInfo>> client = new();
            var responceJson = await client.GetData(uri.Uri);
            if (responceJson == null)
            {
                m_logger.LogError("responceJson is null for RootClanInfo");
            }
            else
            {
                foreach (var p in m_wgPlayers)
                {
                    if (responceJson.Data.TryGetValue(p.AccountId.ToString() ?? "", out var clanInfo))
                    {
                        p.ClanTag = clanInfo?.Clan?.Tag ?? "";
                    }
                }
            }
        }
    }
}
