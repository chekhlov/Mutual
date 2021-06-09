using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using Mutual.Helpers.Utilities;
using _Type = Mutual.Helpers.Utilities._Type;

namespace Mutual.Helpers.Xml
{
	/// <summary>
	/// Класс сериализации объекта в Xml c возможностью добавления комментариев к Xml элементам [XmlComment].
	/// реализует Неполную совместимость и функциональность XmlSerializer.
	/// Реализована серилизация IList, IDictionary- объектов, так же и массивов объектов
	/// Если тип объекта находящийся в IList<>, IDictionary<> не совпадает с типом объявления, но тип объявления является базовым классом или оба класса (объявления и объекта) содержат общий интерфейс класса
	///  - сериализуется текущий объект с атрибутом type="тип текущего объекта"
	/// 
	/// </summary>
	public partial class CustomXmlSerializer 
	{
		protected readonly string TypeAttribute = "Type";

		public object Deserialize(Stream stream)
		{
			return this.Deserialize(new XmlTextReader(stream), null);
		}

		public object Deserialize(XmlReader xmlReader)
		{
			return Deserialize(xmlReader, _type);
		}

		public object Deserialize(XmlReader xmlReader, Type type)
		{

			if (type == null) type = _type;

			if (type == null) throw new XmlSerializerException(this, "Попытка десерилизовать объект не указав его тип");


//			object obj = _Type.CreateObject(type);

			object obj = null;
			
			if (!ReadNode(xmlReader))
				throw new XmlSerializerException(this, "Пустой XML");
			
			if (xmlReader.NodeType == XmlNodeType.XmlDeclaration)
				ReadNode(xmlReader);

			var attr = new CustomXmlAttribute() { ElementName = GetMememberName(_type), ElementType = _type, IsRoot = true };

			attr.Namespace.AddRange(Namespace);
			attr = GetXmlAttribute(type.BaseType, attr);

			intDeserialize(xmlReader, ref obj, type, attr);
			return obj;
		}

		// Пропускаем ненужные узлы
		private bool ReadNode(XmlReader xmlReader)
		{
			do {
				if (!xmlReader.Read()) return false;
			}
			while (xmlReader.NodeType == XmlNodeType.Whitespace || xmlReader.NodeType == XmlNodeType.None || xmlReader.NodeType == XmlNodeType.Comment) ;

			return true;
		}

		// Обертка для GetXmlAttribute - поиска XmlElementAttribute по наименованию - в GetXmlAttribute ищется по значению объекта
		protected CustomXmlAttribute getXmlAttribute(MemberInfo propertyInfo, string elementName, CustomXmlAttribute parentAttr = null)
		{
			var attr = GetXmlAttribute(propertyInfo, parentAttr);
			if (propertyInfo.IsDefined(typeof(XmlElementAttribute), false))
			{
				foreach (var a in (XmlElementAttribute[])propertyInfo.GetCustomAttributes(typeof(XmlElementAttribute), false))
				{
					// проверка для Десерилизатора
					if (!String.IsNullOrEmpty(elementName) && a.ElementName == elementName)
					{
						attr.ElementType = a.Type;
						attr.ElementName = elementName;
						break;
					}
				}
			}
			return attr;
		}


		// Получает ссылку на метод, объект, поле по имени элемента
		private MemberInfo GetProperty(Type typeInfo, string elementName, MemberInfo[] members = null)
		{
			// Цикл по свойствам - мы должны проверить все Xml-атрибуты у элемента
			// возможно, что имя Xml-элемента не совпадает с именем объекта!!!!
			members = members ?? typeInfo.GetMembers();

			foreach (var propertyInfo in members)
			{
				// Обрабатываем только открытые поля класса (field и properties)
				if (propertyInfo.MemberType != MemberTypes.Field && propertyInfo.MemberType != MemberTypes.Property)
					continue;

				if (propertyInfo.MemberType == MemberTypes.Property && !((PropertyInfo) propertyInfo).CanWrite)
					continue;

				// Получаем информацию об атрибутах (если они указаны)
				var attr = getXmlAttribute(propertyInfo, elementName);

				// Усли указано атрибут игнорирования свойства
				if (attr.IsIgnore) continue;

				if (attr.ElementName == elementName)
				{
					if (propertyInfo.MemberType == MemberTypes.Property && !((PropertyInfo)propertyInfo).CanWrite)
						throw new XmlSerializerException(this, $"Свойство {attr.ElementName} объекта {typeInfo.Name} указано только для чтения");

					return propertyInfo;
				}
			}

			// // Что бы не записывать ошибку в лог файл
			// if (elementName == TypeAttribute)
			// 	return null;
			//
			// throw new XmlSerializerException(this, $"Свойство {elementName} не найдено у объекта {typeInfo.Name}");
			return null;
		}


