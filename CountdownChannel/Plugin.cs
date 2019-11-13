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
using TS3AudioBot.Helper;
using TS3AudioBot.CommandSystem;
using Humanizer;
using Humanizer.Localisation;

namespace CountdownChannel
{
	public static class PluginInfo
	{
		public static readonly string ShortName;
		public static readonly string Name;
		public static readonly string Description;
		public static readonly string Url;
		public static readonly string Author = "Bluscream";
		public static readonly Version Version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version;
		static PluginInfo()
		{
			ShortName = typeof(PluginInfo).Namespace;
			var name = System.Reflection.Assembly.GetCallingAssembly().GetName().Name;
			Name = string.IsNullOrEmpty(name) ? ShortName : name;
		}
	}
	public class CountdownChannel : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");
		public Ts3Client TS3Client { get; set; }
		public Ts3FullClient TS3FullClient { get; set; }
		public TickWorker Timer { get; set; }
		private static ChannelIdT cid = 0;
		List<string> colors = new List<string> { "red", "green", "yellow", "orange", "blue" };
		DateTime newyear;

		public void Initialize()
		{
			newyear = new DateTime(DateTime.Now.Year + 1, 1, 1);
			Timer = TickPool.RegisterTick(Tick, TimeSpan.FromSeconds(1), false);
			Log.Info("Plugin {} v{} by {} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		public void Tick()
		{
			try
			{
				if (cid < 1) { Timer.Active = false; }
				DateTime now = DateTime.Now;
				TimeSpan countdown = newyear - now;
				
				if (countdown.TotalSeconds < 0) {
					Timer.Active = false;
				}
				else if (countdown.TotalSeconds == 0) {
					for (int i = 1; i < 3; i++)
					{
						TS3Client.SendServerMessage($"[b][color=red]Frohes Neues {newyear.Year} !!!");
					}
					setChannelName(cid, $"[cspacer]Frohes neues {newyear.Year}");
				}
				else if (countdown.TotalSeconds <= 10)
				{
					int index = new Random().Next(colors.Count);
					var color = colors[index];
					// colors.RemoveAt(index);
					TS3Client.SendServerMessage($"[b][color={color}]Noch {countdown.TotalSeconds} Sekunden bis {newyear.Year}");
				}
				var countdownstr = countdown.ToString(@"hh\:mm\:ss"); // dd\:  .Humanize(maxUnit: TimeUnit.Day, precision: 7); // .ToString(@"dd\.hh\:mm\:ss");
				setChannelName(cid, $"[cspacer]🎆 Noch {countdownstr} bis {newyear.Year} 🎆");
			} catch (Exception ex) { Log.Error(ex.ToString()); }
		}

		public bool setChannelName(ChannelIdT cid, string name)
		{
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() {
						new CommandParameter("cid", cid),
						new CommandParameter("channel_name", name)
				});
			var editResult = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
			if (!editResult.Ok) { Log.Warn($"{PluginInfo.Name}: Could not edit channel! ({editResult.Error.Message})"); return false; }
			return true;
		}

		[Command("countdown", "")]
		public string CommandToggleCountdown(ChannelIdT _cid = 0)
		{
			if (!Timer.Active) { cid = _cid; } else { cid = 0; }
			Timer.Active = !Timer.Active;
			return $"Set timer to {Timer.Active} for channel id {cid}";
		}

		public void Dispose()
		{
			Timer.Active = false;
			TickPool.UnregisterTicker(Timer);
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
