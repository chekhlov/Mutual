using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.Core.Metadata.Edm;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Mutual.Helpers
{
	public class ValueDescription<T>
	{
		public ValueDescription(string name, T value)
		{
			Name = name;
			Value = value;
		}

		public string Name { get; set; }
		public T Value { get; set; }
	}

	public class ValueDescription
	{
		public ValueDescription(Enum value)
		{
			var desc = Helpers.GetDescription(value);
			Name = string.IsNullOrEmpty(desc) ? value.ToString() : desc;
			Value = value;
		}

		public ValueDescription(string name, object value)
		{
			Name = string.IsNullOrEmpty(name) ? value.ToString() : name;
			Value = value;
		}

		public string Name { get; set; }
		public object Value { get; set; }
	}

	public static class Helpers
	{

		public static object ConvertString(object value, Type targetType, CultureInfo culture)
		{
			if (value == null)
				return null;
			var converter = TypeDescriptor.GetConverter(targetType);
			try {
				return converter.ConvertFrom(null, culture, value);
			}
			catch (Exception) {
				return converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
			}
		}

		public static List<ValueDescription> GetDescriptions(Type type)
		{
			return Enum.GetValues(type)
					.Cast<object>()
					.Select(v => new ValueDescription(GetDescription(type.GetField(v.ToString())), v))
					.ToList();
		}

		public static List<ValueDescription<T>> GetDescriptions<T>()
		{
			var enumType = typeof(T);
			return Enum.GetValues(enumType)
					.Cast<T>()
					.Select(v => new ValueDescription<T>(GetDescription(enumType.GetField(v.ToString())), v))
					.ToList();
		}


		public static string GetDescription(ICustomAttributeProvider provider)
		{
			var attributes = provider.GetCustomAttributes(typeof(DescriptionAttribute), false);
			if (attributes.Length == 0)
				return "";
			return ((DescriptionAttribute) attributes[0]).Description;
		}

		public static string GetDescription(Enum value)
		{
			var fieldInfo = value.GetType().GetField(value.ToString());
			return ((DescriptionAttribute[]) fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false))
					.FirstOrDefault()?.Description;
		}

		/// <summary>
		/// Adds a flag value to enum.
		/// Please note that enums are value types so you need to handle the RETURNED value from this method.
		/// Example: myEnumVariable = myEnumVariable.AddFlag(CustomEnumType.Value1);
		/// </summary>
		public static T AddFlag<T>(this T type, T enumFlag) where T : Enum
		{
			try {
				return (T) (object) ((int) (object) type | (int) (object) enumFlag);
			} catch (Exception ex) {
				throw new ArgumentException(string.Format("Could not append flag value {0} to enum {1}", enumFlag, typeof(T).Name), ex);
			}
		}

		/// <summary>
		/// Removes the flag value from enum.
		/// Please note that enums are value types so you need to handle the RETURNED value from this method.
		/// Example: myEnumVariable = myEnumVariable.RemoveFlag(CustomEnumType.Value1);
		/// </summary>
		public static T RemoveFlag<T>(this T type, T enumFlag) where T : Enum
		{
			try {
				return (T) (object) ((int) (object) type & ~(int) (object) enumFlag);
			} catch (Exception ex) {
				throw new ArgumentException(string.Format("Could not remove flag value {0} from enum {1}", enumFlag, typeof(T).Name), ex);
			}
		}

		/// <summary>
		/// Sets flag state on enum.
		/// Please note that enums are value types so you need to handle the RETURNED value from this method.
		/// Example: myEnumVariable = myEnumVariable.SetFlag(CustomEnumType.Value1, true);
		/// </summary>
		public static T SetFlag<T>(this T type, T enumFlag, bool value) where T : Enum
		{
			return value ? type.AddFlag(enumFlag) : type.RemoveFlag(enumFlag);
		}
	}
}