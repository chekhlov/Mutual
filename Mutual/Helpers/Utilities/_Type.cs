using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mutual.Helpers.Utilities
{
	public class _Type
	{
		public class _TypeException : CustomException
		{
			public _TypeException(string message) : base(message)
			{
			}

			public _TypeException(object sender, string message) : base(sender, message)
			{
			}
		}


		// Проверяем - простой ли это тип
		static internal bool isSimpleObject(Type type)
		{
			if (type.IsEnum || type.IsArray) return false;

			switch (Type.GetTypeCode(type)) {
				case TypeCode.Boolean:
				case TypeCode.Char:
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
				case TypeCode.DateTime:
				case TypeCode.String: return true;
			}

			return false;
		}


		static internal object createSimpleObject(Type type)
		{
			switch (Type.GetTypeCode(type)) {
				case TypeCode.Boolean: return new bool();
				case TypeCode.Char: return new char();
				case TypeCode.SByte: return new sbyte();
				case TypeCode.Byte: return new byte();
				case TypeCode.Int16: return new short();
				case TypeCode.UInt16: return new ushort();
				case TypeCode.Int32: return new int();
				case TypeCode.UInt32: return new uint();
				case TypeCode.Int64: return new long();
				case TypeCode.UInt64: return new ulong();
				case TypeCode.Single: return new double();
				case TypeCode.Double: return new double();
				case TypeCode.Decimal: return new decimal();
				case TypeCode.DateTime: return new DateTime();
				case TypeCode.String: return String.Empty;
			}

			return null;
		}

		static public object CreateObject(Type type)
		{
			object obj = null;

			if (isSimpleObject(type)) {
				obj = createSimpleObject(type);
				return obj;
			}

			if (type.IsArray)
				return CreateObject(type, 0);

			return Activator.CreateInstance(type);
		}

		static public object CreateObject(Type type, object param)
		{
			if (param == null)
				throw new _TypeException(typeof(_Type), $"При создании объекта {type.FullName} отсутствует параметр");

			return Activator.CreateInstance(type, new { param });
		}

		/// <summary>
		/// Копирование по свойственно объекта из srcItem в dstItem
		/// </summary>
		/// <param name="srcItem">Источник</param>
		/// <param name="dstItem">Получатель</param>
		/// <param name="ignoreProperties">Игнорируемые свойства</param>
		public static void Copy(object srcItem, object dstItem, string[] ignoreProperties = null)
		{
			var srcProps = srcItem.GetType().GetProperties().Where(x => x.CanRead && x.CanWrite);
			if (ignoreProperties != null) {
				srcProps = srcProps.Where(x => !ignoreProperties.Contains(x.Name));
			}

			var dstProps = dstItem.GetType().GetProperties().Where(x => x.CanRead && x.CanWrite).ToDictionary(x => x.Name);
			foreach (var srcProp in srcProps) {
				var dstProp = dstProps.GetValueOrDefault(srcProp.Name);
				dstProp?.SetValue(dstItem, srcProp.GetValue(srcItem, null), null);
			}
			
			var srcFields = srcItem.GetType().GetFields().Where(x => x.IsPublic);
			if (ignoreProperties != null) {
				srcFields = srcFields.Where(x => !ignoreProperties.Contains(x.Name));
			}

			var dstFields = dstItem.GetType().GetFields().Where(x => x.IsPublic).ToDictionary(x => x.Name);
			foreach (var srcField in srcFields) {
				var dstProp = dstFields.GetValueOrDefault(srcField.Name);
				dstProp?.SetValue(dstItem, srcField.GetValue(srcItem));
			}
			
		}
	}
}