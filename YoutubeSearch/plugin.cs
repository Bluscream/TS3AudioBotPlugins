using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using YoutubeSearch;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using System.Text;

//YTAPIKEY: AIzaSyBHTLNDGw7yj_gnw7_e1_ztZ5nJigP9fEg

namespace YoutubeSearchPlugin
{
	static class StringExtensions
	{

		public static IEnumerable<String> SplitInParts(this String s, Int32 partLength)
		{
			if (s == null)
				throw new ArgumentNullException("s");
			if (partLength <= 0)
				throw new ArgumentException("Part length has to be positive.", "partLength");

			for (var i = 0; i < s.Length; i += partLength)
				yield return s.Substring(i, Math.Min(partLength, s.Length - i));
		}

	}
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
	public class YoutubeSearchPlugin : IBotPlugin
	{
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");


		//private static WebClient wc = new WebClient();

		public Ts3FullClient Ts3Client { get; set; }

		public void Initialize()
		{
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
		}

		[Command("yt", PluginInfo.Description)]
		public string CommandSearchYoutube(params string[] _text)
		{
			var query = Uri.EscapeUriString(string.Join(" ", _text));
			var search = new VideoSearch();
			var result = new StringBuilder($"[color=black]You[/color][color=red]Tube[/color] Results for \"[b]{query}[/b]\":\n");
			var items = search.SearchQuery(query, 1);
			for (int i = 0; i < 5; i++) {
				result.Append($"[url={items[i].Url}]{items[i].Title}[/URL]\n");
			}
			return result.ToString(); // .SplitInParts(500).FirstOrDefault()
		}

		public void Dispose()
		{
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}
	}
}
