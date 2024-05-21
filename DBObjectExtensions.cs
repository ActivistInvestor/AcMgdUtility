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
/// using LINQ.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;

namespace Autodesk.AutoCAD.DatabaseServices.Linq
{
   /// <summary>
   /// Notes: These types do not deal gracefully with
   /// attempts to open entities that reside on locked 
   /// layers for write. 
   /// 
   /// The recommended practice is to open entities for 
   /// read, and then determine if they can or should be 
   /// upgraded to OpenMode.ForWrite, based on whether 
   /// the referenced layer is locked, and the specifics 
   /// of the use case.
   /// 
   /// The included UpgradeOpen<T>() method can be used 
   /// with a transaction to forcibly-upgrade an entity's 
   /// open mode to write even if the entity resides on a 
   /// locked layer.
   /// </summary>

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
      /// <typeparam name="TBase">The common base type of all elements
      /// that appear in the source</typeparam>
      /// <param name="source">An IEnumerable that enumerates ObjectIds</param>
      /// <param name="trans">The transaction to use in the operation</param>
      /// <param name="mode">The OpenMode to open objects with</param>
      /// <param name="exact">A value indicating if enumerated objects must 
      /// be the exact type of the non-abstract generic argument (true), or 
      /// can be any type derived from the generic argument (false)</param>
      /// <param name="openLocked">A value indicating if entities on locked
      /// layers should be opened for write.</param>
      /// <returns>A sequence of opened DBObjects</returns>
      /// <exception cref="ArgumentNullException"></exception>

      static IEnumerable<T> GetObjects<TBase, T>(IEnumerable source, 
            Transaction trans,
            OpenMode mode = OpenMode.ForRead,
            bool exact = false,
            bool openErased = false,
            bool openLocked = false) 

