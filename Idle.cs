using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

/// Idle.cs  ActivistInvestor / Tony Tanzillo
/// 
/// Distributed under the terms of the MIT license
/// 
/// Utilities that aid in the use of AutoCAD's
/// Application.Idle event in various ways.
/// 
/// Original source location:
/// 
///    https://github.com/ActivistInvestor/AcMgdUtility/blob/main/Idle.cs
///
/// 05/12/24: Major refactoring
/// 
/// This unit was refactored to address a shortcoming
/// in the original design, which did not permit custom
/// state to be associated with each handler to the Idle
/// event. The refactored code uses a type derived from
/// TaskCompletionSource<object>, that can be further-
/// specialized to include custom state associated with 
/// each instance.
/// 
/// See the IdleCompletionSource class for details.
/// 
/// O5/17/24: Implementation of the IdleAwaiter class


namespace Autodesk.AutoCAD.ApplicationServices.AsyncHelpers
{
   /// <summary>
   /// Provides support for asynchronous operations 
   /// that are driven by the Application.Idle event.
   /// </summary>

   public static class Idle
   {
      /// <summary>
      /// Wraps a handler for the Application.Idle event and
      /// allows the body of the event handler to be expressed 
      /// as code that follows an awaited call to this method.
      /// 
      /// Hence, awaited calls to this method will not return 
      /// until the next Idle event is raised and optionally,
      /// there is an active document that optionally, is in
      /// a quiescent state. 
      /// 
      /// The default values for the optional arguments require 
      /// an active document that does not need to be quiescent.
      /// Callers can pass false to indicate this method should 
      /// return on the first Idle event, regardless of whether
      /// there is an active document or not.
      /// 
      /// Usage:
      /// 
      ///    public static async void MyMethod()
      ///    {
      ///       var DocMgr = Application.DocumentManager;
      ///       
      ///       // wait for the next Idle event to be raised:
      ///       
      ///       await Idle.WaitAsync();
      ///       
      ///       // Code appearing here will not run until 
      ///       // the next Idle event is raised, and there
      ///       // is an active document. The following code
      ///       // can assume that MdiActiveDocument is not 
      ///       // null.
      ///       
      ///       var doc = DocMgr.MdiActiveDocument;       
      ///       doc.Editor.WriteMessage("An Idle event was raised.");
      ///    }
      ///    
      /// <remarks>
      /// if quiescenceRequired is true, documentRequired is 
      /// not evaluated and is implicitly true.
      /// If there is no active document, quiescenceRequired
      /// is not evaluated and is implicitly false.
      /// </remarks>
      /// </summary>
      /// <param name="quiescenceRequired">A value indicating
      /// if the method should wait until there is an active
      /// document, and it is in a quiescent state.</param>
      /// <param name="documentRequired">A value indicating if the
      /// method should continue to wait until there is an active 
      /// document. If this value is true and there is no active
      /// document when the Idle event is raised, the method will
      /// not return until an Idle event is raised and there is
      /// an active document.</param>
      /// 
      /// <returns>A Task representing the asynchronous operation</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static Task WaitAsync(
         bool quiescenceRequired = false,
         bool documentRequired = true)
      {
         return new IdleCompletionSource(quiescenceRequired,
            documentRequired).Task;
      }

      /// <summary>
      /// Simple, no-frills means of asynchrnously waiting 
      /// until the next Application.Idle event is raised, 
      /// with no conditions.
      /// </summary>

      public static Task WaitForIdle()
      {
         return new IdleCompletionSource(false, false).Task;
      }

      /// <summary>
      /// Waits until there is an active document
      /// </summary>

      public static Task WaitForDocument()
      {
         return new IdleCompletionSource(false, true).Task;
      }

      /// <summary>
      /// Waits until there is a quiescent active document
      /// </summary>

      public static Task WaitForQuiescentDocument()
      {
         return new IdleCompletionSource(true, true).Task;
      }