		// Преобразование Enum по имени с проверкой XmlEnumAttribute
		private object convertEnum(string value, Type type)
		{
			// Проверка атрибутов XmlEnumAttribute для поиска значений
			string[] enumName = value.Split(new Char[] {',', ' '});

			value = String.Empty;
			foreach (var enVal in enumName)
			{
				var ev = enVal;
				if (String.IsNullOrWhiteSpace(enVal)) continue;
				
				foreach (var en in type.GetEnumNames())
				{
					var fld = type.GetField(en, BindingFlags.Static | BindingFlags.Public);

					// Проверяем наличие XmlEnumAttribute и получаем его значение
					if (fld != null && fld.IsDefined(typeof(XmlEnumAttribute), false))
					{
						var attrName = ((XmlEnumAttribute)fld.GetCustomAttributes(typeof(XmlEnumAttribute), false)[0]).Name;
						if (attrName == enVal)
						{
							ev = en;
							break;
						}
					}
				}

				if (!String.IsNullOrWhiteSpace(value))  value = value + ",";
				value = value + ev;
			}
			return Enum.Parse(type, value);
		}

		// Получает тип объекта у свойства
		// Возвращает null - если неподдерживаемый тип или объект не выгружается
		// например: MemberTypes.Property - только с одним методом get или set
		private Type getType(MemberInfo propertyInfo)
		{
			Type type = null;

			switch (propertyInfo.MemberType)
			{
				case MemberTypes.Field:             // Перменная класса
					type = ((FieldInfo)propertyInfo).FieldType;
					break;
				case MemberTypes.Property:          // Свойство класса {get,set}
					var pi = ((PropertyInfo)propertyInfo);
					// выгружаем только свойство имеющее get и set
					if (pi.CanRead && pi.CanWrite) 
						type = pi.GetGetMethod().ReturnType;
					break;
				case MemberTypes.TypeInfo:          // объект
				case MemberTypes.NestedType:        // Класс или структура внутри класса
					type = (Type)propertyInfo;
					break;
				// убрал исключение, неподдерживаемые объекты мы просто игнорируем
//                default:
//                    throw new XmlSerializerException(this, "Обнаружен неподдерживаемый тип объекта {0}, тип {1}", propertyInfo.Name, propertyInfo.MemberType.ToString());
			}
			return type;
		}

		// получаем значение для свойства
		private object getValue(object obj, MemberInfo propertyInfo)
		{
			object o = null;

			switch (propertyInfo.MemberType)
			{
				case MemberTypes.Field:
					o = ((FieldInfo)propertyInfo).GetValue(obj);
					break;
				case MemberTypes.Property:
					var pi = (PropertyInfo) propertyInfo;
					if (pi.CanRead) o = pi.GetValue(obj);
					break;
				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType:
					o = obj;
					break;
				default:
					throw new XmlSerializerException(this, $"Обнаружен неподдерживаемый тип объекта {propertyInfo.Name}, тип {propertyInfo.MemberType}");
		   }
		   return o;
		}

		// заносим значение в объект (поле, свойство) с конвертированием строки в тип объекта
		private void setValue(ref object obj, MemberInfo propertyInfo, string xmlValue)
		{
			var type = getType(propertyInfo);
			
			// Если это класс и не простой тип
			if (type.IsClass && !_Type.isSimpleObject(type))
				throw new XmlSerializerException(this, $"Невозможно преобразовать строку {xmlValue} в сложный класс {type.Name}");

			var val = type.IsEnum ?  convertEnum(xmlValue, type) : Convert.ChangeType(xmlValue, type);

			setValue(ref obj, propertyInfo, val);
		}

		// заносим значение в объект (поле, свойство) 
		private void setValue(ref object obj, MemberInfo propertyInfo, object val)
		{
			switch (propertyInfo.MemberType)
			{
				case MemberTypes.Field:
					((FieldInfo)propertyInfo).SetValue(obj, val);
					break;
				case MemberTypes.Property:
					var pi = (PropertyInfo)propertyInfo;
					if (pi.CanWrite) pi.SetValue(obj, val);
					break;
				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType:  // Класс или структура внутри класса
					obj = val;
					break;
			}
		}

