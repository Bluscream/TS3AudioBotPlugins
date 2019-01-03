using System;
using System.Linq;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using TS3Client.Helper;
using TS3AudioBot.Web.Api;
using TS3AudioBot.Helper;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using TS3AudioBot.Config;
using TS3AudioBot.Helper.Environment;
using System.Collections.Generic;

namespace ToggleSupport
{
	public class ToggleSupport : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");
		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfBot Conf { get; set; }
		public PlayManager BotPlayer { get; set; }
		public IPlayerConnection PlayerConnection { get; set; }
		public IVoiceTarget targetManager { get; set; }
		public ConfRoot ConfRoot { get; set; }
		private ChannelIdT cid = 37;
		private string name_open = "┏ Support | Warteraum";
		private string name_closed = "┏ Support | Warteraum [Geschlossen]";
		private bool closed = false;

		public void Initialize()
		{
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}
		[Command("support toggle")]
		public string CommandToggleSupport(InvokerData invoker)
		{
			closed = !closed;
			return editSupportChannel(cid, closed, invoker);
		}
		[Command("support auf")]
		public string CommandOpenSupport(InvokerData invoker)
		{
			closed = false;
			return editSupportChannel(cid, closed, invoker);
		}
		[Command("support zu")]
		public string CommandCloseSupport(InvokerData invoker)
		{
			closed = true;
			return editSupportChannel(cid, closed, invoker);
		}

		public string editSupportChannel(ChannelIdT cid, bool close, InvokerData invoker)
		{
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() {
						new CommandParameter("cid", cid),
						new CommandParameter("channel_name", close ? name_closed : name_open),
						new CommandParameter("channel_maxclients", close ? "-1" : "-1"),
						new CommandParameter("channel_flag_maxclients_unlimited", close ? false : true)
				});
			var editResult = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
			if (!editResult.Ok) {
				var err = $"Could not {(close?"close":"open")} channel {cid} ! ({editResult.Error.Message})";
				Log.Warn($"{PluginInfo.Name}: {err}"); return err;
			}
			TS3Client.SendServerMessage($"Support channel wurde von \"{invoker.NickName}\" {(close ? "[color=orange]geschlossen" : "[color=green]geöffnet")}!");
			return $"{(close ? "[color=orange]Closed" : "[color=green]Opened")} Support!";
		}

		public void Dispose()
		{
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
	public static class PluginInfo
	{
		public static readonly string ShortName;
		public static readonly string Name;
		public static readonly string Description = "";
		public static readonly string Url = $"https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/{ShortName}";
		public static readonly string Author = "Bluscream <admin@timo.de.vc>";
		public static readonly Version Version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version;
		static PluginInfo()
		{
			ShortName = typeof(PluginInfo).Namespace;
			var name = System.Reflection.Assembly.GetCallingAssembly().GetName().Name;
			Name = string.IsNullOrEmpty(name) ? ShortName : name;
		}
	}
}
