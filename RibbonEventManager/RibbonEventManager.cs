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
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Autodesk.AutoCAD.Ribbon;
using Autodesk.Windows;
using Autodesk.AutoCAD.Runtime;
using AcRx = Autodesk.AutoCAD.Runtime;

namespace Autodesk.AutoCAD.ApplicationServices.AIUtils
{
   /// <summary>
   /// This class provides the functionality of the 
   /// RibbonExtensionApplication class without requiring 
   /// an IExtensionApplication to be defined. 
   /// 
   /// After several revisions and bug fixes were applied
   /// to this class, its use is highly-preferred over the 
   /// use of RibbonExtensionApplication, which has yet to 
   /// be revised and updated to incorporate the bug fixes 
   /// applied to RibbonEventManager. 
   /// 
   /// It is also simpler to use the RibbonEventManager in 
   /// conjunction with existing IExtensionApplications.
   /// 
   /// RibbonEventManager exposes a single event that can be
   /// handled to be notified whenever it may be necessary to 
   /// add or refresh ribbon content.
   /// 
   /// The InitializeRibbon event:
   /// 
   /// The typical usage pattern for using this event, is to
   /// simply add a handler to it when the application/extension
   /// is loaded (e.g., from an IExtensionApplication.Initialize
   /// method). If that is done, it isn't necessary to check to
   /// see if the ribbon exists. One most only add a handler to
   /// the RibbonEventManager's InitializeRibbon event, and in
   /// the handler, add content to the ribbon.
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
   ///         RibbonEventManager.InitializeRibbon += LoadMyRibbonContent;
   ///      }
   ///      
   ///      private void LoadMyRibbonContent(object sender, RibbonStateEventArgs e)
   ///      {
   ///         // Here, one can safely assume
   ///         // that the ribbon exists.
   ///         
   ///         // TODO: Add content to ribbon.
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
   ///   
   ///   2. When the ribbon is first created and shown 
   ///      if it did not exist when the handler was 
   ///      added to the InitializeRibbon event.
   ///      
   ///   3. When a workspace is loaded, after having 
   ///      added content to the ribbon.
   ///   
   /// The State property of the event argument indicates
   /// which of the these three conditions triggered the
   /// event.
   /// 
   /// 6/4/24 Revisons:
   /// 
   /// 1. The IdleAction class has been replaced with the
   ///    IdleAwaiter class, to defer execution of code 
   ///    until the next Application.Idle event is raised.
   /// 
   /// 2. Previous reports that handlers of the InitializeRibbon
   ///    event were adding multiple instances of ribbon items
   ///    (namely Tabs) to the ribbon have proven to be false,
   ///    and was verified by examining the source code that
   ///    raises the event that triggers the InitializeRibbon
   ///    event to fire.
   ///    
   ///    A workaround for the unlikely possiblity that other 
   ///    unknown circumstances can result in items being added 
   ///    to the ribbon in duplicate has been left intact, as a 
   ///    convenience.
   ///    
   /// This example handler for the InitializeRibbon event 
   /// shows how the AddRibbonTabs() method can be used to add 
   /// a one or more ribbon tabs to the ribbon if they are not 
   /// already present on same:
   /// 
   ///   Create two ribbon tabs and assign 
   ///   them to static members:
   ///   
   /// <code>
   /// 
   ///   static RibbonTab myRibbonTab1 = new RibbonTab {....} 
   ///   static RibbonTab myRibbonTab2 = new RibbonTab {....}
   ///   
   /// </code>
   /// 
   ///   A handler for the RibbonEventManager.InitializeRibbon event:
   /// 
   /// <code>
   /// 
   ///   void initializeRibbon(object sender, RibbonStateEventArgs e)
   ///   {
   ///      e.AddRibbonTabs(myRibbonTab1, myRibbonTab2);
   ///   }
   ///   
   /// </code>
   /// 
   /// Test scenarios covered:
   /// 
   /// 1. Client extension application loaded at startup:
   /// 
   ///    - With ribbon existing at startup.
   ///    
   ///    - With ribbon not existing at startup,
   ///      and subsequently created by issuing 
   ///      the RIBBON command.
   ///       
   /// 2. Client extension application loaded at any point
   ///    during session via NETLOAD or demand-loading when 
   ///    a registered command is first issued:
   ///    
   ///    - With ribbon existing at load-time.
   ///    
   ///    - With ribbon not existing at load-time, 
   ///      and subsequently created by issuing the 
   ///      RIBBON command.
   /// 
   /// 3. With client extension loaded and ribbon content
   ///    already added to an existing ribbon, that is
   ///    subsequently removed by one of these actions:
   ///    
   ///    - CUI command
   ///    - MENULOAD command.
   ///    
   /// In all of the above cases, the InitializeRibbon 
   /// event is raised.
   ///    
   /// Feel free to post comments in the repo discussion
   /// regarding other scenarious not covered, or about
   /// any other issues or bugs you may have come across.
   /// 
   /// </summary>

