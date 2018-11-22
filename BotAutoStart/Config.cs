using System;
using System.Collections.Generic;
using System.Text;

namespace BotAutoStart
{
	public class Configuration
	{
		public bool EnableDebug { get; set; }
		public Server Server { get; set; }
		public Client Client { get; set; }
	}

	public class Server
	{
		public TimeSpan Timeout { get; set; }
	}

	public class Client
	{
		public string ServerAddress { get; set; }
	}
}
