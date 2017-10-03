using System;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.CommandSystem;

namespace NowPlayingPrivateChat
{

    public class PluginInfo
    {
        public static readonly string Name = typeof(PluginInfo).Namespace;
        public const string Description = "Sends a message to a client of your choice everytime the track changes.";
        public const string Url = "";
        public const string Author = "Bluscream <admin@timo.de.vc>";
        public const int Version = 1;
    }

    public class NowPlayingPrivateChat : ITabPlugin
    {
        MainBot bot;

        public ushort clid { get; private set; }

        public PluginInfo pluginInfo = new PluginInfo();

        public void PluginLog(Log.Level logLevel, string Message) {
            Log.Write(logLevel, PluginInfo.Name + ": " + Message);
        }

        public void Initialize(MainBot mainBot) {
            bot = mainBot;
            bot.PlayManager.AfterResourceStarted += PlayManager_AfterResourceStarted;
            clid = 0;PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
        }

        private void PlayManager_AfterResourceStarted(object sender, PlayInfoEventArgs e)
        {
            if (clid == 0) return;
            PluginLog(Log.Level.Debug, "Track changed. sending now playing to client " + clid);
            var title = e.ResourceData.ResourceTitle;
			bot.QueryConnection.SendMessage("Now playing " + title, clid);
		}

        public void Dispose() {
            bot.PlayManager.AfterResourceStarted += PlayManager_AfterResourceStarted;
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        [Command("nowplayingprivatechat", PluginInfo.Description)]
        public string CommandToggleNowPlayingPrivateChat(ushort ClientID) {
            if (clid == 0)
            {
                clid = ClientID;
                return PluginInfo.Name + ": Now sending song changes to client #" + clid;
            }
            else
            {
                clid = 0;
                return PluginInfo.Name + ": Disabled";
            }
        }
    }
}
