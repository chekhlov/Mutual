using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Caliburn.Micro;
using Microsoft.Extensions.Logging;

namespace Mutual.Helpers.Window
{
	public class NotifyValueSupport
	{
		public static void Register()
		{
			ViewModelBinder.BindProperties = BindProperties;
			var defaultSetBinding = ConventionManager.SetBinding;

			//ConventionManager.ApplyItemTemplate - Будет пытаться установить
			//шаблон для NotifyValue<List<string>> List
			//тк будет думать что NotifyValue биндинг будет производиться
			//к Producers а на самом деле биндинг будет к List.Value
			//это сделает ConventionManager.Set
			ConventionManager.AddElementConvention<Selector>(Selector.ItemsSourceProperty,
				"SelectedItem",
				"SelectionChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) => {
					Patch(ref path, ref property);
					var ignore = ConventionManager.SetBindingWithoutBindingOrValueOverwrite(viewModelType,
						path,
						property,
						element,
						convention,
						ItemsControl.ItemsSourceProperty);

					if (!ignore)
						return false;

					ConventionManager.ConfigureSelectedItem(element, Selector.SelectedItemProperty, viewModelType, path);
					if (IsArrayOfPrimitive(property.PropertyType))
						return true;

					ConventionManager.ApplyItemTemplate((ItemsControl)element, property);
					return true;
				};

			ConventionManager.ConfigureSelectedItem =
				(selector, selectedItemProperty, viewModelType, path) => {
					if (ConventionManager.HasBinding(selector, selectedItemProperty)) {
						return;
					}

					var baseName = path;
					if (path.EndsWith(".Value")) {
						var index = path.LastIndexOf('.');
						baseName = path.Substring(0, index);
					}

					foreach (var potentialName in ConventionManager.DerivePotentialSelectionNames(baseName)) {
						var propertyInfo = viewModelType.GetPropertyCaseInsensitive(potentialName);
						if (propertyInfo != null) {
							var selectionPath = potentialName;
							if (IsNotifyValue(propertyInfo))
								selectionPath += ".Value";

							var binding = new Binding(selectionPath) { Mode = BindingMode.TwoWay };
							var shouldApplyBinding = ConventionManager.ConfigureSelectedItemBinding(selector,
								selectedItemProperty,
								viewModelType,
								selectionPath,
								binding);

							if (shouldApplyBinding) {
								BindingOperations.SetBinding(selector, selectedItemProperty, binding);
								return;
							}
						}
					}
				};

			ConventionManager.SetBinding =
				(viewModelType, path, property, element, convention, bindableProperty) => {
					Patch(ref path, ref property);
					defaultSetBinding(viewModelType, path, property, element, convention, bindableProperty);
				};

			var basePrepareContext = ActionMessage.PrepareContext;
			ActionMessage.PrepareContext = context => {
				try {
					ActionMessage.SetMethodBinding(context);
					if (context.Target == null || context.Method == null)
						return;

					var guardName = "Can" + context.Method.Name;
					var targetType = context.Target.GetType();

					var guard = targetType.GetProperty(guardName);
					if (guard == null || !IsNotifyValue(guard)) {
						basePrepareContext(context);
						return;
					}

					var inpc = guard.GetValue(context.Target, null) as INotifyPropertyChanged;
					if (inpc == null)
						return;

					PropertyChangedEventHandler handler = null;
					handler = (s, e) => {
						if (context.Message == null) {
							inpc.PropertyChanged -= handler;
							return;
						}
						context.Message.UpdateAvailability();
					};

					inpc.PropertyChanged += handler;
					context.Disposing += delegate { inpc.PropertyChanged -= handler; };
					context.Message.Detaching += delegate { inpc.PropertyChanged -= handler; };

					context.CanExecute = () => (NotifyValue<bool>)guard.GetValue(context.Target, null);
				}
				catch (Exception e) {
					throw new Exception($"Не удалось выполнить PrepareContext для {context.Target} {context.Method}", e);
				}
			};
		}

		public static IEnumerable<FrameworkElement> BindProperties(IEnumerable<FrameworkElement> namedElements, Type viewModelType)
		{
			var unmatchedElements = new List<FrameworkElement>();
			foreach (var element in namedElements) {
				var bindPath = Patch(viewModelType, element.Name, out var property, out var interpretedViewModelType);

				if (property == null) {
					unmatchedElements.Add(element);
					Logger.Log.LogDebug($"Binding Convention Not Applied: Element {element.Name} did not match a property.");
					continue;
				}

				var convention = ConventionManager.GetElementConvention(element.GetType());
				if (convention == null) {
					unmatchedElements.Add(element);
					Logger.Log.LogDebug($"Binding Convention Not Applied: No conventions configured for {element.GetType()}.");
					continue;
				}

				var applied = convention.ApplyBinding(
					interpretedViewModelType,
					bindPath,
					property,
					element,
					convention);

				if (applied) {
					Logger.Log.LogDebug("Binding Convention Applied: Element {0}.", element.Name);
				}
				else {
					Logger.Log.LogDebug("Binding Convention Not Applied: Element {0} has existing binding.", element.Name);
					unmatchedElements.Add(element);
				}
			}

			return unmatchedElements;
		}

		public static string Patch(Type viewModelType, string name, out PropertyInfo property, out Type interpretedViewModelType)
		{
			var cleanName = name.Trim('_');
			var parts = cleanName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries).ToList();

			property = viewModelType.GetPropertyCaseInsensitive(parts[0]);
			interpretedViewModelType = viewModelType;

			for (int i = 1; i < parts.Count && property != null; i++) {
				interpretedViewModelType = property.PropertyType;
				property = interpretedViewModelType.GetPropertyCaseInsensitive(parts[i]);
				if (property == null && IsNotifyValue(interpretedViewModelType)) {
					property = interpretedViewModelType.GetProperty("Value");
					parts.Insert(i, "Value");
				}
			}
			return String.Join(".", parts);
		}

		public static bool IsArrayOfPrimitive(Type posibleArrayOrList)
		{
			if (posibleArrayOrList.IsArray) {
				var elementType = posibleArrayOrList.GetElementType();
				return elementType == typeof(string) || elementType.IsPrimitive;
			}

			return false;
		}

		public static bool IsNotifyValue(PropertyInfo property)
		{
			var type = property.PropertyType;
			return IsNotifyValue(type);
		}

		private static bool IsNotifyValue(Type type)
		{
			return type.IsGenericType
				&& type.GetGenericTypeDefinition() == typeof(NotifyValue<>);
		}

		public static void Patch(ref string path, ref PropertyInfo property)
		{
			if (IsNotifyValue(property)) {
				path += ".Value";
				property = property.PropertyType.GetProperty("Value");
			}
		}
	}
}