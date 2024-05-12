using Autodesk.AutoCAD.ApplicationServices.AsyncHelpers;
using System;
using System.Threading.Tasks;

namespace Autodesk.AutoCAD.ApplicationServices.DocumentExtensions
{
   /// <summary>
   /// DocumentCollectionExtensions.cs:
   /// 
   /// Source location:
   /// 
   ///   https://github.com/ActivistInvestor/AcMgdUtility/blob/main/DocumentCollectionExtensions.cs
   ///   
   /// Prerequisites:
   /// 
   ///   https://github.com/ActivistInvestor/AcMgdUtility/blob/main/Idle.cs
   ///   
   /// Exposes helper APIs as extension methods that
   /// target the DocumentCollection class. 
   /// 
   /// Updated 5/10/24: 
   /// 
   /// Removed code that was added to WaitXxxx() methods 
   /// that was for testing purposes, and inadvertently 
   /// left in the initial distribution.
   /// 
   /// Idle event-related APIs have been refactored out
   /// to a seperate code file (Idle.cs), which this file
   /// is dependent on. The implementation of WaitForIdle()
   /// and WaitUntil() are now members of the Idle class,
   /// and the extension methods of the same name on this
   /// class delegate to the former.
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
   /// </summary>

   public static partial class DocumentCollectionExtensions
   {
      /// <summary>
      /// Wraps a handler for the Application.Idle event and
      /// allows the body of the event handler to be expressed 
      /// as code that follows an awaited call to this method.
      /// 
      /// The code that follows an awaited call to this method
      /// is functionally-equivalent to code within the body of
      /// a handler of the Application.Idle event. This method
      /// automates the task of adding and removing the handler 
      /// for the Idle event, and from that handler, signaling 
      /// that the event was raised, which trigger execution of
      /// the code that follows the awaited call to this method,
      /// in the context of a handler of the event.
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
      ///       await DocMgr.WaitForIdle();
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
         return Idle.WaitForIdle(quiescentRequired, documentRequired);
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
      /// <param name="docs">The DocumentCollection</param>
      /// <param name="waitForIdle">A value indicating if the given
      /// predicate should be evaluated immediately when this method
      /// is called. If this value is true, the predicate will be
      /// evaluated before entering into an asynchronous wait state,
      /// and if the predicate evaluates to true, the method returns 
      /// immediately. If this value is false, the predicate is not 
      /// evaluated until the first Idle event has been raised. The 
      /// default value is false</param>
      /// <param name="predicate">A predicate that returns a value
      /// indicating if this method should return, or continue to
      /// handle Idle events. This predicate is evaluated each time
      /// the Idle event is raised until it returns true, at which 
      /// point this method returns.</param>
      /// <returns>A Task representing the asynchronous operation</returns>
      /// <exception cref="ArgumentNullException"></exception>

      public static Task WaitUntil(this DocumentCollection docs, 
         bool waitForIdle, Func<bool> predicate)
      {
         return Idle.WaitUntil(waitForIdle, predicate);
      }

      /// <summary>
      /// Overload of the above that passes a default
      /// value of false for the waitForIdle argument.
      /// </summary>

      public static Task WaitUntil(this DocumentCollection docs, Func<bool> predicate)
      {
         return Idle.WaitUntil(false, predicate);
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
      /// The behavior of this API when called from the document
      /// execution context is undefined.
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
         return Idle.WaitUntil(predicate, documentRequired, waitForIdle);
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
      /// The asynchronous version of InvokeAsCommand() 
      /// that can be awaited.
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
            : !quiescent || doc.Editor.IsQuiescent;
      }



   }
}