      /// <summary>
      /// Takes a predicate and asynchronously waits until the
      /// predicate evaluates to true. The predicate is evaluated
      /// each time the Idle event is raised and optionally, when 
      /// this method is called. This method returns when a call 
      /// to the predicate returns true.
      /// 
      /// <remarks>
      /// The behavior of this API when called from the document
      /// execution context is undefined.
      /// </remarks>
      /// </summary>
      /// <param name="waitForIdle">A value indicating if the given
      /// predicate should not be evaluated until the next idle event 
      /// is raised. If this value is false, the predicate will be 
      /// evaluated immediately upon calling this method, before it
      /// enters an asynchronous wait state, and if the predicate 
      /// evaluates to true, the method returns immediately. If this 
      /// value is true, the predicate is not evaluated until the 
      /// next Idle event is raised. The default value is false</param>
      /// <param name="predicate">A predicate that returns a value
      /// indicating if this method should return, or continue to
      /// wait for additional Idle events to be raised. The predicate 
      /// is evaluated each time the Idle event is raised, until it 
      /// returns true, at which point this method returns.</param>
      /// <returns>A Task representing the asynchronous operation</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static Task WaitUntil(bool waitForIdle, Func<bool> predicate)
      {
         if(predicate == null)
            throw new ArgumentNullException(nameof(predicate));
         if(!waitForIdle && predicate())
            return Task.CompletedTask;
         else
            return new IdleCompletionSource(predicate).Task;
      }

      /// <summary>
      /// Overload of WaitUntil() that passes a default value
      /// of false for the waitForIdle argument, causing it to
      /// evaluate the predicate immediately, and short-circuit 
      /// the asynchronous wait if the predicate returns true.
      /// </summary>

      public static Task WaitUntil(Func<bool> predicate)
      {
         return WaitUntil(false, predicate);
      }

