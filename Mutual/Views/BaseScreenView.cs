using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Mutual.Controls;
using ReactiveUI;
using Mutual.Helpers;
using Mutual.Helpers.Window;
using Mutual.Model;
using Mutual.ViewModel;
using Mutual.ViewModels;
using WpfAnimatedGif;
using WindowManager = Caliburn.Micro.WindowManager;

namespace Mutual.Views
{
	public class BaseScreenView : UserControl
	{
		// Для совместимости с Window - мы используем UserControl
		public string Title { get; set; }
		public WindowStartupLocation WindowStartupLocation { get; set; } = WindowStartupLocation.CenterScreen;
		public ResizeMode ResizeMode { get; set; } = ResizeMode.CanResize;
		public WindowStyle WindowStyle { get; set; } = WindowStyle.SingleBorderWindow;
		public WindowState WindowState { get; set; } = WindowState.Normal;

		// Список элементов которые становятся неактивными при асинхронных операциях
		public List<FrameworkElement> elem;
		// Статичная анимированная картинка ожидания 
		private static BitmapImage loadingImg;

		// картинка с анимацией на форме
		public Image loadingImage;
		
		public BaseScreenView()
		{
			this.Initialized += (sender, args) => {

				// заменяем на форме все элементы
				var content = (UIElement) this.Content;
				
				// на Grid с двумя строками - в 1й строке выводится существующий контент, а во 2й строке добавляется скрытая картинка
				// ожидания загрузки данных
				var grid = new Grid();
				this.Content = grid;
				grid.Children.Add(content);
		
				loadingImage = new Image();
				loadingImage.Width = 64;
				loadingImage.Height = 64;

				// если мы ранее не загружали из ресурсов картинку, загружаем
				if (loadingImg == null) {
					var uri = new Uri("pack://application:,,,../Asserts/loading.gif");
					loadingImg = new BitmapImage(uri);
				}
				
				// Указываем, что картинка анимированная
				ImageBehavior.SetAnimatedSource(loadingImage, loadingImg);
				loadingImage.Visibility = Visibility.Hidden;
				grid.Children.Add(loadingImage);
				
				// Выбираем первый элемент который может обладать фокусом и устанавливаем фокус
				var firstFocusable = this.GetChildOfType<UIElement>().Where(x => x.Focusable).FirstOrDefault();
				firstFocusable?.Focus();
			};

			KeyDown += (sender, args) => {
				if (args.Key == Key.Enter) {
					var next = new TraversalRequest(FocusNavigationDirection.Next);
					var senderOrigin = args.OriginalSource;

					if (senderOrigin is TextBox textBox)
						textBox.MoveFocus(next);
					if (senderOrigin is PasswordBox passwordBox)
						passwordBox.MoveFocus(next);
					if (senderOrigin is ComboBox comboBox && !comboBox.IsDropDownOpen)
						comboBox.MoveFocus(next);
				}
			};
			
			
			DataContextChanged += (sender, args) => {
				if (args.NewValue is BaseScreen model) {
					if (string.IsNullOrEmpty(Title))
						Title = model.DisplayName;
					else
						model.DisplayName = Title;
					
					model.IsAsyncOperationInProgress.Subscribe(x => {
						if (x) {
							elem = this.GetChildOfType<FrameworkElement>()
								.Where(x => x.IsEnabled 
									&& x.Visibility == Visibility.Visible 
									&& ( x is Button
										|| x is TextBox
										|| x is CheckBox
										|| x is ComboBox
										|| x is ListBox
										|| x is PasswordBox
										|| x is RadioButton
										|| x is MenuItem
										|| x is RichTextBox
										|| x is BindableRichTextBox
										|| x is RadioButton
									))
								.ToList();
						}

						elem?.ToList().ForEach(b => {
							b.IsEnabled = !x;
						});
						loadingImage.Visibility = x ? Visibility.Visible : Visibility.Hidden;
						if (!x) elem = null;
					});
				}

				// Если это форма (Window) то устанавливаем параметры окна
				if (this.Parent is Window window) {
					window.Title = Title;
					window.ResizeMode = ResizeMode;
					window.Width = Width;
					window.Height = Height;
					window.MinWidth = MinWidth;
					window.MinHeight = MinHeight;
					window.MaxWidth = MaxWidth;
					window.MaxHeight = MaxHeight;
					window.WindowStyle = WindowStyle;
					window.WindowState = WindowState;
					window.WindowStartupLocation = WindowStartupLocation;
					window.SizeChanged += (o, eventArgs) => {
						var content = (BaseScreenView) window.Content;
						if (eventArgs.NewSize.Width != 0 && eventArgs.PreviousSize.Width != 0)
							content.Width += eventArgs.NewSize.Width - eventArgs.PreviousSize.Width;
						
						if (eventArgs.NewSize.Height != 0 && eventArgs.PreviousSize.Height != 0)
							content.Height += eventArgs.NewSize.Height - eventArgs.PreviousSize.Height;
					};
				}
			};
		}
	}
}