using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Reflection;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client.Audio;
using TS3Client;
using Nett;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using System.Diagnostics;

namespace YoutubeLive
{
	public class PluginInfo
	{
		public static readonly string ShortName = typeof(PluginInfo).Namespace;
		public static readonly string Name = string.IsNullOrEmpty(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name) ? ShortName : System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
		public static string Description = "";
		public static string Url = $"https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/{ShortName}";
		public static string Author = "Bluscream <admin@timo.de.vc>";
		public static readonly Version Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
		public PluginInfo()
		{
			var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
			Description = versionInfo.FileDescription;
			Author = versionInfo.CompanyName;
		}
	}
	public class YoutubeLive : IBotPlugin
	{
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public TS3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfBot Conf { get; set; }
		public PlayManager BotPlayer { get; set; }
		public IPlayerConnection PlayerConnection { get; set; }
		public IVoiceTarget targetManager { get; set; }
		//public ConfHistory confHistory { get; set; }

		private static WebClient wc = new WebClient();

		public void Initialize()
		{
			BotPlayer.BeforeResourceStarted += BeforeResourceStarted;
			BotPlayer.BeforeResourceStopped += BeforeResourceStopped;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void BeforeResourceStarted(object sender, PlayInfoEventArgs e)
		{
			Log.Debug($"Set Volume to {PlayerConnection.Volume}");
		}

		private void BeforeResourceStopped(object sender, EventArgs e)
		{
			try
			{
				wc.
				// wc.UploadData(string.Format("https://splamy.de/api/teamspeak/version/{0}/{1}?sign={2}",
					//info.ClientVersion, info.ClientPlatform, Uri.EscapeDataString(info.ClientVersionSign)), "POST", Array.Empty<byte>());
			}
			catch { return; }
		}

		public void Dispose()
		{
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
