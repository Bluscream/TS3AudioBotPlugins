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
using IniParser;
using IniParser.Model;
using TS3AudioBot.Config;
using System.Text;

namespace ComplaintReminder
{
	public class Utils {
		public static bool ContainsAny<T>(List<T> a, List<T> b)
		{
			if (a.Count <= 10 && b.Count <= 10)
			{
				return a.Any(b.Contains);
			}

			if (a.Count > b.Count)
			{
				return ContainsAny((IEnumerable<T>)b, (IEnumerable<T>)a);
			}
			return ContainsAny((IEnumerable<T>)a, (IEnumerable<T>)b);
		}
		public static bool ContainsAny<T>(IEnumerable<T> a, IEnumerable<T> b)
		{
			HashSet<T> j = new HashSet<T>(a);
			return b.Any(j.Contains);
		}

		public static string ClientURL(ushort clientID, string uid = "unknown", string nickname = "Unknown User") {
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
	public class ComplaintReminder : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient Ts3FullClient { get; set; }
		public Ts3Client Ts3Client { get; set; }
		public Bot Bot { get; set; }
		public TickWorker Timer { get; set; }
		public ConfRoot ConfRoot { get; set; }
		private static FileIniDataParser ConfigParser;
		private static string PluginConfigFile;
		public static IniData PluginConfig;
		public List<ulong> remindSGIDs = new List<ulong>();
		public List<string> remindUIDs = new List<string>();
		public List<Tuple<ulong,ulong>> ComplaintCache = new List<Tuple<ulong, ulong>>();

		public ComplaintReminder(){}

		public void Initialize()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				PluginConfig["general"]["interval"] = "30";
				PluginConfig["templates"]["newcomplaint"] = "New complaint for {target} from {source}: {message}";
				PluginConfig["templates"]["list title"] = "[b]{complaints}[/b] Complaints:";
				PluginConfig["templates"]["list entry"] = "[{timestamp}] {source} ({sourcedbid}): {target} ({targetdbid}) \"{message}\"";
				PluginConfig.Sections.Add(new SectionData("servers"));
				// PluginConfig["servers"]["SERVERUID"] = "SERVERGROUPID,CLIENTUID";
				PluginConfig.Sections.Add(new SectionData("clients"));
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created!", PluginInfo.Name);
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
			var suid = Ts3FullClient.WhoAmI().Value.VirtualServerUid.Replace("=", ""); // Todo
			if (PluginConfig["servers"].ContainsKey(suid)) {
				LoadServer(suid); RequestComplaintList();
			}
			Ts3FullClient.OnEachComplainList += OnComplainList;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private string ClientURL(ushort clientId)
		{
			var client = Ts3Client.GetCachedClientById(clientId).Value;
			return Utils.ClientURL(clientId, client.Uid, client.Name);
		}

		private Tuple<string, string> ClientURL(ComplainList Complaint)
		{
			var sourceURL = "\"" + Complaint.FromName + "\""; var targetURL = "\"" + Complaint.TargetName + "\"";
			var srcdb = Ts3Client.GetDbClientByDbId(Complaint.FromClientDbId).Value;
			var _srcids = Ts3FullClient.GetClientIds(srcdb.Uid); var srcids = _srcids.Value;
			if (_srcids.Ok) sourceURL = Utils.ClientURL(srcids[0].ClientId, srcids[0].ClientUid, srcids[0].Name);
			var tardb = Ts3Client.GetDbClientByDbId(Complaint.TargetClientDbId).Value;
			var _tarids = Ts3FullClient.GetClientIds(tardb.Uid); var tarids = _tarids.Value;
			if (_tarids.Ok) targetURL = Utils.ClientURL(tarids[0].ClientId, tarids[0].ClientUid, tarids[0].Name);
			return new Tuple<string,string>(sourceURL, targetURL);
		}

		// private ClientData ClientByDBID

		private void LoadServer(string suid)
		{
			ParseSGIDUIDList(PluginConfig["servers"][suid].Split(','));
			//toRemind = PluginConfig[PluginConfigSection]["toRemind"].Split(',').Select(ulong.Parse).ToList();
			Timer = TickPool.RegisterTick(Tick, TimeSpan.FromSeconds(int.Parse(PluginConfig["general"]["interval"])), true);
		}

		private void ParseSGIDUIDList(string[] list)
		{
			remindSGIDs.Clear();remindUIDs.Clear();
			foreach (var item in list)
			{
				if (ulong.TryParse(item, out var sgid)) { remindSGIDs.Add(sgid); }
				else { remindUIDs.Add(item); }
			}
		}

		private void OnComplainList(object sender, ComplainList Complaint) {
			try
			{
				var entry = new Tuple<ulong,ulong>(Complaint.FromClientDbId,Complaint.TargetClientDbId);
				if (ComplaintCache.Contains(entry)) return;
				var (sourceURL, targetURL) = ClientURL(Complaint);
				var clients = Ts3FullClient.ClientList().Value; //TS3Client.ClientListOptions.groups
				foreach (var client in clients) // Ts3Client.clientbuffer
				{
					if (!remindUIDs.Contains(client.Uid)) {
						var sgids = Ts3Client.GetClientServerGroups(client.DatabaseId).Value;
						if (Utils.ContainsAny(sgids, remindSGIDs)) {
							Ts3Client.SendMessage(
								PluginConfig["templates"]["newcomplaint"]
									.Replace("{source}", sourceURL)
									.Replace("{target}", targetURL)
									.Replace("{message}", Complaint.Message)
							, client.ClientId);
						}
					}
				}
				ComplaintCache.Add(entry);
			} catch(Exception ex) { Log.Error(ex); }
		}

		public R<TS3Client.LazyNotification,CommandError> RequestComplaintList()
		{
			Log.Debug($"{Bot.Name}: Requesting Complaints for server \"{Ts3FullClient.ConnectionData.Address}\"");
			var Command = new Ts3Command("complainlist", new List<ICommandPart>(){new CommandParameter("tcldbid", 0)});
			return Ts3FullClient.SendNotifyCommand(Command, NotificationType.ComplainList);
		}

		[Command("complaintreminder set", "")]
		public string CommandSetSetting(string item)
		{
			var suid = Ts3FullClient.WhoAmI().Value.VirtualServerUid;
			var isSgid = ulong.TryParse(item, out var sgid);
			var removed = false;
			if (isSgid)
			{
				if(remindSGIDs.Contains(sgid)) { remindSGIDs.Remove(sgid);removed=true; }
				else { remindSGIDs.Add(sgid); }
			}
			else {
				if (remindUIDs.Contains(item)) { remindUIDs.Remove(item);removed=true; }
				else { remindUIDs.Add(item); }
			}
			string newsett = "";
			if (remindSGIDs.Count > 0) newsett += string.Join(",", remindSGIDs);
			if (remindUIDs.Count > 0) newsett += string.Join(",", remindUIDs);
			PluginConfig["servers"][suid.Replace("=","")] = newsett; // Todo: https://github.com/rickyah/ini-parser/issues/179
			ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
			var str = isSgid ? "ServerGroupID" : "Unique ID";
			var a_r = removed ? "Removed" : "Added";
			var f_t = removed ? "from" : "to";
			return $"{a_r} {str} [b]{item}[/b] {f_t} \"{suid}\"";
		}

		[Command("complaintreminder get", "")]
		public string CommandGetSetting(InvokerData invoker , string suid=null)
		{
			if (suid is null) suid = Ts3FullClient.WhoAmI().Value.VirtualServerUid;
			var str = new StringBuilder(Environment.NewLine);
			str.AppendLine(suid + ": " + PluginConfig["servers"][suid.Replace("=", "")]); // Todo
			str.AppendLine($"remindSGIDs: {string.Join(", ", remindSGIDs)}");
			if (!(invoker.DatabaseId is null))
			{
				var sgids = Ts3Client.GetClientServerGroups((ulong)invoker.DatabaseId).Value;
				str.AppendLine($"sgids: {string.Join(", ", sgids)}");
				str.AppendLine($"remindSGIDs.Intersect(sgids).Any(): {remindSGIDs.Intersect(sgids).Any()}");
				str.AppendLine($"Utils.ContainsAny(sgids, remindSGIDs): {Utils.ContainsAny(sgids, remindSGIDs)}");
				str.AppendLine($"remindSGIDs.Intersect(sgids).Any(): {sgids.Intersect(remindSGIDs).Any()}");
				str.AppendLine($"Utils.ContainsAny(sgids, remindSGIDs): {Utils.ContainsAny(remindSGIDs, sgids)}");
			}
			str.AppendLine($"remindUIDs: {string.Join(", ", remindUIDs)}");
			str.AppendLine($"template: {PluginConfig["templates"]["newcomplaint"]}");
			return str.ToString();
		}

		[Command("complaintreminder list", "")]
		public string CommandListComplaints()
		{
			var Result = RequestComplaintList();
			if (!Result.Ok)
			{
				return $"{PluginInfo.Name}: Could not request complaint list! ({Result.Error.Message})";
			}
			var complaints = Result.Value.Notifications.Cast<ComplainList>();
			var ret_str = new StringBuilder();
			// PluginConfig["templates"]["list title"] = "[b]{complaints}[/b] Complaints:";
			// PluginConfig["templates"]["list entry"] = "[{timestamp}] {source} ({sourcedbid}): {target} ({targetdbid}) \"{message}\"";
			ret_str.AppendLine(PluginConfig["templates"]["list title"].Replace("{complaints}",complaints.Count().ToString()));
			foreach (var complaint in complaints)
			{
				var (sourceURL, targetURL) = ClientURL(complaint);
				ret_str.AppendLine(PluginConfig["templates"]["list entry"]
					.Replace("{timestamp}", complaint.Timestamp.ToString())
					.Replace("{source}", sourceURL)
					.Replace("{sourcedbid}", complaint.FromClientDbId.ToString())
					.Replace("{target}", targetURL)
					.Replace("{targetdbid}", complaint.TargetClientDbId.ToString())
					.Replace("{message}", complaint.Message)
				);
			}
			return ret_str.ToString();
		}

		[Command("complaintreminder clear", "")]
		public string CommandResetComplaints()
		{
			var complains = ComplaintCache.Count;
			ComplaintCache.Clear();
			return $"Cleared [b]{complains}[/b] complaints from cache";
		}

		public void Tick()
		{
			RequestComplaintList();
		}

		public void Dispose()
		{
			Ts3FullClient.OnEachComplainList -= OnComplainList;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
