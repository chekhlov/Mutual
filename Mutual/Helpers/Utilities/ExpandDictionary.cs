using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _Type = Mutual.Helpers.Utilities._Type;


namespace Mutual.Helpers.Utilities
{
    // Класс расширяет возможности Dictionary
    public static class ExpandDictionary
    {
 
        /// <summary>
         /// Добавление в Dictionary другого Dictionary
         /// </summary>
         /// <param name="source">Исходный Dictionary</param>
         /// <param name="addDict">Добавляемый Dictionary</param>
         /// <typeparam name="T">Тип Key</typeparam>
         /// <typeparam name="Q">Тип Value</typeparam>
         /// <returns></returns>
         public static Dictionary<T, Q> AddRange<T,Q>(this Dictionary<T, Q> source, Dictionary<T, Q> addDict)
         {
             foreach (var item in addDict)
             {
                 if (!source.ContainsKey(item.Key)) 
                     source.Add(item.Key, item.Value);
             }
 
             return source;
         }
 
         /// <summary>
         /// Удаление из Dictionary другого Dictionary
         /// </summary>
         /// <param name="source">Исходный Dictionary</param>
         /// <param name="addDict">Удаляемый Dictionary</param>
         /// <typeparam name="T">Тип Key</typeparam>
         /// <typeparam name="Q">Тип Value</typeparam>
         /// <returns></returns>
         public static Dictionary<T, Q> RemoveRange<T,Q>(this Dictionary<T, Q> source, Dictionary<T, Q> removeDict)
         {
             foreach (var item in removeDict)
             {
                 if (!source.ContainsKey(item.Key)) 
                     source.Remove(item.Key);
             }
 
             return source;
         }
 
         /// <summary>
         /// Поиск ключа по значению
         /// </summary>
         /// <param name="source">Исходный Dictionary</param>
         /// <param name="value">Искомое значение</param> 
         /// <typeparam name="T">Тип Key</typeparam>
         /// <typeparam name="Q">Тип Value</typeparam>
         /// <returns></returns>
         public static T GetKeyByValue<T,Q>(this Dictionary<T, Q> source, Q value)
         {
             // Создаем пустой объект типа Т
             T key = (T)_Type.CreateObject(typeof(T));
             if (source.ContainsValue(value))
                 key = source.FirstOrDefault(x => x.Value.Equals(value)).Key;
 
             return key;
         }
         
       /// <summary>
        /// Добавление в SortedDictionary другого SortedDictionary
        /// </summary>
        /// <param name="source">Исходный SortedDictionary</param>
        /// <param name="addDict">Добавляемый SortedDictionary</param>
        /// <typeparam name="T">Тип Key</typeparam>
        /// <typeparam name="Q">Тип Value</typeparam>
        /// <returns></returns>
        public static SortedDictionary<T, Q> AddRange<T,Q>(this SortedDictionary<T, Q> source, SortedDictionary<T, Q> addDict)
        {
            foreach (var item in addDict)
            {
                if (!source.ContainsKey(item.Key)) 
                    source.Add(item.Key, item.Value);
            }

            return source;
        }

        /// <summary>
        /// Удаление из SortedDictionary другого SortedDictionary
        /// </summary>
        /// <param name="source">Исходный SortedDictionary</param>
        /// <param name="addDict">Удаляемый SortedDictionary</param>
        /// <typeparam name="T">Тип Key</typeparam>
        /// <typeparam name="Q">Тип Value</typeparam>
        /// <returns></returns>
        public static SortedDictionary<T, Q> RemoveRange<T,Q>(this SortedDictionary<T, Q> source, SortedDictionary<T, Q> removeDict)
        {
            foreach (var item in removeDict)
            {
                if (!source.ContainsKey(item.Key)) 
                    source.Remove(item.Key);
            }

            return source;
        }

        /// <summary>
        /// Поиск ключа по значению
        /// </summary>
        /// <param name="source">Исходный SortedDictionary</param>
        /// <param name="value">Искомое значение</param> 
        /// <typeparam name="T">Тип Key</typeparam>
        /// <typeparam name="Q">Тип Value</typeparam>
        /// <returns></returns>
        public static T GetKeyByValue<T,Q>(this SortedDictionary<T, Q> source, Q value)
        {
            // Создаем пустой объект типа Т
            T key = (T)_Type.CreateObject(typeof(T));
            if (source.ContainsValue(value))
                key = source.FirstOrDefault(x => x.Value.Equals(value)).Key;

            return key;
        }
    }
}
