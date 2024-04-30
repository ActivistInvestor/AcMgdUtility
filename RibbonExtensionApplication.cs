/// RibbonExtensionApplication
/// ActivistInvestor / Tony T
/// 
/// A class that provides a simplified means of 
/// initializing and managing application-provided
/// content for AutoCAD's ribbon.

using System;
using System.Reflection;
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
/// If your extension requires general, always-required
/// initialization, override the Initialize() method in
/// your derived class and do the needed initalization
/// from within that override.
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
      protected object RibbonContent { get; private set; }
      bool observingWorkspaceLoaded = false;
      bool createContentImplemented = false;

      /// <summary>
      /// optionally override this method to perform general
      /// initialization tasks that can have no dependence
      /// on the ribbon (which may not yet exist when this
      /// method is called). This method serves the same purpose
      /// as the same-named method of ExtensionApplicationAsync.
      /// This method is always called once when an extension is
      /// loaded.
      /// </summary>
      
      protected virtual void Initialize()
      {
      }

      /// <summary>
      /// Because it is necessary to repeatedly add the
      /// same content to the ribbon (because loading a
      /// workspace wipes out previously-added content),
      /// The content should be created only once, and
      /// stored in a variable, so that it can be added
      /// whenever needed.
      /// 
      /// These methods will be called by this class to 
      /// get the ribbon content. If an override returns
      /// content, the override of AddContentToRibbon()
      /// is called to add the content to the ribbon.
      /// 
      /// Derived types don't need to cache ribbon content,
      /// because that is done by this class. Derived types
      /// only need to create the content, and add it to the 
      /// ribbon.
      /// 
      /// See the two overloads below for how this class
      /// provides ways for derived types to do that.

      /// <summary>
      /// Override this method in a derived type to create 
      /// and return the content that will be added to the 
      /// ribbon whenever necessary, there's no need to cache
      /// the created content manually, as this class does
      /// that internally. Do not add the content to the
      /// ribbon from this method. To do that, you override 
      /// the AddRibbonContent() method and add the content
      /// to the ribbon in the override of that method.
      /// 
      /// The purpose of this complexity, is to establish a
      /// clear separation between the task of creating the 
      /// content, and the task of adding the content to the 
      /// ribbon, because the former must be done only once
      /// and the latter must be done many times, and we do
      /// not want be needlessly-burdened with creating the
      /// same content many times over.
      /// 
      /// This method will be called only once per AutoCAD
      /// session (unless InvalidateRibbonContent() is called,
      /// to force it to be recreated, which exists mainly for
      /// special use cases).
      /// </summary>
      /// <param name="state"></param>
      /// <returns>The content to be added to the ribbon</returns>

      protected virtual object CreateRibbonContent(RibbonState state)
      {
         return null;
      }

      /// <summary>
      /// Override this method in a derived type to add 
      /// content to the ribbon. This method can be called 
      /// numerous times, so overrides should do nothing 
      /// but add the content to the ribbon.
      /// </summary>
      /// <param name="ribbon">The RibbonControl</param>
      /// <param name="content">The content to be added to the ribbon</param>
      /// <param name="state">The current RibbonState</param>

      protected virtual void AddContentToRibbon(RibbonControl ribbon, object content, RibbonState state)
      {
      }

      /// <summary>
      /// Clears/invalidates any cached ribbon content,
      /// forcing it to be recreated.
      /// </summary>

      protected void InvalidateRibbonContent()
      {
         RibbonContent = null;
      }

      /// <summary>
      /// Can be overridden in a derived type. This method 
      /// is called at the point where an existing ribbon 
      /// is found, or at some later point when the ribbon
      /// is created (which may never happen), this method
      /// will also be called (possibly many times) when a
      /// workspace is loaded.
      /// 
      /// The context parameter indicates the circumstances
      /// under which the method is being called, but it is
      /// mainly used only in special use-cases.
      /// 
      /// Generally, the context parameter should be ignored 
      /// or not be involved in any type of branching because 
      /// regardless of its value, the ribbon must be uniformly 
      /// initialized every time this method is called.
      /// 
      /// If the CreateRibbonContent() and AddContentToRibbon()
      /// methods are overridden, this method does not have to
      /// be (and probably shouldn't be) overridden. 
      /// </summary>
      /// <param name="context">A value indicating the context
      /// in which the method is called.</param>
      /// <param name="ribbon">The RibbonControl</param>

      protected virtual void InitializeRibbon(RibbonControl ribbon, RibbonState context)
      {
      }

      void InitializeContent(RibbonControl ribbon, RibbonState context)
      {
         RibbonContent = RibbonContent ?? CreateRibbonContent(context);
         if(RibbonContent != null)
            AddContentToRibbon(RibbonControl, RibbonContent, context);
      }

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
               Initialize();
               InitializeRibbonCore(RibbonState.Active);
            }
            catch(System.Exception ex)
            {
               Console.Beep();
               Document.Editor.WriteMessage(ex.ToString());
            }
         }
      }

      static void ExecuteInApplicationContext(Action action)
      {
         if(Application.DocumentManager.IsApplicationContext)
            action();
         else
            IdleAction.OnIdle(action);
      }

      /// <summary>
      /// Override to return the IRibbonContent provider whose
      /// methods will be called to create and add content to
      /// the ribbon. Derived types can implement this interface
      /// and simply return 'this', or obtain an instance through
      /// some other means.
      /// </summary>
      /// <param name="context"></param>
      /// <returns></returns>

      void IExtensionApplication.Terminate()
      {
         this.Terminate();
      }

      void InitializeRibbonCore(RibbonState context)
      {
         if(RibbonPaletteSet != null)
            ExecuteInApplicationContext(() => InitializeRibbonAsync(context));
         else
            RibbonServices.RibbonPaletteSetCreated += ribbonCreated;
      }

      void InitializeRibbonAsync(RibbonState context)
      {
         InitializeRibbon(RibbonControl, context);
         InitializeContent(RibbonControl, context);
         AddWorkspaceLoadedHandler();
      }

      void AddWorkspaceLoadedHandler()
      {
         if(!observingWorkspaceLoaded)
         {
            RibbonPaletteSet.WorkspaceLoaded += workspaceLoaded;
            observingWorkspaceLoaded = true;
         }
      }

      bool HasOverride(Delegate method)
      {
         MethodInfo m = method.Method;
         return m != m.GetBaseDefinition();
      }


      /// <summary>
      /// Event handlers
      /// </summary>

      void ribbonCreated(object sender, EventArgs e)
      {
         RibbonServices.RibbonPaletteSetCreated -= ribbonCreated;
         InitializeRibbonCore(RibbonState.Initalizing);
      }

      void workspaceLoaded(object sender, EventArgs e)
      {
         if(Document != null)
            InitializeRibbonCore(RibbonState.WorkspaceLoaded);
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