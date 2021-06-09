using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Mutual.Helpers;

namespace Mutual.Converters
{
	public class EnumConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Helpers.Helpers.GetDescription((Enum) value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string)
				return Enum.Parse(targetType, (string) value);

			if (value is Int32)
				return Enum.ToObject(targetType, value);

			return value;
		}
	}

	public class IntToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (int) value > 0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class BoolToHiddenConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (bool) value ? Visibility.Visible : Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (Visibility) value == Visibility.Visible;
		}
	}

	public class BoolToCollapsedConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (Visibility) value == Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (bool) value ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	public class IntToCollapsedConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((int) value) > 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class InvertConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !(bool) value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !(bool) value;
		}
	}

	public class NullableConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var forward = TypeDescriptor.GetConverter(targetType);
			if (value == null || forward.CanConvertFrom(value.GetType()))
				return forward.ConvertFrom(value);
			return TypeDescriptor.GetConverter(value.GetType()).ConvertTo(value, targetType);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return TypeDescriptor.GetConverter(targetType).ConvertFrom(value);
		}
	}

	/// <summary>
	/// Для возможности задания более одного параметров для конверторов
	/// </summary>
	public abstract class ConvertorBase<T> : MarkupExtension, IValueConverter where T : class, new()
	{
		public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);
		public abstract object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	public class InputConverter : ConvertorBase<InputConverter>
	{
		public static InputConverter Instance = new InputConverter();

		public int MaxLenght { set; private get; }
		public int MinLenght { set; private get; }

		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}

		public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var strValue = value as string;
			if (strValue != null) {
				if ((MaxLenght > 0 && strValue.Length > MaxLenght) || (MinLenght > 0 && strValue.Length < MinLenght))
					return DependencyProperty.UnsetValue;
			}

			return Helpers.Helpers.ConvertString(value, targetType, culture);
		}
	}

	public class ComboBoxSelectedItemConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var res = ((IEnumerable<ValueDescription>) parameter).FirstOrDefault(d => Equals(d.Value, value));
			return ((IEnumerable<ValueDescription>) parameter).FirstOrDefault(d => Equals(d.Value, value));
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return ((IEnumerable<ValueDescription>) parameter).FirstOrDefault();
			
			return ((ValueDescription) value).Value;
		}

	}
	public class BooleanConverter<T> : IValueConverter
	{
		public BooleanConverter(T trueValue, T falseValue)
		{
			TrueValue = trueValue;
			FalseValue = falseValue;
		}

		public T TrueValue { get; set; }

		public T FalseValue { get; set; }

		public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
				value is bool boolValue
				&& boolValue
						? TrueValue
						: FalseValue;

		public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
				value is T actualValue
				&& EqualityComparer<T>.Default.Equals(actualValue, TrueValue);
	}

	public sealed class BooleanToVisibilityConverter : BooleanConverter<Visibility>
	{
		public BooleanToVisibilityConverter() :
				base(Visibility.Visible, Visibility.Collapsed)
		{
		}
	}

	public sealed class InvertBooleanToCollapsedConverter : BooleanConverter<Visibility>
	{
		public InvertBooleanToCollapsedConverter() :
				base(Visibility.Collapsed, Visibility.Visible)
		{
		}
	}
	public sealed class InvertBooleanToHiddenConverter : BooleanConverter<Visibility>
	{
		public InvertBooleanToHiddenConverter() :
				base(Visibility.Hidden, Visibility.Visible)
		{
		}
	}

	public class NumericValidationRule : ValidationRule
	{
		public NumericValidationRule()
		{
			ValidatesOnTargetUpdated = false;
			ValidationStep = ValidationStep.UpdatedValue;
		}

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var expr = value as BindingExpression;

			if (expr?.Target is TextBox textBox) {
				var strValue = Convert.ToString(textBox.Text);
				var regex = new Regex("^[0-9]*$");
				if (!regex.IsMatch(strValue))
					return new ValidationResult(false, $"Допускается ввод только цифрового значения.");
			}

			return ValidationResult.ValidResult;
		}
	}
}