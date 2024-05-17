/// CommandExtensions.cs  ActivistInvestor / Tony T.
/// 
/// Distributed under the terms of the MIT license.
/// 
///
/// Source location:
/// 
///     https://github.com/ActivistInvestor/AcMgdUtility/blob/main/CommandExtensions.cs
///     
/// Sample code location:
/// 
///     https://github.com/ActivistInvestor/AcMgdUtility/blob/main/CommandExtensionExamples.cs
///     
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices.DocumentExtensions;
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
   /// code below existed and is included as-is, or has been
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
   /// Disclaimer:
   /// 
   /// The included code was intended to be used as-is, from a
   /// consuming application. The author provides no support for 
   /// modified and/or hacked versions of this source.
   /// 
   /// For fun, I threw this problem at ChatGPT-4o, and it gave
   /// me a less-than-correct result, which I had to correct it
   /// on several screw-ups:
   /// 
   ///   https://chat.openai.com/share/990a589f-b9a8-4db4-a95b-04ab44f33feb
   /// 
   /// Considering just how terse and deficient the AutoCAD Managed
   /// API docs are, the old axiom "garbage in, garbage out" would 
   /// seem to apply here, and explain ChatGPT's lack of expertise 
   /// in this domain.
   /// 
   /// See the accompanying file CommandExtensionExamples.cs for
   /// example code.
   /// </summary>

   public static class CommandExtensions
   {

      /// <summary>
      /// Adapted from an enhanced replacement for the Editor's 
      /// Command() method. Most of the functionality from that 
      /// method that isn't included in this vastly watered-down 
      /// version, and focuses only on capturing new objects.
      /// 
      /// The Command<T>() Method:
      /// 
      /// A generic overload of the Editor's Command() method 
      /// that accepts an ObjectIdCollection, to which it adds 
      /// the ObjectIds of all entities that were added to the
      /// current space by the executed command.
      /// 
      /// The same ObjectIdCollection can be passed into to 
      /// multiple calls to this method to collect the ids 
      /// of new objects created by multiple commands.
      /// 
      /// The generic argument specifies the type of entities
      /// to collect. To collect all entities created by a
      /// command, use 'Entity' as the generic argument type.
      /// 
      /// For example, to only collect the Ids of Polylines,
      /// and ignore everything else:
      /// 
      ///   var polylineIds = new ObjectIdCollection();
      ///   
      ///   editor.Command<Polyline>(polylineIds, commandargs....)
      ///    
      /// </summary>
      /// <typeparam name="T">The type of entity to collect</typeparam>
      /// <param name="editor">The Editor of the active document</param>
      /// <param name="ids">The ObjectIdCollection to populate</param>
      /// <param name="args">The command arguments</param>
      /// <returns>The number of objects added to the collection</returns>

      public static int Command<T>(this Editor editor,
            ObjectIdCollection ids,
            params object[] args) where T : Entity
      {
         if(editor == null)
            throw new ArgumentNullException(nameof(editor));
         if(ids == null)
            throw new ArgumentNullException(nameof(ids));
         Database db = editor.Document.Database;
         ObjectId ownerId = db.CurrentSpaceId;
         var predicate = GetObjectIdPredicate<T>(false, false);
         int start = ids.Count;
         db.ObjectAppended += appended;
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
            ObjectId id = e.DBObject.ObjectId;
            if(e.DBObject is T entity
                && entity.BlockId == ownerId
                && predicate(id))
            {
               ids.Add(id);
            }
         }
      }


      /// <summary>
      /// Similar to Command<T>, except that it returns a new
      /// ObjectIdCollection containing the Ids of the objects 
      /// created by the command.
      /// </summary>
      /// <typeparam name="T">The type of entity to collect</typeparam>

      public static ObjectIdCollection CommandWithResult<T>(this Editor ed, params object[] args)
         where T : Entity
      {
         if(ed == null)
            throw new ArgumentNullException(nameof(ed));
         ObjectIdCollection ids = new ObjectIdCollection();
         ed.Command<T>(ids, args);
         return ids;
      }

      /// <summary>
      /// Non-generic version of CommandWithResult() that uses 
      /// Entity as the generic argument type, to collect all 
      /// entities.
      /// </summary>

      public static ObjectIdCollection CommandWithResult(this Editor ed, params object[] args)
      {
         if(ed == null)
            throw new ArgumentNullException(nameof(ed));
         ObjectIdCollection ids = new ObjectIdCollection();
         ed.Command<Entity>(ids, args);
         return ids;
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
      ///    var lastPlineId = myIdCollection.Last<Polyline>();
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
         where T : DBObject
      {
         if(ids == null)
            throw new ArgumentNullException(nameof(ids));
         if(ids.Count > 0)
         {
            var predicate = GetObjectIdPredicate<T>(exactMatch, includingErased);
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
      /// Returns a sequence that produces a subset of the 
      /// ObjectIdCollection consisting of elements that
      /// represent instances of the generic argument type.
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
         where T : DBObject
      {
         if(ids == null)
            throw new ArgumentNullException(nameof(ids));
         if(ids.Count > 0)
         {
            var predicate = GetObjectIdPredicate<T>(exactMatch, includingErased);
            for(int i = 0; i < ids.Count; i++)
            {
               var id = ids[i];
               if(predicate(id))
                  yield return id;
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

      public static Func<ObjectId, bool> GetObjectIdPredicate<T>(
         bool exactMatch = false,
         bool includingErased = true) where T : DBObject
      {
         exactMatch &= !typeof(T).IsAbstract;
         if(includingErased)
         {
            if(exactMatch)
               return static id => id.ObjectClass == RXClass<T>.Value;
            else
               return static id => id.ObjectClass.IsDerivedFrom(RXClass<T>.Value);
         }
         else
         {
            if(exactMatch)
               return static id => id.ObjectClass == RXClass<T>.Value && !id.IsErased;
            else
               return static id => !id.IsErased 
                  && id.ObjectClass.IsDerivedFrom(RXClass<T>.Value);
         }
      }

      /// Excerpted from CollectionExtentsions.cs
      /// and ObjectIdCollectionExtensions.cs:
      /// 
      /// <summary>
      /// A ToArray() method for ObjectIdCollection 
      /// (helps to de-clutter application code).
      /// </summary>
      /// <param name="ids">The ObjectIdCollection to convert to an array</param>
      /// <returns>An array of ObjectId[] containing the elements 
      /// of ObjectIdCollection</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static ObjectId[] ToArray(this ObjectIdCollection ids)
      {
         return ToArray<ObjectId>(ids);
      }

      /// <summary>
      /// ToArray() for non-generic ICollection types.
      /// 
      /// Requires the element type to be explicitly 
      /// passed as the generic argument.
      /// </summary>

      public static T[] ToArray<T>(this ICollection collection)
      {
         if(collection == null)
            throw new ArgumentNullException(nameof(collection));
         T[] array = new T[collection.Count];
         collection.CopyTo(array, 0);
         return array;
      }
   }

   /// <summary>
   /// This allows us to avoid the capture of a 
   /// local RXClass variable in lambda functions.
   /// Referencing the Value field is faster than 
   /// referencing the value stored in a captured 
   /// local variable.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   
   public static class RXClass<T> where T: RXObject
   {
      public static readonly RXClass Value = RXClass.GetClass(typeof(T));
   }
}

