using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace CommandObserverExamplePattern
{
   /// <summary>
   /// A class that collects and operates on objects
   /// that are erased by the ERASE command.
   /// 
   /// This class serves as an example of a more generic
   /// pattern that involves observing specific events 
   /// only while a specified command or set of commands 
   /// are running, with minimal event-handling overhead.
   /// 
   /// The only event handlers that continuously listen to
   /// events are the CommandWillStart event handler (one 
   /// for each open Document), and the DocumentAdded event 
   /// handler. 
   /// 
   /// All other event handlers are added to their respective 
   /// events only when they are needed, and are removed as 
   /// soon as they are no longer needed. 
   /// 
   /// The generic argument specifies the type of objects 
   /// that are observed/operated on.
   /// 
   /// Note: DO NOT use the [PerDocumentClass] attribute on this class!!!
   /// </summary>
   /// <typeparam name="T">The type of DBObject to observe</typeparam>

   public class ErasedObjectObserver<T> where T: DBObject
   {
      static readonly DocumentCollection docs = Application.DocumentManager;
      protected readonly Document Document;
      protected readonly HashSet<ObjectId> erasedObjects = new HashSet<ObjectId>();

      /// <summary>
      /// Private constructor. 
      /// 
      /// Instances of this class are not directly-creatable, 
      /// and are only created by other methods of this class 
      /// that are driven by handlers of events.
      /// 
      /// There is one instance of this class created for each
      /// Document, which it is permanently associated with.
      /// </summary>
      /// <param name="doc">The associated Document</param>

      ErasedObjectObserver(Document doc)
      {
         this.Document = doc;
         doc.CommandWillStart += commandWillStart;
      }

      public static void Initialize()
      {
         /// Dummy method to force static constructor to run
      }

      static ErasedObjectObserver()
      {
         foreach(Document doc in docs)
         {
            Initialze(doc);
         }
         docs.DocumentCreated += documentCreated;
      }

      static void Initialze(Document doc)
      {
         doc.UserData[typeof(ErasedObjectObserver<T>)] = new ErasedObjectObserver<T>(doc);
      }

      public static ErasedObjectObserver<T>? Item(Document doc)
      {
         return doc.UserData[typeof(ErasedObjectObserver<T>)] as ErasedObjectObserver<T>;
      }

      static void documentCreated(object sender, DocumentCollectionEventArgs e)
      {
         Initialze(e.Document);
      }

      void commandWillStart(object sender, CommandEventArgs e)
      {
         if(e.GlobalCommandName == "ERASE")
         {
            Document doc = this.Document;
            doc.CommandEnded += commandEnded;
            doc.CommandCancelled += commandCancelled;
            doc.CommandFailed += commandCancelled;
            Document.Database.ObjectErased += objectErased;
            erasedObjects.Clear();
         }
      }

      private void objectErased(object sender, ObjectErasedEventArgs e)
      {
         if(e.DBObject is T)
         {
            if(e.Erased)
               erasedObjects.Add(e.DBObject.Id);
            else
               erasedObjects.Remove(e.DBObject.Id);
         }
      }

      private void OnCommandEnded(bool cancelled = false)
      {
         Document.CommandEnded -= commandEnded;
         Document.CommandCancelled -= commandCancelled;
         Document.CommandFailed -= commandCancelled;
         Document.Database.ObjectErased -= objectErased;
         if(!cancelled)
            ProcessErasedObjects();
         erasedObjects.Clear();
      }

      private void commandCancelled(object sender, CommandEventArgs e)
      {
         OnCommandEnded(true);
      }

      private void commandEnded(object sender, CommandEventArgs e)
      {
         OnCommandEnded(false);
      }

      void ProcessErasedObjects()
      {
         if(erasedObjects.Count == 0)
            return;
         using(var tr = new OpenCloseTransaction())
         {
            foreach(var id in erasedObjects)
            {
               T obj = (T)tr.GetObject(id, OpenMode.ForRead, true);
               ProcessErasedObject(obj);
            }
            tr.Commit();
         }
      }

      private void ProcessErasedObject(T erasedObject)
      {
         // TODO: Operate on each erased object here
      }
   }

   /// <summary>
   /// Example IExtensionApplication that initializes the 
   /// ErasedObjectObserver to observe the erasure of all 
   /// BlockReferences in every document.
   /// </summary>

   public class MyApplication : IExtensionApplication
   {
      public void Initialize()
      {
         Application.Idle += idle;
      }

      private void idle(object? sender, EventArgs e)
      {
         if(Application.DocumentManager.MdiActiveDocument != null)
         {
            Application.Idle -= idle;
            ErasedObjectObserver<BlockReference>.Initialize();
         }
      }

      public void Terminate()
      {
      }
   }
}


