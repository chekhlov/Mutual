using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using ReactiveUI;
using Caliburn.Micro;
using DynamicData.Binding;
using Mutual.Helpers;
using Microsoft.Extensions.Logging;
using Mutual.Helpers.Collections;
using Mutual.Helpers.Window;
using Mutual.Model;
using Mutual.ViewModel;
using NHibernate.Util;

namespace Mutual.ViewModels
{
	public class MainFormViewModel : MainWindowScreen, ILogger, ILoggerProvider
	{
		// Анализируемые файлы
		public virtual ObservableCollection<string> Files { get; set; }
		public virtual NotifyValue<string> SelectedFile { get; set; }

		// Для отображения Лога анализа в окне
		public virtual NotifyValue<FlowDocument> Logs { get; protected set; }

		public ILogger CreateLogger(string categoryName) => this;

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
			if (formatter != null) {
				var msg = formatter(state, exception);
				if (exception != null)
					msg += " - " + exception.Message;

				var color = logLevel switch {
					LogLevel.Information => Colors.Black,
					LogLevel.Error => Colors.Red,
					LogLevel.Critical => Colors.Crimson,
					LogLevel.Warning => Colors.Blue,
					LogLevel.Debug => Colors.Green,
					LogLevel.Trace => Colors.Magenta,
					_ => Colors.Yellow
				};

				Application.Current.Dispatcher.BeginInvoke(new System.Action(() => {
					if (!Logs.HasValue)
						Logs.Value = new FlowDocument();

					var paragraph = new Paragraph(new Run(msg)) {
						Margin = new Thickness(0, 0, 0, 0),
						Foreground = new SolidColorBrush(color),
					};

					Logs.Value.Blocks.Add(paragraph);
					Logs.Refresh();
				}));
			}
		}

		public MainFormViewModel()
		{
			Logger.Factory.AddProvider(this);
			DisplayName = Global.Constaint.ProgramName;
		}

		public async Task Exit()
		{
			await TryCloseAsync();
		}

		public async Task About()
		{
			var dlg = new AboutViewModel();
			await Show(dlg);
		}

		public async Task Settings()
		{
			var dlg = new SettingViewModel();
			await Show(dlg, true);
			if (!dlg.WasCanceled)
				WindowManager.Warning("Для применения настроек, необходимо перезагрузить программу!");
		}

		protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
		{
			Global.MainWindow = this;
		}

		public void AddFile(string value)
		{
			if (String.IsNullOrEmpty(value)) return;

			if (!Files.Any(x => x == value)) {
				Files.Add(value);
				Logger.Log.LogInformation(Directory.Exists(value) ? $"Добавлен путь {value}" : $"Добавлен файл {value}");
			} else
				Logger.Log.LogWarning($"Выбранный путь к файлу (каталогу) уже существует в списке {value}");
		}

		public void ClearAll()
		{
			if (!Files.Any()) return;
			Files.Clear();
			Logger.Log.LogInformation($"Список файлов (каталогов) очищен");
		}

		public void RemoveFile()
		{
			if (!SelectedFile.HasValue) return;

			var path = SelectedFile.Value;
			Files.Remove(path);
			Logger.Log.LogInformation($"Удален путь {path}");
		}

		public async Task Analyze()
		{
			using (TryLockUI(600)) {
				Logger.Log.LogInformation("Запуск анализ файлов");

				var paths = Files.ToList();
				var analyze = new Analyze();
				await Task.Run(async () => await analyze.Run(paths, CancellationToken.Token));
			}
		}
		
		public void StopAnalyze()
		{
			if (CancellationToken.IsCancellationRequested)
				return;
			
			CancellationToken.Cancel();
			Logger.Log.LogInformation("Анализ файлов остановлен пользователем");
		}
	}
}