   public static class RibbonEventManager
   {

      static DocumentCollection documents = Application.DocumentManager;
      static bool initialized = false;
      static event RibbonStateEventHandler initializeRibbon = null;

      static RibbonEventManager()
      {
         if(RibbonControl != null)
            Initialize(RibbonState.Active);
         else
            RibbonServices.RibbonPaletteSetCreated += ribbonPaletteSetCreated;
      }

      static async void Initialize(RibbonState state)
      {
         await RaiseInitializeRibbon(state);
         RibbonPaletteSet.WorkspaceLoaded += workspaceLoaded;
         initialized = true;
      }
      
      static async Task RaiseInitializeRibbon(RibbonState state)
      {
         if(initializeRibbon != null)
         {
            await WaitForApplicationContext();
            initializeRibbon?.Invoke(RibbonPaletteSet, new RibbonStateEventArgs(state));
         }
      }

      private static void ribbonPaletteSetCreated(object sender, EventArgs e)
      {
         RibbonServices.RibbonPaletteSetCreated -= ribbonPaletteSetCreated;
         Initialize(RibbonState.Initalizing);
      }

      private static async void workspaceLoaded(object sender, EventArgs e)
      {
         await RaiseInitializeRibbon(RibbonState.WorkspaceLoaded);
      }

      /// <summary>
      /// If a handler is added to this event and the ribbon 
      /// exists, the handler will be invoked immediately, or
      /// on the next Idle event, depending on the execution
      /// context the handler is added from.
      /// 
      /// Note: Adding the same event handler to this event
      /// multiple times will result in undefined behavior.
      /// </summary>

      public static event RibbonStateEventHandler InitializeRibbon
      {
         add
         {
            if(value == null)
               throw new ArgumentNullException(nameof(value));
            if(initialized)
               InvokeHandler(value);
            else
               initializeRibbon += value;
         }
         remove
         {
            initializeRibbon -= value;
         }
      }

      static async void InvokeHandler(RibbonStateEventHandler handler)
      {
         await WaitForApplicationContext();
         handler(RibbonPaletteSet, new RibbonStateEventArgs(RibbonState.Active));
         initializeRibbon += handler;
      }

      static RibbonPaletteSet RibbonPaletteSet =>
         RibbonServices.RibbonPaletteSet;

      static RibbonControl? RibbonControl =>
         RibbonPaletteSet?.RibbonControl;

      /// Helper classes and methods excerpted from 
      /// DocumentCollectionExtensions.cs
      /// 
      /// <summary>
      /// Delays execution of code that follows an 
      /// awaited call to this method until the next 
      /// Idle event is raised.
      /// </summary>

      public static IdleAwaiter WaitForIdle()
      {
         return new IdleAwaiter();
      }

      /// <summary>
      /// Similar to WaitForIdle(), except that it doesn't
      /// wait if called from the application context.
      /// 
      /// If called from the document context, this method
      /// waits for the next Idle event to be raised before
      /// returning, and the continuation executes in the 
      /// application context. 
      /// 
      /// If called from the application context, this 
      /// method returns immediately. 
      /// 
      /// In any case, the continuation executes in
      /// the application context.
      /// </summary>
      /// <returns></returns>
      
      public static async Task WaitForApplicationContext()
      {
         if(!documents.IsApplicationContext)
            await WaitForIdle();
      }

      /// <summary>
      /// A class that implements a simple means of delaying 
      /// execution of code until the next Idle event is raised.
      /// This class eliminates the need to implement a handler 
      /// for the Idle event and add/remove it from the event.
      /// 
      /// Instead, to delay the execution of an arbitrary block
      /// of code until the next Idle event is raised, one only 
      /// needs to call 'await IdleAWaiter.WaitOne()'. Any code 
      /// immediately following that awaited call will not run 
      /// until the next Idle event is raised.
      /// </summary>
      
      public struct IdleAwaiter : INotifyCompletion
      {
         static ConcurrentQueue<Wrapper> actions = 
            new ConcurrentQueue<Wrapper>();

