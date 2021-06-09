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
	public partial class Logger
	{
		public class FileLogger : ILogger
		{
			private string filePath;
			private object _lock = new object();

			public FileLogger(string path)
			{
				filePath = path;
			}

			public IDisposable BeginScope<TState>(TState state)
			{
				return null;
			}

			public bool IsEnabled(LogLevel logLevel)
			{
				return true;
			}

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
				Func<TState, Exception, string> formatter)
			{
				// formatter - FormattedLogValues class - аналог string.Format
				if (IsEnabled(logLevel) && formatter != null)
				{
					lock (_lock)
					{
						var path = Path.GetDirectoryName(filePath);

						if (!String.IsNullOrEmpty(path) && !Directory.Exists(path))
							Directory.CreateDirectory(path);

						var msg = exception != null 
							? $"Intercepted exception {exception.GetType().FullName} {state.ToString()} - {exception.Message}{Environment.NewLine}{exception.StackTrace}"
							: state.ToString();

						File.AppendAllText(filePath, $"{DateTime.Now} {logLevel.ToString()} {msg}{Environment.NewLine}");
					}
				}
			}
		}

		public class FileLoggerProvider : ILoggerProvider
		{
			private string path;

			public FileLoggerProvider(string _path, string appName)
			{
				path = GenerateLogFilename(_path, appName);
			}

			public ILogger CreateLogger(string categoryName) => new FileLogger(path);

			public void Dispose()
			{
			}

			private static string GenerateLogFilename(string maskFile, string appName)
			{
				var now = DateTime.Now;
				maskFile = maskFile.Replace("%APP%", "{0}");
				maskFile = maskFile.Replace("%DD%", "{1:00}");
				maskFile = maskFile.Replace("%MM%", "{2:00}");
				maskFile = maskFile.Replace("%YYYY%", "{3:0000}");
				maskFile = maskFile.Replace("%YY%", "{4:00}");

				var fileName = String.Format(maskFile, appName, now.Day, now.Month, now.Year, (now.Year % 100));

				return fileName;
			}
		}
	}

	public static class FileLoggerExtensions
	{
		/// <summary>
		/// Добавляет логгирование в файл
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="filePath">Путь для куда будут писаться логи</param>
		/// <param name="appName">Имя приложения для определения маски %APP%</param>
		/// <returns></returns>
		public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePath, string appName)
		{
			builder.AddProvider(new Logger.FileLoggerProvider(filePath, appName));
			return builder;
		}
	}
}