      /// <summary>
      /// An overloaded version of WaitUntil() that requires
      /// a predicate that takes a Document as an argument.
      ///
      /// Waits until there is an active document, and the
      /// supplied predicate returns true. The predicate is
      /// passed the Document that is active at the point 
      /// when the predicate is invoked. That may not be the
      /// same document that was active when this method was
      /// called.
      /// 
      /// Usage:
      /// <code>
      /// 
      ///    public static async void MyMethod()
      ///    {
      ///       var DocMgr = Application.DocumentManager;
      ///       
      ///       // Waits until there is an active document
      ///       // that is in a quiescent state:
      ///       
      ///       await Idle.WaitUntil(doc => doc.Editor.IsQuiescent);
      ///       
      ///       // Code appearing here will not run until 
      ///       // the next Idle event is raised; there is
      ///       // an active document; and that document
      ///       // is in a quiescent state.
      ///       
      ///       var doc = DocMgr.MdiActiveDocument;       
      ///       doc.Editor.WriteMessage("The Drawing Editor is quiescent");
      ///    }
      ///    
      /// </code>
      /// <remarks>
      /// If this method is called at startup (such as from an
      /// IExtensionApplication.Initialize() method), it can use
      /// the default value of documentRequired (true), to wait 
      /// for an active document before evaluating the predicate.
      /// 
      /// The behavior of this method if called from the document 
      /// execution context is undefined.
      /// 
      /// </remarks>
      /// </summary>
      /// <param name="predicate">A method taking a Document 
      /// as its argument, that returns a value indicating if
      /// this method should return, or continue to wait.
      /// </param>
      /// <param name="waitForIdle">A value indicating if the given
      /// predicate should be evaluated immediately when this method
      /// is called. If this value is false, the predicate will be
      /// evaluated when this method is called, before entering into
      /// an asynchronous wait state. If the predicate evaluates to 
      /// true, the method returns immediately without entering into
      /// the asynchronous wait state. If this value is true, the
      /// predicate is not evaluated until the first Idle event has
      /// been raised. The default value is true.</param>
      /// <returns>A Task representing the asynchronous operation</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static Task WaitUntil(Func<Document, bool> predicate,
         bool waitForIdle = true)
      {
         if(predicate == null)
            throw new ArgumentNullException(nameof(predicate));
         Document doc = ActiveDocument;
         if(!waitForIdle && doc != null && predicate(doc))
            return Task.CompletedTask;
         else
            return new IdleCompletionSource(() =>
               ActiveDocument != null && predicate(ActiveDocument)).Task;
      }

      /// <summary>
      /// Indicates if an operation can execute based on
      /// specified conditions.
      /// <param name="document">A value indicating if an 
      /// active document is required to execute the operation</param>
      /// <param name="quiescent">A value indicating if a
      /// quiescent active document is required to execute
      /// the operation</param>
      /// <remarks>
      /// if quiescent is true, document is not evaluated
      /// and is effectively true.
      /// If there is no active document, quiescent is not 
      /// evaluated and is effectively false.
      /// </remarks>
      /// </summary>

      public static bool CanInvoke(bool quiescent = false, bool document = true)
      {
         document |= quiescent;
         Document doc = ActiveDocument;
         return doc == null ? !document
            : !quiescent || doc.Editor.IsQuiescent;
      }

      static readonly DocumentCollection docs = Application.DocumentManager;
      static Document ActiveDocument => Application.DocumentManager.MdiActiveDocument;

   }

   /// <summary>
   /// Can be used in place of the base type to simplify 
   /// asynchronous wait-for-idle operations, and perform
   /// custom conditional testing to determine if waiting
   /// should continue, and to associate custom state with 
   /// each instance if needed.
   /// </summary>

   public class IdleCompletionSource : TaskCompletionSource<object>
   {
      Func<bool> predicate = null;

      public IdleCompletionSource(
         bool quiescentRequired = false,
         bool documentRequired = true)
      {
         predicate = () => Idle.CanInvoke(documentRequired, quiescentRequired);
         Application.Idle += idle;
      }

      public IdleCompletionSource(Func<bool> predicate)
      {
         if(predicate == null)
            throw new ArgumentNullException(nameof(predicate));
         this.predicate = predicate;
         Application.Idle += idle;
      }

      /// <summary>
      /// Can be overridden to perform custom/additional
      /// conditional testing to determine if asynchronous
      /// wait should end. 
      /// 
      /// This default implementation for an instance 
      /// created with the default constructor signals 
      /// completion if there is an active document,
      /// quiescent or not.
      /// </summary>

      protected virtual bool IsCompleted
      {
         get
         {
            return predicate();
         }
      }

      private void idle(object sender, EventArgs e)
      {
         if(IsCompleted)
         {
            Application.Idle -= idle;
            TrySetResult(null);
         }
      }
   }

   public static class Foo
   {
      public static async void Main()
      {
         await IdleAwaiter.WaitForIdle();
         Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nAn Idle event just occured");
      }
   }

   /// <summary>
   /// An alternative means of doing what the methods of the 
   /// Idle.WaitForIdle() method does, with greater control 
   /// over how awaited continuation statements execute, but 
   /// lacking any means to conditionally wait.
   /// 
   /// If there are multiple actions queued up, only one of 
   /// them is dequeued and invoked on each call to the Idle 
   /// event handler, so as to not impair UI responsiveness.
   /// </summary>
   
   public struct IdleAwaiter : INotifyCompletion
   {
      static ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();

      static void idle(object sender, EventArgs e)
      {
         Action action = null;
         bool flag = actions.Count > 0;
         if(flag && actions.TryDequeue(out action) && action != null)
         {
            if(actions.Count == 0)
               Application.Idle -= idle;
            SynchronizationContext.Current.Post(
               state => ((Action)state)(), action);
         }
      }

      public static IdleAwaiter WaitForIdle()
      {
         return new IdleAwaiter();
      }

      public IdleAwaiter GetAwaiter() { return this; }

      public bool IsCompleted { get { return false; } }

      public void GetResult() { }

      public void OnCompleted(Action continuation)
      {
         bool flag = actions.Count == 0;
         actions.Enqueue(continuation);
         if(flag && actions.Count > 0)
            Application.Idle += idle;
      }
   }

}