		// Обрабатываем атрибут
		private void setAttribute(ref object obj, string value, string attribute)
		{
			var typeInfo = obj.GetType();

			var propertyInfo = GetProperty(typeInfo, attribute);
			
			if (propertyInfo.MemberType == MemberTypes.Property)
			{
				typeInfo = ((PropertyInfo) propertyInfo).GetGetMethod().ReturnType;

				// Если это класс
				if (typeInfo.IsClass)
				{
					// Если кол-во методов в классе > 1
					var memembers = typeInfo.GetMembers();
					if  (memembers.Length > 1)
						throw new XmlSerializerException(this, $"Невозможно десериализовать класс {typeInfo.Name} из XML-атрибута {attribute}");

					var newObj = _Type.CreateObject(typeInfo);

					// заносим  значение в свойство класс
					setValue(ref newObj, memembers[0], value);

					//заносим сам объект
					setValue(ref obj, propertyInfo, newObj);
					return;
				}

			}

			setValue(ref obj, propertyInfo, value);
		}

		// Получаем для Xml элемента все пространства имен
		private CustomXmlNamespace getNamespaces(XmlReader xmlReader)
		{
			var nsDict = new CustomXmlNamespace();
			
			// Десериализуем атрибуты и пространства имен
			if (xmlReader.HasAttributes)
			{
				// Получаем все пространства имен
				xmlReader.MoveToFirstAttribute();

				for (int i = 0; i < xmlReader.AttributeCount; i++)
				{
					string attrName = xmlReader.LocalName;
					string ns = xmlReader.Prefix;

					if (attrName == NsPrefix)
					{
						ns = xmlReader.LocalName;
						attrName = xmlReader.Prefix;
					}

					// префикс пространства имен
					if (ns == NsPrefix)
					{
						if (!nsDict.ContainsKey(attrName))
							nsDict.Add(attrName, xmlReader.Value);
					}

					xmlReader.MoveToNextAttribute();
				}
			}
			return nsDict;
		}

