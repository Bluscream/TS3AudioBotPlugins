using System;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.Commands;
using TS3AudioBot.Helper;
using TS3Client.Commands;
using TS3Client.Full;
using System.Text;

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
        public string[] badges = {
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
            "f81ad44d-e931-47d1-a3ef-5fd160217cf8", // 4Netplayers customer
			"f22c22f1-8e2d-4d99-8de9-f352dc26ac5b", // Rocket Beans TV
        };

        public bool Enabled { get; private set; }
	    public TickWorker Timer { get; private set; }
		public PluginInfo pluginInfo = new PluginInfo();

        public void PluginLog(Log.Level logLevel, string Message) {
            Log.Write(logLevel, PluginInfo.Name + ": " + Message);
        }

        public void Initialize(MainBot mainBot) {
			bot = mainBot;
            lib = mainBot.QueryConnection.GetLowLibrary<Ts3FullClient>();
            lib.OnConnected += Lib_OnConnected;
            Enabled = true; PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
        }

        private void SetMetaData() {
			var af = bot.ConfigManager.GetDataStruct<AudioFrameworkData>("AudioFramework", true);
			var qc = bot.ConfigManager.GetDataStruct<Ts3FullClientData>("QueryConnection", true);
			var metaData = "\nQueryConnection::AudioBitrate=" + qc.AudioBitrate;
            metaData += "\nAudioFramework::AudioMode=" + af.AudioMode;
            metaData += "\nAudioFramework::DefaultVolume=" + af.DefaultVolume;
            metaData += "\nQueryConnection::Address=" + qc.Address;
            metaData += "\nQueryConnection::DefaultNickname=" + qc.DefaultNickname;
            metaData += "\nQueryConnection::IdentityLevel=" + qc.IdentityLevel;
            lib.Send("clientupdate", new CommandParameter("client_meta_data", metaData));
        }

        private void Lib_OnConnected(object sender, EventArgs e) {
            if (!Enabled) { return; }
            PluginLog(Log.Level.Debug, "Our client is now connected, setting meta data");
            SetMetaData();
	        lib.Send("clientupdate", new CommandParameter("client_badges", "overwolf=0:badges=94ec66de-5940-4e38-b002-970df0cf6c94"));
			//Timer = RegisterTick(() => SetMetaData(), TimeSpan.FromSeconds(60), true);
		}
		private int currentBadge;
        public void SetRandomBadge() {
			currentBadge = (currentBadge + 1) % badges.Length;
			var build = new StringBuilder("overwolf=1:badges=");
			for (int i = 0; i < 15; i++)
				build.Append(badges[(currentBadge + i) % badges.Length] + ",");
			build.Length--;
			lib.Send("clientupdate", new CommandParameter("client_badges", build.ToString()));
		}

        public void Dispose() {
            lib.OnConnected -= Lib_OnConnected;
			//TickPool.UnregisterTick(Timer);
			PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        [Command("metadata toggle", PluginInfo.Description)]
        public string CommandToggleMetaData() {
            Enabled = !Enabled;
            return PluginInfo.Name + " is now " + Enabled;
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
			if (!Timer.Active) {
				Timer = TickPool.RegisterTick(SetRandomBadge, TimeSpan.FromMilliseconds(500), true);
				return "Auto toggeling badges";
			} else {
				Timer.Active = false;
				Timer = null;
				return "Stopped toggeling badges";
			}
        }
    }
}
