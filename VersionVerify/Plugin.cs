using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using TS3AudioBot.CommandSystem.Text;
using System.Text.RegularExpressions;

using ClientIdT = System.UInt16;
using ClientUidT = System.String;

namespace VersionVerify
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
	public class VersionVerify : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfRoot ConfRoot { get; set; }

		public static readonly byte[] Ts3VerionSignPublicKey = Convert.FromBase64String("UrN1jX0dBE1vulTNLCoYwrVpfITyo+NBuq/twbf9hLw=");
		public static Lazy<Regex> VersionRegex = new Lazy<Regex>(() => new Regex(@"^(3.(\?\.)*\?|(\d\.)+(\d|\?)(-[a-z]*)?) \[Build: (\d+)\]$", RegexOptions.Compiled));

		private static List<ClientUidT> whitelistUID = new List<ClientUidT>() { };
		private static List<ulong> whitelistSGID = new List<ulong>() { 2,204,203,210,254,252,228,268 };
		private bool PluginEnabled = true;

		public static string TruncateLongString(string str, int maxLength)
		{
			if (string.IsNullOrEmpty(str))
				return str;
			return str.Substring(0, Math.Min(str.Length, maxLength));
		}

		public void Initialize()
		{
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void CheckClient(ClientIdT id, ClientType type, ClientUidT uid, ulong[] sgids, string version, string platform, string sign, string name = null)
		{
			if (id == TS3FullClient.ClientId) return;
			if (type != ClientType.Full) return;
			if (whitelistUID.Contains(uid)) return;
			/*Log.Debug("whitelistSGID: " + string.Join(", ", whitelistSGID));
			Log.Debug("sgids: " + string.Join(", ", sgids));
			Log.Debug("Intersect-> " + string.Join(", ", whitelistSGID.Intersect(sgids)));
			Log.Debug("Intersect<- " + string.Join(", ", sgids.Intersect(whitelistSGID)));
			Log.Debug("Any: " + whitelistSGID.Intersect(sgids).Any());*/
			if (whitelistSGID.Intersect(sgids).Any()) return;
			// new VersionSign(version, platform, sign);
			if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(version)) { TakeAction(id, uid, name, version, platform, sign); return; }
			if (sign != null) {
				if (string.IsNullOrWhiteSpace(sign)) { TakeAction(id, uid, name, version, platform, sign); return; }
				byte[] ver = Encoding.ASCII.GetBytes(platform + sign);
				if (!Chaos.NaCl.Ed25519.Verify(Convert.FromBase64String(sign), ver, Ts3VerionSignPublicKey)) {
					TakeAction(id, uid, name, version, platform, sign); return;
				}
			}
			var parsed = VersionRegex.Value.Match(version);
			if (!parsed.Success) { TakeAction(id, uid, name, version, platform, sign); return; }
			if (parsed.Groups[1].Value == "3.?.?") { TakeAction(id, uid, name, version, platform, sign, "?"); return; }
			var build_valid = Int32.TryParse(parsed.Groups[6].Value, out Int32 version_unix);
			if (!build_valid) { TakeAction(id, uid, name, version, platform, sign); return; }
			Int32 now_unix = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
			if (version_unix > now_unix) { TakeAction(id, uid, name, version, platform, sign, "Aus der Zukunft O.o"); return; }
		}

		private void OnEachClientEnterView(object sender, ClientEnterView client) {
			if (!PluginEnabled) return;
			try {
				var clientInfo = TS3Client.GetClientInfoById(client.ClientId).Value;
				CheckClient(client.ClientId, client.ClientType, client.Uid, client.ServerGroups, clientInfo.ClientVersion, clientInfo.ClientPlatform, clientInfo.ClientVersionSign, client.Name);
			} catch (Exception ex) { Log.Error(ex.Message); }
		}

		private void TakeAction(ClientIdT clientId, ClientUidT uid, string name, string version, string platform, string sign, string Reason = "Ungültige Version!")
		{
			Log.Warn("Client {} ({}) has an invalid version: ({}, {}, {})", name, uid, version, platform, sign);
			TS3FullClient.KickClientFromServer(clientId, Reason);
		}

		[Command("plugin toggle versionverify", "")]
		public string CommandCheckName(InvokerData invoker)
		{
			PluginEnabled = !PluginEnabled;
			return $"{(PluginEnabled?"Enabled":"Disabled")} {PluginInfo.Name}";
		}

		[Command("version check name", "")]
		public string CommandCheckName(InvokerData invoker, params string[] _name)
		{
			var name = string.Join(" ", _name);
			var c = TS3Client.GetClientByName(name).Unwrap();
			var client = TS3Client.GetClientInfoById(c.ClientId).Value;
			CheckClient(c.ClientId, c.ClientType, c.Uid, client.ServerGroups, client.ClientVersion, client.ClientPlatform, client.ClientVersionSign, c.Name);
			return $"Checked {client.Name}";
		}

		public void Dispose()
		{
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
