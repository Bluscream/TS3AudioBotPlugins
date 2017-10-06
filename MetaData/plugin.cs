using System;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Helper;
using TS3Client.Commands;
using TS3Client.Full;

namespace MetaData
{

    public class PluginInfo
    {
        public static readonly string Name = typeof(PluginInfo).Namespace;
        public const string Description = "Automatically sets the bots metadata so users and apps can retrieve it.";
        public const string Url = "";
        public const string Author = "Bluscream <admin@timo.de.vc>";
        public const int Version = 1;
    }

    public class MetaData : ITabPlugin
    {
        private MainBot bot;
        private Ts3FullClient lib;

        public string[] _badges = {
            "1cb07348-34a4-4741-b50f-c41e584370f7", // TeamSpeak Addon Author
            "50bbdbc8-0f2a-46eb-9808-602225b49627", // Gamescom 2016
            "d95f9901-c42d-4bac-8849-7164fd9e2310", // Paris Games Week 2016
            "62444179-0d99-42ba-a45c-c6b1557d079a", // Gamescom 2014
            "d95f9901-c42d-4bac-8849-7164fd9e2310", // Paris Games Week 2014
            "450f81c1-ab41-4211-a338-222fa94ed157", // TeamSpeak Addon Developer (Bronze)
            "c9e97536-5a2d-4c8e-a135-af404587a472", // TeamSpeak Addon Developer (Silver)
            "94ec66de-5940-4e38-b002-970df0cf6c94", // TeamSpeak Addon Developer (Gold)
            "534c9582-ab02-4267-aec6-2d94361daa2a", // Gamescom 2017
            "34dbfa8f-bd27-494c-aa08-a312fc0bb240", // Gamescom Hero 2017
            "7d9fa2b1-b6fa-47ad-9838-c239a4ddd116", // MIFCOM

            "17dfa0dc-b6e6-42fd-8c9c-b7d168f0823e", // Coolest Hat
            "ef85ab02-8236-4e38-96cb-02c73789734f", // Best Bug Hunter
            "facee3a7-1db0-4493-a5cf-24c9f938d35d", // Informed User
            "c9a170ca-62c2-47bf-990b-db75a5d7b086"  // I'm a Scanner / I'm a Floppy
        };

        public int currentBadge = 0;
        public string[] badges = {
            "450f81c1-ab41-4211-a338-222fa94ed157,c9e97536-5a2d-4c8e-a135-af404587a472,94ec66de-5940-4e38-b002-970df0cf6c94,1cb07348-34a4-4741-b50f-c41e584370f7,50bbdbc8-0f2a-46eb-9808-602225b49627,d95f9901-c42d-4bac-8849-7164fd9e2310",
            "1cb07348-34a4-4741-b50f-c41e584370f7,50bbdbc8-0f2a-46eb-9808-602225b49627,d95f9901-c42d-4bac-8849-7164fd9e2310,62444179-0d99-42ba-a45c-c6b1557d079a,d95f9901-c42d-4bac-8849-7164fd9e2310,d95f9901-c42d-4bac-8849-7164fd9e2310",
            "62444179-0d99-42ba-a45c-c6b1557d079a,d95f9901-c42d-4bac-8849-7164fd9e2310,d95f9901-c42d-4bac-8849-7164fd9e2310,534c9582-ab02-4267-aec6-2d94361daa2a,34dbfa8f-bd27-494c-aa08-a312fc0bb240,7d9fa2b1-b6fa-47ad-9838-c239a4ddd116",
            "534c9582-ab02-4267-aec6-2d94361daa2a,34dbfa8f-bd27-494c-aa08-a312fc0bb240,7d9fa2b1-b6fa-47ad-9838-c239a4ddd116,450f81c1-ab41-4211-a338-222fa94ed157,c9e97536-5a2d-4c8e-a135-af404587a472,94ec66de-5940-4e38-b002-970df0cf6c94"
        };

        public bool Enabled { get; private set; }
	    public TickWorker Timer { get; private set; }

		public PluginInfo pluginInfo = new PluginInfo();

        public void PluginLog(Log.Level logLevel, string Message) {
            Log.Write(logLevel, PluginInfo.Name + ": " + Message);
        }

        public void Initialize(MainBot mainBot) {
            bot = mainBot;
            lib = bot.QueryConnection.GetLowLibrary<Ts3FullClient>();
            lib.OnConnected += Lib_OnConnected;
            Enabled = true; PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
        }

        private void SetMetaData() {
            var metaData = "\n" + bot.CommandSettings("QueryConnection::AudioBitrate", "").ToString();
            metaData += "\n" + bot.CommandSettings("AudioFramework::AudioMode", "");
            metaData += "\n" + bot.CommandSettings("AudioFramework::DefaultVolume", "");
            metaData += "\n" + bot.CommandSettings("QueryConnection::Address", "");
            metaData += "\n" + bot.CommandSettings("QueryConnection::DefaultNickname", "");
            metaData += "\n" + bot.CommandSettings("QueryConnection::IdentityLevel", "");
            lib.Send("clientupdate", new CommandParameter("client_meta_data", metaData));
        }

        private void Lib_OnConnected(object sender, EventArgs e) {
            if (!Enabled) { return; }
            PluginLog(Log.Level.Debug, "Our client is now connected, setting meta data");
            SetMetaData();
	        lib.Send("clientupdate", new CommandParameter("client_badges", "overwolf=0:badges=94ec66de-5940-4e38-b002-970df0cf6c94"));
			//Timer = RegisterTick(() => SetMetaData(), TimeSpan.FromSeconds(60), true);
		}

        public void SetRandomBadge() {
            //var i = Random.Next(0, badges.Length);
            currentBadge++;
            if (currentBadge > badges.Length - 1) currentBadge = 0;
            lib.Send("clientupdate", new CommandParameter("client_badges", "overwolf=1:badges=" + badges[currentBadge]));
        }

        public void Dispose() {
            lib.OnConnected -= Lib_OnConnected;
			//TickPool.UnregisterTick(Timer);
			PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        [Command("metadata toggle", PluginInfo.Description)]
        public string CommandToggleMetaData() {
            Enabled = !Enabled;
            return PluginInfo.Name + " is now " + Enabled.ToString();
        }

        [Command("metadata refresh", PluginInfo.Description)]
        public string CommandRefreshMetaData() {
            SetMetaData();
            return PluginInfo.Name + ": refreshed Meta Data";
        }

        [Command("metadata badges", PluginInfo.Description)]
        public string CommandSetClientBadges(string Badges) {
            lib.Send("clientupdate", new CommandParameter("client_badges", Badges));
            PluginLog(Log.Level.Info, "Set Badges to: " + Badges);
            return PluginInfo.Name + ": set Badges to " + Badges;
        }

        [Command("metadata togglebadges", PluginInfo.Description)]
        public string CommandSetClientToggleBadges() {
            Timer = TickPool.RegisterTick(SetRandomBadge, TimeSpan.FromSeconds(5), true);
            return "Auto toggeling badges";
        }
    }
}
