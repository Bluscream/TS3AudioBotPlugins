using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3Client;
using TS3Client.Messages;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Helper;
using System.Reflection;
using TS3Client.Commands;
using TS3Client.Full;

namespace TestPlugin
{
    public class TestPlugin : ITabPlugin {

        public class PluginInfo {
            public static readonly string Name = typeof(PluginInfo).Namespace;
            public const string Description = "Sends a message to the current channel everytime the track changes.";
            public const string URL = "";
            public const string Author = "Bluscream <admin@timo.de.vc>";
            public const int Version = 1337;
        }
		private MainBot bot;
        private Ts3FullClient lib;

        public void PluginLog(Log.Level logLevel, string Message) { Log.Write(logLevel, PluginInfo.Name + ": " + Message); }

        public void Initialize(MainBot mainBot) {
			bot = mainBot;
			mainBot.RightsManager.RegisterRights("TestPlugin.dummyperm");
            lib = mainBot.QueryConnection.GetLowLibrary<Ts3FullClient>();
			mainBot.QueryConnection.OnClientConnect += QueryConnection_OnClientConnect;
			mainBot.QueryConnection.OnClientDisconnect += QueryConnection_OnClientDisconnect;
			mainBot.QueryConnection.OnMessageReceived += QueryConnection_OnMessageReceived;
			mainBot.PlayManager.AfterResourceStarted += PlayManager_AfterResourceStarted;
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");

        }

        private void PlayManager_AfterResourceStarted(object sender, PlayInfoEventArgs e) {
            Console.WriteLine("sd");
            var title = e.ResourceData.ResourceTitle;
            var length = 2.0m.ToString(bot.PlayerConnection.Length.ToString());
            bot.QueryConnection.SendChannelMessage("Now playing " + title + " (" + length + ")");
        }

        private void QueryConnection_OnMessageReceived(object sender, TextMessage e) {
            Console.WriteLine("got message " + e.Message);
            bot.QueryConnection.SendMessage(e.Message, e.InvokerId);
        }

        private void QueryConnection_OnClientDisconnect(object sender, ClientLeftView e) {
            bot.QueryConnection.SendMessage("ciao", e.ClientId);
        }

        private void QueryConnection_OnClientConnect(object sender, ClientEnterView e) {
            bot.QueryConnection.SendMessage("hallo", e.ClientId);
        }

        public void SendChannelMessage(string Message) {
            bot.QueryConnection.ChangeName(PluginInfo.Name);
            bot.QueryConnection.SendChannelMessage(Message);
            bot.QueryConnection.ChangeName("Bluscream's Bitch");
        }

        public void Dispose() {
            bot.QueryConnection.OnClientConnect -= QueryConnection_OnClientConnect;
            bot.QueryConnection.OnClientDisconnect -= QueryConnection_OnClientDisconnect;
            bot.QueryConnection.OnMessageReceived -= QueryConnection_OnMessageReceived;
			bot.RightsManager.UnregisterRights("TestPlugin.dummyperm");
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        [Command("isowner", "Bla")]
        public string CommandCheckOwner(ExecutionInformation info) {
            return info.HasRights("TestPlugin.dummyperm").ToString();
        }

        [Command("tprequest", "Request Talk Power!")]
        public string CommandTPRequest(string name, int number) {
            var owner = bot.QueryConnection.GetClientByName("Bluscream").UnwrapThrow();
            return "Hi " + name + ", you choose " + number + ". My owner is " + owner.Uid;
        }
    }
}
