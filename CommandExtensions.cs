/// CommandExtensions.cs  ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
///
/// Source location:
/// 
///     https://github.com/ActivistInvestor/AcMgdUtility/blob/main/CommandExtensions.cs
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace Autodesk.AutoCAD.ApplicationServices.EditorExtensions
{
   /// <summary>
   /// Quickly hacked-together example showing how to build
   /// a version of the Editor's Command() method, that can
   /// collect the ObjectIds of all objects created by the
   /// command sequence executed by the method. Most of the
   /// code below exists and is included as-is, or has been
   /// extracted/adapted from existing solutions that address 
   /// the same problem addressed by this code. All extension 
   /// methods have been rolled-up into a single static type.
   /// 
   /// Includes code excerpted or adapted from:
   /// 
   ///   EditorExtensions.cs
   ///   NewObjectCollection.cs
   ///   RuntimeExtensions.cs
   ///   ObjectIdExtensions.cs
   ///   ObjectIdCollectionExtensions.cs
   ///   
   /// The included code was intended to be used as-is, from a
   /// consuming application. The author cannot not provide any
   /// support for modified or hacked versions of this source.
   /// </summary>

   public static class CommandExtensions
   {
      
      /// <summary>
      /// An overload of the Editor's Command() method that
      /// accepts an ObjectIdCollection, to which it adds the
      /// ObjectIds of all entities that were added to the
      /// current space by the executed commands.
      /// 
      /// The same ObjectIdCollection can be passed into to 
      /// multiple calls to this method if needed.
      /// 
      /// </summary>
      /// <param name="editor">The Editor of the active document</param>
      /// <param name="ids">The ObjectIdCollection to populate</param>
      /// <param name="args">The command arguments</param>
      /// <returns>The number of objects added to the collection</returns>

      public static int Command(this Editor editor, ObjectIdCollection ids, params object[] args)
      {
         return Command<Entity>(editor, ids, args);
      }

      /// An generic version of the above that allows filtering 
      /// by entity type. Only those types that are instances of
      /// the generic argument type are collected.
      /// 
      /// For example, to only collect the Ids of Polylines:
      /// 
      ///   var polylineIds = new ObjectIdCollection();
      ///   editor.Command<Polyline>(polylineIds, commandargs....)
      ///    
      /// <typeparam name="T">The type of entity to collect</typeparam>
      
      public static int Command<T>(this Editor editor, 
            ObjectIdCollection ids, 
            params object[] args) where T: Entity
      {
         if(editor == null)
            throw new ArgumentNullException(nameof(editor));
         if(ids == null)
            throw new ArgumentNullException(nameof(ids));
         Database db = editor.Document.Database;
         ObjectId ownerId = db.CurrentSpaceId;
         Func<ObjectId, bool> predicate = GetPredicate(typeof(T), false, false);
         db.ObjectAppended += appended;
         int start = ids.Count;
         try
         {
            editor.Command(args);
            return ids.Count - start;
         }
         finally
         {
            db.ObjectAppended -= appended;
         }

         void appended(object sender, ObjectEventArgs e)
         {
            if(e.DBObject is T entity
                && entity.BlockId == ownerId
                && predicate(entity.ObjectId))
            {
               ids.Add(entity.ObjectId);
            }
         }
      }

      /// <summary>
      /// Helper extension method for ObjectIdCollection that
      /// returns the last occurrence of an element of the given 
      /// ObjectIdCollection that is an instance of the given type, 
      /// or ObjectId.Null if no element of the given type exists 
      /// in the collection.
      /// 
      /// E.g., get the ObjectId of the last non-erased PolyLine 
      /// in an ObjectIdCollection:
      /// 
      ///    var lastPolyLineId = myIdCollection.Last<Polyline>();
      ///    
      /// </summary>
      /// <typeparam name="T">The type of object being requested</typeparam>
      /// <param name="ids">The ObjectIdCollection to query</param>
      /// <param name="exactMatch">A value indicating if types that
      /// are derived from the generic argument type are to be
      /// returned.</param>
      /// <returns>The ObjectId of the last occurrence of an element
      /// whose type is the requested type.</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static ObjectId Last<T>(
            this ObjectIdCollection ids, 
            bool exactMatch = false, 
            bool includingErased = false)
         where T:RXObject
      {
         if(ids == null)
            throw new ArgumentNullException(nameof(ids));
         if(ids.Count > 0)
         {
            var predicate = GetPredicate(typeof(T), exactMatch, includingErased);
            int len = ids.Count;
            for(int i = len - 1; i >= 0; i--)
            {
               var id = ids[i];
               if(predicate(id))
                  return id;
            }
         }
         return ObjectId.Null;
      }

      /// <summary>
      /// Returns a subset of the ObjectIdCollection whose
      /// elements represent instances of the given generic 
      /// argument type.
      /// 
      /// If the exact argument is true and the given type
      /// is not abstract, types derived from the given type 
      /// are not included.
      /// </summary>
      /// <typeparam name="T">The type whose ObjectIds are to 
      /// be included in the result.</typeparam>
      /// <param name="ids">The ObjectIdCollection to query</param>
      /// <param name="exactMatch">A value indicating if types that
      /// are derived from the generic argument type are to be
      /// excluded.</param>
      /// <param name="includingErased">A value indicating if
      /// erased ObjectIds should be included.</param>
      /// <returns>A sequence of ObjectIds representing objects 
      /// that are instances of the given generic argument  type, 
      /// or instances of objects derived from same</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static IEnumerable<ObjectId> OfType<T>(
            this ObjectIdCollection ids, 
            bool exactMatch = false,
            bool includingErased = false)
         where T: RXObject
      {
         if(ids == null)
            throw new ArgumentNullException(nameof(ids));
         if(ids.Count > 0)
         {
            var predicate = GetPredicate<T>(exactMatch, includingErased);
            for(int i = 0; i < ids.Count; i++)
            {
               if(predicate(ids[i]))
                  yield return ids[i];
            }
         }
      }

      /// <summary>
      /// Returns a predicate that takes an ObjectId and
      /// returns a value indicating if the id's runtime
      /// class is equal to, or derived from the runtime
      /// class of the given Type, depending on the exact
      /// and includingErased arguments.
      /// 
      /// If the given type is abstract, the exact argument
      /// is ignored and is effectively-false.
      /// </summary>
      /// <param name="type">The managed type to match</param>
      /// <param name="exactMatch">True if the type must be an
      /// instance of the the given Type, or false if the
      /// type must be derived from the given Type.</param>
      /// <param name="includingErased">A value indicating
      /// if erased elements should match</param>
      /// <returns>A predicate that takes an ObjectId and 
      /// returns a value indicating if the Id matches the
      /// query criteria.</returns>
      /// <exception cref="ArgumentNullException"></exception>
      /// <exception cref="ArgumentException"></exception>

      public static Func<ObjectId, bool> GetPredicate(Type type, 
         bool exactMatch = false, 
         bool includingErased = true)
      {
         if(type == null) 
            throw new ArgumentNullException(nameof(type));
         if(!typeof(RXObject).IsAssignableFrom(type))
            throw new ArgumentException("invalid type");
         RXClass rxclass = RXClass.GetClass(type);
         if(includingErased)
         {
            if(exactMatch && !type.IsAbstract)
               return id => id.ObjectClass == rxclass;
            else
               return id => id.ObjectClass.IsDerivedFrom(rxclass);
         }
         else
         {
            if(exactMatch && !type.IsAbstract)
               return id => id.ObjectClass == rxclass && !id.IsErased;
            else
               return id => id.ObjectClass.IsDerivedFrom(rxclass) && !id.IsErased;
         }
      }

      public static Func<ObjectId, bool> GetPredicate<T>(
         bool exactMatch = false,
         bool includingErased = true) where T: RXObject
      {
         return GetPredicate(typeof(T), exactMatch, includingErased);
      }
   }




}
