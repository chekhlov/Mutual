using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;

namespace Mutual.Helpers.Window
{
	public class WindowManager : global::Caliburn.Micro.WindowManager
	{
		public virtual MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon)
		{
			var window = InferOwnerOf(null);
			var res = window == null
				? MessageBox.Show(text, caption, buttons, icon)
				: MessageBox.Show(window, text, caption, buttons, icon);
			return res;
		}

		public void Warning(string text)
		{
			ShowMessageBox(text, "Внимание",
				MessageBoxButton.OK,
				MessageBoxImage.Warning);
		}

		public void Notify(string text)
		{
			ShowMessageBox(text, "Информация",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		public void Error(string text)
		{
			ShowMessageBox(text, "Ошибка",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
		}

		public MessageBoxResult Question(string text, MessageBoxButton button = MessageBoxButton.YesNo)
		{
			return ShowMessageBox(text, "Внимание",
				button,MessageBoxImage.Warning);
		}

		public override async Task<bool?> ShowDialogAsync(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
			try {
				return await base.ShowDialogAsync(rootModel, context, settings);

			} catch (Exception e) {
				if (rootModel is Screen model)
					await model.TryCloseAsync();

				throw new CustomException(e.Message, e.InnerException);
			}
		}


		public override async Task ShowPopupAsync(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
			try {
				await base.ShowPopupAsync(rootModel, context, settings).ConfigureAwait(true);
			} catch (Exception e) {
				if (rootModel is Screen model) 
					await model.TryCloseAsync();

				throw new CustomException(e.Message, e.InnerException);
			}
		}

		public override async Task ShowWindowAsync(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
			try {
				await base.ShowWindowAsync(rootModel, context, settings).ConfigureAwait(true);
			} catch (Exception e) {
				if (rootModel is Screen model)
					await model.TryCloseAsync();
				throw new CustomException(e.Message, e.InnerException);
			}
		}
	}
}
