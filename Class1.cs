using System;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.CommandSystem;
using TS3Client.Full;

namespace TestPlugin
{
    public class TestPlugin : ITabPlugin
    {
        private MainBot bot;
        private Ts3FullClient lib;

        public class PluginInfo
        {
            public static readonly string Name = typeof(PluginInfo).Namespace;
            public const string Description = "Test Description";
            public const string Url = "test";
            public const string Author = "Bluscream <admin@timo.de.vc>";
            public const int Version = 1337;
        }

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
            Log.Write(logLevel, Message);
            Console.ResetColor();
        }

        public void Initialize(Core mainBot) {
            bot = mainBot.Bots.GetBot(0);
            lib = bot.QueryConnection.GetLowLibrary<Ts3FullClient>();
            mainBot.RightsManager.RegisterRights("TestPlugin.dummyperm");
            //lib.OnErrorEvent()
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
        }

        public void Dispose() {
            mainBot.RightsManager.UnregisterRights("TestPlugin.dummyperm");
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        [Command("tester", "Test Description")]
        public void CommandTest(string str) {
            lib.SendGlobalMessage(str);
        }
    }
}