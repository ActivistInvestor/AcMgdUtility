/// RibbonExtensionApplication
/// ActivistInvestor / Tony T
/// 
/// A class that provides a simplified means of 
/// initializing and managing application-provided
/// content for AutoCAD's ribbon.

using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Ribbon;
using Autodesk.Windows;

/// Prerequisites/dependencies:
/// 
///   RibbonEventManager: 
///   IdleAction.cs: https://github.com/ActivistInvestor/AcMgdUtility/blob/main/IdleAction.cs
///   
/// This class provides the same functionality 
/// founds in ExtensionApplicationAsync, along 
/// with additional functionality for managing 
/// an extension's ribbon content.
/// 
/// See ExtensionApplicationAsync.cs for details
/// on using this class for initialization of an 
/// extension application.
/// 
/// Also note that this class does not complement 
/// ExtensionApplicationAsync, it replaces it and
/// provides all of its functionality. 
/// 
/// Hence, if you derive a type from this class, 
/// there is no need to derive another class from 
/// ExtensionApplicationAsync, as this class also
/// provides the functionality of the latter.
/// 
/// Ribbon Initalization and content management:
/// 
/// Adding application-provided content to the ribbon 
/// is not as simple as it may at first seem. There are 
/// several scenarios that must be dealt with:
/// 
///   1. The application is loaded (at startup,
///      or at any later point during the AutoCAD 
///      session), and the ribbon exists. In that
///      case, the application can simply add its
///      content to the ribbon.
///   
///   2. The application is loaded (at startup,
///      or at any later point during the AutoCAD 
///      session), and the ribbon does not exist.
///      In this case, the application must wait 
///      for the ribbon to be created. If and when 
///      that happens, the application must add its 
///      content to the ribbon. 
///         
///      It should be noted that the ribbon may have 
///      been turned off by the end user, and may 
///      never be created.
///      
///   3.  The application has been loaded and has
///       added content to the ribbon (case 1 or 2), 
///       and a workspace is subsequently loaded.
///       Loading the workspace clears application-
///       supplied content, requiring the application 
///       to add its content to the ribbon again.
///   
/// This class accomodates all of the above scenarios
/// in a unified and simplified way. It does this by
/// abstracting away the complex logic of detecting if
/// and when ribbon content must be added to the ribbon 
/// and then delgates the task of doing that to a single 
/// overridden method in derived types (InitializeRibbon), 
/// which will be called whenever application-provided 
/// content must be added to the ribbon. 
/// 
/// The InitializeRibbon() method is passed an argument
/// that provides a hint as to why the method is being 
/// called (e.g., one of the aformentioned scenarios).
/// 
/// Regardless of how/when the method is called, the 
/// override should add application-provided content 
/// to the ribbon from within this method.
/// 
/// The context argument indicates the context in which
/// the method is called, which can be for one of three 
/// possible reasons:
/// 
///   Active:  
///   
///     The ribbon exists and application-provided 
///     content should be added to it. 
///     
///     This is typically the context when applications 
///     are loaded at startup; when the NETLOAD command 
///     is used; or when the application is demand-loaded 
///     because one of its commands was issued.
///              
///   Initalizing:   
///   
///     The ribbon was just created, and application-
///     provided content should be added to it. This is
///     the context that is passed when the ribbon does
///     not exist at startup and is subsequently created
///     at some point in the AutoCAD session as a result 
///     of the user issuing the RIBBON command.
///              
///   WorkspaceLoaded: 
///   
///     The ribbon exists and was previously-initialized
///     with application-provided content, and susequently
///     a workspace was loaded that requires application-
///     provided content to be added to the ribbon again.
///
/// Applications must override this method to add their
/// content to the ribbon. 
/// 
/// This method may be called any number of times during 
/// an AutoCAD session, with the context argument set to 
/// RibbonState.WorkspaceLoaded. In such cases, content
/// should be added to the ribbon because all previously-
/// added content is discarded when a workspace is loaded.
/// 
/// Remarks: 
///
/// This method should not be used to perform other types
/// of always-required application initialization tasks 
/// unrelated to the ribbon, because this method may never 
/// be called (for example, the ribbon is not visible at 
/// startup and is never made visible for the life of the 
/// AutoCAD session).
/// 
/// It is highly-recommended that ribbon context be created 
/// only once and cached in memory, so that the same content 
/// can be added to the ribbon again, each time this method 
/// is called.
/// </summary>
/// 
/// In addition to ribbon-related initialization, this
/// class also provides an entry point for more-general
/// initialization in the exact same form it takes in the
/// sibling ExtensionApplicationAsync class (an override
/// of the Initialize() method).
/// 
/// </summary>

