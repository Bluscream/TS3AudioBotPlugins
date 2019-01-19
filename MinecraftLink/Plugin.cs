using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3Client.Full;
using TS3Client.Audio;
using TS3Client;
using TS3AudioBot.History;
using TS3AudioBot.Helper;
using Newtonsoft.Json;
using IniParser;
using IniParser.Model;
using TS3Client.Commands;
using Newtonsoft.Json;

using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using ClientUidT = System.String;
using TS3Client.Messages;
using System.Text;
using TS3AudioBot.Sessions;
using TS3AudioBot.CommandSystem.Text;

namespace MinecraftLink
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

	public class MinceraftProfile
	{
		public string id { get; set; }
		public string name { get; set; }
		public string error { get; set; }
		public string errorMessage { get; set; }
	}

	public class MinecraftLink : IBotPlugin
	{
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfRoot ConfRoot { get; set; }

		private static FileIniDataParser ConfigParser;
		private static string PluginConfigFile;
		private static IniData PluginConfig;

		private static string CacheDir;

		private bool LinkingEnabled = true;

		public static string TruncateLongString(string str, int maxLength)
		{
			if (string.IsNullOrEmpty(str))
				return str;
			return str.Substring(0, Math.Min(str.Length, maxLength));
		}
		public static string ClientURL(ushort clientID, string uid = "unknown", string nickname = "Unknown User")
		{
			var sb = new StringBuilder("[URL=client://");
			sb.Append(clientID);
			sb.Append("/");
			sb.Append(uid);
			//sb.Append("~");
			sb.Append("]\"");
			sb.Append(nickname);
			sb.Append("\"[/URL]");
			return sb.ToString();
		}

		public void Initialize()
		{
			LoadConfig();
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}", "cache");
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnEachClientEnterView(object sender, ClientEnterView client)
		{
			if (!LinkingEnabled) return;
			if (client.ClientType == ClientType.Query) return;
			if (client.ClientId == TS3FullClient.ClientId) return;
		}

		private Tuple<string,string> getMcProfileFromName(string name)
		{
			var url = $"https://api.mojang.com/users/profiles/minecraft/{name}";
			using (var w = new WebClient())
			{
				var json_data = string.Empty;
				try {
					json_data = w.DownloadString(url);
					if (string.IsNullOrEmpty(json_data)) return string.Empty;
					var parsed = JsonConvert.DeserializeObject<MinceraftProfile>(json_data);
					if (string.IsNullOrEmpty(parsed.id)) return null;
					return parsed.id;
				}
				catch (Exception) { return null; }
		}

		private bool HeadIcon()
		{

			return true;
		}

		[Command("link", "")]
		public string CommandAcceptToS(InvokerData invoker, string input)
		{
			if (!LinkingEnabled) return PluginConfig["Templates"]["Linking Disabled"];
			var mcuuid = getMcUuidFromName(input);
			// Add icon
			return PluginConfig["Templates"]["Linked Response"].Replace("%mcname%",mcname).Replace("%mcuuid%",mcuuid);
		}

		private void LoadConfig()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			if (ConfigParser == null) ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				var section = "Templates";
				PluginConfig[section]["Linked Response"] = @"[color=green]Du wurdest erfolgreich mit dem Minecraft Account  [url=https://mcuuid.net/?q=%mcuuid%]%mcname%[/url]"; // .Mod().Color(Color.Green).ToString()
				PluginConfig[section]["Linking Disabled"] = "Minecraft linking is currently disabled!";
				section = "Icons";
				PluginConfig[section]["URL"] = "https://cravatar.eu/helmavatar/%mcuuid%/16.png";
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created, please modify it and reload!", PluginInfo.Name);
				return;
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
		}

		public void Dispose()
		{
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			if (ConfigParser == null) ConfigParser = new FileIniDataParser();
			ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
