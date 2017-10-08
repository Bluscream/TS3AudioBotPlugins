using System;
using System.IO;
using System.Net;
using ClientQuery.Properties;
using TelnetServer;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.CommandSystem;
using TS3Client.Full;

namespace ClientQuery
{

    public class PluginInfo
    {
        public static readonly string Name = typeof(PluginInfo).Namespace;
        public const string Description = "TS3AudioBot alternative to the Teamspeak 3 ClientQuery Plugin.";
        public const string Url = "";
        public const string Author = "Bluscream <admin@timo.de.vc>";
        public const int Version = 1;
    }

    public class ClientQuery : ITabPlugin {

        public PluginInfo pluginInfo = new PluginInfo();
        private MainBot bot;
        private Ts3FullClient lib;
        private static Server s;

        private static void PluginLog(Log.Level logLevel, string Message) {
            Log.Write(logLevel, PluginInfo.Name + ": " + Message);
        }

        public void Initialize(MainBot mainBot) {
            bot = mainBot;
            lib = bot.QueryConnection.GetLowLibrary<Ts3FullClient>();
            var PluginPath = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(Path.Combine(PluginPath, "ClientQuery"));
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");

            s = new Server(IPAddress.Any);
            s.ClientConnected += clientConnected;
            s.ClientDisconnected += clientDisconnected;
            s.ConnectionBlocked += connectionBlocked;
            s.MessageReceived += messageReceived;
            s.start(IPAddress.Parse(Settings.Default.HostAddress), Settings.Default.Port);

            PluginLog(Log.Level.Info, "Telnet Server listening on " + Settings.Default.HostAddress + ":" + Settings.Default.Port);
        }

        public void Dispose() {
            s.ClientConnected -= clientConnected;
            s.ClientDisconnected -= clientDisconnected;
            s.ConnectionBlocked -= connectionBlocked;
            s.MessageReceived -= messageReceived;
            s.stop();
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        private static void clientConnected(Client c) {
            PluginLog(Log.Level.Info, "CONNECTED: " + c);
            s.sendMessageToClient(c, "TS3 Client\r\nWelcome to the TS3AudioBot ClientQuery interface, type \"help\" for a list of commands and \"help <command> \" for information on a specific command.\r\nUse the \"auth\" command to authenticate yourself.See \"help auth\" for details.");
        }

        private static void clientDisconnected(Client c) {
            PluginLog(Log.Level.Info, "DISCONNECTED: " + c);
        }

        private static void connectionBlocked(IPEndPoint ep) {
            PluginLog(Log.Level.Warning, string.Format("BLOCKED: {0}:{1} at {2}", ep.Address, ep.Port, DateTime.Now));
        }

        private static void messageReceived(Client c, string message) {
            PluginLog(Log.Level.Debug, "MESSAGE: " + message);
            EClientStatus status = c.getCurrentStatus();
            if (message == "help")
            {
                s.sendMessageToClient(c, Resources.help);
            } else if (message.StartsWith("help "))
            {
                var helpcmd = message.Replace("help ", "");
                s.sendMessageToClient(c, "error id=1337 msg=currently\\snot\\simplemented");
                //s.sendMessageToClient(c, Resources[helpcmd]);
            }
            if (status == EClientStatus.Guest)
            {
                if (message == "auth apikey=" + Settings.Default.ApiKey)
                {
                    s.clearClientScreen(c);
                    s.sendMessageToClient(c, "error id=0 msg=ok\r\n");
                    c.setStatus(EClientStatus.LoggedIn);
                }
                else
                {
                    s.sendMessageToClient(c, "error id=1796 msg=currently\\snot\\spossible");
                    return;
                }
            }
            switch (message)
            {
                case "kickme":
                    s.kickClient(c);
                    break;
                default:
                    s.sendMessageToClient(c, "error id=256 msg=command\\snot\\sfound");
                    break;
            }
        }
    }
}
