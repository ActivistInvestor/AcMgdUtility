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
      /// exposes it as an asynchronous, await-able method:
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
      ///       // wait for an Idle event to be raised:
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
         Document active = docs.MdiActiveDocument;
         var source = new TaskCompletionSource<object>();
         Application.Idle += idle;
         source.Task.Wait();
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
      /// immediately upon calling this method. If it returns
      /// true, the method returns immediately without entering
      /// an asynchrnous wait state.
      /// 
      /// If the initial call to the predicate returns false,
      /// the predicate is evaluated every time the Idle event
      /// is raised, and this method returns when the predicate
      /// returns true.
      /// </summary>
      /// <param name="docs">The DocumentCollection</param>
      /// <param name="predicate">A delegate that returns a value
      /// indicating if this method should return, or continue to
      /// handle Idle events.</param>
      /// <returns>A Task representing the asynchronous operation</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static Task WaitUntil(this DocumentCollection docs, Func<bool> predicate)
      {
         if(docs == null)
            throw new ArgumentNullException(nameof(docs));
         if(predicate == null)
            throw new ArgumentNullException(nameof(predicate));
         if(predicate())
            return Task.CompletedTask;
         var source = new TaskCompletionSource<object>();
         Application.Idle += idle;
         source.Task.Wait();
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
      /// The predicate is evaluated before entering the 
      /// asynchrnous wait state and in that case, if it 
      /// returns true, this method returns immediately
      /// without waiting. 
      /// 
      /// Usage:
      /// <code>
      /// 
      ///    public static async void MyMethod()
      ///    {
      ///       var DocMgr = Application.DocumentManager;
      ///       
      ///       // Waits until the Editor is in a
      ///       // a quiescent state:
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
      /// the default value of documentRequired (true), to tell
      /// this method to wait for an active document before it
      /// evaluates the predicate.
      /// </remarks>
      /// </summary>
      /// <param name="docs">The Document that is active at
      /// the point when the given predicate is called.</param>
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
      /// no active document and this method will continue to wait.</param>
      /// <returns>A Task representing the asynchronous operation</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static Task WaitUntil(this DocumentCollection docs, 
         Func<Document, bool> predicate,
         bool documentRequired = true)
      {
         if(docs == null)
            throw new ArgumentNullException(nameof(docs));
         if(predicate == null)
            throw new ArgumentNullException(nameof(predicate));
         if((!documentRequired || docs.MdiActiveDocument != null) && predicate(docs.MdiActiveDocument))
            return Task.CompletedTask;
         var source = new TaskCompletionSource<object>();
         Application.Idle += idle;
         source.Task.Wait();
         return source.Task;

         void idle(object sender, EventArgs e)
         {
            Document doc = docs.MdiActiveDocument;
            if((!documentRequired || doc != null) && predicate(doc))
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
