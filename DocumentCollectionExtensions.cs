using System;
using System.Threading.Tasks;

namespace Autodesk.AutoCAD.ApplicationServices.AsyncHelpers
{
   /// <summary>
   /// DocumentCollectionExtensions.cs:
   /// 
   /// Source location:
   /// 
   ///   https://github.com/ActivistInvestor/AcMgdUtility/blob/main/DocumentCollectionExtensions.cs
   /// 
   /// Exposes helper APIs as extension methods that
   /// target the DocumentCollection class. 
   /// 
   /// Updated 5/10/24: Removed code that was added
   /// to WaitXxxx() methods for testing purposes, and 
   /// was inadvertently not removed. 
   /// 
   /// Additional extension methods will be added to 
   /// this class once they have been documented.
   /// 
   /// Notes: 
   /// 
   /// Some of the methods of this class depart from 
   /// the standard practice of naming asynchronous,
   /// awaitable methods with the suffix "Async". 
   /// 
   /// The reason is mainly due to the fact that the 
   /// asynchronous nature of these methods is implied 
   /// by the fact that their names start with "Wait".
   /// I don't wanna hear it ;)
   /// </summary>

   public static partial class DocumentCollectionExtensions
   {
      /// <summary>
      /// Indicates if an operation can execute based on
      /// specified conditions.
      /// <param name="docs">The DocumentCollection</param>
      /// <param name="document">A value indicating if an 
      /// active document is required to execute the operation</param>
      /// <param name="quiescent">A value indicating if a
      /// quiescent active document is required to execute
      /// the operation</param>
      /// <remarks>
      /// if quiescent is true, document is not evaluated
      /// and is implicitly true.
      /// If there is no active document, quiescent is not 
      /// evaluated and is implicitly false.
      /// </remarks>
      /// </summary>

      public static bool CanInvoke(this DocumentCollection docs,
         bool quiescent = false, bool document = true)
      {
         if(docs == null)
            throw new ArgumentNullException(nameof(docs));
         document |= quiescent;
         Document doc = docs.MdiActiveDocument;
         return doc == null ? !document
            : !quiescent || docs.MdiActiveDocument.Editor.IsQuiescent;
      }

      /// <summary>
      /// Wraps a handler for the Application.Idle event and
      /// exposes it as an asynchronous, awaitable method:
      /// 
      /// An awaited call to this method will not return 
      /// until the next Idle event is raised, and there 
      /// is an active document.
      ///
      /// Usage:
      /// 
      ///    public static async void MyMethod()
      ///    {
      ///       var DocMgr = Application.DocumentManager;
      ///       
      ///       // wait for the next Idle event to be raised:
      ///       
      ///       await DocMgr.WaitForIdle();
      ///       
      ///       // Code appearing here will not run until 
      ///       // the next Idle event is raised and there
      ///       // is an active document.
      ///       
      ///       var doc = DocMgr.MdiActiveDocument;       
      ///       doc.Editor.WriteMessage("An Idle event was raised.");
      ///    }
      ///    
      /// </summary>
      /// <param name="docs">The DocumentCollection</param>
      /// <param name="quiescentRequired">A value indicating
      /// if the method should wait until there is an active
      /// document, and it is in a quiescent state.</param>
      /// <param name="documentRequired">A value indicating if the
      /// method should continue to wait until there is an active 
      /// document. If this value is true and there is no active
      /// document when the Idle event is raised, the method will
      /// not return until an Idle event is raised and there is
      /// an active document.</param>
      /// <returns>A Task representing the asynchronous operation</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static Task WaitForIdle(this DocumentCollection docs, 
         bool quiescentRequired = false,
         bool documentRequired = true)
      {
         if(docs == null)
            throw new ArgumentNullException(nameof(docs));
         var source = new TaskCompletionSource<object>();
         Application.Idle += idle;
         return source.Task;

         void idle(object sender, EventArgs e)
         {
            Document doc = docs.MdiActiveDocument;
            if(CanInvoke(docs, quiescentRequired, documentRequired))
            {
               Application.Idle -= idle;
               source.TrySetResult(null);
            }
         }
      }

