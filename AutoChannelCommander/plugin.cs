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
	public static class Extensions
	{
		public static T Next<T>(this T src) where T : struct
		{
			if (!typeof(T).IsEnum)
				throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));
			T[] Arr = (T[])Enum.GetValues(src.GetType());
			int j = (Array.IndexOf<T>(Arr, src) + 1) % Arr.Length;
			return Arr[j];
		}
	}
	public class PluginInfo
	{
		public static readonly string Name = typeof(PluginInfo).Namespace;
		public const string Description = "Automatically toggles Channel Commander.";
		public const string Url = "https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/AutoChannelCommander";
		public const string Author = "Bluscream <admin@timo.de.vc>";
		public const int Version = 1;
	}

	public enum MODE {
		OFF,
		CHANNEL,
		TALKING
	}

	public class AutoChannelCommander : IBotPlugin
	{
		public Bot bot;
		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }

		public TickWorker Timer { get; set; }
		public MODE Mode { get; private set; } = MODE.OFF;
		public bool CCState;

		public void PluginLog(LogLevel logLevel, string Message) { Console.WriteLine($"[{logLevel.ToString()}] {PluginInfo.Name}: {Message}"); }

		public void Initialize()
		{
			TS3FullClient.OnEachClientMoved += OnEachClientMoved;
			TS3FullClient.OnChannelListFinished += OnChannelListFinished;
			//TS3FullClient.On
			Timer = TickPool.RegisterTick(Tick, TimeSpan.FromSeconds(1), false);
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
		}

		private void OnEachClientMoved(object sender, ClientMoved e)
		{
			if (Mode != MODE.CHANNEL) { return; }
			if (TS3FullClient.ClientId != e.ClientId) return;
			PluginLog(LogLevel.Debug, "Our client was moved to " + e.TargetChannelId.ToString() + " because of " + e.Reason + ", setting channel commander :)");
			TS3Client.SetChannelCommander(true);
			return;
		}

		private void OnChannelListFinished(object sender, IEnumerable<ChannelListFinished> e)
		{
			if (Mode == MODE.OFF) { return; }
			PluginLog(LogLevel.Debug, "Our client is now fully connected, setting channel commander :)");
			TS3Client.SetChannelCommander(true);
		}

		public void Tick()
		{
			CCState = !CCState;
			TS3Client.SetChannelCommander(CCState);
		}

		[Command("acc toggle", PluginInfo.Description)]
		public string CommandToggleAutoChannelCommander()
		{
			Mode = Mode.Next();
			if (Mode != MODE.OFF) TS3Client.SetChannelCommander(true);
			return PluginInfo.Name + " is now " + Mode.ToString();
		}
		[Command("acc blink", "")]
		//[RequiredParameters(0)]
		public void CommandAutoChannelCommander(int interval = 1)
		{
			Timer.Interval = TimeSpan.FromSeconds(interval); // TODO: Change Interval dynamically
			Timer.Active = !Timer.Active;
		}

		public void Dispose()
		{
			Timer.Active = false;
			TickPool.UnregisterTicker(Timer);
			TS3FullClient.OnEachClientMoved -= OnEachClientMoved;
			TS3FullClient.OnChannelListFinished -= OnChannelListFinished;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}
	}
}
