using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ReactiveUI;
using Caliburn.Micro;
using DynamicData.Binding;
using Mutual.Helpers;
using Microsoft.Extensions.Logging;
using Mutual.Helpers.Collections;
using Mutual.Helpers.Window;
using Mutual.Model;
using Mutual.ViewModel;

namespace Mutual.ViewModels
{
	public class MainWindowScreen : BaseScreen
	{
		// коллекция открытых окн (контента) на форме, нужна для возврата при закрытии окна к предыдущему окну
		private ObservableStack<BaseScreen> _contentViewStack = new ObservableStack<BaseScreen>();
		public NotifyValue<BaseScreen> ContentView { get; set; }

		public MainWindowScreen()
		{
			_contentViewStack.Subscribe(x => {
				BaseScreen screen = null;
				if (x.Action == NotifyCollectionChangedAction.Add && x.NewItems.Count > 0) {
					screen = (BaseScreen) x.NewItems[0];
					screen?.OnCloseDisposable.Add(Disposable.Create(() => _contentViewStack.Pop()));
				}

				if (x.Action == NotifyCollectionChangedAction.Remove && _contentViewStack.Count > 0) {
					screen = _contentViewStack.Peek();
				}

				ContentView.Value = screen;
			});
		}

		public virtual void ShowContent(BaseScreen content)
		{
			_contentViewStack.Push(content);
		}
		protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
		{
			await base.OnInitializeAsync(cancellationToken);
			Global.MainWindow = this;
		}

		public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken)
		{
			return !IsAsyncOperationInProgress.Value && WindowManager.Question("Вы хотите выйти из программы?", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
		}
	}
}