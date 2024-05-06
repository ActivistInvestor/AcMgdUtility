/// RibbonEventManager.cs
/// ActivistInvestor / Tony T
/// 
/// A class that provides a simplified means of 
/// initializing and managing application-provided
/// content added to AutoCAD's ribbon.
/// 
/// Source:
///   
///   https://github.com/ActivistInvestor/AcMgdUtility/blob/main/RibbonExtensionApplication/RibbonEventManager.cs
///

using System;
using System.Diagnostics;
using Autodesk.AutoCAD.Ribbon;
using Autodesk.Windows;

namespace Autodesk.AutoCAD.ApplicationServices.AIUtils
{
   /// <summary>
   /// This class provides the functionality of the 
   /// RibbonExtensionApplication class without requiring 
   /// an IExtensionApplication to be defined. 
   /// 
   /// Instead, it exposes a single event can be handled 
   /// to be notified whenever it is necessary to add or
   /// refresh ribbon content.
   /// 
   /// The InitializeRibbon event:
   /// 
   /// The typical usage pattern for using this event, is to
   /// simply add a handler to it when the application/extension
   /// is loaded (e.g., from an IExtensionApplication.Initialize
   /// method). If that is done, it isn't necessary to check to
   /// see if the ribbon exists, or do anything else related to
   /// the ribbbon, since the RibbonEventManager does all of that 
   /// for the applications using it.
   /// 
   /// Using this class and its single event relieves the developer
   /// from the complicated burden of having to check conditions and
   /// handle multiple events to ensure that their content is always 
   /// present on the ribbon.
   /// 
   /// A minimal example IExtensionApplication that uses this class
   /// to manage ribbon content:
   /// 
   /// <code>
   ///  
   ///   public class MyApplication : IExtensionApplication
   ///   {
   ///      public void Initialize()
   ///      {
   ///         RibbonEventManager.InitializeRibbon += LoadRibbonContent;
   ///      }
   ///      
   ///      private void LoadRibbonContent(object sender, RibbonStateEventArgs e)
   ///      {
   ///         // TODO: Add content to ribbon here.
   ///      }
   ///
   ///      public void Terminate()
   ///      {
   ///      }
   ///   }
   /// 
   /// </code>
   /// 
   /// The handler for the InitializeRibbon event will be 
   /// called whenever it is necessary to add content to 
   /// the ribbon, which includes:
   ///   
   ///   1. At startup if the ribbon exists.
   ///   2. When the ribbon is first created and shown.
   ///   3. When a workspace is loaded.
   /// 
   /// The State property of the event argument indicates
   /// which of the these three conditions triggered the
   /// event.
   /// 
   /// </summary>

   public static partial class RibbonEventManager
   {

      static DocumentCollection docs = Application.DocumentManager;
      static bool initialized = false;
      static event RibbonStateEventHandler initializeRibbon;

      static RibbonEventManager()
      {
         if(RibbonControl != null)
            Initialize(RibbonState.Active);
         else
            RibbonServices.RibbonPaletteSetCreated += ribbonPaletteSetCreated;
      }

      private static void ribbonPaletteSetCreated(object sender, EventArgs e)
      {
         RibbonServices.RibbonPaletteSetCreated -= ribbonPaletteSetCreated;
         Initialize(RibbonState.Initalizing);
      }

      static void Initialize(RibbonState state)
      {
         Debug.Assert(!initialized);
         RaiseInitializeRibbonEvent(state, () =>
         {
            RibbonPaletteSet.WorkspaceLoaded += workspaceLoaded;
            initialized = true;
         });
      }

      private static void workspaceLoaded(object sender, EventArgs e)
      {
         RaiseInitializeRibbonEvent(RibbonState.WorkspaceLoaded);
      }

      static void RaiseInitializeRibbonEvent(RibbonState state, Action continuation = null)
      {
         IdleAction.OnIdle(() =>
         {
            initializeRibbon?.Invoke(RibbonPaletteSet, new RibbonStateEventArgs(state));
            continuation?.Invoke();
         });
      }

