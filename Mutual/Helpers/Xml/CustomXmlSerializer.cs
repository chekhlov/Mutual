using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using Mutual.Helpers.Utilities;
using _Type = Mutual.Helpers.Utilities._Type;

namespace Mutual.Helpers.Xml
{
	/// <summary>
	/// Атрибут для указания комментария при сериализации объектов с помощью CustomXmlSerializer
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true)]
	public class XmlCommentAttribute : Attribute
	{
		public string Value { get; set; }
	}

	public class CustomXmlNamespace : Dictionary<string, string>
	{
	}

	/// <summary>
	/// Класс для сериализации/десериализации объекта в Xml c дополнительными возможностями:
	/// - добавления комментариев в Xml файл к Xml элементам (с помощью атрибута XmlCommentAttribute - XmlComment) 
	/// - позволяет сериализовать/десериализовать Dictionary<type1, type2> и List<type> - с различными типами
	/// - позволяет сериализовать/десериализовать классы наследуемые от Dictionary и List
	/// - реализована простая работа с кодировкой и добавления своих пространств имен
	/// 
	/// При десериализации - объекты создается с помощью конструктора (в отличие от Microsoft - XmlSerialize), что позволяет инициализировать классы!
	/// Поддерживает атрибуты XmlAttributeAttribute, XmlElementAttribute, XmlIgnoreAttribute, XmlTypeAttribute, XmlRootAttribute, XmlArrayItemAttribute, XmlEnumAttribute
	/// и дополнительно свой атрибут XmlCommentAttribute
	/// 
	/// ВНИМАНИЕ!!! возможно реализована Неполная совместимость и функциональность XmlSerializer.
	/// - не поддерживается работа с интерфейсом ISerializationInfo - и так выгружаются все public поля и свойства у объекта
	/// - не сможет сериализовать особо хитрые классы и хитрые типы
	/// - неподдерживается сериализация системных классов пространства имен "System....", кроме Dictionary, List
	/// 
	/// Выгружаются объекты с учетом наследования. Объект должен наследовать от объекта объявления, либо иметь общий интерфейс Iинтерфейс
	/// Пример: если тип объявлен как object - выгрузится текущий объект с атрибутом Type="тип объекта". 
	/// Десерилизацию таких объектов не проверял"""
	/// 
	/// Аналогично выгрузятся объекты находящиеся в IList<>, IDictionary<> если они не совпадает с типом объявления, то для каждого элемента добавляется
	///  атрибут Type="тип текущего объекта". Десерилизация таких объектов реализована в полном объеме
	/// 
	/// Класс позволил выгрузить сложный xml (по описанию xsd) для загрузки на сайт гоззакупки
	/// 
	/// </summary>
	public partial class CustomXmlSerializer
	{
		protected readonly string NsPrefix = "xmlns";
		protected readonly string NsXsd = "http://www.w3.org/2001/XMLSchema";
		protected readonly string NsXsi = "http://www.w3.org/2001/XMLSchema-instance";

		public CustomXmlNamespace Namespace = new CustomXmlNamespace();
		public Encoding Encode = Encoding.UTF8;


		protected System.Type _type;

		/// <summary>
		/// Внутренний класс для получения информации о атрибутах свойств и полей сериализуемого/десериализуемого класса
		/// </summary>
		protected class CustomXmlAttribute
		{
			public string ElementName = null;
			public Type ElementType = null;
			public bool IsNullable = false;
			public bool IsIgnore = false;
			public bool IsAttribute = false;
			public bool IsRoot = false;
			public List<string> Comment = new List<string>();
			public CustomXmlNamespace ParentNamespace = new CustomXmlNamespace();
			public CustomXmlNamespace Namespace = new CustomXmlNamespace();
		}

		public CustomXmlSerializer()
		{
			_type = null;
			constuctorInit();
		}

		public CustomXmlSerializer(System.Type type)
		{
			_type = type;
			constuctorInit();
		}

		private void constuctorInit()
		{
			Namespace.Add("xsi", NsXsi);
			Namespace.Add("xsd", NsXsd);
		}

		public void Serialize(Stream stream, object o)
		{
			var xmlWriter = new XmlTextWriter(stream, Encode) {
				Formatting = Formatting.Indented,
				Indentation = 2
			};

			this.Serialize(xmlWriter, o);
		}

		public void Serialize(XmlWriter xmlWriter, object o)
		{
			if (o == null) return;

			// Проверяем серилизуется ли данный объект - класс
			if (_type == null)
				_type = o.GetType();

			var tp = o.GetType();

			// проверяем совпадает ли тип объекта
			if (!tp.Equals(_type))
				if (!tp.IsInstanceOfType(_type))
					throw new XmlSerializerException(this, $"Попытка сериализовать класс типа {tp.FullName} указав тип {_type.FullName}");

			// проверяем установлен ли атрибут у обекта 
			if (!_type.Attributes.HasFlag(System.Reflection.TypeAttributes.Serializable))
				throw new XmlSerializerException(this, $"У класса {_type.FullName} не указан аттрибут [Serializable]");

			xmlWriter.WriteStartDocument();

			// добавляем корневые атрибуты в глобальное пространоство имен
			var attr = new CustomXmlAttribute() { ElementName = GetMememberName(_type), ElementType = _type, IsRoot = true };

			if (!Namespace.ContainsKey(""))
				attr.Namespace.Add("", "");

			attr.Namespace.AddRange(Namespace);

			intSerialize(xmlWriter, o, attr);

			xmlWriter.WriteEndDocument();
			xmlWriter.Flush();
		}


		private void intSerialize(XmlWriter xmlWriter, object obj, CustomXmlAttribute par_attr = null)
		{
			if (obj == null) return;

			var typeInfo = obj.GetType();

			var baseType = par_attr.ElementType;

			var attr = GetXmlAttribute(typeInfo, par_attr);

			// Если указано игнорирование 
			if (attr.IsIgnore) return;

			// Если указан комментарий, то записываем вначале комментарий - комментарий для Xml атрибута игнорируем
			if (!attr.IsAttribute && attr.Comment.Count > 0) {
				foreach (var comm in attr.Comment) {
					xmlWriter.WriteComment(" " + comm + " ");
				}
			}

			// поддерживаемые базовые типы 
			if (_Type.isSimpleObject(typeInfo)) {
				// Создаем для объекта значение по умолчанию - для проверки выгрузки
				var nul = _Type.createSimpleObject(typeInfo);

				//!!! не выгружаем поля с значениями по умолчанию (кроме boolean)
				if (!obj.Equals(nul) || typeInfo.FullName == "System.Boolean")
					intWriteValue(xmlWriter, CustomConvert(obj), attr);

				return;
			}

			// Серилизовать класс или массив в атрибуты невозможно
			if (attr.IsAttribute && typeInfo.IsClass) {
				var memembers = typeInfo.GetMembers();
				if (memembers.Length > 1)
					throw new XmlSerializerException(this, $"Невозможно сериализовать сложный класс {typeInfo.FullName} как XmlAttribute");

				var propertyInfo = memembers[0];
				var value = propertyInfo.MemberType == MemberTypes.Field ? ((FieldInfo) propertyInfo).GetValue(obj) : ((PropertyInfo) propertyInfo).GetValue(obj, null);

				// заносим 
				intSerialize(xmlWriter, value, attr);
			}

			// Добавляем сериализацию Dictionary
			//typeInfo.FullName.StartsWith("System.Collections.Generic.Dictionary"))
			if (obj is IDictionary) {
				intSerializeDictionary(xmlWriter, obj, attr);
				return;
			}

			// Добавляем сериализацию List
			//if (typeInfo.FullName.StartsWith("System.Collections.Generic.List"))
			if (obj is IList) {
				intSerializeList(xmlWriter, obj, attr);
				return;
			}

			if (typeInfo.IsArray || obj is IEnumerable) {
				intSerializeArray(xmlWriter, obj, attr);
				return;
			}

			// Для системных типов и свойств, которые не являются классом или массивом
			if (typeInfo.FullName.StartsWith("System.")) {
				// Другие системные типы при сериализации мы не поддерживаем
				intWriteValue(xmlWriter, obj.ToString(), attr);
				return;
			}

			if (typeInfo.IsEnum) {
				// получаем значение Enum
				string val = obj.ToString();

				string[] enumName = val.Split(',');

				foreach (var enVal in enumName) {
					foreach (var en in typeInfo.GetEnumNames()) {
						// проверяем для выгружаемого значения значения атрибут
						var fld = typeInfo.GetField(en, BindingFlags.Static | BindingFlags.Public);

						// Проверяем наличие XmlEnumAttribute и при выгрузке используем значение XmlEnumAttribute
						// Проверяем наличие XmlEnumAttribute и получаем его значение
						if (fld != null && fld.IsDefined(typeof(XmlEnumAttribute), false)) {
							if (enVal == en) {
								var attrName = ((XmlEnumAttribute) fld.GetCustomAttributes(typeof(XmlEnumAttribute), false)[0]).Name;
								val = val.Replace(enVal, attrName);
								break;
							}
						}
					}
				}

				val = val.Replace(",", " ");

				intWriteValue(xmlWriter, val, attr);
				return;
			}


			intWriteStartElement(xmlWriter, attr);

			if (typeInfo != attr.ElementType) {
				var baseTypeName = ((Type) baseType).FullName;

				var fnd = false;

				var baseTypeInfo = typeInfo;
				while (baseTypeInfo != null && !baseTypeInfo.IsGenericType) {
					if (baseTypeInfo.FullName == baseTypeName) {
						fnd = true;
						break;
					}

					baseTypeInfo = baseTypeInfo.BaseType;
				}

				var intf = typeInfo.GetInterfaces();

				foreach (var i in intf) {
					if (i.FullName == baseTypeName) {
						fnd = true;
						break;
					}
				}

				if (fnd) {
					intSerialize(xmlWriter, typeInfo.FullName, new CustomXmlAttribute() { IsAttribute = true, ElementName = TypeAttribute });
				}
			}


			intSerializeClass(xmlWriter, obj, attr);
			xmlWriter.WriteEndElement();
		}

		protected void intSerializeClass(XmlWriter xmlWriter, object obj, CustomXmlAttribute parrentAttr, MemberInfo[] memebers = null)
		{
			var typeInfo = obj.GetType();

			// Остается обработка класса
			// Обработку разделяем на 2 части - почему-то XML атрибуты должны быть выгружены всегда первыми :
			// 1-я для свойств класса у которых указано XmlAttribute 
			// 2-я для остальных
			memebers = memebers ?? typeInfo.GetMembers();

			// Для обработки элементов как атрибуты Xml
			foreach (var propertyInfo in memebers) {
				// Обрабатываем только открытые поля класса (field и properties)
				if (propertyInfo.MemberType != MemberTypes.Field && propertyInfo.MemberType != MemberTypes.Property)
					continue;

				if (propertyInfo.MemberType == MemberTypes.Property && !((PropertyInfo) propertyInfo).CanWrite)
					continue;

				parrentAttr.ElementName = null;
				parrentAttr.ElementType = null;

				// Пропускаем только свойства у которых указано XmlIgnore или возможны для выгрузки
				// проверяем здесь, потому, что getValue может вызвать исключение
				var attr = GetXmlAttribute(propertyInfo, parrentAttr);
				if (attr.IsIgnore) continue;

				// Пропускаем только свойства у которых указано XmlAttribute 
				// и не является классом или массивом (сложно их записать как Xml атрибуты :)
				var value = getValue(obj, propertyInfo);

				// Проверяем атрибуты непосредственно у значения элемента
				attr = GetXmlAttribute(propertyInfo, parrentAttr, value);
				if (attr.IsIgnore || !attr.IsAttribute) continue;

				intSerialize(xmlWriter, value, attr);
			}

			// Для обработки элементов Xml
			foreach (var propertyInfo in memebers) {
				// Обрабатываем только открытые поля класса (field и properties)
				if (propertyInfo.MemberType != MemberTypes.Field && propertyInfo.MemberType != MemberTypes.Property)
					continue;

				if (propertyInfo.MemberType == MemberTypes.Property && !((PropertyInfo) propertyInfo).CanWrite)
					continue;

				parrentAttr.ElementName = null;
				parrentAttr.ElementType = null;

				// Пропускаем только свойства у которых указано XmlIgnore или возможны для выгрузки
				// проверяем здесь, потому, что getValue может вызвать исключение
				var attr = GetXmlAttribute(propertyInfo, parrentAttr);
				if (attr.IsIgnore) continue;

				// Пропускаем только свойства у которых указано XmlAttribute 
				// и не является классом или массивом (сложно их записать как Xml атрибуты :)
				var value = getValue(obj, propertyInfo);

				// Проверяем атрибуты непосредственно у значения элемента
				attr = GetXmlAttribute(propertyInfo, parrentAttr, value);

				// Усли указано игнорирование и не указан признак Xml атрибута
				if (attr.IsIgnore || attr.IsAttribute) continue;

				// Если указан у свойства комментарий, то записываем вначале комментарий - коментарии для типа объекта обрабатываются в атрибутах
				if (propertyInfo.IsDefined(typeof(XmlCommentAttribute), false)) {
					foreach (var a in (XmlCommentAttribute[]) propertyInfo.GetCustomAttributes(typeof(XmlCommentAttribute), false)) {
						xmlWriter.WriteComment(" " + a.Value + " ");
					}
				}

				// для пустых членов если указано свойство у атрибута, что можно записывать пустые значения
				if (attr.IsNullable && value == null) {
					// иначе записываем в виде  <ElementName xsi:nil = "true" />
					xmlWriter.WriteStartElement(attr.ElementName);
					xmlWriter.WriteAttributeString("xsi:nil", "true");
					xmlWriter.WriteEndElement();
				} else
					intSerialize(xmlWriter, value, attr);
			}
		}

		// поддерживаемые базовые типы 
		private void intWriteValue(XmlWriter xmlWriter, string value, CustomXmlAttribute attr)
		{
			// Если этот элемент указан как Xml аттрибут 
			if (attr.IsAttribute)
				xmlWriter.WriteAttributeString(attr.ElementName, value);
			else {
				intWriteStartElement(xmlWriter, attr);
				xmlWriter.WriteString(value);
				xmlWriter.WriteEndElement();
			}
		}

		protected void intWriteStartElement(XmlWriter xmlWriter, CustomXmlAttribute attr)
		{
			if (attr.Namespace.Count == 0) {
				xmlWriter.WriteStartElement(attr.ElementName);
			} else {
				string key = null;
				string ns = null;
				var frst = attr.Namespace.First();
				ns = frst.Value;

				// проверяем наличие пространства имен в глобальном списке
				key = attr.ParentNamespace.ContainsValue(ns) ? attr.ParentNamespace.GetKeyByValue(ns) : frst.Key;

				if (String.IsNullOrWhiteSpace(key))
					xmlWriter.WriteStartElement(attr.ElementName);
				else
					xmlWriter.WriteStartElement(key, attr.ElementName, ns);

				foreach (var a in attr.Namespace) {
					if (!String.IsNullOrWhiteSpace(a.Value) && !attr.ParentNamespace.ContainsValue(a.Value))
						xmlWriter.WriteAttributeString(NsPrefix, a.Key, null, a.Value);
				}
			}

			if (attr.IsRoot) {
				foreach (var a in attr.ParentNamespace) {
					if (!String.IsNullOrWhiteSpace(a.Value))
						xmlWriter.WriteAttributeString(NsPrefix, a.Key, null, a.Value);
				}
			}
		}

		protected void intSerializeDictionary(XmlWriter xmlWriter, object obj, CustomXmlAttribute attr)
		{
			intWriteStartElement(xmlWriter, attr);

			var typeInfo = obj.GetType();
			var propertyInfo = attr.ElementType;

			while (!typeInfo.IsGenericType) {
				if (typeof(IDictionary).IsAssignableFrom(typeInfo.BaseType)) {
					var memebers = typeInfo.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
					intSerializeClass(xmlWriter, obj, attr, memebers);
					typeInfo = typeInfo.BaseType;
				} else
					throw new XmlSerializerException(this, "Неудалось получить базовый класс Dictionary");
			}

			attr.ElementName = null;

			if (propertyInfo.IsDefined(typeof(XmlArrayItemAttribute), false)) {
				var a = (XmlArrayItemAttribute) propertyInfo.GetCustomAttributes(typeof(XmlArrayItemAttribute), false)[0];

				AddNamespaceToAttr(ref attr, a.Namespace);

				attr.ElementName = a.ElementName;
				attr.IsNullable = a.IsNullable;
			}

			attr.ElementName = String.IsNullOrEmpty(attr.ElementName) ? "item" : attr.ElementName;

			// Сохраняем каждый элемент массива
			foreach (DictionaryEntry val in (IDictionary) obj) {
				xmlWriter.WriteStartElement(attr.ElementName);

				xmlWriter.WriteAttributeString("key", null, val.Key.ToString());
				intSerialize(xmlWriter, val.Value);

				xmlWriter.WriteEndElement(); // Item
			}

			xmlWriter.WriteEndElement(); // object of Dictionary
		}

		protected void intSerializeList(XmlWriter xmlWriter, object obj, CustomXmlAttribute attr)
		{
			// Если указывается XmlArrayItemAttribute, то добавляется корневой элемент
			var typeInfo = obj.GetType();
			var propertyInfo = attr.ElementType;

			while (!typeInfo.IsGenericType) {
				if (typeof(IList).IsAssignableFrom(typeInfo.BaseType)) {
					var memebers = typeInfo.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
					intSerializeClass(xmlWriter, obj, attr, memebers);
					typeInfo = typeInfo.BaseType;
				} else
					throw new XmlSerializerException(this, "Неудалось получить базовый класс List");
			}

			var listBaseType = typeInfo.GenericTypeArguments[0];

			intWriteStartElement(xmlWriter, attr);
			attr.ElementName = null;

			if (propertyInfo.IsDefined(typeof(XmlArrayItemAttribute), false)) {
				var a = (XmlArrayItemAttribute) propertyInfo.GetCustomAttributes(typeof(XmlArrayItemAttribute), false)[0];

				AddNamespaceToAttr(ref attr, a.Namespace);
				attr.ElementName = a.ElementName;
				attr.IsNullable = a.IsNullable;
			}

			// Сохраняем каждый элемент массива
			foreach (var val in (IList) obj) {
				attr.ElementType = listBaseType;
				intSerialize(xmlWriter, val, attr);
			}

			xmlWriter.WriteEndElement(); // object of List
		}


		protected void intSerializeArray(XmlWriter xmlWriter, object obj, CustomXmlAttribute attr)
		{
			var propertyInfo = attr.ElementType;

			// Если указывается XmlArrayItemAttribute, то добавляется корневой элемент
			bool isHasArrayItemAttribute = false;

			if (propertyInfo.IsDefined(typeof(XmlArrayItemAttribute), false)) {
				var a = (XmlArrayItemAttribute) propertyInfo.GetCustomAttributes(typeof(XmlArrayItemAttribute), false)[0];

				intWriteStartElement(xmlWriter, attr);

				AddNamespaceToAttr(ref attr, a.Namespace);

				attr.ElementName = a.ElementName;
				attr.IsNullable = a.IsNullable;
				isHasArrayItemAttribute = true;
			}


			// Сохраняем каждый элемент массива
			foreach (var val in (IEnumerable) obj) {
				intSerialize(xmlWriter, val, attr);
			}

			// Если добавляли корневой элемент - закрываем
			if (isHasArrayItemAttribute)
				xmlWriter.WriteEndElement(); // object of List
		}

		/// <summary>
		/// Получаем тип наименования объекта в формате XML
		/// Преобразование используются только для простых типов
		/// </summary>
		protected string GetMememberName(MemberInfo type)
		{
			string ret;
			var gt = type as Type ?? type.GetType();

			switch (Type.GetTypeCode(gt)) {
				case TypeCode.Boolean:
					ret = "boolean";
					break;
				case TypeCode.Char:
					ret = "char";
					break;
				case TypeCode.SByte:
					ret = "unsignedByte";
					break;
				case TypeCode.Byte:
					ret = "byte";
					break;
				case TypeCode.Int16:
					ret = "short";
					break;
				case TypeCode.UInt16:
					ret = "unsignedShort";
					break;
				case TypeCode.Int32:
					ret = "int";
					break;
				case TypeCode.UInt32:
					ret = "unsignedInt";
					break;
				case TypeCode.Int64:
					ret = "long";
					break;
				case TypeCode.UInt64:
					ret = "unsignedLong";
					break;
				case TypeCode.Single:
					ret = "float";
					break;
				case TypeCode.Double:
					ret = "double";
					break;
				case TypeCode.Decimal:
					ret = "decimal";
					break;
				case TypeCode.DateTime:
					ret = "dateTime";
					break;
				case TypeCode.String:
					ret = "string";
					break;
				default:
					ret = gt == typeof(Guid) ? "guid" : type.Name;
					break;
			}

			if (ret == "Object") ret = "object";
			return ret;
		}


		protected void AddNamespaceToAttr(ref CustomXmlAttribute attr, string ns, string prefix = "")
		{
			if (String.IsNullOrEmpty(ns) || attr.Namespace.ContainsValue(ns))
				return;

			if (attr.ParentNamespace.ContainsValue(ns))
				prefix = attr.ParentNamespace.GetKeyByValue(ns);

			if (prefix == "" && attr.Namespace.ContainsKey(prefix)) prefix = null;

			if (prefix == null) {
				// генерируем префикс
				// выделяем из строки вторую подстроку

				var regex = new Regex("^http:\\/\\/\\w+\\.\\w+\\.\\w+\\/(\\w+)\\/");
				var match = regex.Match(ns);

				prefix = match.Success ? match.Groups[1].Value : "ns";

				var pr = prefix;
				var i = 1;
				while (attr.Namespace.ContainsKey(pr)) {
					pr = String.Format("{0}{1}", prefix, i++);
				}

				prefix = pr;
			}

			if (!attr.Namespace.ContainsKey(prefix))
				attr.Namespace.Add(prefix, ns);
		}

		/// <summary>
		/// Функция получает информацию об аттрибутах класса/субкласса/переменной - если указано
		/// </summary>
		/// <param name="propertyInfo">Информация об класса/субкласса/переменной</param>
		/// <param name="parentAttr">Указывает - получать наследовать ли аттрибут для корневого элемента</param>
		/// <returns></returns>
		protected CustomXmlAttribute GetXmlAttribute(MemberInfo propertyInfo, CustomXmlAttribute parentAttr = null, object value = null)
		{
			// Если возвратили null - значит это неподдерживаемый элемент - пропускаем
			var type = getType(propertyInfo);

			var attr = new CustomXmlAttribute() { ElementName = GetMememberName(propertyInfo), ElementType = type };

			if (type == null) {
				attr.IsIgnore = true;
				return attr;
			}

			if (parentAttr != null) {
				if (!String.IsNullOrEmpty(parentAttr.ElementName)) {
					attr.ElementName = parentAttr.ElementName;
					attr.ElementType = parentAttr.ElementType;
					attr.IsAttribute = parentAttr.IsAttribute;
					attr.IsNullable = parentAttr.IsNullable;
					attr.IsIgnore = parentAttr.IsIgnore;
					attr.IsRoot = parentAttr.IsRoot;
				}

				attr.ParentNamespace.AddRange(parentAttr.Namespace);
				attr.ParentNamespace.AddRange(parentAttr.ParentNamespace);
			}

			// Проверяем наличие корневого аттрибута
			if (attr.IsRoot && _type.IsDefined(typeof(XmlRootAttribute), false)) {
				var a = ((XmlRootAttribute[]) _type.GetCustomAttributes(typeof(XmlRootAttribute), false))[0];

				AddNamespaceToAttr(ref attr, a.Namespace);

				attr.ElementName = a.ElementName;
				attr.IsNullable = a.IsNullable;
			}

			// Проверяем XmlTypeAttribute, который может быть установлен для класса, структуры, enum и интерфейса
			if (propertyInfo.IsDefined(typeof(XmlTypeAttribute), false)) {
				foreach (var a in (XmlTypeAttribute[]) propertyInfo.GetCustomAttributes(typeof(XmlTypeAttribute), false)) {
					attr.ElementName = String.IsNullOrEmpty(a.TypeName) ? attr.ElementName : a.TypeName;
					AddNamespaceToAttr(ref attr, a.Namespace);
				}
			}

			// Проверяем признак игнорирование выгрузки атрибутов
			attr.IsIgnore = attr.IsIgnore | propertyInfo.IsDefined(typeof(XmlIgnoreAttribute), false);

			// Проверяем признак игнорирование выгрузки атрибутов
			attr.IsIgnore = attr.IsIgnore | propertyInfo.IsDefined(typeof(NonSerializedAttribute), false);

			if (propertyInfo.IsDefined(typeof(XmlElementAttribute), false)) {
				foreach (var a in (XmlElementAttribute[]) propertyInfo.GetCustomAttributes(typeof(XmlElementAttribute), false)) {
					if (a.Type == null || value == null || a.Type == value.GetType()) {
						AddNamespaceToAttr(ref attr, a.Namespace);

						attr.ElementName = String.IsNullOrEmpty(a.ElementName) ? attr.ElementName : a.ElementName;
						attr.IsNullable = a.IsNullable;
						attr.IsAttribute = false;
						break;
					}
				}
			}

			// Проверяем XmlTypeAttribute, который может быть установлен для типа
			if (type.IsDefined(typeof(XmlTypeAttribute), false)) {
				foreach (var a in (XmlTypeAttribute[]) type.GetCustomAttributes(typeof(XmlTypeAttribute), false)) {
					attr.ElementName = String.IsNullOrEmpty(a.TypeName) ? attr.ElementName : a.TypeName;
					AddNamespaceToAttr(ref attr, a.Namespace);
				}
			}

			// Проверяем XmlCommentAttribute для объявления типа
			if (type.IsDefined(typeof(XmlCommentAttribute), false)) {
				foreach (
					var a in (XmlCommentAttribute[]) type.GetCustomAttributes(typeof(XmlCommentAttribute), false)) {
					attr.Comment.Add(a.Value);
				}
			}

			// Проверяем указание, что этот элемент должен быть Xml аттрибутом 
			// В базовом классе будет получено исключение, если будет указаны одновременно XmlElementAttribute и XmlAttributeAttribute 
			if (propertyInfo.IsDefined(typeof(XmlAttributeAttribute), false)) {
				var a = (XmlAttributeAttribute) propertyInfo.GetCustomAttributes(typeof(XmlAttributeAttribute), false)[0];

				AddNamespaceToAttr(ref attr, a.Namespace);

				if (a.Type == null || value == null || a.Type == value.GetType()) {
					AddNamespaceToAttr(ref attr, a.Namespace);

					attr.ElementName = String.IsNullOrEmpty(a.AttributeName) ? attr.ElementName : a.AttributeName;
					attr.IsAttribute = true;
					attr.IsNullable = false;
				}
			}


			if (attr.ElementName.Contains("'")) attr.ElementName = attr.ElementName.Replace("'", "");
			if (attr.ElementName.Contains("`")) attr.ElementName = attr.ElementName.Replace("`", "");

			if (attr.Namespace.Count == 0) {
				var ns = attr.ParentNamespace.FirstOrDefault();
				AddNamespaceToAttr(ref attr, ns.Value);
			}

			return attr;
		}


		/// <summary>
		/// Внутренняя функция преобразования объекта в формат Xml стандарта (взято XmlSerializer)
		/// используется в основном для примитив языка C/C++/C#. 
		/// Для примера: Использование ToString() для bool приводит к выдаче True/False, а в соотвествии с XML стандартом должно быть true/false
		/// </summary>
		internal string CustomConvert(object o)
		{
			var data = String.Empty;
			if (o == null) return data;

			var type = o.GetType();

			if (type.IsEnum) {
				return o.ToString();
			}

			switch (Type.GetTypeCode(type)) {
				case TypeCode.Boolean:
					data = XmlConvert.ToString((bool) o);
					break;

				case TypeCode.Char:
					data = XmlConvert.ToString((ushort) o);
					break;

				case TypeCode.SByte:
					data = XmlConvert.ToString((sbyte) o);
					break;

				case TypeCode.Byte:
					data = XmlConvert.ToString((byte) o);
					break;

				case TypeCode.Int16:
					data = XmlConvert.ToString((short) o);
					break;

				case TypeCode.UInt16:
					data = XmlConvert.ToString((ushort) o);
					break;

				case TypeCode.Int32:
					data = XmlConvert.ToString((int) o);
					break;

				case TypeCode.UInt32:
					data = XmlConvert.ToString((uint) o);
					break;

				case TypeCode.Int64:
					data = XmlConvert.ToString((long) o);
					break;

				case TypeCode.UInt64:
					data = XmlConvert.ToString((ulong) o);
					break;

				case TypeCode.Single:
					data = XmlConvert.ToString((float) o);
					break;

				case TypeCode.Double:
					data = XmlConvert.ToString((double) o);
					break;

				case TypeCode.Decimal:
					data = XmlConvert.ToString((decimal) o);
					break;

				case TypeCode.DateTime:
					data = XmlConvert.ToString((DateTime) o, XmlDateTimeSerializationMode.RoundtripKind);
					break;

				case TypeCode.String:
					data = (string) o;
					break;

				default:
					data = type == typeof(Guid) ? XmlConvert.ToString((Guid) o) : o.ToString();
					break;
			}

			return data;
		}

		public static void SaveAsXml<T>(string fileName, T xml)
		{
			if (xml == null) return;

			var xmlFormat = new CustomXmlSerializer(typeof(T));
			using (var fStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None)) {
				try {
					xmlFormat.Serialize(fStream, xml);
				} catch (Exception e) {
					throw new XmlSerializerException(null, $"Произошла ошибка при сериализации объекта {typeof(T).Name} в Xml файл '{fileName}'\n{e.Message}");
				}

				fStream.Close();
				fStream.Dispose();
			}
		}
	}
}