		private void intDeserialize(XmlReader xmlReader, ref object obj, Type typeInfo, CustomXmlAttribute parentAttr = null, CustomXmlNamespace nsParentDict = null)
		{
			if (nsParentDict == null)
			{
				nsParentDict = new CustomXmlNamespace();
				// Добавляем пустой префик - если его нет, для случая, если пространство имен по умолчанию не указывалось
				if (!nsParentDict.ContainsKey("")) nsParentDict.Add("", "");
			}

			var fullElementName = xmlReader.Name;
			var elementName = xmlReader.LocalName;
			var prefix = xmlReader.Prefix;

			// Получаем все указанные для элемента Namespaces
			var nsDict = getNamespaces(xmlReader);
			nsDict.AddRange(nsParentDict);

			var attrValue = xmlReader.GetAttribute(TypeAttribute);
			// Десериализуем атрибуты и пространства имен
			if (xmlReader.HasAttributes && !string.IsNullOrEmpty(attrValue))
			{
				// Проверяем наличие свойства с наименованием аттрибута
				var propertyInfo = GetProperty(typeInfo, TypeAttribute);

				// Если свойства нет, но есть тип, то меняем тип объекта
				if (propertyInfo == null) {
					typeInfo = Type.GetType(xmlReader.Value);
					if (typeInfo == null)
						throw new XmlSerializerException(this, $"Не найден тип объекта {attrValue}");
				}
			}			
			
			obj = _Type.CreateObject(typeInfo);
			
			if (parentAttr == null) 
				parentAttr = getXmlAttribute(typeInfo, elementName);
			
			if (parentAttr.ElementName != elementName)
				throw new XmlSerializerException(this, $"Несответствие типа объекта {elementName}, ожидалось {parentAttr.ElementName}");

			// Десериализуем атрибуты и пространства имен
			if (xmlReader.HasAttributes)
			{
				// Теперь обрабатываем все атрибуты
				xmlReader.MoveToFirstAttribute();

				for (int i = 0; i < xmlReader.AttributeCount; i++)
				{
					var attrName = xmlReader.LocalName;
					var ns = xmlReader.Prefix;
					var value = xmlReader.Value;

					// Меняем местами атрибут и префикс
					if (attrName == NsPrefix)
					{
						ns = xmlReader.LocalName;
						attrName = xmlReader.Prefix;
					}

					// если это не namespaces - указан префикс пространства имен, мы их обработали раньше в getNamespaces()
					if (ns == NsPrefix)
						goto loop;

					if (!nsDict.ContainsKey(ns))
						throw new XmlSerializerException(this, $"Неизвестный префикс пространства имен {ns} у {attrName}");

					if (!parentAttr.Namespace.ContainsValue(nsDict[ns]) && !parentAttr.ParentNamespace.ContainsValue(nsDict[ns]))
						throw new XmlSerializerException(this, $"Неверное пространство имен {ns} для {attrName}");

					// пропускаем системный атрибут xsi:nil обозначающий, что может быть пустое значение
					if (attrName == "nil" && nsDict[ns].Equals(NsXsi, StringComparison.CurrentCultureIgnoreCase))
						goto loop;

					// Если это аттрибут Type и свойства такого нет
					if (attrName == TypeAttribute) {
						var propertyInfo = GetProperty(typeInfo, TypeAttribute);
						if (propertyInfo == null)
							goto loop;
					}
					
					// Сохраняем аттрибут в соответствующее свойство
					setAttribute(ref obj, value, attrName);
					
loop:
					xmlReader.MoveToNextAttribute();
				}

				xmlReader.MoveToElement();
			}

		   
			// проверяем пространство имен для элемента
			if (!nsDict.ContainsKey(prefix))
			   throw new XmlSerializerException(this, $"Неизвестный префикс пространства имен {prefix} у {elementName}");


			if (!parentAttr.Namespace.ContainsValue(nsDict[prefix]) && !parentAttr.ParentNamespace.ContainsValue(nsDict[prefix]))
			   throw new XmlSerializerException(this, $"Неверное пространство имен {prefix} для {elementName}");

			// Десериализуем свойства класса
			// Если Xml элемент пустой то и сериализовать нечего
			if (xmlReader.IsEmptyElement) return;

			try
			{
				// должно быть присваивание простых типов (базовых)
				// обработка Enum сделана в setValue - потому, что в XML атрибутах может Enum
				if (_Type.isSimpleObject(typeInfo) || typeInfo.IsEnum)
				{
					string val = null;
					if (xmlReader.HasValue)
						val = xmlReader.Value;
					else
					{
						ReadNode(xmlReader);
						if (xmlReader.NodeType == XmlNodeType.Text)
							val = xmlReader.Value;
					}
					// пропускаем текущее значение, что бы встать на закрывающую скобку
					ReadNode(xmlReader);

					if (val != null)
						setValue(ref obj, typeInfo, val);

					return;
				}

				if (obj is IDictionary)
				{
					intDeserializeDictionary(xmlReader, ref obj, parentAttr, nsDict);
					return;
				}

				if (obj is IList)
				{
					intDeserializeList(xmlReader, ref obj, parentAttr, nsDict);
					return;
				}

				if (typeInfo.IsArray || obj is IEnumerable)
				{
					intDeserializeArray(xmlReader, ref obj, parentAttr, nsDict);
					return;
				}

				// Системных типов List, Dictionaty
				if (typeInfo.FullName.StartsWith("System."))
				{
					// Пропускаем все необрабатываемые системные типы
					while (ReadNode(xmlReader) &&
						   (xmlReader.NodeType != XmlNodeType.EndElement || xmlReader.LocalName != elementName))
					{
					}

					return;
				}

				intDeserializeClass(xmlReader, ref obj, parentAttr, nsDict);
			}
			finally
			{
				// Проверяем закрывающий элемент
				if (!parentAttr.IsIgnore && xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name != fullElementName)
					throw new XmlSerializerException(this, $"Обнаружен закрывающий элемент </{xmlReader.Name}>, ожидался </{fullElementName}>");
			}
		}

		private void intDeserializeClass(XmlReader xmlReader, ref object obj, CustomXmlAttribute parentAttr = null, CustomXmlNamespace nsDict = null, MemberInfo[] memebers = null)
		{
			var typeInfo = obj.GetType();
			// Цикл по элементам внутри XML
			while (ReadNode(xmlReader) && xmlReader.NodeType != XmlNodeType.EndElement)
			{
				// Ищем информацию о найденом элементе класса
				var propertyInfo = GetProperty(typeInfo, xmlReader.LocalName, memebers);

				var attr = getXmlAttribute(propertyInfo, xmlReader.LocalName);
				//// наследуем пространство имен родительского элемента (по умолчанию)
				attr.ParentNamespace.AddRange(parentAttr.Namespace);
				attr.ParentNamespace.AddRange(parentAttr.ParentNamespace);

				var type = getType(propertyInfo);

				object newObj = null;
				intDeserialize(xmlReader, ref newObj, type, attr, nsDict);

				setValue(ref obj, propertyInfo, newObj);
			} 
		}


