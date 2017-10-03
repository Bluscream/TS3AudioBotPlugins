using System;
using System.Collections.Generic;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3Client.Messages;
using TS3AudioBot.CommandSystem;
using TS3Client.Full;

namespace AutoChannelCommander
{

    public class PluginInfo
    {
        public static readonly string Name = typeof(PluginInfo).Namespace;
        public const string Description = "Automatically requests Channel Commander.";
        public const string Url = "";
        public const string Author = "Bluscream <admin@timo.de.vc>";
        public const int Version = 1;
    }

    public class AutoChannelCommander : ITabPlugin
    {
        private MainBot bot;
	    private Ts3FullClient lib;

		public bool Enabled { get; private set; }

        public PluginInfo pluginInfo = new PluginInfo();

        public void PluginLog(Log.Level logLevel, string Message) {
            Log.Write(logLevel, PluginInfo.Name + ": " + Message);
        }

        public void Initialize(MainBot mainBot) {
            bot = mainBot;
            lib = bot.QueryConnection.GetLowLibrary<Ts3FullClient>();
			lib.OnClientMoved += Lib_OnClientMoved;
            lib.OnConnected += Lib_OnConnected;
            Enabled = true; PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
        }

        private void Lib_OnConnected(object sender, EventArgs e) {
            if (!Enabled) { return; }
            PluginLog(Log.Level.Debug, "Our client is now connected, setting channel commander :)");
            bot.QueryConnection.SetChannelCommander(true);
        }

        private void Lib_OnClientMoved(object sender, IEnumerable<ClientMoved> e) {
            if (!Enabled) { return; }
			foreach (var client in e)
			{
			    if (lib.ClientId == client.ClientId)
			    {
			        PluginLog(Log.Level.Debug, "Our client was moved to " + client.TargetChannelId.ToString() + " because of " + client.Reason + ", setting channel commander :)");
			        bot.QueryConnection.SetChannelCommander(true);
			        return;
			    }
			}
			
		}

        public void Dispose() {
            lib.OnClientMoved -= Lib_OnClientMoved;
            lib.OnConnected -= Lib_OnConnected;
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        [Command("autochannelcommander toggle", PluginInfo.Description)]
        public string CommandToggleAutoChannelCommander() {
            Enabled = !Enabled;
            return PluginInfo.Name + " is now " + Enabled.ToString();
        }
    }
}
