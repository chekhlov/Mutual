using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Caliburn.Micro;
using Mutual.Helpers;
using Mutual.Helpers.Utilities;
using Microsoft.Extensions.Logging;
using Mutual.Model;
using Mutual.Model.Config;
using Mutual.ViewModels;
using Application = System.Windows.Application;

namespace Mutual
{
	public class DelegateTraceListner : TraceListener
	{
		public override void TraceEvent(TraceEventCache eventCache, string source,
			TraceEventType eventType, int id, string format, params object[] args)
		{
			Logger.Log.LogInformation(format, args);
		}

		public override void Write(string message)
		{
		}

		public override void WriteLine(string message)
		{
			Logger.Log.LogError(message);
		}
	}


	public class AppBootstrapper : BootstrapperBase
	{
		public static SynchronizationContext UIContext { get; protected set; }
		
		public AppBootstrapper()
		{
			Initialize();
		}

		protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			Logger.Log.LogError("Необработанная ошибка в приложении", e.Exception);
		}

		protected override void OnStartup(object sender, StartupEventArgs e)
		{
			UIContext = SynchronizationContext.Current;
			
			PresentationTraceSources.Refresh();
			PresentationTraceSources.SetTraceLevel(sender, PresentationTraceLevel.High);
			PresentationTraceSources.DataBindingSource.Listeners.Add(new DelegateTraceListner());

			Global.Config = Config.LoadXml($"{Global.AppName}.config");

			Logger.Init(x => {
				var config = Global.Config;
				if (config.Logging.Logger.HasFlag(LogOutput.Debug))
					x.AddDebug();

				if (config.Logging.Logger.HasFlag(LogOutput.Console))
					x.AddConsole();

				if (config.Logging.Logger.HasFlag(LogOutput.File)) {
					var path = System.IO.Path.Combine(config.Logging.Path ?? String.Empty, config.Logging.MaskFileName ?? String.Empty);
					x.AddFile(path, Global.AppName);
				}
				x.SetMinimumLevel(config.Logging.LogLevel);
			});

			Application.DispatcherUnhandledException += (o, args) => {
				Logger.Log.LogError($"Перехвачено исключение {args.Exception?.ToString()}");
			};

			Logger.Log.LogInformation($"Thread: {Thread.CurrentThread.ManagedThreadId}");

        	Logger.Log.LogInformation("---------------------------------------------------------------------------------------------------");
			Logger.Log.LogInformation(Global.Constaint.BuildCopyright);
			Logger.Log.LogInformation("---------------------------------------------------------------------------------------------------");
			Logger.Log.LogInformation($"Program start at {DateTime.Now} with '{Global.Config.ConfigFileName}' configuration file");

			// Инициализируем расширение Caliburn
			Mutual.Helpers.Window.Caliburn.Init();
			DisplayRootViewFor<MainFormViewModel>();
		}

		protected override void OnExit(object sender, EventArgs e)
		{
			Logger.Log.LogInformation($"Terminate program at {DateTime.Now}");
			Logger.Log.LogInformation("---------------------------------------------------------------------------------------------------");
		}
	}
}