		// Десериализует объект типа Dictionary
		private void intDeserializeDictionary(XmlReader xmlReader, ref object obj, CustomXmlAttribute attr, CustomXmlNamespace nsParentDict = null)
		{
			var typeInfo = obj.GetType();

			while(!typeInfo.IsGenericType)
			{
				if (typeof (IDictionary).IsAssignableFrom(typeInfo.BaseType))
				{
					var members = typeInfo.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

					intDeserializeClass(xmlReader, ref obj, attr, nsParentDict, members);
					typeInfo = typeInfo.BaseType;
				}
				else
					throw new XmlSerializerException(this, $"Неудалось получить базовый класс Dictionary");
			}

			var dict = (IDictionary)obj;
			var arg = typeInfo.GenericTypeArguments;
			dict.Clear();

			var elementName = attr.ElementName; 
			var propertyInfo = attr.ElementType; 
			attr.ElementType = arg[1];
			attr.ElementName = GetMememberName(attr.ElementType);

			string itemName = "Item";

			if (propertyInfo.IsDefined(typeof(XmlArrayItemAttribute), false))
			{
				var a = (XmlArrayItemAttribute)propertyInfo.GetCustomAttributes(typeof(XmlArrayItemAttribute), false)[0];

				AddNamespaceToAttr(ref attr, a.Namespace);
				itemName = a.ElementName;
			}

			var nsDict = getNamespaces(xmlReader);
			nsDict.AddRange(nsParentDict);

			// все элементы Item
			while (ReadNode(xmlReader))
			{
				var xmlElementName = xmlReader.LocalName;
				var xmlPrefix = xmlReader.Prefix;

				if (!attr.Namespace.ContainsValue(nsDict[xmlPrefix]) && !attr.ParentNamespace.ContainsValue(nsDict[xmlPrefix]))
					throw new XmlSerializerException(this, $"Неверное пространство имен {xmlPrefix} для {xmlElementName}");


				// Считываем Элемент - проверяем завершение Dictionary
				if (xmlReader.NodeType == XmlNodeType.EndElement)
				{
					// Если закончился элемент
					if (xmlElementName == itemName) continue;

					// Если закончился Dictionaty
					if (xmlElementName == elementName) break;

					throw new XmlSerializerException(this, $"Обнаружен закрывающий элемент </{xmlElementName}>, ожидался </{itemName}>");
				}

				// Иначе неизвестный тэг
				if (xmlElementName != itemName)
					throw new XmlSerializerException(this, $"Неверный элемент Dictionary <{xmlElementName}>. Элемент должен начинатся с тэга <{itemName}>");

				var key = Convert.ChangeType(xmlReader.GetAttribute("key"), (Type)arg[0]);

				// Пропускаем Item
				ReadNode(xmlReader);
				
				if (xmlReader.NodeType == XmlNodeType.EndElement)
					continue;

				var type = xmlReader.GetAttribute(TypeAttribute);
				if (!String.IsNullOrEmpty(type))
				{
					attr.ElementType = Type.GetType(type);
					if (attr.ElementType == null) 
						throw new XmlSerializerException(this, $"Неудалось обнаружить тип объекта {type}");
					attr.ElementName= String.Empty;
					attr = GetXmlAttribute(attr.ElementType, attr);
				}

				object itemObject = null;
				intDeserialize(xmlReader, ref itemObject, attr.ElementType, attr, nsParentDict);
				
				if (itemObject != null)
					dict.Add(key, itemObject);

				ReadNode(xmlReader);
			};
		}

