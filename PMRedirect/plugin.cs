using System.Collections.Generic;
using System.Linq;
using PMRedirect.Properties;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.CommandSystem;
using TS3Client;
using TS3Client.Full;
using TS3Client.Messages;

namespace PMRedirect {

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
        public bool Enabled { get; private set; }

        public void PluginLog(Log.Level logLevel, string Message) {
            Log.Write(logLevel, PluginInfo.Name + ": " + Message);
        }

        public void Initialize(MainBot mainBot)
        {
			bot = mainBot;
            lib = bot.QueryConnection.GetLowLibrary<Ts3FullClient>();
			mainBot.RightsManager.RegisterRights("PMRedirect.isowner");
            lib.OnTextMessageReceived += Lib_OnTextMessageReceived;
            Enabled = true;PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
        }

        private string ParseInvoker(TextMessage msg)
        {
            return "[URL=client://" + msg.InvokerId + "/" + msg.InvokerUid + "]" + msg.InvokerName + "[/URL]";
        }

        private void Lib_OnTextMessageReceived(object sender, IEnumerable<TextMessage> e)
        {
            if (!Enabled) return;
            List<ClientData> clientbuffer = null;
                foreach (var msg in e)
                {
                    if (msg.Target != TextMessageTargetMode.Private || msg.InvokerId == lib.WhoAmI().ClientId) continue;
                    try {
                        clientbuffer = clientbuffer ?? lib.ClientList(ClientListOptions.uid).ToList();
                        foreach (var client in clientbuffer) {
                            if (client.Uid == msg.InvokerUid) continue;
                            if (!bot.RightsManager.HasAllRights(new InvokerData(client.Uid), "PMRedirect.isowner")) continue;
                            // PluginLog(Log.Level.Debug, "Got PM from " + msg.InvokerUid + ". Redirecting to " + client.Uid);
                            bot.QueryConnection.SendMessage("Got PM from " + ParseInvoker(msg) + ": " + msg.Message, client.ClientId);
                        }
                        //foreach (var uid in Settings.Default.uids)
                        //{
                        //    PluginLog(Log.Level.Debug, "uid: " + uid);
                        //}
                    } catch (Ts3CommandException exception) {
                        PluginLog(Log.Level.Error, exception.ToString());
                    }
            }
        }

        public void Dispose() {
            lib.OnTextMessageReceived -= Lib_OnTextMessageReceived;
            bot.RightsManager.UnregisterRights("PMRedirect.isowner");
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
