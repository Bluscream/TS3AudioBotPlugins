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

namespace NoIdentity
{
	public class NoIdentity : IBotPlugin
	{

		private List<ChannelList> channelList = new List<ChannelList>();

		public Ts3FullClient Ts3FullClient { get; set; }

		public Ts3Client Ts3Client { get; set; }

		public ConfBot Conf { get; set; }

		public void Initialize()
		{
			Ts3FullClient.OnChannelListFinished += Ts3Client_OnChannelListFinished;
		}

		private void Ts3Client_OnChannelListFinished(object sender, IEnumerable<ChannelListFinished> e)
		{
			Conf.GetParent().
			Conf.SaveWhenExists();
		}

		public void Dispose()
		{
			Ts3FullClient.OnChannelListFinished -= Ts3Client_OnChannelListFinished;
		}
	}
}
