using System;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Console = Microsoft.Extensions.Logging.Console;
using Debug = Microsoft.Extensions.Logging.Debug;

namespace Mutual.Helpers
{
	/// <summary>
	/// Класс ведения журнала лога в программе
	/// </summary>
	public partial class Logger
	{
		private static ILoggerFactory factory = null;
		private static ILogger logger = null;

		public static ILogger Log
		{
			get
			{
				if (logger == null) 
					logger = Factory.CreateLogger<Application>();

				return logger;
			}
		}

		public static ILoggerFactory Factory
		{
			get
			{
				if (factory == null)
					Init();

				return factory;
			}
		}

		public static void Init()
		{
			factory = LoggerFactory.Create(x => x.SetMinimumLevel(LogLevel.None));
		}

		public static void Init(Action<ILoggingBuilder> action)
		{
			factory = LoggerFactory.Create(x => action(x));
		}
	}
}