using System;
using System.Collections.Generic;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3Client.Messages;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Helper;
using TS3Client.Full;
using TS3Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;

namespace AutoChannelCommander
{
	public class PluginInfo
	{
		public static readonly string Name = typeof(PluginInfo).Namespace;
		public const string Description = "Automatically requests Channel Commander.";
		public const string Url = "";
		public const string Author = "Bluscream <admin@timo.de.vc>";
		public const int Version = 1;
	}

	public class AutoChannelCommander : IBotPlugin
	{
		public Bot bot;
		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }

		public TickWorker Timer { get; set; }
		public bool Enabled { get; private set; }
		public bool CCState;

		public void PluginLog(LogLevel logLevel, string Message) { Console.WriteLine($"[{logLevel.ToString()}] {PluginInfo.Name}: {Message}"); }

		public void Initialize()
		{
			//TS3FullClient.OnClientMoved += Lib_OnClientMoved;
			TS3FullClient.OnConnected += Lib_OnConnected;
			Timer = TickPool.RegisterTick(Tick, TimeSpan.FromSeconds(1), false);
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
			Enabled = true;
		}

		private void Lib_OnConnected(object sender, EventArgs e)
		{
			if (!Enabled) { return; }
			PluginLog(LogLevel.Debug, "Our client is now connected, setting channel commander :)");
			TS3Client.SetChannelCommander(true);
		}

		private void Lib_OnClientMoved(object sender, IEnumerable<ClientMoved> e)
		{
			if (!Enabled) { return; }
			foreach (var client in e)
			{
				if (TS3FullClient.ClientId != client.ClientId) continue;
				PluginLog(LogLevel.Debug, "Our client was moved to " + client.TargetChannelId.ToString() + " because of " + client.Reason + ", setting channel commander :)");
				TS3Client.SetChannelCommander(true);
				return;
			}
		}

		public void Tick()
		{
			CCState = !CCState;
			TS3Client.SetChannelCommander(CCState);
		}

		public void Dispose()
		{
			Timer.Active = false;
			TickPool.UnregisterTicker(Timer);
			TS3FullClient.OnClientMoved -= Lib_OnClientMoved;
			TS3FullClient.OnConnected -= Lib_OnConnected;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}

		[Command("acc toggle", PluginInfo.Description)]
		public string CommandToggleAutoChannelCommander()
		{
			Enabled = !Enabled;
			TS3Client.SetChannelCommander(Enabled);
			return PluginInfo.Name + " is now " + Enabled.ToString();
		}
		[Command("acc blink", "")]
		//[RequiredParameters(0)]
		public void CommandAutoChannelCommander(int interval = 1)
		{
			Timer.Interval = TimeSpan.FromSeconds(interval); // TODO: Change Interval dynamically
			Timer.Active = !Timer.Active;
		}
	}
}
