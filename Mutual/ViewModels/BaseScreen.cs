using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using NHibernate;
using Mutual.Helpers;
using Mutual.Helpers.NHibernate;
using Mutual.Model;
using Mutual.ViewModels;
using Mutual.Views;
using WindowManager = Mutual.Helpers.Window.WindowManager;

namespace Mutual.ViewModel
{
	public class BaseScreen : Screen, IDisposable
	{
		private static WindowManager _windowManager;
		public virtual WindowManager WindowManager => _windowManager ?? (_windowManager = new WindowManager());

		// соединения с БД
		public virtual BaseConnection Connection => Global.Connection;
		protected Task<T> Session<T>(Func<ISession, T> action, CancellationToken token = default) => Connection.Session<T>(action, token == default ? CancellationToken.Token : token);

		protected Task Session(Action<ISession> action, CancellationToken token = default) => Connection.Session(action, token == default ? CancellationToken.Token : token);

		// Признак асинхронной операции
		public NotifyValue<bool> IsAsyncOperationInProgress { get; set; }

		// Признак закрытия формы
		public virtual bool WasCanceled { get; set; } = true;

		// Используется завершения асинхронных операций, например при закрытии формы, мы должны 
		protected CancellationTokenSource _cancellationToken = new CancellationTokenSource();
		public virtual CancellationTokenSource CancellationToken => _cancellationToken;

		/// <summary>
		/// Освобождает ресурсы при закрытии формы
		/// </summary>
		public CompositeDisposable OnCloseDisposable { get; } = new CompositeDisposable();

		protected static void InitFields(BaseScreen screen)
		{
			// Инициализируем property NotifyValue формы через рефлексию.
			var notifiable = screen.GetType()
				.GetProperties()
				.Where(x => x.PropertyType.IsGenericType
					&& (typeof(NotifyValue<>).IsAssignableFrom(x.PropertyType.GetGenericTypeDefinition())
						|| typeof(ObservableCollection<>).IsAssignableFrom(x.PropertyType.GetGenericTypeDefinition())
						|| typeof(List<>).IsAssignableFrom(x.PropertyType.GetGenericTypeDefinition()))
					&& x.CanWrite);

			foreach (var propertyInfo in notifiable)
				if (propertyInfo.GetValue(screen, null) == null)
					propertyInfo.SetValue(screen, Activator.CreateInstance(propertyInfo.PropertyType), null);
		}

		public BaseScreen()
		{
			InitFields(this);

			// Подвязываемся на событие изменения текущего пользователя,
			// и сохраняем IDisposable для отписки при закрытии формы, что бы не допустить утечки памяти 
		}

		/// <summary>
		/// Предназначения для использования внутри блока - using (var _ = TryLockUI2()) {};
		/// для блокирования интерфейса - не желательно использовать в виде формы using var _ = TryLockUI2();
		/// иначе TryClose() вызывающий CanClose не закроет форму из-за IsAsyncOperationInProgress
		/// </summary>
		protected IDisposable TryLockUI(uint waitSeconds = 30)
		{
			if (IsAsyncOperationInProgress)
				throw new CustomException("Выполняется операция дождитесь ее завершения.");

			_cancellationToken?.Cancel();
			_cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(waitSeconds));

			IsAsyncOperationInProgress.Value = true;
			return Disposable.Create(() => IsAsyncOperationInProgress.Value = false);
		}

		public virtual void Dispose()
		{
			if (OnCloseDisposable.IsDisposed)
				return;

			// Освобождаем используемые ресурсы не принадлежащие форме
			OnCloseDisposable.Dispose();
		}

		protected override void OnViewLoaded(object view)
		{
			var task = OnLoadedAsync(System.Threading.CancellationToken.None);
			task.ContinueWith(async x => {
				IsAsyncOperationInProgress.Value = false;
				if (x.IsFaulted) {
					Logger.Log.LogError("Загрузка формы завершилась ошибкой", x.Exception);
					WindowManager.Error(x.Exception.Message);
					try {
						await TryCloseAsync();
					} catch (Exception e) {
						Logger.Log.LogError("Ошибка при авварийном закрытии формы", e);
					}
				}
			}, Global.UiScheduler);
		}
		
		protected virtual async Task OnLoadedAsync(CancellationToken cancellationToken)
		{
		}
		
		
		public override async Task TryCloseAsync(bool? dialogResult = null)
		{
			await base.TryCloseAsync(dialogResult);
			_cancellationToken?.Cancel();
			OnCloseDisposable.Dispose();
			Dispose();
		}

		public override Task<bool> CanCloseAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(!IsAsyncOperationInProgress.Value || !WasCanceled);
		}

		public virtual async Task Show(BaseScreen content, bool dialog = false)
		{
			if (!dialog && Global.MainWindow is MainWindowScreen model) {
				model.ShowContent(content);
			} else {
				await WindowManager.ShowDialogAsync(content);
			}
		}
	}
}