         public void OnCompleted(Action continuation)
         {
            if(continuation != null)
            {
               bool wasEmpty = actions.Count == 0;
               actions.Enqueue(continuation);
               if(wasEmpty && actions.Count > 0)
                  Application.Idle += idle;
            }
         }

         /// To avoid bogging down the UI, only one 
         /// continuation is dequeued and run each 
         /// time the Idle event is raised.

         static void idle(object sender, EventArgs e)
         {
            bool flag = actions.Count > 0;
            if(flag && actions.TryDequeue(out Wrapper action) && action != null)
            {
               if(actions.Count == 0)
                  Application.Idle -= idle;
               /// Not really necessary, because Idle event 
               /// handlers always run on the main thread:
               AcRx.SynchronizationContext.Current?.Post(
                  (a) => ((Wrapper)a).Invoke(), action);
            }
         }

         /// <summary>
         /// Delays execution of code following an awaited call 
         /// to this method until the next Idle event is raised.
         /// </summary>
         /// <returns></returns>

         public static IdleAwaiter WaitOne()
         {
            return new IdleAwaiter();
         }

         public IdleAwaiter GetAwaiter() { return this; }

         public bool IsCompleted { get { return false; } }

         public void GetResult() { }

         /// <summary>
         /// A wrapper for a deferred Action that executes
         /// when the Idle event is raised. This wrapper is
         /// primarily intended for diagnostic purposes.
         /// </summary>

         public class Wrapper
         {
            DateTime start = DateTime.Now;
            TimeSpan elapsed = TimeSpan.Zero;
            TimeSpan delay = TimeSpan.Zero;
            Action action;

            public Wrapper(Action action)
            {
               this.action = action;
            }

            /// <summary>
            /// The timespan from instance creation to
            /// the point when the action is invoked.
            /// 
            /// Execution time of the action is Elapsed - Delay
            /// </summary>
            public TimeSpan Delay => delay;

            /// <summary>
            /// The timespan from instance creation to 
            /// the point when the invoked action returns.
            /// </summary>
            public TimeSpan Elapsed => elapsed;

            public void Invoke()
            {
               if(action != null)
               {
                  delay = DateTime.Now - start;
                  action();
                  elapsed = DateTime.Now - start;
                  action = null;
               }
            }

            public static implicit operator Action(Wrapper wrapper)
            {
               return wrapper.action;
            }

            public static implicit operator Wrapper(Action action)
            {
               return new Wrapper(action);
            }
         }

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

      /// <summary>
      /// Conditionally adds one or more tabs to the Ribbon
      /// if they are not already present on it.
      /// </summary>
      /// <param name="items">One or more RibbonTab instances</param>
      /// <returns>The number of tabs added to the ribbon</returns>

      public int AddRibbonTabs(params RibbonTab[] items)
      {
         return AddRibbonTabs((IEnumerable<RibbonTab>)items);
      }

      public int AddRibbonTabs(IEnumerable<RibbonTab> items)
      {
         if(RibbonControl == null)
            return 0;
         if(items == null)
            throw new ArgumentNullException(nameof(items));
         return RibbonControl.TryAddTabs(items.ToArray());
      }

      public RibbonState State { get; private set; }
      public RibbonPaletteSet RibbonPaletteSet =>
         RibbonServices.RibbonPaletteSet;
      public RibbonControl RibbonControl =>
         RibbonPaletteSet?.RibbonControl;
   }
}

namespace Autodesk.AutoCAD.Ribbon
{ 
   public static partial class RibbonControlExtensions
   {
      /// <summary>
      /// An extension method targeting the RibbonControl, that 
      /// conditionally adds one or more tabs to the ribbon if 
      /// not already present.
      /// </summary>
      /// <param name="items">One or more RibbonTab instances</param>
      /// <returns>The number of tabs added to the ribbon</returns>

      public static int TryAddTabs(this RibbonControl ribbon, params RibbonTab[] items)
      {
         if(items == null)
            throw new ArgumentNullException(nameof(items));
         return TryAddTabs(ribbon, (IEnumerable<RibbonTab>)items);
      }

      public static int TryAddTabs(this RibbonControl ribbon, IEnumerable<RibbonTab> items)
      {
         if(ribbon == null)
            throw new ArgumentNullException(nameof(ribbon));
         if(items == null)
            throw new ArgumentNullException(nameof(items));
         int result = 0;
         var tabs = ribbon.Tabs;
         foreach(RibbonTab tab in items)
         {
            if(tab == null)
               throw new ArgumentException("null element");
            if(!tabs.Contains(tab))
            {
               ++result;
               tabs.Add(tab);
            }
         }
         return result;
      }

   }

}