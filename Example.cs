using System;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3Client;
using TS3Client.Commands;
using TS3Client.Full;

namespace Example
{
    public class Example : ITabPlugin {
        private MainBot bot;
        private Ts3FullClient lib;
        public void Initialize(MainBot mainBot)
        {
            bot = mainBot;
            lib = bot.QueryConnection.GetLowLibrary<Ts3FullClient>();
            lib.OnClientMoved += Lib_OnClientMoved;
        }
        private void Lib_OnClientMoved(object sender, System.Collections.Generic.IEnumerable<TS3Client.Messages.ClientMoved> e) {
            foreach (var client in e)
            {
                lib.SendPrivateMessage("Hello, you just moved to another channel", client.ClientId);
				try {
					lib.Send("clientpoke", new CommandParameter("clid", client.ClientId),
						new CommandParameter("msg", "Oh,\\sno\\swhat\\sare\\syou\\sdoing?"));
				} catch (Exception ex) { Log.Write(Log.Level.Warning, string.Format("Exception thrown while trying to poke client #{0}: {1}", client.ClientId, ex.Message)); }
			}
        }
        public void Dispose() {
            lib.OnClientMoved -= Lib_OnClientMoved;
        }
    }
}