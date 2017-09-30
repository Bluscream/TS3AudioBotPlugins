using System;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.CommandSystem;

namespace NowPlayingNick
{

    public class PluginInfo
    {
        public static readonly string Name = typeof(PluginInfo).Namespace;
        public const string Description = "Sets the bot's nickname to the current track everytime it changes.";
        public const string Url = "";
        public const string Author = "Bluscream <admin@timo.de.vc>";
        public const int Version = 2;
    }

    public class NowPlayingNick : ITabPlugin
    {
        MainBot bot;

        public bool Enabled { get; private set; }

        public PluginInfo pluginInfo = new PluginInfo();

        public void PluginLog(Log.Level logLevel, string Message) {
            Log.Write(logLevel, PluginInfo.Name + ": " + Message);
        }

        public void Initialize(MainBot mainBot) {
            bot = mainBot;
            bot.PlayManager.AfterResourceStarted += PlayManager_AfterResourceStarted;
            Enabled = true; PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
        }

        private void PlayManager_AfterResourceStarted(object sender, PlayInfoEventArgs e) {
            if (!Enabled) { return; }
            PluginLog(Log.Level.Debug, "Track changed. setting new track title as nickname");
            var title = e.ResourceData.ResourceTitle;
            bot.QueryConnection.ChangeName(title);
		}

        public void Dispose() {
            bot.PlayManager.AfterResourceStarted += PlayManager_AfterResourceStarted;
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        [Command("nowplayingnick toggle", PluginInfo.Description)]
        public string CommandToggleNowPlayingNick() {
            Enabled = !Enabled;
            return PluginInfo.Name + " is now " + Enabled.ToString();
        }
    }
}
