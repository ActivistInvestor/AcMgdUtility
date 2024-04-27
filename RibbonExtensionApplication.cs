/// RibbonExtensionApplication
/// ActivistInvestor / Tony T
/// 
/// A specialization of ExtensionApplicationAsync
/// that provides a simplified means of initializing
/// and/or adding content to AutoCAD's ribbon UI.

using System;
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
   public abstract class RibbonExtensionApplication 
      : ExtensionApplicationAsync
   {
      /// <summary>
      /// InitializeRibbon() method
      /// 
      /// When overridden in a derived type, this method 
      /// will be called at startup if the ribbon exists at 
      /// that point. Otherwise, the method will be called
      /// if/when the ribbon is created. If the ribbon is
      /// currently hidden at startup, this method will only
      /// be called if the ribbon is subsequently shown, and
      /// may never be called if the user never activates the
      /// ribbon during the current AutoCAD session.
      /// 
      /// The context argument indicates the context in which
      /// the method is called, which can be for one of three 
      /// possible reasons:
      /// 
      ///   Active:  
      ///   
      ///     The ribbon already exists and Application-
      ///     provided content must be added to it. 
      ///     
      ///     This is typically the context when applications 
      ///     are first loaded at startup; when the NETLOAD 
      ///     command is used; or when the application is 
      ///     demand-loaded because one of its commands was
      ///     issued.
      ///              
      ///   Initalizing:   
      ///   
      ///     The ribbon was just created and Application-
      ///     provided content must be added to it. This is
      ///     the context that is passed when the ribbon does
      ///     not exist at startup and is subsequently shown
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
      /// content to the ribbon. The override should first check 
      /// to see if the content it adds to the ribbon is already 
      /// added or not and act only if the content is not present, 
      /// as there is no guarantee that this method will only be 
      /// called if the ribbon content has not already been added
      /// to the ribbon.
      /// 
      /// This method may be called any number of times during 
      /// an AutoCAD session, with the context argument set to 
      /// RibbonContext.WorkspaceLoaded.
      /// 
      /// Remarks: 
      ///
      /// This method should not be used to perform various
      /// other application initialization tasks, because it
      /// may never be called (e.g., the ribbon is not visible
      /// at startup and is never used in the AutoCAD session).
      /// 
      /// The Initialize() method must be overriden to perform
      /// various other application initialization tasks. The
      /// Initialize() method is always called when an extension
      /// is loaded and is called before InitializeRibbon() is
      /// called. Hence, the Initialize() method can not have
      /// any depenence on the ribbon.
      /// 
      /// It is highly-recommended that ribbon context be
      /// created only once and stored in memory, so that
      /// the same content can be added to the ribbon each 
      /// time this method is called.
      /// </summary>

      protected abstract void InitializeRibbon(RibbonControl ribbon, RibbonState context);

      /// All remaining code supports the above method and need 
      /// not be modified.

      void initializeRibbon(RibbonState context)
      {
         if(RibbonPaletteSet == null || RibbonControl == null)
            throw new InvalidOperationException("RibbonPalleteSet does not exist");
         InitializeRibbon(RibbonControl, context);
         RibbonPaletteSet.WorkspaceLoaded += workspaceLoaded;
      }

      protected sealed override void OnInitalize()
      {
         if(RibbonPaletteSet != null)
            initializeRibbon(RibbonState.Active);
         else
            RibbonServices.RibbonPaletteSetCreated += ribbonCreated;
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

      protected static RibbonPaletteSet RibbonPaletteSet => RibbonServices.RibbonPaletteSet;
      protected static RibbonControl RibbonControl => RibbonPaletteSet.RibbonControl;

   }

   /// <summary>
   /// Indicates the context in which InitializeRibbon() is
   /// called.
   /// </summary>
   
   public enum RibbonState
   {
      Active = 0,           // The ribbon exists.
      Initalizing = 1,      // The ribbon exists and was just created.
      WorkspaceLoaded = 2   // The ribbon exists and a workspace was loaded/reloaded
   }

}