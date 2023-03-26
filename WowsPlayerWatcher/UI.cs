using Microsoft.Toolkit.Uwp.Notifications;
using System.Media;
using WowsPlayerWatcher.enums;
using Microsoft.VisualBasic;
using Microsoft.Extensions.Logging;
using WowsPlayerWatcher.models;
using WowsPlayerWatcher.Services;

namespace WowsPlayerWatcher

{
    public partial class UI : Form
    {


        private FileSystemWatcher m_Watcher = new FileSystemWatcher();
        private Settings m_Settings;
        private Dictionary<string, int> m_ImageStatusIndex = new();
        private List<PlayerToWatch> m_PlayersToWatch;
        private ILogger<UI> m_logger = LoggerHelper.Factory.CreateLogger<UI>();
        private WowsApiService m_wowsService;
        public UI()
        {
            InitializeComponent();
            m_Settings = JsonHelper.LoadJson<Settings>("appsettings.json");
            m_wowsService = new WowsApiService(m_Settings.WowsSection.ApiAddress, m_Settings.WowsSection.AppId);

            m_PlayersToWatch = JsonHelper.LoadJson<List<PlayerToWatch>>(m_Settings.WatcherSection.PlayerList);
            LoadInitialState();
        }

        /// <summary>
        /// Loading the player list and set the status
        /// </summary>
        private void LoadInitialState()
        {

            SetStatusInStatusBar(
                string.IsNullOrWhiteSpace(m_Settings.WowsSection.ReplayFolder) || !Directory.Exists(m_Settings.WowsSection.ReplayFolder)
                ? AppStatus.Disconnected
                : AppStatus.Connected);

            if (Directory.Exists(m_Settings.WatcherSection.CategoryImageFolder))
            {
                foreach (string file in Directory.EnumerateFiles(m_Settings.WatcherSection.CategoryImageFolder))
                {
                    try
                    {
                        Image categoryImage = Image.FromFile(file);
                        string categoty = Path.GetFileNameWithoutExtension(file);
                        m_ImageStatusIndex.Add(categoty, imageList.Images.Count);
                        imageList.Images.Add(categoryImage);
                        comboBoxAddStatus.Items.Add(categoty);
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "Error loading images for player state");
                    }
                }
            }

            foreach (var p in m_PlayersToWatch)
            {
                AddPlayerToWatchListLV(p);
            }
        }