      /// <summary>
      /// Takes a predicate and asynchronously waits until the
      /// predicate returns true. The predicate is evaluated
      /// each time the Idle event is raised, and optionally,
      /// when this method is called. This method will return
      /// when a call to the predicate returns true.
      /// 
      /// <remarks>
      /// The behavior of this API when called from the document
      /// execution context is undefined.
      /// </remarks>
      /// </summary>
      /// <param name="docs">The DocumentCollection</param>
      /// <param name="waitForIdle">A value indicating if the given
      /// predicate should be evaluated immediately when this method
      /// is called. If this value is true, the predicate will be
      /// evaluated when this method is called, before entering into
      /// an asynchronous wait state. If the predicate evaluates to 
      /// true, the method returns immediately without entering into
      /// the asynchronous wait state. If this value is false, the
      /// predicate is not evaluated until the first Idle event has
      /// been raised. The default value is false</param>
      /// <param name="predicate">A predicate that returns a value
      /// indicating if this method should return, or continue to
      /// handle Idle events. This predicate is evaluated on each 
      /// Idle event until it returns true, at which point this 
      /// method returns.</param>
      /// <returns>A Task representing the asynchronous operation</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static Task WaitUntil(this DocumentCollection docs, 
         bool waitForIdle, Func<bool> predicate)
      {
         if(docs == null)
            throw new ArgumentNullException(nameof(docs));
         if(predicate == null)
            throw new ArgumentNullException(nameof(predicate));
         if(!waitForIdle && predicate())
            return Task.CompletedTask;
         var source = new TaskCompletionSource<object>();
         Application.Idle += idle;
         return source.Task;

         void idle(object sender, EventArgs e)
         {
            if(predicate())
            {
               Application.Idle -= idle;
               source.TrySetResult(null);
            }
         }
      }

      /// <summary>
      /// Overload of the above that passes a default
      /// value of false for the waitForIdle argument.
      /// </summary>

      public static Task WaitUntil(this DocumentCollection docs, Func<bool> predicate)
      {
         return WaitUntil(docs, false, predicate);
      }

      /// <summary>
      /// An overloaded version of WaitUntil() that requires
      /// a predicate that takes a Document as an argument.
      ///
      /// Waits until the supplied predicate returns true 
      /// and there is an active document. The predicate is
      /// passed the Document that is active at the point 
      /// when the predicate is invoked. That may not be the
      /// same document that was active when this method was
      /// called.
      /// 
      /// Notes: 
      /// 
      /// If there is an active document when this method 
      /// is called, the method will evaluate the predicate 
      /// before entering an asynchronous wait state, and if 
      /// the predicate returns true, this method returns 
      /// immediately, without entering an asynchronous wait 
      /// state.
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
      ///       await DocMgr.WaitUntil(doc => doc.Editor.IsQuiescent);
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
      /// IExtensionApplication.Initialize) method, it can use
      /// the default value of documentRequired (true), to wait 
      /// for an active document before evaluating the predicate.
      /// 
      /// Caveats:
      /// 
      /// If this method is called from the document execution
      /// context (which is not recommended), code that follows 
      /// the awaited call executes in the application context.
      /// 
      /// </remarks>
      /// </summary>
      /// <param name="docs">The DocumentsCollection</param>
      /// <param name="predicate">A method taking a Document 
      /// as its argument, that returns a value indicating if
      /// this method should return, or continue to wait.
      /// The predicate will be evaluated immediately upon
      /// calling this method, and if it returns true, this
      /// method returns immediately. Otherwise, the predicate 
      /// will be evaluated each time the Idle event is raised, 
      /// and this method will not return until the predicate
      /// returns true.</param>
      /// <param name="documentRequired">A value indicating if the 
      /// given predicate should be evaluated if there is no active
      /// document. If this value is false and there is no active
      /// document, the predicate will be passed null, and should
      /// check its argument before attempting to use it. If this
      /// value is true, the predicate is not called if there is
      /// no active document and this method will continue to wait.
      /// The default value is true</param>
      /// <param name="waitForIdle">A value indicating if the given
      /// predicate should be evaluated immediately when this method
      /// is called. If this value is true, the predicate will be
      /// evaluated when this method is called, before entering into
      /// an asynchronous wait state. If the predicate evaluates to 
      /// true, the method returns immediately without entering into
      /// the asynchronous wait state. If this value is false, the
      /// predicate is not evaluated until the first Idle event has
      /// been raised. The default value is true.</param>
      /// <returns>A Task representing the asynchronous operation</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static Task WaitUntil(this DocumentCollection docs, 
         Func<Document, bool> predicate,
         bool documentRequired = true,
         bool waitForIdle = true)
      {
         if(docs == null)
            throw new ArgumentNullException(nameof(docs));
         if(predicate == null)
            throw new ArgumentNullException(nameof(predicate));
         if(!waitForIdle && Evaluate())
            return Task.CompletedTask;
         var source = new TaskCompletionSource<object>();
         Application.Idle += idle;
         return source.Task;

         bool Evaluate()
         {
            Document doc = docs.MdiActiveDocument;
            return (!documentRequired || doc != null) && predicate(doc);
         }

         void idle(object sender, EventArgs e)
         {
            Document doc = docs.MdiActiveDocument;
            if(Evaluate())
            {
               Application.Idle -= idle;
               source.TrySetResult(null);
            }
         }
      }


      /// <summary>
      /// Wrapper for ExecuteInCommandContextAsync().
      /// 
      /// Executes the given action in the document/command 
      /// context. If called from the application context, the
      /// command executes asynchronously and callers should
      /// not rely on side effects of the action which may not
      /// execute until after the calling code returns.
      /// </summary>
      /// <param name="docs">The DocumentCollection</param>
      /// <param name="action">The Action to execute</param>
      /// <exception cref="ArgumentNullException"></exception>

      public static void InvokeAsCommand(this DocumentCollection docs, Action action)
      {
         if(docs == null)
            throw new ArgumentNullException(nameof(docs));
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         if(docs.IsApplicationContext)
         {
            docs.ExecuteInCommandContextAsync((o) =>
            {
               action();
               return Task.CompletedTask;
            }, null);
         }
         else
         {
            action();
         }
      }

      /// <summary>
      /// An await-able version of InvokeAsCommand() that 
      /// can be awaited by the caller.
      /// </summary>
      /// <param name="docs">The DocumentCollection</param>
      /// <param name="action">The Action to execute</param>
      /// <exception cref="ArgumentNullException"></exception>

      public static async void InvokeAsCommandAsync(this DocumentCollection docs, Action action)
      {
         if(docs == null)
            throw new ArgumentNullException(nameof(docs));
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         if(!docs.IsApplicationContext)
         {
            action();
         }
         else
         {
            await docs.ExecuteInCommandContextAsync((o) =>
            {
               action();
               return Task.CompletedTask;
            }, null);
         }
      }

   }
}
