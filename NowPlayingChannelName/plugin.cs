using System;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.CommandSystem;
using TS3Client.Full;

namespace NowPlayingChannelName
{

    public class PluginInfo
    {
        public static readonly string Name = typeof(PluginInfo).Namespace;
        public const string Description = "Sets the name of the current channel everytime the track changes.";
        public const string Url = "";
        public const string Author = "Bluscream <admin@timo.de.vc>";
        public const int Version = 2;
    }

    public class NowPlayingChannelName : ITabPlugin
    {
        private MainBot bot;
        private Ts3FullClient lib;

        public bool Enabled { get; private set; }

        public PluginInfo pluginInfo = new PluginInfo();

        public void PluginLog(Log.Level logLevel, string Message) {
            switch (logLevel)
            {
                case Log.Level.Debug:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case Log.Level.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case Log.Level.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }
            Log.Write(logLevel, PluginInfo.Name + ": " + Message);
            Console.ResetColor();
        }

        public void Initialize(MainBot mainBot) {
            bot = mainBot;
            lib = bot.QueryConnection.GetLowLibrary<Ts3FullClient>();
            bot.PlayManager.AfterResourceStarted += PlayManager_AfterResourceStarted;
            Enabled = true; PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
        }

        private void PlayManager_AfterResourceStarted(object sender, PlayInfoEventArgs e) {
            if (!Enabled) { return; }
            PluginLog(Log.Level.Debug, "Track changed. sending now playing to current channel");
            var title = e.ResourceData.ResourceTitle;
            var ownChannelId = lib.WhoAmI().ChannelId;
            lib.Send() /* TODO: make stuff */
        }

        public void Dispose() {
            bot.PlayManager.AfterResourceStarted += PlayManager_AfterResourceStarted;
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        [Command("NowPlayingChannelName toggle", PluginInfo.Description)]
        public string CommandToggleNowPlayingChannelName() {
            Enabled = !Enabled;
            return PluginInfo.Name + " is now " + Enabled.ToString();
        }
    }
}
