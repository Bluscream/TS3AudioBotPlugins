using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using TS3AudioBot.Plugins;
using TS3Client;
using TS3Client.Full;
using TS3Client.Messages;

namespace VersionDetector
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
	public class VersionDetector : IBotPlugin
	{
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		private static WebClient wc = new WebClient();

		const string versionFile = "versions.csv";

		private HashSet<string> versions = new HashSet<string>();

		public Ts3FullClient Ts3Client { get; set; }

		public VersionDetector()
		{
			if (File.Exists(versionFile))
				versions.IntersectWith(File.ReadAllLines(versionFile));
			else
				File.Create(versionFile).Dispose();
		}

		public void Initialize()
		{
			Ts3Client.OnEachClientEnterView += Ts3Client_OnEachClientEnterView;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void Ts3Client_OnEachClientEnterView(object sender, ClientEnterView e)
		{
			try
			{
				var info = Ts3Client.ClientInfo(e.ClientId).Unwrap();

				var checkVersion = string.Format("{0},{1},{2}", info.ClientVersion, info.ClientPlatform, info.ClientVersionSign);

				if (!versions.Contains(checkVersion))
				{
					//Ts3Client.SendPrivateMessage("Thanks for submitting your soul!", e.ClientId);
					try
					{
						wc.UploadData(string.Format("https://splamy.de/api/teamspeak/version/{0}/{1}?sign={2}",
							info.ClientVersion, info.ClientPlatform, Uri.EscapeDataString(info.ClientVersionSign)), "POST", Array.Empty<byte>());
					}
					catch { return; }

					versions.Add(checkVersion);
					File.AppendAllText(versionFile, checkVersion + "\n");
				}
			}
			catch (InvalidOperationException)
			{
			}
		}

		public void Dispose()
		{
			Ts3Client.OnEachClientEnterView -= Ts3Client_OnEachClientEnterView;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
