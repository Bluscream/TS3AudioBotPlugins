using System.Collections.Generic;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3Client.Messages;
using TS3AudioBot.Commands;
using TS3Client.Full;

namespace AutoFollow {

    public class PluginInfo
    {
        public static readonly string Name = typeof(PluginInfo).Namespace;
        public const string Description = "Automatically follows someone (. ) (. ).";
        public const string Url = "";
        public const string Author = "Bluscream <admin@timo.de.vc>";
        public const int Version = 1;
    }

    public class AutoFollow : ITabPlugin
    {
        private MainBot bot;
	    private Ts3FullClient lib;

		public int clid { get; private set; }

        public PluginInfo pluginInfo = new PluginInfo();

        public void PluginLog(Log.Level logLevel, string Message) {
            Log.Write(logLevel, PluginInfo.Name + ": " + Message);
        }

        public void Initialize(MainBot mainBot) {
			bot = mainBot;
            lib = mainBot.QueryConnection.GetLowLibrary<Ts3FullClient>();
			lib.OnClientMoved += Lib_OnClientMoved;
            clid = 0; PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
        }

        private void Lib_OnClientMoved(object sender, IEnumerable<ClientMoved> e) {
			foreach (var client in e)
			{
			    if (client.ClientId != clid) continue;
			    PluginLog(Log.Level.Debug, "Client-to-follow changed channels, following into #" + client.TargetChannelId);
			    bot.QueryConnection.MoveTo(client.TargetChannelId);
			    return;
			}
				}

        public void Dispose() {
            lib.OnClientMoved -= Lib_OnClientMoved;
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        [Command("autofollow", PluginInfo.Description)]
        public string CommandToggleAutoFollow(int ClientID) {
            if (clid == 0)
            {
                clid = ClientID;
                return PluginInfo.Name + ": Now following client #" + clid;
            } else {
                clid = 0;
                return PluginInfo.Name + ": Disabled";
            }
        }
    }
}
