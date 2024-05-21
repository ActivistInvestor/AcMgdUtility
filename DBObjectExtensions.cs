/// DBObjectExtensions.cs  
/// 
/// ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
/// Source location:
/// 
///     https://github.com/ActivistInvestor/AcMgdUtility/blob/main/DBObjectExtensions.cs
///     
/// A collection of some old helper APIs that support 
/// access/querying of the contents of AutoCAD drawings 
/// using LINQ

using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using System.Collections;

namespace Autodesk.AutoCAD.DatabaseServices.Linq
{
   public static partial class DBObjectExtensions
   {
      /// <summary>
      /// Internal worker for the GetObjects() family
      /// of extension methods.
      /// 
      /// The documentation for public GetObjects() extension 
      /// methods that delegate to this method are typical.
      /// </summary>
      /// <typeparam name="T">The type of DBObject to enumerate</typeparam>
      /// <param name="ids">An IEnumerable that enumerates ObjectIds</param>
      /// <param name="trans">The transaction to use in the operation</param>
      /// <param name="mode">The OpenMode to open objects with</param>
      /// <param name="exact">A value indicating if enumerated objects must 
      /// be the exact type of the non-abstract generic argument (true), or 
      /// can be any type derived from the generic argument (false)</param>
      /// <param name="openLocked">A value indicating if entities on locked
      /// layers should be opened for write.</param>
      /// <returns>A sequence of opened DBObjects</returns>
      /// <exception cref="ArgumentNullException"></exception>

      static IEnumerable<T> GetObjectsCore<T>(IEnumerable ids, Transaction trans,
         OpenMode mode = OpenMode.ForRead,
         bool exact = false,
         bool openErased = false,
         bool openLocked = false) where T : DBObject
      {
         if(ids == null)
            throw new ArgumentNullException(nameof(ids));
         if(trans == null)
            throw new ArgumentNullException(nameof(trans));
         Func<ObjectId, bool> predicate = GetObjectIdPredicate<T>(exact);
         foreach(ObjectId id in ids)
         {
            if(predicate(id))
               yield return (T)trans.GetObject(id, mode, openErased, openLocked);
         }
      }

      /// <summary>
      /// Returns a predicate function that takes an ObjectId
      /// as an argument and returns a value indicating if it
      /// represents a DBObject whose type matches the generic 
      /// argument type exactly, or is a type derived from the
      /// generic argument type, depending on the exact argument.
      /// 
      /// The returned predicate uses a generic type that stores
      /// the runtime class associated with the generic argument
      /// type that allows it to avoid capturing a local variable.
      /// 
      /// In the method, 'RXClass<T>.Value' is merely a reference
      /// to a static field of a static type.
      /// </summary>
      /// <typeparam name="T">The type of DBObject to match</typeparam>
      /// <param name="exactMatch">A value indicating if the
      /// DBObject represented by an ObjectId must be equal to
      /// the type of the generic argument, or be a type that
      /// is derived from same. If the generic argument type is
      /// abstract, this argument is ignored and is effectively-
      /// false</param>
      /// <returns>A value indicating if the ObjectId matches</returns>

      public static Func<ObjectId, bool> GetObjectIdPredicate<T>(
         bool exactMatch = false) where T : DBObject
      {
         if(exactMatch && !typeof(T).IsAbstract)
            return static id => id.ObjectClass == RXClass<T>.Value; 
         else
            return static id => id.ObjectClass.IsDerivedFrom(RXClass<T>.Value);
      }

      /// <summary>
      /// Allows the delegates returned by the above
      /// method to avoid having to capture a local 
      /// variable (faster than referencing same).
      /// </summary>

      public static class RXClass<T> where T : RXObject
      {
         public static readonly RXClass Value = RXObject.GetClass(typeof(T));
      }

      /// <summary>
      /// A version of GetObjects() that targets BlockTableRecords
      /// </summary>
      /// <param name="blockTableRecord">The BlockTableRecord from
      /// which to retrieve the entities from.</param>
      /// 
      /// See the GetObjectsCore<T>() method for a desription of 
      /// all other parameters.

      public static IEnumerable<T> GetObjects<T>(this BlockTableRecord blockTableRecord,
            Transaction tr,
            OpenMode mode = OpenMode.ForRead,
            bool exact = false,
            bool openLocked = false) where T : Entity
      {
         if(blockTableRecord == null)
            throw new ArgumentNullException(nameof(blockTableRecord));
         return GetObjectsCore<T>(blockTableRecord, tr, mode, exact, false, openLocked);
      }

