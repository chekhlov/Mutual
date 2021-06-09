using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Caliburn.Micro;
using Microsoft.Extensions.Logging;
using Mutual.Converters;
using System.Windows.Controls.Primitives;
using NHibernate.Util;

namespace Mutual.Helpers.Window
{
	// Расширение Caliburn
	public static class Caliburn
	{
		// private static bool NotBindedAndNull(FrameworkElement element, DependencyProperty property)
		// {
		// 	return !ConventionManager.HasBinding(element, property)
		// 		&& element.GetValue(property) == null;
		// }

		public static void Init()
		{
			//нужно затем что бы можно было делать модели без суффикса ViewModel
			//достаточно что бы они лежали в пространстве имен ViewModels
			ViewLocator.NameTransformer.AddRule(
				@"(?<nsbefore>([A-Za-z_]\w*\.)*)(?<subns>ViewModels\.)"
				+ @"(?<nsafter>([A-Za-z_]\w*\.)*)(?<basename>[A-Za-z_]\w*)"
				+ @"(?!<suffix>ViewModel)$",
				"${nsbefore}Views.${nsafter}${basename}View");

			//что бы не нужно было использовать суффиксы View и ViewModel
			ViewLocator.NameTransformer.AddRule(
				@"(?<nsbefore>([A-Za-z_]\w*\.)*)(?<subns>ViewModels\.)"
				+ @"(?<nsafter>([A-Za-z_]\w*\.)*)(?<basename>[A-Za-z_]\w*)"
				+ @"(?!<suffix>)$",
				"${nsbefore}Views.${nsafter}${basename}");
			ViewLocator.NameTransformer.AddRule(
				@"(?<name>.+)",
				"${name}View");

			//безумие - сам по себе Caliburn если не найден view покажет текст Cannot find view for
			//ни исключения ни ошибки в лог
			ViewLocator.LocateForModelType = (modelType, displayLocation, context) => {
				var viewType = ViewLocator.LocateTypeForModelType(modelType, displayLocation, context);
				if (viewType == null) {
					Logger.Log.LogError($"Не удалось найти вид для отображения {modelType}");
				}
				try { 
				return viewType == null
					? new TextBlock()
					: ViewLocator.GetOrCreateViewType(viewType);
				}
				catch(Exception e) {
					Logger.Log.LogError($"Ошибка инициализации формы {modelType}");
					do {
						Logger.Log.LogError(e.Message);
						e = e.InnerException;
					} while (e != null);
					return new TextBlock() { Text = $"Ошибка инициализации формы {modelType}" };
				}
			};

			NotifyValueSupport.Register();

			// Добавляем привязку к PasswordBox
			ConventionManager.AddElementConvention<PasswordBox>(ContentElementBinder.PasswordProperty, "Password", "PasswordChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) => {
				//по умолчанию для PasswordBox установлен шрифт times new roman
				//высота поля ввода вычисляется на основе шрифта
				//если расположить TextBox и PasswordBox на одном уровне то разница в высоте будет заметна
				//правим эту кривизну
				((PasswordBox) element).FontFamily = SystemFonts.MessageFontFamily;
				return ConventionManager.SetBindingWithoutBindingOverwrite(viewModelType, path, property, element, convention, convention.GetBindableProperty(element));
			};

			ConventionManager.AddElementConvention<ComboBox>(ItemsControl.ItemsSourceProperty, "SelectedItem", "SelectionChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) => {
				NotifyValueSupport.Patch(ref path, ref property);
				if (property.PropertyType.IsEnum) {
					if (ContentElementBinder.NotBindedAndNull(element, ItemsControl.ItemsSourceProperty)
						&& !ConventionManager.HasBinding(element, Selector.SelectedItemProperty)) {
						var items = Helpers.GetDescriptions(property.PropertyType);
						element.SetValue(ItemsControl.DisplayMemberPathProperty, "Name");
						element.SetValue(ItemsControl.ItemsSourceProperty, items);

						var binding = new Binding(path);
						binding.Converter = new ComboBoxSelectedItemConverter();
						binding.ConverterParameter = items;
						BindingOperations.SetBinding(element, Selector.SelectedItemProperty, binding);
					}
				} else {
					var fallback = ConventionManager.GetElementConvention(typeof(Selector));
					if (fallback != null) {
						return fallback.ApplyBinding(viewModelType, path, property, element, fallback);
					}
				}

				return true;
			};

			var defaultBind = ViewModelBinder.Bind;
			ViewModelBinder.Bind = (viewModel, view, context) => {
				if ((bool) view.GetValue(ViewModelBinder.ConventionsAppliedProperty)) {
					return;
				}

				defaultBind(viewModel, view, context);
				ContentElementBinder.Bind(viewModel, view, context);
			};
		}
	}
}