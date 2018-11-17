using System.Collections.Generic;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3Client.Messages;
using TS3AudioBot.CommandSystem;
using TS3Client.Full;

namespace AutoFollow
{
    public class PluginInfo
    {
        public static readonly string ShortName = typeof(PluginInfo).Namespace;
        public static readonly string Name = string.IsNullOrEmpty(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name) ? ShortName : System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        public static string Description = "";
        public static string Url = $"https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/{ShortName}";
        public static string Author = "Bluscream <admin@timo.de.vc>";
        public static readonly Version Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        public PluginInfo()
        {
            var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
            Description = versionInfo.FileDescription;
            Author = versionInfo.CompanyName;
        }
    }
    public class AutoFollow : IBotPlugin
    {
        private static readonly PluginInfo PluginInfo = new PluginInfo();
        private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

        private Bot bot;
	    private Ts3FullClient lib;

		public int clid { get; private set; }

        public PluginInfo pluginInfo = new PluginInfo();

        public void PluginLog(Log.Level logLevel, string Message) {
            Log.Write(logLevel, PluginInfo.Name + ": " + Message);
        }

        public void Initialize(Core Core) {
            bot = Core.Bots.GetBot(0);
            lib = bot.QueryConnection.GetLowLibrary<Ts3FullClient>();
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
