using System;
using System.Net;
using ClientQuery.Properties;
using TelnetServer;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.Commands;
using TS3Client.Full;

namespace ClientQuery {

    public class PluginInfo
    {
        public static readonly string Name = typeof(PluginInfo).Namespace;
        public const string Description = "TS3AudioBot alternative to the Teamspeak 3 ClientQuery Plugin.";
        public const string Url = "";
        public const string Author = "Bluscream <admin@timo.de.vc>";
        public const int Version = 1;
    }

    public static class Extension {
        public static bool IsNumeric(this string s) {
            float output;
            return float.TryParse(s, out output);
        }
    }

    public class ClientQuery : ITabPlugin {

        public PluginInfo pluginInfo = new PluginInfo();
        private MainBot bot;
        private static Ts3FullClient lib;
        private static Server s;


        private static void PluginLog(Log.Level logLevel, string Message) {
            Log.Write(logLevel, PluginInfo.Name + ": " + Message);
        }

        public void Initialize(MainBot mainBot) {
			bot = mainBot;
            lib = mainBot.QueryConnection.GetLowLibrary<Ts3FullClient>();
            //var PluginPath = Directory.GetCurrentDirectory();
            //Directory.CreateDirectory(Path.Combine(PluginPath, "ClientQuery"));
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
            StartTelnetServer(IPAddress.Parse(Settings.Default.HostAddress), Settings.Default.Port);
        }

        public void StartTelnetServer(IPAddress ip, int port) {
            s = new Server(ip);
            s.ClientConnected += clientConnected;
            s.ClientDisconnected += clientDisconnected;
            s.ConnectionBlocked += connectionBlocked;
            s.MessageReceived += messageReceived;
            s.start(ip, port);
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

        [Command("clientquery start", "Changes a setting of this plugin")]
        [RequiredParameters(0)]
        public string CommandClientQueryStart(string ip = "", int port = 0)
        {
            if (string.IsNullOrWhiteSpace(ip)) ip = Settings.Default.HostAddress;
            if (port == 0) port = Settings.Default.Port;
            StartTelnetServer(IPAddress.Parse(ip), port);
            return PluginInfo.Name + ": Telnet server started at " + ip + ":" + port;
        }

        [Command("clientquery stop", "Changes a setting of this plugin")]
        public string CommandClientQueryStop() {
            s.ClientConnected -= clientConnected;
            s.ClientDisconnected -= clientDisconnected;
            s.ConnectionBlocked -= connectionBlocked;
            s.MessageReceived -= messageReceived;
            s.stop();
            return PluginInfo.Name + ": Telnet server stopped";
        }

        [Command("clientquery set", "Changes a setting of this plugin")]
        public string CommandClientQuerySet(string setting, string value) {
            try
            {
                var _value = int.Parse(value);
                Settings.Default[setting] = _value;
            }
            catch (Exception)
            {
                Settings.Default[setting] = value;
            }
            Settings.Default.Save();
            return PluginInfo.Name + ": Set " + setting + " to " + value;
        }
        [Command("clientquery get", "Retrieves a setting of this plugin")]
        public string CommandClientQueryGet(string setting) {
            return PluginInfo.Name + ": " + setting + " = " + Settings.Default[setting];
        }

        private static void clientConnected(Client c) {
            PluginLog(Log.Level.Info, "CONNECTED: " + c);
            s.sendMessageToClient(c, "TS3 Client\r\nWelcome to the TS3AudioBot ClientQuery interface, type \"help\" for a list of commands and \"help <command> \" for information on a specific command.\r\nUse the \"auth\" command to authenticate yourself. See \"help auth\" for details.\r\n");
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
            if (message == "quit")
            {
                s.kickClient(c);
                return;
            }
            if (message == "help")
            {
                s.sendMessageToClient(c, "\r\n" + Resources.help + "\r\n");
                return;
            }
            if (message.StartsWith("help "))
            {
                var helpcmd = message.Replace("help ", "");
                s.sendMessageToClient(c, "\r\nerror id=1337 msg=currently\\snot\\simplemented\r\n");
                //s.sendMessageToClient(c, Resources[helpcmd]);
                return;
            }
            switch (status)
            {
                case EClientStatus.Guest:
                    if (message == "auth apikey=" + Settings.Default.ApiKey) {
                        s.clearClientScreen(c);
                        s.sendMessageToClient(c, "\r\nerror id=0 msg=ok\r\n");
                        c.setStatus(EClientStatus.LoggedIn);
                        return;
                    } else {
                        s.sendMessageToClient(c, "\r\nerror id=1796 msg=currently\\snot\\spossible\r\n");
                        return;
                    }
                case EClientStatus.LoggedIn:
                    if (message == "logout") { 
                        c.setStatus(EClientStatus.Guest);
                        s.sendMessageToClient(c, "\r\nerror id=0 msg=ok\r\n");
                        return;
                    }
                    break;
            }
            if (message.StartsWith("gm msg="))
            {
                try {
                    lib.SendGlobalMessage(message.Replace("gm msg=", ""));
                    s.sendMessageToClient(c, "\r\nerror id=0 msg=ok\r\n");
                } catch (Exception) {
                    s.sendMessageToClient(c, "\r\nerror id=1 msg=error\r\n");
                }
                return;
            }
            switch (message)
            {
                default:
                    s.sendMessageToClient(c, "\r\nerror id=256 msg=command\\snot\\sfound\r\n");
                    return;
            }
        }
    }
}
