/// RibbonExtensionApplication
/// ActivistInvestor / Tony T
/// 
/// A class that provides a vastly-simplified means 
/// of initializing and managing application-provided
/// content added to AutoCAD's ribbon.
/// 
/// Source:
///   
///   https://github.com/ActivistInvestor/AcMgdUtility/blob/main/RibbonEventManager.cs
///

using System;
using System.Diagnostics;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Ribbon;
using Autodesk.Windows;

namespace Autodesk.AutoCAD.Runtime.AIUtils
{
   /// <summary>
   /// This class provides all of the functionality of the
   /// RibbonExtensionApplication class without requiring an 
   /// IExtensionApplication to be defined. 
   /// 
   /// Instead, a single event can be handled to be notified 
   /// whenever it is necessary to add content to the ribbon.
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
   ///      // Handler for the InitializeRibbon event:
   ///      private void LoadRibbonContent(object sender, RibbonStateEventArgs e)
   ///      {
   ///         // TODO: Add content to ribbon.
   ///      }
   ///
   ///      public void Terminate()
   ///      {
   ///      }
   ///   }
   /// 
   /// The handler for the InitializeRibbon event will be called 
   /// whenever it is necessary to add content to the ribbon, which
   /// includes at startup or when the ribbon is first shown, and 
   /// when a workspace is loaded.
   /// 
   /// </code>
   /// 
   /// </summary>

   public static class RibbonEventManager
   {
      
      static bool initialized = false;
      static event RibbonStateEventHandler initializeRibbon;

      static RibbonEventManager()
      {
         IdleAction<int>.OnIdle((i) => InitializeAsync(), 0);
      }

      static void InitializeAsync()
      {
         if(RibbonControl != null)
            Initialize(RibbonState.Active);
         else
            RibbonServices.RibbonPaletteSetCreated += ribbonPaletteSetCreated;
      }

      static void Initialize(RibbonState state)
      {
         Debug.Assert(!initialized);
         initializeRibbon(RibbonControl, new RibbonStateEventArgs(state));
         RibbonPaletteSet.WorkspaceLoaded += workspaceLoaded;
         initialized = true;
      }

      private static void ribbonPaletteSetCreated(object sender, EventArgs e)
      {
         RibbonServices.RibbonPaletteSetCreated -= ribbonPaletteSetCreated;
         if(Application.DocumentManager.IsApplicationContext)
            Initialize(RibbonState.Initalizing);
         else
            IdleAction<int>.OnIdle((i) => Initialize(RibbonState.Active), 0);
      }

      private static void workspaceLoaded(object sender, EventArgs e)
      {
         if(Application.DocumentManager.IsApplicationContext)
            initializeRibbon(RibbonPaletteSet,
               new RibbonStateEventArgs(RibbonState.WorkspaceLoaded));
         else
            IdleAction<int>.OnIdle((i) => initializeRibbon(RibbonPaletteSet,
               new RibbonStateEventArgs(RibbonState.WorkspaceLoaded)), 0);
      }

      public static event RibbonStateEventHandler InitializeRibbon
      {
         add
         {
            if(value == null)
               throw new ArgumentNullException(nameof(value));

            if(initialized)
            {
               IdleAction<RibbonStateEventHandler>.OnIdle((handler) =>
               {
                  handler(RibbonPaletteSet, new RibbonStateEventArgs(RibbonState.Active));
                  initializeRibbon += handler;
               }, value);
            }
            else
            {
               initializeRibbon += value;
            }
         }
         remove 
         {
            if(value == null)
               throw new ArgumentNullException(nameof(value));
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

      class IdleAction<T>
      {
         Action<T> action;
         T parameter;
         
         public IdleAction(Action<T> action, T parameter)
         {
            this.action = action;
            this.parameter = parameter;
            Application.Idle += idle;
         }

         public static IdleAction<T> OnIdle(Action<T> action, T parameter)
         {
            return new IdleAction<T>(action, parameter);
         }

         void idle(object sender, EventArgs e)
         {
            if(action != null && Application.DocumentManager.MdiActiveDocument != null)
            {
               Application.Idle -= idle;
               action(parameter);
               action = null;
            }
         }
      }

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