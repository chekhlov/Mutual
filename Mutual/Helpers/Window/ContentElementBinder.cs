using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Caliburn.Micro;

namespace Mutual.Helpers.Window
{
	public class ContentElementBinder
	{
		public static bool NotBindedAndNull(FrameworkElement element, DependencyProperty property)
		{
			return !ConventionManager.HasBinding(element, property)
				&& element.GetValue(property) == null;
		}
		
		public static readonly DependencyProperty PasswordProperty =
			DependencyProperty.RegisterAttached("Password",
				typeof(string),
				typeof(ContentElementBinder),
				new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal, OnPasswordPropertyChanged));

		private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var passwordBox = sender as PasswordBox;
			passwordBox.PasswordChanged -= PasswordChanged;

			if (!Equals(passwordBox.Password, e.NewValue))
				passwordBox.Password = (string)e.NewValue;

			passwordBox.PasswordChanged += PasswordChanged;
		}

		public static string GetPassword(DependencyObject dp)
		{
			return (string)dp.GetValue(PasswordProperty);
		}

		public static void SetPassword(DependencyObject dp, string value)
		{
			dp.SetValue(PasswordProperty, value);
		}

		private static void PasswordChanged(object sender, RoutedEventArgs e)
		{
			var passwordBox = sender as PasswordBox;
			SetPassword(passwordBox, passwordBox.Password);
		}

		public static void Bind(object viewModel, DependencyObject view, object context)
		{
			var viewModelType = viewModel.GetType();

			var elements = view.GetChildOfType<FrameworkElement>()
				.Where(e => !string.IsNullOrEmpty(e.Name))
				.Distinct()
				.ToList();

			foreach (var element in elements) {
				var convention = ConventionManager.GetElementConvention(element.GetType());
				if (convention == null)
					continue;

				PropertyInfo property;
				Type resultViewModelType;
				var bindPath = NotifyValueSupport.Patch(viewModelType, element.Name, out property, out resultViewModelType);
				if (property == null)
					continue;

				Bind(convention, element, bindPath, property, viewModelType);
			}
		}

		private static bool Bind(ElementConvention convention, FrameworkElement element, string bindPath, PropertyInfo property, Type viewModelType)
		{
			var bindableProperty = convention.GetBindableProperty(element);

			if (bindableProperty == null || element.GetBindingExpression(bindableProperty) != null) {
				return false;
			}
			
			if (!NotBindedAndNull(element, bindableProperty)) {
				return true;
			}
			
			var binding = new Binding(bindPath);

			ConventionManager.ApplyBindingMode(binding, property);
			ConventionManager.ApplyValueConverter(binding, bindableProperty, property);
			ConventionManager.ApplyStringFormat(binding, convention, property);
			ConventionManager.ApplyValidation(binding, viewModelType, property);
			ConventionManager.ApplyUpdateSourceTrigger(bindableProperty, element, binding, property);

			BindingOperations.SetBinding(element, bindableProperty, binding);
			return true;
		}
	}
}