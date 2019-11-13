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
    public class TestPlugin : IBotPlugin
    {
        private static readonly PluginInfo PluginInfo = new PluginInfo();
        private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

        private Core core;
        private Bot bot;
        private Ts3FullClient lib;

        public void PluginLog(Log.Level logLevel, string Message) { Log.Write(logLevel, PluginInfo.Name + ": " + Message); }

        public void Initialize(Core Core) {
			core = Core;
            bot = Core.Bots.GetBot(0);
			Core.RightsManager.RegisterRights("TestPlugin.dummyperm");
            lib = bot.QueryConnection.GetLowLibrary<Ts3FullClient>();
            bot.QueryConnection.OnClientConnect += QueryConnection_OnClientConnect;
            bot.QueryConnection.OnClientDisconnect += QueryConnection_OnClientDisconnect;
            bot.QueryConnection.OnMessageReceived += QueryConnection_OnMessageReceived;
            bot.PlayManager.AfterResourceStarted += PlayManager_AfterResourceStarted;
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
			core.RightsManager.UnregisterRights("TestPlugin.dummyperm");
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
