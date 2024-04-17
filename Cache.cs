/// Cache.cs
/// ActivistInvestor / Tony T

using System;
using System.Collections.Generic;
using System.Collections;

namespace Autodesk.AutoCAD.DatabaseServices
{
   /// <summary>
   /// Implments a Dictionary of key/value pairs that
   /// is implicitly populated using a user-supplied
   /// selector delegate that produces values for keys
   /// that do not already exist in the map.
   /// 
   /// This type was intended to be used as a read-only
   /// dictionary whose content is produced by requests
   /// for values for given keys. However, it can also be 
   /// used like a standard Dictionary<TKey, TValue> to 
   /// add keys and values or modify existing values as 
   /// well. The Invalidate() methods are synonyms for 
   /// the Clear() and Remove() methods of the standard
   /// Dictionary<TKey, TValue>
   /// </summary>
   /// <typeparam name="TKey">The type of the key used
   /// to retreive values</typeparam>
   /// <typeparam name="TValue">The type of the values</typeparam>

   public class Cache<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
   {
      Dictionary<TKey, TValue> map;
      Func<TKey, TValue> selector;

      public Cache(Func<TKey, TValue> keySelector, IEqualityComparer<TKey> comparer = null)
      {
         if(keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
         this.selector = keySelector;
         this.map = new Dictionary<TKey, TValue>(comparer);
      }

      public TValue this[TKey key]
      {
         get
         {
            return GetValue(key);
         }
         set
         {
            map[key] = value;
         }
      }

      /// <summary>
      /// Invalidates (e.g., removes) the cached value
      /// for the given key.
      /// </summary>

      public bool Invalidate(TKey key) => map.Remove(key);

      /// <summary>
      /// Invalidates (e.g., clears) the entire cache.
      /// </summary>
      public void Invalidate() => map.Clear();

      public int Count => map.Count;

      public bool Contains(TKey key) => map.ContainsKey(key);

      public TValue GetValue(TKey key)
      {
         if(!map.TryGetValue(key, out var result))
            map[key] = result = selector(key);
         return result;
      }

      public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
      {
         return ((IEnumerable<KeyValuePair<TKey, TValue>>)map).GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return ((IEnumerable)map).GetEnumerator();
      }
   }
}

