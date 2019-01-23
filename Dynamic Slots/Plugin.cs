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
using TS3Client.Commands;

using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using ClientUidT = System.String;
using TS3Client.Messages;
using System.Text;
using TS3AudioBot.Sessions;

namespace Dynamic_Slots
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

	public class Dynamic_Slots : IBotPlugin
	{
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfRoot ConfRoot { get; set; }

		private readonly bool PluginEnabled = true;
		private bool Initialized = false;

		private readonly List<int> steps = new List<int>() { 32, 64, 128, 256, 512, 1024 }; // TODO: Config
		private int currentStep = 0;
		private const int stepTriggerUp = 0;
		private const int stepTriggerDown = 4;

		public void Initialize()
		{
			TS3FullClient.OnEachServerEdited += OnEachServerEdited;
			TS3FullClient.OnEachServerUpdated += OnEachServerUpdated;
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			TS3FullClient.OnEachClientLeftView += OnEachClientLeftView;
			TS3FullClient.OnEachChannelListFinished += OnEachChannelListFinished;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnEachChannelListFinished(object sender, ChannelListFinished e)
		{
			if (Initialized) return;
			Initialized = true;
			ServerGetVariables();
		}

		private void OnEachServerUpdated(object sender, ServerUpdated server)
		{
			if (!Initialized) return;

			var realSlots = server.MaxClients - server.ReservedSlots;
			var realClients = server.ClientsOnline;
			// var realClients = server.ClientsOnline - server.QueriesOnline;

			// t = stepTriggerRange
			// Case A
			// (cur: 30) | (max: 64)
			// cur > step[current] - t => step up

			// Case B
			// cur: 65 | max 128
			// cur: 64
			// cur: 62 | max 64
			// cur < step[current - 1] + 2

			var lastStep = currentStep;
			if (realClients >= steps[currentStep] - stepTriggerUp)
			{
				while (realClients >= steps[currentStep] - stepTriggerUp && currentStep < steps.Count - 1)
					++currentStep;
			}
			else if (currentStep > 0 && realClients <= steps[currentStep - 1] - stepTriggerDown)
			{
				while (currentStep > 0 && realClients <= steps[currentStep - 1] - stepTriggerDown)
					--currentStep;
			}

			if (lastStep != currentStep)
			{
				EditMaxClients(steps[currentStep]);
			}
		}

		private void ServerGetVariables()
		{
			var Ok = TS3FullClient.Send<ResponseVoid>("servergetvariables", new List<ICommandPart>() { }).Ok;
		}

		private void OnEachServerEdited(object sender, ServerEdited server)
		{
			if (server.InvokerId == TS3FullClient.ClientId) return;
			ServerGetVariables();
		}

		private void OnEachClientEnterView(object sender, ClientEnterView client)
		{
			if (!Initialized) return;
			ServerGetVariables();
		}

		private void OnEachClientLeftView(object sender, ClientLeftView client)
		{
			if (!Initialized) return;
			ServerGetVariables();
		}

		private bool EditMaxClients(int maxClients)
		{ // serveredit [sid=6] virtualserver_maxclients=63
			var command = new Ts3Command("serveredit", new List<ICommandPart>() {
					new CommandParameter("virtualserver_maxclients", maxClients)
			});
			var Result = TS3FullClient.SendNotifyCommand(command, NotificationType.ServerEdited);
			return Result.Ok;
		} // c===3

		[Command("dynamicslots", "")]
		public string CommandListWaiting()
		{
			var sb = new StringBuilder(PluginInfo.Name);
			sb.AppendLine();
			// sb.AppendLine($"Clients: {{CurrentVisibleUsers}} ({CurrentUsersQueries}) / {CurrentSlots}");
			sb.AppendLine($"PluginEnabled: {PluginEnabled}");
			// sb.AppendLine($"CheckNext: {CheckNext}");
			// sb.AppendLine($"InitializedVisible: {InitializedVisible}");
			// sb.AppendLine($"clientCache ({clientCache.Count}): {string.Join(", ", clientCache)}");
			// sb.AppendLine($"queryCache ({queryCache.Count}): {string.Join(", ", queryCache)}");
			sb.AppendLine($"steps: {string.Join(", ", steps)}");
			return sb.ToString();
		}

		public void Dispose()
		{
			TS3FullClient.OnEachChannelListFinished -= OnEachChannelListFinished;
			TS3FullClient.OnEachClientLeftView -= OnEachClientLeftView;
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			TS3FullClient.OnEachServerUpdated -= OnEachServerUpdated;
			TS3FullClient.OnEachServerEdited -= OnEachServerEdited;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