namespace Autodesk.AutoCAD.Runtime.AIUtils
{
   public abstract class RibbonExtensionApplication : IExtensionApplication
   {

      /// <summary>
      /// optionally override this method to perform general
      /// initialization tasks that can have no dependence
      /// on the ribbon, which may not yet exist when this
      /// method is called.
      /// </summary>
      
      protected virtual void Initialize()
      {
      }

      /// <summary>
      /// Must be overridden in a derived type. This method 
      /// is called at the point where an existing ribbon 
      /// is found, or at some later point when the ribbon
      /// is created (which may never happen), this method
      /// will also be called (possibly many times) when a
      /// workspace is loaded.
      /// </summary>
      /// <param name="context">A value indicating the context
      /// in which the method is called.</param>
      /// <param name="ribbon">The RibbonControl</param>

      protected abstract void InitializeRibbon(RibbonControl ribbon, RibbonState context);

      /// <summary>
      /// Optionally, override this to perform cleanup
      /// tasks at shutdown.
      /// </summary>

      protected virtual void Terminate()
      {
      }

      /// <summary>
      /// For advanced/specialized use cases:
      /// 
      /// Override this and return true if initialization
      /// should be deferred until the editor is quiescent.
      /// </summary>

      protected virtual bool Quiescent => false;

      /// All code below this point is supporting
      /// code that should not have to be modified.

      void IExtensionApplication.Initialize()
      {
         Application.Idle += OnIdle;
      }

      void OnIdle(object sender, EventArgs e)
      {
         if(Document != null && ! Quiescent || Document.Editor.IsQuiescent)
         {
            Application.Idle -= OnIdle;
            try
            {
               this.Initialize();
               if(RibbonPaletteSet != null)
                  initializeRibbon(RibbonState.Active);
               else
                  RibbonServices.RibbonPaletteSetCreated += ribbonCreated;
            }
            catch(System.Exception ex)
            {
               Console.Beep();
               Document.Editor.WriteMessage(ex.ToString());
            }
         }
      }

      void IExtensionApplication.Terminate()
      {
         this.Terminate();
      }

      void initializeRibbon(RibbonState context)
      {
         if(RibbonPaletteSet == null || RibbonControl == null)
            throw new InvalidOperationException("Ribbon does not exist");
         InitializeRibbon(RibbonControl, context);
         RibbonPaletteSet.WorkspaceLoaded += workspaceLoaded;
      }

      void ribbonCreated(object sender, EventArgs e)
      {
         RibbonServices.RibbonPaletteSetCreated -= ribbonCreated;
         initializeRibbon(RibbonState.Initalizing);
      }

      void workspaceLoaded(object sender, EventArgs e)
      {
         if(Document != null)
            InitializeRibbon(RibbonControl, RibbonState.WorkspaceLoaded);
      }

      /// <summary>
      /// These properties are avaialble for use by 
      /// derived types. Note that any of them may
      /// return null and should be checked accordingly.
      /// </summary>

      protected static Document Document =>
         Application.DocumentManager.MdiActiveDocument;

      protected static RibbonPaletteSet RibbonPaletteSet => 
         RibbonServices.RibbonPaletteSet;

      protected static RibbonControl RibbonControl => 
         RibbonPaletteSet.RibbonControl;

   }

   /// <summary>
   /// Indicates the context in which InitializeRibbon() is called.
   /// </summary>

   public enum RibbonState
   {
      /// <summary>
      /// The ribbon exists but was not 
      /// previously-initialized.
      /// </summary>
      Active = 0,

      /// <summary>
      /// The ribbon was just created.
      /// </summary>
      Initalizing = 1,

      /// <summary>
      /// The ribbon exists and was previously
      /// initialized, and a workspace was just
      /// loaded, requiring application-provided
      /// ribbon content to be added again.
      /// </summary>
      WorkspaceLoaded = 2
   }
}