      /// <summary>
      /// A version of GetObjects() that targets ObjectIdCollection
      /// </summary>
      /// <param name="ids">The ObjectIdCollection containing the
      /// ObjectIds of the objects to be opened and returned</param>
      /// 
      /// See the GetObjectsCore<T>() method for a desription of 
      /// all other parameters.

      public static IEnumerable<T> GetObjects<T>(this ObjectIdCollection ids,
            Transaction tr,
            OpenMode mode = OpenMode.ForRead,
            bool exact = false,
            bool openLocked = false)
         where T : DBObject
      {
         if(ids == null)
            throw new ArgumentNullException(nameof(ids));
         return GetObjectsCore<T>(ids, tr, mode, exact, false, openLocked);
      }

      /// <summary>
      /// A version of GetObjects() that targets IEnumerable<ObjectId>
      /// </summary>
      /// <parm name="ids">The sequence of ObjectIds that are to 
      /// be opened and returned</parm>
      /// 
      /// See the GetObjectsCore<T>() method for a desription of 
      /// all other parameters.

      public static IEnumerable<T> GetObjects<T>(this IEnumerable<ObjectId> ids,
            Transaction tr,
            OpenMode mode = OpenMode.ForRead,
            bool exact = false,
            bool openLocked = false) where T : DBObject
      {
         if(ids == null)
            throw new ArgumentNullException(nameof(ids));
         return GetObjectsCore<T>(ids, tr, mode, exact, false, openLocked);
      }

      /// <summary>
      /// An overload of GetObjects() that targets SymbolTables
      /// </summary>
      /// <param name="table">The SymbolTable whose contents are
      /// to be opened and enumerated</param>
      /// <param name="includingErased">A value indicating if
      /// erased entries should be included.</param>
      /// 
      /// Note: Do NOT pass the result of SymbolTable.IncludingErased
      /// to this method, as it will access that property internally
      /// if the includingErased argument is true.
      /// 
      /// See the GetObjectsCore<T>() method for a desription of 
      /// all other parameters.

      public static IEnumerable<T> GetObjects<T>(this SymbolTable table,
         Transaction tr,
         OpenMode mode = OpenMode.ForRead,
         bool includingErased = false) where T : SymbolTableRecord
      {
         if(includingErased)
            table = table.IncludingErased;
         return GetObjectsCore<T>(table, tr, mode, includingErased, false);
      }

      /// <summary>
      /// Returns a sequence of DBObjects of type T, that are
      /// owned by the DBObject whose ObjectId this method is
      /// invoked on. 
      /// 
      /// The DBObject which this method is invoked on must 
      /// be an IEnumerable that enumerates ObjectIds (e.g., 
      /// SymbolTable, BlockTableRecord, etc.).
      /// 
      /// External use of this method is not recommended.
      /// 
      /// </summary>
      /// <typeparam name="T">The type of elements to enumerate</typeparam>
      /// <param name="ownerId">The ObjectId of a DBObject that
      /// enumerates ObjectIds</param>
      /// 
      /// See the GetObjectsCore<T>() method for a desription of 
      /// all other parameters.

      static IEnumerable<T> GetObjects<T>(ObjectId ownerId,
         Transaction tr,
         OpenMode mode = OpenMode.ForRead,
         bool exact = false,
         bool openLocked = false) where T : DBObject
      {
         if(tr == null)
            throw new ArgumentNullException(nameof(tr));
         if(ownerId.IsNull)
            throw new ArgumentNullException(nameof(ownerId));
         DBObject owner = tr.GetObject(ownerId, OpenMode.ForRead, false, false);
         if(!(owner is IEnumerable enumerable))
            throw new ArgumentException($"Owner not enumerable: {owner.GetType().Name}");
         var first = enumerable.First();
         if(first == null)
            return Enumerable.Empty<T>();
         if(!(first is ObjectId id))
            throw new ArgumentException($"Invalid element type: {first?.GetType().Name ?? "(null)"}");
         return GetObjectsCore<T>(enumerable, tr, mode, exact, false, openLocked);
      }

      /// <summary>
      /// Upgrades the OpenMode of a sequence of DBObjects to
      /// OpenMode.ForWrite. If a Transaction is provided the
      /// objects are upgraded using the Transaction, otherwise
      /// the objects are upgraded using UpgradeOpen(). When a
      /// transaction is provided, the objects are upgraded for
      /// write even if they are entities that reside on locked
      /// layers.
      /// </summary>
      /// <typeparam name="T">The type of the elements in the
      /// output and resulting sequences</typeparam>
      /// <param name="source">The input sequence of DBObjects</param>
      /// <param name="tr">The transaction to use in the operation.
      /// If a Transaction is provided, the objects will be upgraded
      /// to OpenMode.ForWrite even if they are entities residing on
      /// a locked layer</param>
      /// <returns>The input sequence upgraded to OpenMode.ForWrite</returns>