      /// <summary>
      /// If a handler is added to this event and the ribbon exists,
      /// the handler will be invoked on the next Idle event.
      /// 
      /// Note: Adding the same event handler to this event
      /// multiple times can lead to undefined behavior.
      /// </summary>

      public static event RibbonStateEventHandler InitializeRibbon
      {
         add
         {
            if(value == null)
               throw new ArgumentNullException(nameof(value));

            if(initialized)
            {
               IdleAction.OnIdle(() =>
               {
                  value(RibbonPaletteSet, new RibbonStateEventArgs(RibbonState.Active));
                  initializeRibbon += value;
               });
            }
            else
            {
               initializeRibbon += value;
            }
         }
         remove
         {
            initializeRibbon -= value;
         }
      }

      static Document Document =>
         Application.DocumentManager.MdiActiveDocument;

      static bool IsQuiescent =>
         Document?.Editor.IsQuiescent == true;

      static RibbonPaletteSet RibbonPaletteSet =>
         RibbonServices.RibbonPaletteSet;

      static RibbonControl RibbonControl =>
         RibbonPaletteSet?.RibbonControl;

      static bool IsAppContext => docs.IsApplicationContext;


      /// Helper classes excerpted from the
      /// DocumentCollectionExtensions class
      /// 
      /// <summary>
      /// Indicates if an action can execute based on
      /// the specified conditions.
      /// <param name="action">The action to execute</param>
      /// <param name="document">A value indicating if an 
      /// active document is required to execute the action</param>
      /// <param name="quiescent">A value indicating if a
      /// quiescent active document is required to execute
      /// the action</param>
      /// </summary>

      public static bool CanInvoke(bool quiescent = false, bool document = true)
      {
         return docs.MdiActiveDocument == null ? !document
            : !quiescent || docs.MdiActiveDocument.Editor.IsQuiescent;
      }

      /// <summary>
      /// Conditionally executes an action on a subsequent 
      /// raising of the Application.Idle event.
      /// </summary>

      class IdleAction
      {
         Action action;
         bool document;
         bool quiescent;

         /// <summary>
         /// If document is true, execution of the action
         /// is deferred until there is an active document.
         /// if quiescent is true, execution of the action
         /// is deferred until there is an active document
         /// and it is in a quiescent state. If quiescent
         /// is true, document is effectively-true.
         /// 
         /// If the quiescent and document conditions are not 
         /// satisified, invocation of the action is retried 
         /// on the next raising of the idle event.
         /// 
         /// </summary>
         /// <param name="action">The action to execute</param>
         /// <param name="document">A value indicating if an 
         /// active document is required to execute the action</param>
         /// <param name="quiescent">A value indicating if a
         /// quiescent active document is required to execute
         /// the action</param>

         IdleAction(Action action, bool quiescent = false, bool document = true)
         {
            if(action == null)
               throw new ArgumentNullException(nameof(action));
            this.action = action;
            this.quiescent = quiescent;
            this.document = document || quiescent;
            Application.Idle += idle;
         }

         void idle(object sender, EventArgs e)
         {
            if(CanInvoke(quiescent, document))
            {
               Application.Idle -= idle;
               action();
               action = null;
            }
         }

         public static void OnIdle(Action action, bool quiescent = false, bool document = true)
         {
            new IdleAction(action, quiescent, document);
         }
      }

      /// <summary>
      /// Indicates the context in which the 
      /// InitializeRibbon event is raised.
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
         WorkspaceLoaded = 2,

         /// <summary>
         /// Indicates that ribbon content should be
         /// reloaded for unspecified reasons.
         /// </summary>
         RefreshContent = 3
      }

      public delegate void RibbonStateEventHandler(object sender, RibbonStateEventArgs e);

      public class RibbonStateEventArgs : EventArgs
      {
         public RibbonStateEventArgs(RibbonState state)
         {
            this.State = state;
         }
         public RibbonState State { get; private set; }
         public RibbonPaletteSet RibbonPaletteSet =>
            RibbonServices.RibbonPaletteSet;
         public RibbonControl RibbonControl =>
            RibbonPaletteSet?.RibbonControl;
      }

   }
}