#define NOACAD
// using Autodesk.AutoCAD.DatabaseServices;
// using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Autodesk.AutoCAD.ApplicationServices
{
   /// <summary>
   /// Partial conversion of TypedValueList methods to extension 
   /// methods that can target TypedValueList, or any type that 
   /// implements IList<TypedValue> (such as List<TypedValue>).
   /// 
   /// These classes are essentially the evolution of the original 
   /// TypedValueList class that was initially published here:
   /// 
   ///    http://www.theswamp.org/index.php?topic=14495.msg186823#msg186823
   /// 
   /// Significant new functionality has also been implemented as 
   /// extension methods.
   /// </summary>

   public static class TypedValueListExtensions
   {

      public static void Test()
      {
         List<TypedValue> list = new List<TypedValue>();
      }

      /// <summary>
      /// Validates an IList<TypedValue> as not being fixed-size 
      /// (e.g., it is not an array) and as having elements that 
      /// can be modified via the set indexer.
      /// 
      /// While arrays support IList<T>, they cannot be expanded.
      /// </summary>

      static IList<TypedValue> CheckIsResizable(IList<TypedValue> list)
      {
         if(list == null)
            throw new ArgumentNullException(nameof(list));
         if(list is TypedValue[] || list.IsReadOnly)
            throw new InvalidOperationException("the collection is read-only or not expandable");
         return list;
      }

      /// <summary>
      /// The original Add() overloads from TypedValueList:
      /// </summary>

      public static void Add(this IList<TypedValue> list, short typeCode, object value)
      {
         CheckIsResizable(list).Add(new TypedValue(typeCode, value));
      }

      public static void Add(this IList<TypedValue> list, LispDataType type, object value)
      {
         CheckIsResizable(list).Add(new TypedValue((short)type, value));
      }

      public static void Add(this IList<TypedValue> list, DxfCode code, object value)
      {
         CheckIsResizable(list).Add(new TypedValue((short)code, value));
      }

      /// <summary>
      /// New overload of AddRange() that adds one or more values 
      /// with a type code determined by ResultBuffer.ObjectsToResbuf();
      /// </summary>

#if(!NOACAD)
      public static void AddRange<T>(this IList<TypedValue> list, params T[] values)
      {
         CheckIsResizable(list);
         if(values != null && values.Length > 0)
         {
            var ptr = Autodesk.AutoCAD.Runtime.Marshaler.ObjectsToResbuf(values);
            if(ptr == IntPtr.Zero)
               throw new InvalidOperationException($"failed to convert {nameof(values)} to resbuf");
            ResultBuffer buffer = (ResultBuffer)DisposableWrapper.Create(typeof(ResultBuffer), ptr, true);
            AddRange(list, buffer.AsArray());
         }
      }

      /// <summary>
      /// Overload of the above that accepts values as an IEnumerable<T>
      /// </summary>

      public static void AddRange<T>(this IList<TypedValue> list, IEnumerable<T> values)
      {
         if(values == null)
            throw new ArgumentNullException(nameof(values));
         AddRange<T>(list, values as T[] ?? values.ToArray());
      }

#endif

      /// Adds a range of elements expressed as IEnumerable<TypedValue>

      static void AddRange(IList<TypedValue> list, IEnumerable<TypedValue> values)
      {
         CheckIsResizable(list);
         List<TypedValue>? tmp = list as List<TypedValue>;
         if(tmp != null)
         {
            tmp.AddRange(values);
         }
         else
         {
            foreach(TypedValue tv in values)
               list.Add(tv);
         }
      }

      /// <summary>
      /// Adds a range of elements expressed as ValueTyple(short, object):
      /// </summary>

      public static void AddRange(this IList<TypedValue> list, params (short code, object value)[] args)
      {
         CheckIsResizable(list);
         if(args == null)
            throw new ArgumentNullException(nameof(args));
         AddRange(list, args.Select(arg => new TypedValue(arg.code, arg.value)));
      }

      /// <summary>
      /// Adds a range of elements all having the same 
      /// given type code, and each having one of the 
      /// given values:
      /// 
      /// e.g.:
      /// <code>
      ///  
      ///    var list = new List<TypedValue>();
      ///    
      ///    list.AddRange(DxfCode.Text, "Moe", "Larry", "Curly");
      ///    
      /// Which is equivlaent to
      /// 
      ///    list.Add(DxfCode.Text, "Moe");
      ///    list.Add(DxfCode.Text, "Larry");
      ///    list.Add(DxfCode.Text, "Curly");
      ///    
      /// </code>
      /// 
      /// This method is overloaded with 2 variants x 3 versions, 
      /// varying by the type of the type code (LispDataType, DxfCode,
      /// and short), and by the form in which the values are provided
      /// (one as params and one as IEnumerable<T>).
      /// 
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="list"></param>
      /// <param name="code"></param>
      /// <param name="values"></param>

      public static void AddRange<T>(this IList<TypedValue> list, short code, params T[] values)
      {
         CheckIsResizable(list);
         if(values == null)
            throw new ArgumentNullException(nameof(values));
         if(values.Length > 0)
            AddRange(list, values.Select(v => new TypedValue(code, v)));
      }

      public static void AddRange<T>(this IList<TypedValue> list, short code, IEnumerable<T> values)
      {
         CheckIsResizable(list);
         if(values == null)
            throw new ArgumentNullException(nameof(values));
         if(values.Any())
            AddRange(list, values.Select(v => new TypedValue(code, v)));
      }

      public static void AddRange<T>(this IList<TypedValue> list, DxfCode code, params T[] values)
      {
         AddRange(list, (short)code, values);
      }

      public static void AddRange<T>(this IList<TypedValue> list, DxfCode code, IEnumerable<T> values)
      {
         AddRange(list, (short)code, values);
      }

      public static void AddRange<T>(this IList<TypedValue> list, LispDataType code, params T[] values)
      {
         AddRange(list, (short)code, values);
      }

      public static void AddRange<T>(this IList<TypedValue> list, LispDataType code, IEnumerable<T> values)
      {
         AddRange(list, (short)code, values);
      }

      /// <summary>
      /// Returns a sequence of IList<TypedValue> where each
      /// element represents a repeating sequence that starts
      /// with an element having the given typeCode.
      /// 
      /// Each returned List<TypedValue> contains the elements
      /// that start with an element having the given typeCode,
      /// and all elements following it, up to the next element
      /// having the given typeCode.
      /// 
      /// This method assumes that all repeating sequences have 
      /// an equal number of elements following each element that
      /// starts with the given typeCode.
      /// 
      /// Elements that precede the first occurence of an element
      /// with the given typeCode, and elements following the last 
      /// repeating sequence are excluded.
      /// 
      /// Example:
      /// <code>
      /// static void TestTypedValueListExtensions()
      /// {
      ///    TypedValueList list = new TypedValueList(
      /// 
      ///       (1, "Moe"),
      ///       (2, "Larry"),
      ///       (3, "Curly"),
      ///       (4, "Foo"),
      ///       (5, "Bar"),
      ///       (330, "Group 1"),
      ///       (10, 0.0),
      ///       (40, 0.25),
      ///       (210, 1.0),
      ///       (330, "Group 2"),
      ///       (10, 0.0),
      ///       (40, 0.25),
      ///       (210, 1.0),
      ///       (330, "Group 3"),
      ///       (10, 0.0),
      ///       (40, 0.25),
      ///       (210, 1.0),
      ///       (330, "Group 4"),
      ///       (10, 0.0),
      ///       (40, 0.25),
      ///       (210, 1.0),
      ///       (330, "Group 5"),
      ///       (10, 0.0),
      ///       (40, 0.25),
      ///       (210, 1.0),
      ///       (7, "Seven"),
      ///       (8, "Eight"),
      ///       (9, "Nine"),
      ///       (10, "Ten")
      ///    );
      /// 
      ///    var items = list.GroupBy(330);
      /// 
      ///    foreach(var item in items)
      ///    {
      ///       Console.WriteLine(item.ToString<short>());
      ///    }
      /// }
      /// 
      /// The above code will produce the following output:
      /// 
      ///   (330: Group 1) (10: 0) (40: 0.25) (210: 1)
      ///   (330: Group 2) (10: 0) (40: 0.25) (210: 1)
      ///   (330: Group 3) (10: 0) (40: 0.25) (210: 1)
      ///   (330: Group 4) (10: 0) (40: 0.25) (210: 1)
      ///   (330: Group 5) (10: 0) (40: 0.25) (210: 1)
      /// 
      /// In the above example, each repeating sequence starts
      /// with an element having the TypeCode 330, and ends with 
      /// an element having the TypeCode 210.
      /// </code>
      /// 
      /// </summary>

      public static IEnumerable<IList<TypedValue>> GroupBy(
         this IList<TypedValue> list, short typeCode)
      {
         int len = -1;
         TypedValueList sublist = null;
         using(var e = list.GetEnumerator())
         {
            while(e.MoveNext())
            {
               TypedValue tv = e.Current;
               if(tv.TypeCode == typeCode)
               {
                  sublist = new TypedValueList(tv);
                  break;
               }
            }
            while(e.MoveNext())
            {
               TypedValue tv = e.Current;
               if(tv.TypeCode != typeCode)
               {
                  sublist.Add(tv);
                  continue;
               }
               len = sublist.Count;
               yield return sublist;
               sublist = new TypedValueList(tv);
               sublist.Capacity = len;
               break;
            }
            while(e.MoveNext())
            {
               TypedValue tv = e.Current;
               if(tv.TypeCode == typeCode)
               {
                  yield return sublist;
                  sublist = new TypedValueList(tv);
                  sublist.Capacity = len;
                  continue;
               }
               else if(sublist.Count == len)
               {
                  yield return sublist;
                  break;
               }
               else
               {
                  sublist.Add(tv);
               }
            }
         }
      }

      //public static IEnumerable<IList<TypedValue>> GroupBy(
      //   this IList<TypedValue> list, short key, bool fixedLength = false)
      //{
      //   TypedValueList values = new TypedValueList();
      //   int len = -1;
      //   int cnt = 0;
      //   using(var e = list.GetEnumerator())
      //   {
      //      while(e.MoveNext())
      //      {
      //         TypedValue tv = e.Current;
      //         /// if we have a key or the lenght of the list == count
      //         if(tv.TypeCode == key || cnt > 0 && values.Count == len)
      //         {
      //            if(values.Count > 0) 
      //            {
      //               if(cnt > 0 && fixedLength && len < 0)
      //                  len = values.Count;
      //               yield return values;
      //               ++cnt;
      //               values = new TypedValueList(tv);
      //            }
      //         }
      //         values.Add(tv);
      //      }
      //   }
      //   yield return values;
      //}

      public static IEnumerable<TypedValue> Ungroup(this IEnumerable<IList<TypedValue>> list)
      {
         foreach(var sublist in list)
         {
            foreach(TypedValue tv in sublist)
               yield return tv;
         }
      }

      /// <summary>
      /// Returns the index of the first element having the given type code
      /// or -1 if no element having the given type code exists.
      /// </summary>

      public static int IndexOfType(this IList<TypedValue> list, short code)
      {
         for(int i = 0; i < list.Count; i++)
         {
            if(list[i].TypeCode == code)
               return i;
         }
         return -1;
      }

      /// <summary>
      /// Inserts one or more new elements into the existing liSt, all of
      /// which have the given type code, and each of which has one of the
      /// given values.
      /// 
      /// One element is inserted into the list for each provided value.
      /// 
      /// Note: This method is only applicable to List<TypedValue> or 
      /// TypedValueList. It cannot be used on any IList<TypedValue>.
      /// </summary>
      /// <param name="list">The List<TypedValue> to insert the items into</param>
      /// <param name="index">The index at which to insert the new item(s)</param>
      /// <param name="code">The type code assigned to all newly-inserted items</param>
      /// <param name="values">The values assigned to each newly-inserted item</param>

      public static void InsertRange(this List<TypedValue> list, int index, short code, params object[] values)
      {
         CheckIsResizable(list);
         IEnumerable<TypedValue> newItems = values.Select(val => new TypedValue(code, val));
         list.InsertRange(index, newItems);
      }

      /// <summary>
      /// Inserts the specified number of new elements into the list starting
      /// at the specified index. Each newly-inserted element has the specified
      /// type code, and a value produced by the given function, which takes the
      /// integer offset of the newly-inserted item, relative to the index argument.
      /// </summary>
      /// <param name="list">The List<TypedValue> to insert the items into</param>
      /// <param name="index">The index at which to insert the new item(s)</param>
      /// <param name="count">The number of elements to be inserted</param>
      /// <param name="code">The type code assigned to all newly-inserted items</param>
      /// <param name="func">A function that takes an integer offset from the
      /// index parameter, and returns the value to be assigned to the element</param>

      public static void InsertRange(this List<TypedValue> list, int index, int count, short code, Func<int, object> func)
      {
         CheckIsResizable(list);
         list.InsertRange(index, Enumerable.Range(0, count)
            .Select(i => new TypedValue(code, func(i))));
      }

      /// <summary>
      /// Returns a sequence of values of the 
      /// elements having the given type code.
      /// 
      /// While this can easily be done using Linq, 
      /// (e.g., Where(...).Select(...)), this is a 
      /// tad more effecient.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="list"></param>
      /// <param name="code"></param>
      /// <returns></returns>

      public static IEnumerable<T> ValuesOfType<T>(this IList<TypedValue> list, short code)
      {
         for(int i = 0; i < list.Count; i++)
         {
            TypedValue item = list[i];
            if(item.TypeCode == code)
               yield return (T)item.Value;
         }
      }

      /// <summary>
      /// Returns a sequence of indices of elements
      /// having the specified type code.
      /// </summary>

      public static IEnumerable<int> IndicesOfType(this IList<TypedValue> list, short code)
      {
         for(int i = 0; i < list.Count; i++)
         {
            if(list[i].TypeCode == code)
               yield return i;
         }
      }

      /// <summary>
      /// Gets the index of the nth occurence of an element 
      /// having the specified type code.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="list"></param>
      /// <param name="code">The type code to search for</param>
      /// <param name="index">The 0-based sub-index of the type.
      /// A value of 0 returns the first occurence of an element
      /// having the given type code. A negative value returns the 
      /// last occurence of the element having the given type code.</param>
      /// <returns>The index of the requested element having the given
      /// type code, or -1 if no element was found with the given index.</returns>

      public static int GetIndexOfTypeAt<T>(this IList<TypedValue> list, short code, int index)
      {
         int idx = -1;
         if(index < 0)
         {
            for(int i = list.Count - 1; i >= 0; i--)
            {
               if(list[i].TypeCode == code)
                  return i;
            }
            return -1;
         }
         else
         {
            for(int i = 0; i < list.Count; i++)
            {
               if(list[i].TypeCode == code && ++idx == index)
                  return i;
            }
            return -1;
         }
      }

      static void CheckIndex(this IList<TypedValue> list, int index)
      {
         if(index < 0 || index >= list.Count)
            throw new IndexOutOfRangeException(index.ToString());
      }

      public static T SetValueAt<T>(this IList<TypedValue> list, int index, T value)
      {
         CheckIsResizable(list);
         CheckIndex(list, index);
         var tv = list[index];
         if(!(tv.Value is T))
            throw new ArgumentException("type mismatch");
         list[index] = new TypedValue(tv.TypeCode, value);
         return (T)tv.Value;
      }

      /// <summary>
      /// Replaces the values of all elements having the given type code.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="list"></param>
      /// <param name="code"></param>
      /// <param name="converter">A function that takes the subindex of
      /// the element and the existing value, and returns the new value
      /// for the element. The subindex is the nth occurence of an element 
      /// having the given type code.</param>

      public static void ReplaceValuesOfType<T>(this IList<TypedValue> list, short code, Func<int, T, T> converter)
      {
         CheckIsResizable(list);
         int subindex = -1;
         for(int i = 0; i < list.Count; i++)
         {
            var tv = list[i];
            if(tv.TypeCode == code)
            {
               list[i] = new TypedValue(tv.TypeCode, converter(++subindex, (T)tv.Value));
            }
         }
      }

      public static string ToString(this IList<TypedValue> list)
      {
         return ToString<DxfCode>(list);
      }

      public static string ToString<T>(this IList<TypedValue> list, string delimiter = " ") where T : struct
      {
         if(list == null)
            throw new ArgumentNullException(nameof(list));
         Type type = typeof(T);
         if(! (type == typeof(DxfCode) || type == typeof(LispDataType) || type == typeof(short)))
            throw new ArgumentException("Invalid type");
         StringBuilder sb = new StringBuilder();
         bool flag = type.IsEnum;
         foreach(var tv in list)
         {
            object o = flag ? Enum.ToObject(type, tv.TypeCode) : tv.TypeCode;
            if(o != null)
               sb.Append($"({o}: {tv.Value})");
            else
               sb.Append($"({tv.TypeCode}: {tv.Value})");
            sb.Append(delimiter);
         }
         return sb.ToString();
      }
   }

}

/// static void TestTypedValueListExtensions()
/// {
///    TypedValueList list = new TypedValueList(
/// 
///       (1, "Moe"),
///       (2, "Larry"),
///       (3, "Curly"),
///       (4, "Foo"),
///       (5, "Bar"),
///       (330, "Group 1"),
///       (10, 0.0),
///       (40, 0.25),
///       (210, 1.0),
///       (330, "Group 2"),
///       (10, 0.0),
///       (40, 0.25),
///       (210, 1.0),
///       (330, "Group 3"),
///       (10, 0.0),
///       (40, 0.25),
///       (210, 1.0),
///       (330, "Group 4"),
///       (10, 0.0),
///       (40, 0.25),
///       (210, 1.0),
///       (330, "Group 5"),
///       (10, 0.0),
///       (40, 0.25),
///       (210, 1.0),
///       (7, "Seven"),
///       (8, "Eight"),
///       (9, "Nine"),
///       (10, "Ten")
///    );
/// 
///    var items = list.GroupBy(330);
/// 
///    foreach(var item in items)
///    {
///       TypedValue[] array = item.ToArray();
///       Console.WriteLine(array.ToString("({0}: {1})", " "));
///    }
/// }


