/// RibbonEventManager.cs
/// ActivistInvestor / Tony T
/// 
/// A class that provides a simplified means of 
/// initializing and managing application-provided
/// content added to AutoCAD's ribbon.
/// 
/// Source:
///   
///   https://github.com/ActivistInvestor/AcMgdUtility/blob/main/RibbonEventManager/RibbonEventManager.cs
///

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
// using Autodesk.AutoCAD.ApplicationServices.AsyncHelpers;
using Autodesk.AutoCAD.Ribbon;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using AcRx = Autodesk.AutoCAD.Runtime;

namespace Autodesk.AutoCAD.Runtime
{
   /// <summary>
   /// RibbonEventManager exposes a single event that can be
   /// handled to be notified whenever it is necessary to add 
   /// or refresh ribbon content.
   /// 
   /// The InitializeRibbon event:
   /// 
   /// The typical usage pattern for using this event, is to
   /// simply add a handler to it when the application/extension
   /// is loaded (e.g., from an IExtensionApplication.Initialize
   /// method). If that is done, it isn't necessary to check to
   /// see if the ribbon exists, add handlers to other events, etc.. 
   /// One need only add a handler to the RibbonEventManager's 
   /// InitializeRibbon event, and in the handler, add content to 
   /// the ribbon.
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
   ///         // Here, one can safely assume the ribbon exists,
   ///         // and that content should be added to it.
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
   ///   1. When the handler is first added to the 
   ///      InitializeRibbon event and the ribbon 
   ///      currently exists.
   ///   
   ///   2. When the ribbon is first created and shown 
   ///      when it did not exist when the handler was 
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
   /// 2. A new AddRibbonTabs() method was added to the event
   ///    argument type (RibbonStateEventArgs), that will add
   ///    one or more ribbon tabs to the ribbon if they are not
   ///    already present on the ribbon.
   ///    
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
   ///    - CUILOAD/CUIUNLOAD commands.
   ///    
   /// In all of the above cases, the InitializeRibbon 
   /// event is raised to signal that content should be
   /// added to the ribbon.
   /// 
   /// To summarize, if your app adds content to the ribbon
   /// and you want to ensure that it is always added when
   /// needed, just handle the InitializeRibbon event, and 
   /// add the content to the ribbon in the event's handler.
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
         if(RibbonCreated)
            Initialize(RibbonState.Active);
         else
            RibbonServices.RibbonPaletteSetCreated += ribbonPaletteSetCreated;
      }

      static async void Initialize(RibbonState state)
      {
         await RaiseInitializeRibbon(state);
         try
         {
            RibbonPaletteSet.WorkspaceLoaded += workspaceLoaded;
            initialized = true;
         }
         catch(System.Exception ex) 
         {
            ex.ShowDialog();
         }
      }
      
      static async Task RaiseInitializeRibbon(RibbonState state)
      {
         if(initializeRibbon != null)
         {
            await WaitForApplicationContext();
            try
            {
               initializeRibbon?.Invoke(RibbonPaletteSet, new RibbonStateEventArgs(state));
            }
            catch(System.Exception ex)
            {
               ex.ShowDialog();
            }
         }
      }

      private static void ribbonPaletteSetCreated(object sender, EventArgs e)
      {
         RibbonServices.RibbonPaletteSetCreated -= ribbonPaletteSetCreated;
         Initialize(RibbonState.Initalizing);
      }

      private static async void workspaceLoaded(object sender, EventArgs e)
      {
         if(RibbonControl != null)
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
         try
         {
            handler(RibbonPaletteSet, new RibbonStateEventArgs(RibbonState.Active));
         }
         catch(Exception ex)
         {
            UnhandledExceptionFilter.CerOrShowExceptionDialog(ex);
            return;
         }
         initializeRibbon += handler;
      }

      public static bool RibbonCreated => RibbonControl != null;

      public static RibbonPaletteSet RibbonPaletteSet =>
         RibbonServices.RibbonPaletteSet;

      public static RibbonControl? RibbonControl =>
         RibbonPaletteSet?.RibbonControl;

      /// Helper classes and methods excerpted from 
      /// DocumentCollectionExtensions.cs
      /// 
      /// <summary>
      /// Delays execution of code that follows an 
      /// awaited call to this method until the next 
      /// Idle event is raised.
      /// </summary>

      public async static Task WaitForIdle()
      {
         await new IdleAwaiter(); 
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
      
      public async static Task WaitForApplicationContext()
      {
         if(!documents.IsApplicationContext)
            await IdleAwaiter.WaitOne();
      }

      /// <summary>
      /// A class that implements a simple means of delaying 
      /// execution of code until the next Idle event is raised.
      /// This class eliminates the need to implement a handler 
      /// for the Idle event and add/remove it to/from the event.
      /// 
      /// Instead, to delay the execution of an arbitrary block
      /// of code until the next Idle event is raised, one only 
      /// needs to call 'await IdleAWaiter.WaitOne()'. Any code 
      /// immediately following that awaited call will not run 
      /// until the next Idle event is raised.
      /// </summary>

      public class IdleAwaiter : INotifyCompletion
      {
         static ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();

         public void OnCompleted(Action continuation)
         {
            bool flag = actions.Count == 0;
            actions.Enqueue(continuation);
            if(flag && actions.Count > 0)
               Application.Idle += idle;
         }

         static void idle(object sender, EventArgs e)
         {
            Action continuation = null;
            if(actions.TryDequeue(out continuation) && continuation != null)
            {
               if(actions.Count == 0)
                  Application.Idle -= idle;
               try
               {
                  continuation.Invoke();
               }
               catch(System.Exception ex)
               {
                  Application.DocumentManager.MdiActiveDocument?.
                     Editor.WriteMessage(ex.ToString());
                  // Cannot rethrow the exception
               }
            }
         }

         /// <summary>
         /// Executes the awaited continuation when 
         /// the next Application.Idle event is raised.
         /// </summary>
         /// <returns></returns>

         public static IdleAwaiter WaitOne()
         {
            return new IdleAwaiter();
         }

         public IdleAwaiter GetAwaiter() { return this; }

         public bool IsCompleted { get { return false; } }

         public void GetResult() { }

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

   public static partial class ExceptionExtensions
   {
      public static System.Exception ShowDialog(this System.Exception ex)
      {
         if(ex != null)
         {
            UnhandledExceptionFilter.CerOrShowExceptionDialog(ex);
         }
         return ex;
      }
   }

}