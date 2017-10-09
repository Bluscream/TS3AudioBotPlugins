using System;
using System.Linq;
using System.Data.SQLite;
using System.IO;
using PMRedirect.Properties;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.CommandSystem;
using TS3Client;
using TS3Client.Full;

namespace PMRedirect
{

    public class PluginInfo
    {
        public static readonly string Name = typeof(PluginInfo).Namespace;
        public const string Description = "Redirects all private messages to all avaialable predefined clients.";
        public const string Url = "";
        public const string Author = "Bluscream <admin@timo.de.vc>";
        public const int Version = 1;
    }

    public class PMRedirect : ITabPlugin {

        public PluginInfo pluginInfo = new PluginInfo();
        private MainBot bot;
        private Ts3FullClient lib;

        public void PluginLog(Log.Level logLevel, string Message) {
            Log.Write(logLevel, PluginInfo.Name + ": " + Message);
        }

        public void Initialize(MainBot mainBot)
        {
            bot = mainBot;
            lib = bot.QueryConnection.GetLowLibrary<Ts3FullClient>();
            lib.OnTextMessageReceived += Lib_OnTextMessageReceived;
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
        }

        private string ParseInvoker(TS3Client.Messages.TextMessage msg)
        {
            return "[URL=client://" + msg.InvokerId + "/" + msg.InvokerUid + "]" + msg.InvokerName + "[/URL]";
        }

        private void Lib_OnTextMessageReceived(object sender, System.Collections.Generic.IEnumerable<TS3Client.Messages.TextMessage> e) {
            foreach (var msg in e)
            {
                if (msg.Target != TextMessageTargetMode.Private || msg.InvokerId == lib.WhoAmI().ClientId) continue;
                var clientbuffer = lib.ClientList(ClientListOptions.uid).ToList();
                foreach (var client in clientbuffer)
                {
                    foreach (var uid in Settings.Default.uids)
                    {
                        if (client.Uid != uid || uid == msg.InvokerUid) continue;
                        bot.QueryConnection.SendMessage(ParseInvoker(msg) + ": " + msg.Message, client.ClientId);
                    }
                }
            }
        }

        public void Dispose() {
            lib.OnTextMessageReceived -= Lib_OnTextMessageReceived;
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        [Command("pmredirect toggle", PluginInfo.Description)]
        public string CommandTogglePMRedirect()
        {
            Settings.Default.enabled = !Settings.Default.enabled;
            Settings.Default.Save();
            return PluginInfo.Name + " is now " + Settings.Default.enabled;
        }
    }
}