         where TBase : DBObject 
         where T : TBase 
      {
         if(source == null)
            throw new ArgumentNullException(nameof(source));
         if(trans == null)
            throw new ArgumentNullException(nameof(trans));
         if(source is DBObject dbObj && dbObj.Database != null)
            CheckTransaction(dbObj.Database, trans);
         if(typeof(T) != typeof(TBase))
         {
            Func<ObjectId, bool> predicate = GetObjectIdPredicate<T>(exact);
            foreach(ObjectId id in source)
            {
               if(predicate(id))
                  yield return (T)trans.GetObject(id, mode, openErased, openLocked);
            }
         }
         else
         {
            foreach(ObjectId id in source)
            {
               yield return (T)trans.GetObject(id, mode, openErased, openLocked);
            }
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
      /// type, allowing it to avoid an expensive local variable 
      /// capture.
      /// 
      /// In the method, 'RXClass<T>.Value' is merely a reference
      /// to a static field of a static type.
      /// </summary>
      /// <typeparam name="T">The type of DBObject to match</typeparam>
      /// <param name="exactMatch">A value indicating if the
      /// type of the DBObject represented by an ObjectId must 
      /// be equal to the type of the generic argument, or be a 
      /// type that is derived from same. 
      /// 
      /// If the generic argument type is abstract, this argument 
      /// is ignored and is effectively-false</param>
      /// <returns>A value indicating if the ObjectId matches</returns>

      public static Func<ObjectId, bool> GetObjectIdPredicate<T>(
         bool exactMatch = false) where T : DBObject
      {
         if(exactMatch && !typeof(T).IsAbstract)
            return id => id.ObjectClass == RXClass<T>.Value; 
         else
            return id => id.ObjectClass.IsDerivedFrom(RXClass<T>.Value);
      }

      /// <summary>
      /// A version of GetObjects() that targets BlockTableRecords,
      /// and enumerates block entities.
      /// </summary>
      /// <param name="source">The BlockTableRecord from
      /// which to retrieve the entities from.</param>
      /// 
      /// See the GetObjects<TBase, T>() method for a desription of 
      /// all other parameters.

      public static IEnumerable<T> GetObjects<T>(this BlockTableRecord source,
            Transaction trans,
            OpenMode mode = OpenMode.ForRead,
            bool exact = false,
            bool openLocked = false) 
         where T : Entity
      {
         return GetObjects<Entity, T>(source, trans, mode, exact, false, openLocked);
      }

      /// <summary>
      /// A version of GetObjects() targeting ObjectIdCollection,
      /// that enumerates DBObjects represented by the ObjectIds
      /// in the source collection.
      /// </summary>
      /// <param name="source">The ObjectIdCollection containing the
      /// ObjectIds of the objects to be opened and returned</param>
      /// 
      /// See the GetObjects<T, TBase>() method for a desription of 
      /// all other parameters.

      public static IEnumerable<T> GetObjects<T>(this ObjectIdCollection source,
            Transaction trans,
            OpenMode mode = OpenMode.ForRead,
            bool exact = false,
            bool openLocked = false)
         where T : DBObject
      {
         return GetObjects<DBObject, T>(source, trans, mode, exact, false, openLocked);
      }

      /// <summary>
      /// Can be used like the above method when it can be 
      /// assumed that all elements in the ObjectIdCollection 
      /// represent an Entity or a type derived from same.
      /// 
      /// This method can be used when the requested type is
      /// Entity, but should not be used if only a subset of
      /// the source entities should be returned, consisting
      /// of a specific derived type (use GetObjects<T>() in
      /// that case, with the derived type specified as the
      /// generic argument).
      /// </summary>

      public static IEnumerable<Entity> GetEntities(this ObjectIdCollection source,
         Transaction trans,
         OpenMode mode = OpenMode.ForRead,
         bool openLocked = false)
      {
         return GetObjects<Entity, Entity>(source, trans, mode, false, false, openLocked);
      }

      /// <summary>
      /// A version of GetObjects() that targets IEnumerable<ObjectId>
      /// that enumerates the DBObjects represented by the ObjectIds
      /// in the sequence.
      /// 
      /// The GetEntities() variant can be used when it can 
      /// be assumed that all elements in the source sequence 
      /// represent an Entity or a type derived from same, and
      /// the requested type of the resulting sequence is Entity.
      /// Using GetEntities() in that case allows the underlying
      /// worker method to take an optimized path.
      /// 
      /// Alternately, one can use this overload and specify
      /// Entity for <em>both</em> generic arguments, to achieve 
      /// the same result.
      /// </summary>
      /// <parm name="ids">The sequence of ObjectIds representing
      /// the entities that are to be opened and returned</parm>
      /// 
      /// See the GetObjects<TBase, T>() method for a description 
      /// of all other parameters.

      public static IEnumerable<T> GetObjects<T>(this IEnumerable<ObjectId> source,
            Transaction trans,
            OpenMode mode = OpenMode.ForRead,
            bool exact = false,
            bool openLocked = false) where T : DBObject
      {
         return GetObjects<DBObject, T>(source, trans, mode, exact, false, openLocked);
      }

      public static IEnumerable<Entity> GetEntities(this IEnumerable<ObjectId> source,
            Transaction trans,
            OpenMode mode = OpenMode.ForRead,
            bool openLocked = false) 
      {
         return GetObjects<Entity, Entity>(source, trans, mode, false, false, openLocked);
      }

      /// <summary>
      /// An overload of GetObjects() that targets SymbolTables,
      /// and enumerates the table's SymbolTableRecords.
      /// </summary>
      /// <param name="source">The SymbolTable whose contents are
      /// to be opened and enumerated</param>
      /// <param name="includingErased">A value indicating if
      /// erased entries should be included.</param>
      /// 
      /// Note: Do NOT pass the result of SymbolTable.IncludingErased
      /// to this method, as it will access that property internally
      /// if the includingErased argument is true.
      /// 
      /// See the GetObjects<TBase, T>() method for a description 
      /// of all other parameters.

      public static IEnumerable<T> GetObjects<T>(this SymbolTable source,
         Transaction trans,
         OpenMode mode = OpenMode.ForRead,
         bool includingErased = false) where T : SymbolTableRecord
      {
         if(source == null)
            throw new ArgumentNullException(nameof(source));
         CheckTransaction(source.Database, trans);
         if(includingErased)
            source = source.IncludingErased;
         return GetObjects<T, T>(source, trans, mode, includingErased, false);
      }

      /// <summary>
      /// Returns a sequence of SymbolTableRecord-based types
      /// from the Database, where the symbol table is determined
      /// by the generic argument type. E.g., to get records from
      /// the layer table, specify LayerTableRecord as the generic
      /// argument type.
      /// </summary>
      /// <typeparam name="T">The type of SymbolTableRecord to be
      /// returned, which also determines which SymbolTable is to 
      /// be accessed. This must be a concrete type derived from 
      /// the SymbolTableRecord type.</typeparam>
      /// <param name="db">The Database to access</param>
      /// <param name="trans">The transaction to use for the operation</param>
      /// <param name="mode">The OpenMode to open resulting objects in</param>
      /// <param name="includingErased">A value indicating if erased
      /// SymbolTableRecords should be included</param>
      /// <returns>A sequence of SymbolTableRecord-based elements</returns>

      public static IEnumerable<T> GetRecords<T>(this Database db, 
         Transaction trans, 
         OpenMode mode = OpenMode.ForRead,
         bool includingErased = false) where T : SymbolTableRecord
      {
         CheckTransaction(db, trans);
         if(typeof(T) == typeof(SymbolTableRecord))
            throw new ArgumentException("Requires a type derived from SymbolTableRecord");
         Func<Database, ObjectId> func;
         if(!tableAccessors.TryGetValue(typeof(T), out func))
            throw new ArgumentException($"Invalid SymbolTableRecord type: {typeof(T).Name}");
         return func(db).GetObject<SymbolTable>(trans)
            .GetObjects<T>(trans, mode, includingErased);
      }

      /// <summary>
      /// Upgrades the OpenMode of a sequence of DBObjects to
      /// OpenMode.ForWrite. If a Transaction is provided, the
      /// objects are upgraded using the Transaction, otherwise
      /// the objects are upgraded using UpgradeOpen(). When a
      /// transaction is provided, the objects are upgraded to
      /// OpenMode.ForWrite <em>even if they are entities that 
      /// reside on locked layers</em>.
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

      //////////////////////////////////////////////////////////////////
      /// From ObjectIdExtensions.cs
      ///
      /// <summary>
      /// An extension of ObjectId that opens the ObjectId and
      /// casts it to the specified argument type (no checking
      /// is done to verify that the ObjectId is compatible).
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="trans"></param>
      /// <param name="id"></param>
      /// <param name="mode"></param>
      /// <returns></returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static T GetObject<T>(this ObjectId id,
            Transaction trans,
            OpenMode mode = OpenMode.ForRead,
            bool openOnLockedLayer = false) where T : DBObject
      {
         if(trans == null)
            throw new ArgumentNullException(nameof(trans));
         if(id.IsNull)
            throw new ArgumentNullException(nameof(id));
         return (T)trans.GetObject(id, mode, false, openOnLockedLayer);
      }

      /// <summary>
      /// A version that targets Transactions:
      /// </summary>

      public static T GetObject<T>(this Transaction trans,
            ObjectId id,
            OpenMode mode = OpenMode.ForRead,
            bool openOnLockedLayer = false) where T : DBObject
      {
         if(trans == null)
            throw new ArgumentNullException(nameof(trans));
         if(id.IsNull) 
            throw new ArgumentNullException(nameof(id));
         return (T)trans.GetObject(id, mode, false, openOnLockedLayer);
      }

      /// What follows are high-level convenience methods 
      /// that are built on top of the above APIs.

      /// <summary>
      /// Returns a sequence of entities from the given database's
      /// model space. The type of the generic argument is used
      /// to filter the types of entities that are produced.
      /// </summary>
      /// <typeparam name="T">The type of entity to return</typeparam>
      /// <param name="db">The target Database</param>
      /// 
      /// See the GetObjects<TBase, T>() method for a desription of 
      /// all other parameters.
      /// <exception cref="ArgumentNullException"></exception>

      public static IEnumerable<T> GetModelSpaceObjects<T>(this Database db,
         Transaction trans,
         OpenMode mode = OpenMode.ForRead,
         bool exact = false,
         bool openLocked = false) where T : Entity
      {
         CheckTransaction(db, trans);
         return SymbolUtilityServices.GetBlockModelSpaceId(db)
            .GetObject<BlockTableRecord>(trans)
            .GetObjects<T>(trans, mode, exact, openLocked);
      }

      /// <summary>
      /// A common error is using the wrong Transaction manager
      /// to obtain a transaction for a Database that's not open
      /// in the editor. This attempts to check that.
      /// The check cannot be fully-performed without a depenence
      /// on AcMgd/AcCoreMgd.dll.
      /// </summary>
      /// <param name="db"></param>
      /// <param name="trans"></param>
      /// <exception cref="ArgumentNullException"></exception>
      /// <exception cref="ArgumentException"></exception>

      static void CheckTransaction(this Database db, Transaction trans)
      {
         if(trans == null)
            throw new ArgumentNullException(nameof(trans));
         if(db == null)
            throw new ArgumentNullException(nameof(db));
         if(trans is OpenCloseTransaction)
            return;
         if(trans.GetType() != typeof(Transaction))
            return; // can't perform check without pulling in AcMgd/AcCoreMgd
         if(trans.TransactionManager != db.TransactionManager)
            throw new ArgumentException("Transaction not from this Database");
      }

      static void CheckTransaction(object source, Transaction trans)
      {
         if(trans == null)
            throw new ArgumentNullException(nameof(trans));
         if(source == null)
            throw new ArgumentNullException(nameof(source));
         if(source is DBObject dbObject && dbObject.Database != null)
            CheckTransaction(dbObject.Database, trans);
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
      /// Returns a sequence of entities from the given database's
      /// current space (which could be either model space, a paper
      /// space layout, or any block if the block editor is active). 
      /// 
      /// The type of the generic argument is used to filter the types 
      /// of entities that are produced.
      /// </summary>
      /// <typeparam name="T">The type of entity to return</typeparam>
      /// <param name="db">The target Database</param>
      /// 
      /// See the GetObjects<TBase, T>() method for a desription of 
      /// all other parameters.
      /// <exception cref="ArgumentNullException"></exception>

      public static IEnumerable<T> GetCurrentSpaceObjects<T>(this Database db,
         Transaction tr,
         OpenMode mode = OpenMode.ForRead,
         bool exact = false,
         bool openLocked = false) where T : Entity
      {
         if(db == null)
            throw new ArgumentNullException(nameof(db));
         return db.CurrentSpaceId
            .GetObject<BlockTableRecord>(tr)
            .GetObjects<T>(tr, mode, exact, openLocked);
      }

      /// <summary>
      /// Non-generic version of GetCurrentSpaceObjects() that
      /// enumerates all objects in the current space.
      /// </summary>

      public static IEnumerable<Entity> GetCurrentSpaceObjects(this Database db,
         Transaction tr,
         OpenMode mode = OpenMode.ForRead,
         bool exact = false,
         bool openLocked = false)
      {
         return GetCurrentSpaceObjects<Entity>(db, tr, mode, exact, openLocked);
      }


      /// <summary>
      /// Returns a sequence containing entities from ALL paper
      /// space layout blocks.
      /// </summary>
      /// <typeparam name="T">The type of objects to enumerate</typeparam>
      /// <param name="db">The Database to obtain the objects from</param>
      /// 
      /// See the GetObjects<TBase, T>() method for a desription of 
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
      /// including anonymous dynamic block references.
      /// </summary>

      public static IEnumerable<BlockReference> GetBlockReferences(
         this BlockTableRecord btr, 
         Transaction tr, 
         OpenMode mode = OpenMode.ForRead, 
         bool directOnly = true)
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
      /// Returns a sequence containing all BlockReferences in
      /// the given Database whose names match the given pattern.
      /// 
      /// Anonymous dynamic block references are included if the
      /// dynamic block definition's name matches the pattern.
      /// </summary>
      /// <param name="db">The Database</param>
      /// <param name="pattern">A wcmatch-style pattern that
      /// matches the name of one or more blocks</param>
      /// <param name="tr">The transaction to use in the
      /// operation.</param>
      /// <param name="mode">The OpenMode to open the 
      /// BlockReferences in</param>
      /// <returns>A sequence containing all matching
      /// BlockReference objects.</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static IEnumerable<BlockReference> GetBlockReferences(
         this Database db, 
         string pattern,
         Transaction tr,
         OpenMode mode = OpenMode.ForRead)
      {
         if(db == null || db.IsDisposed)
            throw new ArgumentNullException(nameof(db));
         if(tr == null || tr.IsDisposed)
            throw new ArgumentNullException(nameof(tr));
         if(string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentNullException(nameof(pattern));

         return db.BlockTableId.GetObject<BlockTable>(tr)
            .GetObjects<BlockTableRecord>(tr)
            .Where(btr => btr.IsUserBlock() && btr.Name.Matches(pattern))
            .SelectMany(btr => btr.GetBlockReferences(tr, mode, true));
      }

      /// <summary>
      /// Get all AttributeReferences with the given tag from
      /// every insertion of the given BlockTableRecord.
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
      /// Can enumerate AttributeReferences of database resident and
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

      static bool IsUserBlock(this BlockTableRecord btr)
      {
         return !(btr.IsAnonymous || btr.IsLayout || btr.IsFromExternalReference || btr.IsDependent);
      }

      static bool Matches(this string str, string pattern, bool ignoreCase = true)
      {
         return Utils.WcMatchEx(str, pattern, ignoreCase);
      }

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

      static Dictionary<Type, Func<Database, ObjectId>> tableAccessors
         = new Dictionary<Type, Func<Database, ObjectId>>();

      static DBObjectExtensions()
      {
         tableAccessors[typeof(BlockTableRecord)] = db => db.BlockTableId;
         tableAccessors[typeof(LayerTableRecord)] = db => db.LayerTableId;
         tableAccessors[typeof(LinetypeTableRecord)] = db => db.LinetypeTableId;
         tableAccessors[typeof(ViewportTableRecord)] = db => db.ViewportTableId;
         tableAccessors[typeof(ViewTableRecord)] = db => db.ViewTableId;
         tableAccessors[typeof(DimStyleTableRecord)] = db => db.DimStyleTableId;
         tableAccessors[typeof(RegAppTableRecord)] = db => db.RegAppTableId;
         tableAccessors[typeof(TextStyleTableRecord)] = db => db.TextStyleTableId;
         tableAccessors[typeof(UcsTableRecord)] = db => db.UcsTableId;
      }

   }

}

namespace Autodesk.AutoCAD.Runtime
{
   /// <summary>
   /// Allows the delegates returned by methodsthat use an 
   /// RXClass to avoid having to capture a local variable,
   /// and eliminates the overhead of repeated calls to the
   /// RXObject.GetClass() method with the same argument.
   /// </summary>

   public static partial class RXClass<T> where T : RXObject
   {
      public static readonly RXClass Value = RXObject.GetClass(typeof(T));
   }
}


