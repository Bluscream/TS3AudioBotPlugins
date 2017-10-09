using System;
using System.Collections.Generic;
using System.Linq;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.CommandSystem;
using TS3Client.Full;
using TS3Client.Messages;

namespace RegistriertChannel
{

    public class PluginInfo
    {
        public static readonly string Name = typeof(PluginInfo).Namespace;
        public const string Description = "Mainly for the GommeHD teamspeak.";
        public const string Url = "";
        public const string Author = "Bluscream <admin@timo.de.vc>";
        public const int Version = 1;
    }

    public class RegistriertChannel : ITabPlugin
    {
        private MainBot bot;
        private Ts3FullClient lib;
        public bool Enabled { get; private set; }
        public PluginInfo pluginInfo = new PluginInfo();
        public void PluginLog(Log.Level logLevel, string Message) { Log.Write(logLevel, PluginInfo.Name + ": " + Message); }
        private readonly List<string> clientUIDs = new List<string>();

        public string welcomeMSG = "Hey {nickname} du bist leider nicht registriert,\n" +
                                   "deshalb hast du hier nur eine Chance talkpower zu bekommen falls ein Moderator im channel ist.\n" +
                                   "Um dich auf diesem Teamspeak Server zu registrieren musst du folgendes tun:\n\n" +
                                   "1. Auf den Minecraft Server [color=green]gommehd.net[/color] joinen.\n" +
                                   "2. Im Minecraft chat [color=red]/ts set {uid}[/color] eingeben.\n" +
                                   "3. Im Teamspeak Chat dem User [URL=client://0/serveradmin~Gomme-Bot]Gomme-Bot[/URL] deinen Minecraft Namen schreiben (Groß/Kleinschreibung beachten)\n" +
                                   "4. Wenn die Registrierung erfolgreich warst erhälst du die Server Gruppe \"Registriert\". Es kann eine Zeit lang dauern bis dein Minecraft Kopf hinter deinem Namen erscheint.";

        public void Initialize(MainBot mainBot) {
            bot = mainBot;
            lib = bot.QueryConnection.GetLowLibrary<Ts3FullClient>();
            lib.OnClientMoved += Lib_OnClientMoved;
            Enabled = true; PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
        }

        private void Lib_OnClientMoved(object sender, IEnumerable<ClientMoved> e) {
            if (!Enabled) return;
            foreach (var client in e)
            {   
                if (lib.WhoAmI().ChannelId == client.TargetChannelId) continue;
                var clientinfo = lib.ClientInfo(client.ClientId);
                if (!clientUIDs.Contains(clientinfo.Uid)) continue;
                if (!clientinfo.ServerGroups.Contains(Convert.ToUInt64(13))) continue;
                var welcomemsg = welcomeMSG.Replace("{nickname}", clientinfo.NickName).Replace("{uid}", clientinfo.Uid);
                PluginLog(Log.Level.Debug, "\n" + welcomemsg + "\n");
                bot.QueryConnection.SendMessage(welcomemsg, client.ClientId);
                clientUIDs.Add(clientinfo.Uid);
            }
        }

        public void Dispose()
        {
            lib.OnClientMoved -= Lib_OnClientMoved;
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        [Command("registriertchannel toggle", PluginInfo.Description)]
        public string CommandToggleRegistriertChannel() {
            Enabled = !Enabled;
            return PluginInfo.Name + " is now " + Enabled;
        }

        [Command("registriertchannel list", PluginInfo.Description)]
        public string CommandRegistriertChannelList()
        {
            return string.Join(", ", clientUIDs);
        }

        [Command("registriertchannel msg", PluginInfo.Description)]
        public string CommandRegistriertChannelMSG()
        {
            var whoami = lib.WhoAmI();
            return welcomeMSG.Replace("{nickname}", whoami.NickName).Replace("{uid}", whoami.Uid);
        }
    }
}
