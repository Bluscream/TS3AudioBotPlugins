using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Helper;
using TS3AudioBot.Plugins;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3AudioBot.Config;
using System.Text;

namespace AntiVPN
{
	public class Utils
	{
	}
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
	public class AntiVPN : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient Ts3FullClient { get; set; }
		public Ts3Client Ts3Client { get; set; }
		public Bot Bot { get; set; }
		public TickWorker Timer { get; set; }
		public ConfRoot ConfRoot { get; set; }
		private static string PluginConfigFile;
		public List<ulong> remindSGIDs = new List<ulong>();
		public List<string> remindUIDs = new List<string>();
		public List<Tuple<ulong, ulong>> ComplaintCache = new List<Tuple<ulong, ulong>>();

		public AntiVPN() { }

		public void Initialize()
		{
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}
		public void Dispose()
		{
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
