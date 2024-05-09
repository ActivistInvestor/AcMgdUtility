using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices.LayoutUtils;
using Autodesk.AutoCAD.ApplicationServices.AsyncHelpers;

/// Prerequisites:
/// 
///   https://github.com/ActivistInvestor/AcMgdUtility/blob/main/DocumentCollectionExtensions.cs
/// 

/// Coding example showing how to maintain and display the 
/// number of paper space layouts in a field or expression.
/// Requires DocumentCollectionExtensions.cs located at the
/// above URL.

/// This attribute tells AutoCAD what type implements the
/// IExtensionApplication interface, allowing it to avoid
/// having to scan every type in the assembly:

[assembly: ExtensionApplication(typeof(LayoutCountApplication))]

namespace Autodesk.AutoCAD.ApplicationServices.LayoutUtils
{
   /// <summary>
   /// IExtension application (Note: do not create 
   /// instances of this class from code, AutoCAD
   /// will create an instance when the containing
   /// assembly loads. Also note that there can be
   /// one and only one IExtensionApplication in an
   /// assembly).
   /// </summary>

   public class LayoutCountApplication : IExtensionApplication
   {
      LayoutCountMonitor layoutCountMonitor;

      public async void Initialize()
      {
         await Application.DocumentManager.WaitForIdle();

         layoutCountMonitor = new LayoutCountMonitor();
      }

      public void Terminate()
      {
      }
   }

   /// <summary>
   /// A class that monitors the number of paperspace layouts
   /// in each document and exposes the count through a USERIx
   /// system variable, for use in fields. The default name of
   /// the system variable that stores the values is USERI5, 
   /// which has document-scope. The USSERIx system variable
   /// used by this class must not be used for other purposes.
   /// 
   /// An alternative to using a USERIx system variable is to
   /// define a custom system variable in the registry, that
   /// has document scope (e.g., has a distinct value for each
   /// document). Doing that, is beyond the scope of this basic
   /// example.
   /// </summary>

   public class LayoutCountMonitor : LazyDocumentManager
   {
      /// <summary>
      /// The system variable that stores the count of
      /// layouts for each document. This system variable
      /// has document-scope, and can be referenced from 
      /// fields, DIESEL, LISP, etc.
      /// </summary>

      public const string SystemVariable = "USERI5";

      public LayoutCountMonitor()
      {
         AutoUpdateFields = true;
      }

      protected override void Initialize(Document document)
      {
         LayoutManager manager = LayoutManager.Current;
         manager.LayoutCreated += layoutsChanged;
         manager.LayoutRemoved += layoutsChanged;
         manager.LayoutCopied += layoutCopied;
         Update();
      }

      /// <summary>
      /// Gets/sets a value indicating if fields should be
      /// updated when the number of layouts changes:
      /// </summary>

      public bool AutoUpdateFields { get; set; }

      void layoutCopied(object sender, LayoutCopiedEventArgs e)
      {
         Update();
      }

      void layoutsChanged(object sender, LayoutEventArgs e)
      {
         Update();
      }

      /// <summary>
      /// Updates the system variable when a layout
      /// is added to or removed from the Layouts 
      /// collection. Calls EvaluateFields() to update
      /// fields in the active document if the value
      /// has changed and AutoUpdateFields is true.
      /// </summary>

      void Update()
      {
         var docmgr = Application.DocumentManager;
         Document doc = docmgr.MdiActiveDocument;
         if(doc != null)
         {
            int curval = (int)Application.GetSystemVariable(SystemVariable);
            int newval = LayoutManager.Current.LayoutCount - 1;
            if(curval != newval)
            {
               Application.SetSystemVariable(SystemVariable, newval);
               if(AutoUpdateFields)
                  docmgr.InvokeAsCommand(() => doc.Database.EvaluateFields());
            }
         }
      }
   }


   /// <summary>
   /// A variant of DocumentManager that operates in a lazy
   /// mode, deferring initialization of each document until
   /// the first time it becomes active. This is sometimes
   /// necessary when initialization requires a document to
   /// be active.
   /// 
   /// Unlike DocumentManager, this class does not initialize
   /// documents that exist when an instance is created in an
   /// 'eager' mode. All documents except the active document
   /// are subsequently initialized the first time they are 
   /// activated after the instance of this class is created. 
   /// The active document is initialized when the instance is 
   /// created.
   /// 
   /// Derive a class from LazyDocumentManager, and override
   /// the Initialize() method to do one-time initialization
   /// when the document passed as the argument becomes active.
   /// </summary>

   public abstract class LazyDocumentManager
   {
      HashSet<Document> initialized = new HashSet<Document>();
      static DocumentCollection docs = Application.DocumentManager;

      protected LazyDocumentManager()
      {
         docs.DocumentActivated += documentActivated;
         Document doc = docs.MdiActiveDocument;
         if(doc != null && initialized.Add(doc))
            Initialize(doc);
      }

      void documentActivated(object sender, DocumentCollectionEventArgs e)
      {
         if(initialized.Add(e.Document))
            Initialize(e.Document);
      }

      protected abstract void Initialize(Document document);

   }
}
