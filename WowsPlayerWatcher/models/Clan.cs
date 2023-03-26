namespace WowsPlayerWatcher.models
{

    public class Clan
    {
        public string Tag { get; set; } = null!;
    }

    public class RootClanInfo
    {
        public Clan Clan { get; set; } = null!;
    }

}
