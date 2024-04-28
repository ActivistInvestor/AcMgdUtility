/// RibbonExtensionApplication
/// ActivistInvestor / Tony T
/// 
/// A specialization of ExtensionApplicationAsync
/// that provides a simplified means of initializing
/// and/or adding content to AutoCAD's ribbon UI.
/// 

using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Ribbon;
using Autodesk.Windows;

/// See the documentation for the base type in 
/// ExtensionApplicationAsync.cs for details
/// on generic initialization of an extension.
/// 
/// Also note that this class does not complement 
/// ExtensionApplicationAsync, it replaces it and
/// provides all of its functionality. 
/// 
/// Hence, if you derive a type from this class, 
/// there is no need to derive another class from 
/// ExtensionApplicationAsync (which is what this
/// class derives from).
/// 
/// Ribbon Initalization:
/// 
/// Adding application-provided content to the ribbon 
/// is not as simple as it may at first seem. There are 
/// several scenarios, all of which must be dealt with:
/// 
///   1. An extension is loaded at startup. 
///   
///      In this case, there are two possiblities:
///      
///      A. The ribbon has just been created.
///      
///      B. The ribbon is turned off by the user,
///         but may be turned on at any point in
///         the current AutoCAD session.
///      
///      In the case of A, the application can add
///      its content to the ribbon at startup via
///      an override of the InitializeRibbon() method.
///      
///      In the case of B, the application can do
///      nothing because the ribbon does not exist,
///      but could be created at any point during
///      the AutoCAD session as a result of the user
///      issuing the RIBBON command. At that point,
///      InitializeRibbon() will be called, and the 
///      application must then add its content to the
///      newly-created ribbon.
///      
///   2. An extension is loaded at any point during
///      the AutoCAD session using NETLOAD.
///      
///      When the extension loads, the ribbon may or
///      may not exist, resulting in the same set of
///      conditions as scenario 1.
///      
///   In either of the above cases, if the ribbon does
///   not exist, the application must wait until it is
///   created (which may never happen), and then add
///   its content to the ribbon.
///   
///   3.  The third scenario is where the ribbon exists,
///       and the application has already added content
///       to it, but a workspace is subsequently loaded, 
///       causing the application-provided content to be 
///       discarded, requiring it to be added again.
///   
/// This class accomodates all of the above scenarios
/// in a unified manner, by allowing a derived type to
/// override the InitializeRibbon() method, which will
/// be called whenever the application-provided content
/// must be added to the ribbon. The InitializeRibbon()
/// method has a RibbonState argument, that provides a
/// hint as to why the method is being called.
/// 
/// </summary>

namespace Autodesk.AutoCAD.Runtime.AIUtils
{
   public abstract class RibbonExtensionApplication : IExtensionApplication
   {

      /// <summary>
      /// Must override this method, and perform one-time
      /// initialization tasks that do not have a depenence
      /// on the ribbon.
      /// </summary>
      
      protected abstract void Initialize();

      /// <summary>
      /// Optionally, override this to perform cleanup
      /// tasks at shutdown.
      /// </summary>

      protected virtual void Terminate()
      {
      }

      /// <summary>
      /// InitializeRibbon() method
      /// 
      /// When overridden in a derived type, this method 
      /// is called at the point where an existing ribbon 
      /// is found, or at some later point when the ribbon
      /// is created (which may never happen), this method
      /// will also be called (possibly many times) when a
      /// workspace is loaded.
      /// 
      /// Regardless of how/when the method is called, the 
      /// override should add application-provided content 
      /// to the ribbon in this method.
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
      /// RibbonContext.WorkspaceLoaded. In every case, content
      /// should be added to the ribbon because previously-added
      /// content is discarded when a workspace is loaded.
      /// 
      /// Remarks: 
      ///
      /// This method should not be used to perform other types
      /// of application initialization tasks unrelated to the
      /// ribbon, because this method may never be called (for
      /// example, the ribbon is not visible at startup and is 
      /// never made visible for the life of the AutoCAD session).
      /// 
      /// The Initialize() method should be used to perform other
      /// application initialization tasks. That method is always
      /// called when an extension is loaded, and is called before 
      /// InitializeRibbon() is called. Hence, the Initialize() 
      /// method should not have any depenence on the ribbon.
      /// 
      /// It is highly-recommended that ribbon context be created 
      /// only once and cached in memory, so that the same content 
      /// can be added to the ribbon again, each time this method 
      /// is called.
      /// </summary>

      protected abstract void InitializeRibbon(RibbonControl ribbon, RibbonState context);

      /// <summary>
      /// For advanced or specialized use cases:
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
      Active = 0,           // The ribbon exists.
      Initalizing = 1,      // The ribbon exists and was just created.
      WorkspaceLoaded = 2   // The ribbon exists and a workspace was loaded/reloaded
   }

}