      public static IEnumerable<T> UpgradeOpen<T>(this IEnumerable<T> source, Transaction tr = null)
         where T : DBObject
      {
         if(source == null)
            throw new ArgumentNullException(nameof(source));
         foreach(T obj in source)
         {
            if(!obj.IsWriteEnabled)
            {
               if(tr != null)
                  tr.GetObject(obj.ObjectId, OpenMode.ForWrite, false, true);
               else
                  obj.UpgradeOpen();
            }
            yield return obj;
         }
      }

      static bool IsObjectIdIterator(IEnumerable enumerable)
      {
         IEnumerator e = enumerable.GetEnumerator();
         try
         {
            return enumerable.GetEnumerator().GetType().Name == "ObjectIterator";
         }
         finally
         {
            (e as IDisposable)?.Dispose();
         }
      }

      //////////////////////////////////////////////////////////////////
      /// From ObjectIdExtensions.cs
      ///
      /// <summary>
      /// An extension of ObjectId that opens the ObjectId and
      /// casts it to the specified argument type (no checking
      /// is done to verify that the ObjectId is compatible).
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="tr"></param>
      /// <param name="id"></param>
      /// <param name="mode"></param>
      /// <returns></returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static T GetObject<T>(this ObjectId id,
            Transaction tr,
            OpenMode mode = OpenMode.ForRead,
            bool openOnLockedLayer = false) where T : DBObject
      {
         if(tr == null)
            throw new ArgumentNullException(nameof(tr));
         if(id.IsNull)
            throw new ArgumentNullException(nameof(id));
         return (T)tr.GetObject(id, mode, false, openOnLockedLayer);
      }

      public static T GetObject<T>(this Transaction tr,
            ObjectId id,
            OpenMode mode = OpenMode.ForRead,
            bool openOnLockedLayer = false) where T : DBObject
      {
         if(tr == null)
            throw new ArgumentNullException(nameof(tr));
         if(id.IsNull) 
            throw new ArgumentNullException(nameof(id));
         return (T)tr.GetObject(id, mode, false, openOnLockedLayer);
      }

      /// Higher-level convenience methods built on 
      /// top of the above APIs.

      /// <summary>
      /// Returns a sequence of entities from the given database's
      /// model space. The type of the generic argument is used
      /// to filter the types of entities that are produced.
      /// </summary>
      /// <typeparam name="T">The type of entity to return</typeparam>
      /// <param name="db">The target Database</param>
      /// 
      /// See the GetObjectsCore<T>() method for a desription of 
      /// all other parameters.
      /// <exception cref="ArgumentNullException"></exception>

      public static IEnumerable<T> GetModelSpaceObjects<T>(this Database db,
         Transaction tr,
         OpenMode mode = OpenMode.ForRead,
         bool exact = false,
         bool openLocked = false) where T : Entity
      {
         if(db == null)
            throw new ArgumentNullException(nameof(db));
         return SymbolUtilityServices.GetBlockModelSpaceId(db)
            .GetObject<BlockTableRecord>(tr)
            .GetObjects<T>(tr, mode, exact, openLocked);
      }

      /// <summary>
      /// Non-generic implementation of the above that gets all
      /// entities from model space:
      /// </summary>

      public static IEnumerable<Entity> GetModelSpaceObjects(this Database db,
         Transaction tr,
         OpenMode mode = OpenMode.ForRead,
         bool exact = false,
         bool openLocked = false)
      {
         return GetModelSpaceObjects<Entity>(db, tr, mode, exact, openLocked);
      }


      /// <summary>
      /// Returns a sequence containing entities from all paper
      /// space layout blocks, excluding the model tab.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="db">The Database to obtain the objects from</param>
      /// 
      /// See the GetObjectsCore<T>() method for a desription of 
      /// all other parameters.

      public static IEnumerable<T> GetPaperSpaceObjects<T>(this Database db,
      Transaction tr,
      OpenMode mode = OpenMode.ForRead,
      bool exact = false,
      bool openLocked = false) where T : Entity
      {
         if(db == null)
            throw new ArgumentNullException(nameof(db));
         if(tr == null)
            throw new ArgumentNullException(nameof(tr));
         var idModel = SymbolUtilityServices.GetBlockModelSpaceId(db);
         return db.BlockTableId.GetObject<BlockTable>(tr)
            .GetObjects<BlockTableRecord>(tr)
            .Where(btr => btr.IsLayout && btr.ObjectId != idModel)
            .SelectMany(btr => btr.GetObjects<T>(tr, mode, exact, openLocked));
      }