		private void intDeserializeList(XmlReader xmlReader, ref object obj, CustomXmlAttribute attr, CustomXmlNamespace nsParentDict = null)
		{
			var typeInfo = obj.GetType(); 

			while (!typeInfo.IsGenericType)
			{
				if (typeof(IList).IsAssignableFrom(typeInfo.BaseType))
				{
					var members = typeInfo.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
					intDeserializeClass(xmlReader, ref obj, attr, nsParentDict, members);
					typeInfo = typeInfo.BaseType;
				}
				else
					throw new XmlSerializerException(this, $"Неудалось получить базовый класс List");
			}

			var elementName = attr.ElementName;
			var propertyInfo = attr.ElementType;

			attr.ElementType = typeInfo.GenericTypeArguments[0];
			attr.ElementName = GetMememberName(attr.ElementType);

			if (propertyInfo.IsDefined(typeof (XmlArrayItemAttribute), false))
			{
				var a = (XmlArrayItemAttribute) propertyInfo.GetCustomAttributes(typeof (XmlArrayItemAttribute), false)[0];

				AddNamespaceToAttr(ref attr, a.Namespace);
				attr.ElementName = a.ElementName;
				attr.IsNullable = a.IsNullable;
			}

			var nsDict = getNamespaces(xmlReader);
			nsDict.AddRange(nsParentDict);

			var list = (IList) obj;
			list.Clear();

			while (ReadNode(xmlReader))
			{

				var xmlElementName = xmlReader.LocalName;
				var xmlPrefix = xmlReader.Prefix;

				var type = xmlReader.GetAttribute(TypeAttribute);
				if (!String.IsNullOrEmpty(type))
				{
					attr.ElementType = Type.GetType(type);
					if (attr.ElementType == null) 
						throw new XmlSerializerException(this, $"Неудалось обнаружить тип объекта {type}");
					attr.ElementName = String.Empty;
					attr = GetXmlAttribute(attr.ElementType, attr);
				}


				if (!attr.Namespace.ContainsValue(nsDict[xmlPrefix]) &&
					!attr.ParentNamespace.ContainsValue(nsDict[xmlPrefix]))
					throw new XmlSerializerException(this, $"Неверное пространство имен {xmlPrefix} для {xmlElementName}");


				// Считываем Элемент - проверяем завершение List c XmlArrayItemAttribute
				if (xmlReader.NodeType == XmlNodeType.EndElement)
				{
					// Если закончился List
					if (xmlElementName == elementName) break;

					// Если завершен элемент
					if (xmlElementName == attr.ElementName) continue;

					throw new XmlSerializerException(this, $"Обнаружен закрывающий элемент </{xmlElementName}>. Ожидался </{attr}>");
				}

				if (xmlReader.NodeType != XmlNodeType.Element) continue;

				if (xmlElementName != attr.ElementName)
					throw new XmlSerializerException(this, $"Неверный элемент списка <{xmlElementName}>. Элемент списка должен начинаться с <{attr.ElementName}>");

				object itemObject = null;
				intDeserialize(xmlReader, ref itemObject, getType(attr.ElementType), attr, nsDict);

				if (itemObject != null)
					list.Add(itemObject);
			};
		}

		private void intDeserializeArray(XmlReader xmlReader, ref object obj, CustomXmlAttribute attr, CustomXmlNamespace nsParentDict = null)
		{
			// создаем список - и добавляем массив в список - потом создадим массив объектов и присвоим
			var typeInfo = obj.GetType();

			// Получаем тип массива (возвращаемого значения у Метода Get
			var propertyInfo = typeInfo.GetMethod("Get");
			if (propertyInfo == null) throw new XmlSerializerException(this, $"Неудалось определить тип массива");

			// создаем динамически List< с типом ReturnType >
			Type listType = typeof(List<>).MakeGenericType(new[] { propertyInfo.ReturnType });
			var o = _Type.CreateObject(listType);

			// Десериализуем List - мы не знаем размер массива
			intDeserializeList(xmlReader, ref o, attr, nsParentDict);
			obj = ((List<object>)o).ToArray(); 
		}

		// Функция загрузки класса с помощью десериализации
		public static T LoadFromXml<T>(string fileName)
		{
			// Load object from a file named Word_Reort.xml in XML format.
			var xmlFormat = new CustomXmlSerializer(typeof(T));

			using (var fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				try
				{
					var xml = (T)xmlFormat.Deserialize(fStream);
					fStream.Close();
					return xml;
				}
				catch (Exception e)
				{
					fStream.Close();
					fStream.Dispose();
					throw new XmlSerializerException(null, $"Произошла ошибка при десериализации объекта {typeof(T).Name} из Xml файла '{fileName}'\n{e.Message}");
				}
			}
		}
	}
}