        #region UI events
        private void LabelStatus_Click(object? sender, EventArgs e)
        {
            OpenFileDialog op = new();
            op.Title = "Select the WoWs replays folder";
            if (op.ShowDialog() == DialogResult.OK)
            {

                string replayDir = Path.GetDirectoryName(op.FileName) ?? "";
                var aReplay = Directory.GetFiles(replayDir, "*.wowsreplay").FirstOrDefault();
                if (aReplay != null || MessageBox.Show("No '.wowsreplay' files was fond in the folder. Are you sure you selected the correct replay folder?", "Please confirm", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                {
                    m_Settings.WowsSection.ReplayFolder = replayDir;
                    JsonHelper.SaveJson("appsettings.json", m_Settings);
                    SetStatusInStatusBar(AppStatus.Connected);
                }
            }
        }

        private void tbPlayerList_DragDrop(object sender, DragEventArgs e)
        {
            string? newPlayer = e?.Data?.GetData(typeof(string)) as string;
            if (!string.IsNullOrEmpty(newPlayer))
            {
                textBoxAddName.Text = newPlayer;
            }
        }

        private void lvTeam1_MouseDown(object sender, MouseEventArgs e)
        {
            if (((ListView)sender).SelectedItems.Count > 0)
            {
                var name = ((ListView)sender).SelectedItems[0].Text;
                textBoxShowGreeting.Text = string.Format(m_Settings.WatcherSection.Greetings, name);
                DoDragDrop(name, DragDropEffects.All);
            }
        }

        private void tbPlayerList_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void buttonAddPlayerToWatchlist_Click(object sender, EventArgs e)
        {
            if (m_PlayersToWatch.Any(p => p.Name.Equals(textBoxAddName.Text, StringComparison.OrdinalIgnoreCase)))
            {
                if (MessageBox.Show("This player is already in the list, replace", "Please confirm", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    RemovePlayerFromWatchList(textBoxAddName.Text);
                }
                else
                {
                    return;
                }
            }
            AddPlayerToWatchList(textBoxAddName.Text, comboBoxAddStatus.SelectedItem as string ?? "", checkBoxLoud.Checked, textBoxAddNotes.Text);
            SaveWatchList();
        }

        private void menuDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Delete all selected player from the list?", "Please confirm", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (ListViewItem si in lvWatchList.SelectedItems)
                {
                    RemovePlayerFromWatchList(si.Text);
                }
                SaveWatchList();
            }
        }

        private void menuEditWatcher_Click(object sender, EventArgs e)
        {
            if (lvWatchList.SelectedItems.Count != 1)
            {
                MessageBox.Show("Please select one player", "Please confirm");
            }
            else
            {
                ListViewItem si = lvWatchList.SelectedItems[0];
                textBoxAddName.Text = si.Text;
                checkBoxLoud.Checked = si.SubItems[(int)WlPIIndex.Notify].Text == "true";

                foreach (var item in m_ImageStatusIndex)
                {
                    if (item.Value == si.StateImageIndex)
                    {
                        comboBoxAddStatus.SelectedItem = item.Key;
                    }
                }
                RemovePlayerFromWatchList(si.Text);
            }
        }

        private void lvWatchList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvWatchList.SelectedItems.Count > 0)
            {
                ListViewItem si = lvWatchList.SelectedItems[0];
                textBoxAddNotes.Text = $"o7 {si.Text}, nice to see you.";
            }
        }

        #endregion

        #region UI - related functions
        private void SetStatusInStatusBar(AppStatus status)
        {
            Action setLabel = () => { };
            switch (status)
            {
                case AppStatus.Disconnected:
                    setLabel = () =>
                    {
                        labelStatus.Text = "Disconnected, click to set folder";
                        labelStatus.ForeColor = Color.Red;

                    };
                    break;
                case AppStatus.Connected:
                    setLabel = () =>
                    {
                        labelStatus.Text = "Connected";
                        labelStatus.ForeColor = Color.Green;
                        m_Watcher.Path = m_Settings.WowsSection.ReplayFolder;
                        m_Watcher.Filter = m_Settings.WowsSection.TeamFileName;
                        m_Watcher.EnableRaisingEvents = true;
                        m_Watcher.Created -= OnTempArenaInfoJsonCreated;
                        m_Watcher.Created += OnTempArenaInfoJsonCreated;
                    };
                    break;
            }
            if (this.InvokeRequired) this.Invoke(setLabel); else setLabel.Invoke();
        }
        private async void OnTempArenaInfoJsonCreated(object sender, FileSystemEventArgs e)
        {
            Battle? battle = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    battle = JsonHelper.LoadJson<Battle>(e.FullPath);
                    break;
                }
                catch
                {
                    Thread.Sleep(1000 * (int)Math.Pow(2, i));
                }
            }
            if (battle != null)
            {
                ResetProgressPar(battle.WgPlayers.Length + 4);

                await m_wowsService.LoadDataForBattle(battle.WgPlayers);

                string logDir = Path.Combine(Environment.CurrentDirectory, "log");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                JsonHelper.SaveJson(Path.Combine(logDir, DateTime.UtcNow.Ticks.ToString()), battle);
                if (this.InvokeRequired) await this.Invoke(async () => await FillListViews()); else await FillListViews();
            }
        }
        private void IncreaseProgressPar()
        {
            if (this.InvokeRequired) this.Invoke(() => pbLoading.Value++); else pbLoading.Value++;
        }
        private void ResetProgressPar(int max)
        {
            if (this.InvokeRequired) this.Invoke(() => { pbLoading.Value = 0; pbLoading.Maximum = max; });
            else
            {
                pbLoading.Value = 0;
                pbLoading.Maximum = max;
            }
        }
        private async Task FillListViews()
        {

            foreach (var player in m_wowsService.WgPlayers)
            {

                player.Ship = m_wowsService.GetShipInfo(player.ShipId);
                if (!player.StatInfo.HiddenProfile)
                {
                    await player.StatInfo.LoadFromWgAPI(m_Settings.WowsSection.ApiAddress, m_Settings.WowsSection.AppId, player.AccountId.ToString()!, player.ShipId.ToString());
                }
                IncreaseProgressPar();
            }
            bool showNotification = false;
            lvTeam1.Items.Clear();
            lvTeam2.Items.Clear();
            var ordered = m_wowsService.WgPlayers.OrderBy(v => v.Ship.type).ThenBy(v => v.Ship.nation).ThenBy(v => v.Ship.name);
            foreach (var player in ordered)
            {
                ListView targetLv = (player.Relation == 2) ? lvTeam2 : lvTeam1;
                showNotification |= AddPlayerToTeamLV(targetLv, player);
            }
            FillTotals(lvTeam1);
            FillTotals(lvTeam2);
            if (showNotification)
            {
                new ToastContentBuilder()
                    .AddText("Someone you watch is in the battle!")
                    .AddText("Check the Player Watcher to see the list")
                    .Show();

                if (File.Exists(m_Settings.WatcherSection.SoundFile))
                {
                    new SoundPlayer(m_Settings.WatcherSection.SoundFile).Play();
                }
                else
                {
                    SystemSounds.Exclamation.Play();
                }
            }
        }

        private void FillTotals(ListView targetLv)
        {
            WgStat teamStat = new WgStat();
            int groupCounter = 0;
            foreach (ListViewGroup lvg in targetLv.Groups.Cast<ListViewGroup>().Where(g => g.Items.Cast<ListViewItem>().Any(p => !((WgPlayer)p.Tag).StatInfo.HiddenProfile))) //g => g.Items.Count > 0 && 
            {
                var wr = lvg.Items.Cast<ListViewItem>().Where(p => !((WgPlayer)p.Tag).StatInfo.HiddenProfile).Average(p => ((WgPlayer)p.Tag).StatInfo.Overall.WR);
                var dmg = lvg.Items.Cast<ListViewItem>().Where(p => !((WgPlayer)p.Tag).StatInfo.HiddenProfile).Average(p => ((WgPlayer)p.Tag).StatInfo.Overall.Damage);
                var dmgs = lvg.Items.Cast<ListViewItem>().Where(p => !((WgPlayer)p.Tag).StatInfo.HiddenProfile).Average(p => ((WgPlayer)p.Tag).StatInfo.CurrentShipStat.Damage);
                lvg.Header = $"{lvg.Header} : [ {wr:N2}% ]  / [ {dmgs:N0} ]";

                teamStat.WR += wr;
                teamStat.Damage += dmg;
                groupCounter++;
            }
            teamStat.WR /= groupCounter;
            teamStat.Damage /= groupCounter;

            ListViewItem item = new ListViewItem();
            item.UseItemStyleForSubItems = false;
            item.Text = "Team summary";
            item.SubItems.Add("");
            item.SubItems.Add("");
            item.SubItems.Add("");
            item.SubItems.Add(createColoredSubItem(teamStat.WrFormatted, teamStat.WrColor));
            item.SubItems.Add(createColoredSubItem(teamStat.DamageFormatted, teamStat.DamageColor(WowsNumbersStatSummary.ServerTotal.AvgDamage)));
            item.Group = targetLv.Groups.Cast<ListViewGroup>().Last();
            item.BackColor = Color.FromArgb(217, 231, 252);
            targetLv.Items.Add(item);
        }

        private bool AddPlayerToTeamLV(ListView targetLv, WgPlayer player)
        {

            ListViewItem item = new ListViewItem();
            //item.BackColor = item.Index % 2 == 0 ? Color.FromArgb(240, 240, 240) : Color.FromArgb(255, 255, 0);
            item.Tag = player;
            item.UseItemStyleForSubItems = false;
            item.Group = targetLv.Groups.Cast<ListViewGroup>().FirstOrDefault(g => g.Name!.Contains(player.Ship.type)) ?? targetLv.Groups.Cast<ListViewGroup>().Last();
            var aplayer = m_PlayersToWatch.FirstOrDefault(p => p.Name.Equals(player.Name, StringComparison.OrdinalIgnoreCase));
            item.Text = player.Name;
            item.ForeColor = player.StatInfo.PlayerColor();
            item.SubItems.Add(createColoredSubItem(player.StatInfo.HiddenProfile ? "" : player.ClanTag, Color.Black));
            item.SubItems.Add(createColoredSubItem(player.StatInfo.HiddenProfile ? "" : player.Ship.name, player.StatInfo.HiddenProfile ? Color.Black : player.StatInfo.ShipColor(player.ShipId)));

            item.SubItems.Add(createColoredSubItem(player.StatInfo.HiddenProfile ? "" : player.StatInfo.Overall.BattelsFormatted, Color.Black));
            item.SubItems.Add(createColoredSubItem(player.StatInfo.HiddenProfile ? "" : player.StatInfo.Overall.WrFormatted, player.StatInfo.HiddenProfile ? Color.Black : player.StatInfo.Overall.WrColor));

            item.SubItems.Add(createColoredSubItem(player.StatInfo.HiddenProfile ? "" : player.StatInfo.Overall.DamageFormatted, player.StatInfo.HiddenProfile ? Color.Black : player.StatInfo.Overall.DamageColor(WowsNumbersStatSummary.ServerTotal.AvgDamage)));

            item.SubItems.Add(createColoredSubItem(player.StatInfo.HiddenProfile ? "" : player.StatInfo.CurrentShipStat.BattelsFormatted, Color.Black));
            item.SubItems.Add(createColoredSubItem(player.StatInfo.HiddenProfile ? "" : player.StatInfo.CurrentShipStat.WrFormatted, player.StatInfo.HiddenProfile ? Color.Black : player.StatInfo.CurrentShipStat.WrColor));
            if (WowsNumbersStatSummary.ShipData.TryGetValue(player.ShipId.ToString(), out var shipStat))
            {
                item.SubItems.Add(createColoredSubItem(player.StatInfo.HiddenProfile ? "" : player.StatInfo.CurrentShipStat.DamageFormatted, player.StatInfo.HiddenProfile ? Color.Black : player.StatInfo.CurrentShipStat.DamageColor(shipStat.AvgDamage)));
            }
            else
            {
                item.SubItems.Add(createColoredSubItem(player.StatInfo.HiddenProfile ? "" : player.StatInfo.CurrentShipStat.DamageFormatted, Color.Black));
            }

            item.SubItems.Add(createColoredSubItem($"{player.Ship.tier:D2}, {player.Ship.nation}", Color.Black));
            targetLv.Items.Add(item);
            if (aplayer != null)
            {
                if (m_ImageStatusIndex.TryGetValue(aplayer.ImageStatus, out int index))
                {
                    item.StateImageIndex = index;
                }
                return aplayer.LoudNotification;
            }
            else return false;
        }

        private string getShipType(string type) => type switch
        {
            "Cruiser" => "CR",
            "Battleship" => "BB",
            "Destroyer" => "DD",
            "AirCarrier" => "CV",
            _ => "NA"
        };


        private ListViewItem.ListViewSubItem createColoredSubItem(string text, Color color)
        {
            ListViewItem.ListViewSubItem si = new ListViewItem.ListViewSubItem();
            si.Text = text;
            si.ForeColor = color;
            return si;
        }

        private void AddPlayerToWatchListLV(PlayerToWatch player)
        {
            ListViewItem item = new ListViewItem(player.Name);

            item.SubItems.Add(player.LoudNotification ? "true" : "");
            item.SubItems.Add(player.Notes);

            if (m_ImageStatusIndex.TryGetValue(player.ImageStatus, out int index))
            {
                item.StateImageIndex = index;
            }
            lvWatchList.Items.Add(item);
        }

        private void AddPlayerToWatchList(string text, string imageStatus, bool loudNotification, string notes)
        {
            PlayerToWatch p = new PlayerToWatch() { Name = text, LoudNotification = loudNotification, ImageStatus = imageStatus, Notes = notes };
            AddPlayerToWatchListLV(p);
            m_PlayersToWatch.Add(p);
        }

        private void RemovePlayerFromWatchList(string playerName)
        {
            try
            {
                m_PlayersToWatch.Remove(m_PlayersToWatch.First(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)));
                var lvItem = lvWatchList.Items.Cast<ListViewItem>().First(i => i.Text == playerName);
                lvWatchList.Items.Remove(lvItem);
            }
            catch
            {
                MessageBox.Show("Unable to remove a player from the list, see log for Ship / delete manually", "Please confirm");
            }
        }

        private void SaveWatchList()
        {
            JsonHelper.SaveJson(m_Settings.WatcherSection.PlayerList, m_PlayersToWatch);
        }
        #endregion

        private void labelGreetings_DoubleClick(object sender, EventArgs e)
        {
            m_Settings.WatcherSection.Greetings = Interaction.InputBox("{0} will be replaced by the player", "Please enter a new greetings template", m_Settings.WatcherSection.Greetings);
            JsonHelper.SaveJson("appsettings.json", m_Settings);
        }
    }
}