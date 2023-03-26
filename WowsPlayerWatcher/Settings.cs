public class Settings
{
    public Wows WowsSection { get; set; } = new Wows();
    public Watcher WatcherSection { get; set; } = new Watcher();
}
public class Wows
{
    public string ReplayFolder { get; set; } = string.Empty;
    public string TeamFileName { get; set; } = "tempArenaInfo.json";
    public string AppId { get; set; } = "";
    public string ApiAddress { get; set; } = "https://api.worldofwarships.com/";
}

public class Watcher
{
    public string PlayerList { get; set; } = "playerList.json";
    public string ShipInfoList { get; set; } = "shipInfoList.json";
    public string CategoryImageFolder { get; set; } = "categories";
    public string SoundFile { get; set; } = "tada.wav";
    public string Greetings { get; set; } = "o7, {0}! Nice to see you.";

}