      /// <summary>
      /// Get all references to the given BlockTableRecord,
      /// including dynamic block references.
      /// </summary>

      public static IEnumerable<BlockReference> GetBlockReferences(this BlockTableRecord btr, Transaction tr, OpenMode mode = OpenMode.ForRead, bool directOnly = true)
      {
         if(btr == null)
            throw new ArgumentNullException(nameof(btr));
         if(tr == null)
            throw new ArgumentNullException(nameof(tr));
         ObjectIdCollection ids = btr.GetBlockReferenceIds(directOnly, true);
         int cnt = ids.Count;
         for(int i = 0; i < cnt; i++)
         {
            yield return (BlockReference)tr.GetObject(ids[i], mode, false, false);
         }
         if(btr.IsDynamicBlock)
         {
            ObjectIdCollection blockIds = btr.GetAnonymousBlockIds();
            cnt = blockIds.Count;
            for(int i = 0; i < cnt; i++)
            {
               BlockTableRecord btr2 = blockIds[i].GetObject<BlockTableRecord>(tr);
               ids = btr2.GetBlockReferenceIds(directOnly, true);
               int cnt2 = ids.Count;
               for(int j = 0; j < cnt2; j++)
               {
                  yield return (BlockReference)tr.GetObject(ids[j], mode, false, false);
               }
            }
         }
      }

      /// <summary>
      /// Get all AttributeReferences with the given tag, from
      /// every insertion of the given block.
      /// </summary>

      public static IEnumerable<AttributeReference> GetAttributeReferences(
         this BlockTableRecord btr, 
         Transaction tr, 
         string tag)
      {
         if(btr == null)
            throw new ArgumentNullException(nameof(btr));
         if(tr == null)
            throw new ArgumentNullException(nameof(tr));
         string s = tag.ToUpper();
         foreach(var blkref in btr.GetBlockReferences(tr))
         {
            var attref = blkref.GetAttributes(tr).FirstOrDefault(a => a.Tag.ToUpper() == s);
            if(attref != null)
               yield return attref;
         }
      }

      /// <summary>
      /// Get AttributeReferences from the given block reference (lazy).
      /// Can enumerate AttributeReferences of database-resident and
      /// non-database resident BlockReferences.
      /// </summary>

      public static IEnumerable<AttributeReference> GetAttributes(this BlockReference blkref, Transaction tr, OpenMode mode = OpenMode.ForRead)
      {
         if(blkref == null)
            throw new ArgumentNullException(nameof(blkref));
         if(tr == null)
            throw new ArgumentNullException(nameof(tr));
         var objects = blkref.AttributeCollection.Cast<object>();
         object first = blkref.AttributeCollection.First();
         if(first != null)
         {
            if(first is AttributeReference)
            {
               foreach(AttributeReference attref in blkref.AttributeCollection)
                  yield return attref;
            }
            else
            {
               foreach(ObjectId id in blkref.AttributeCollection)
                  yield return (AttributeReference)tr.GetObject(id, mode, false, false);
            }
         }
      }

      /// <summary>
      /// Returns a Dictionary<string, AttributeReference> containinng
      /// all AttributeReferences for the given block reference, keyed
      /// to each AttributeReference's Tag.
      /// </summary>
      /// <param name="blkref"></param>
      /// <param name="tr"></param>
      /// <param name="mode"></param>
      /// <returns></returns>

      public static Dictionary<string, AttributeReference> GetAllAttributes(
         this BlockReference blkref, 
         Transaction tr, 
         OpenMode mode = OpenMode.ForRead)
      {
         return blkref.GetAttributes(tr, mode)
            .ToDictionary(att => att.Tag.ToUpper(), att => att);
      }

      public static Dictionary<string, string> GetAttributeValues(
         this BlockReference blkref,
         Transaction tr)
      {
         return blkref.GetAttributes(tr)
            .ToDictionary(att => att.Tag.ToUpper(), att => att.TextString);
      }

      // Helper methods

      public static object First(this IEnumerable enumerable)
      {
         if(enumerable == null)
            throw new ArgumentNullException(nameof(enumerable));
         var e = enumerable.GetEnumerator();
         try
         {
            if(e.MoveNext())
               return e.Current;
            else
               return null;
         }
         finally
         {
            (e as IDisposable)?.Dispose();
         }
      }

   }




}


