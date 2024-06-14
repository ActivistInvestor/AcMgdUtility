using System;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using AcRx = Autodesk.AutoCAD.Runtime;

namespace Autodesk.AutoCAD.ApplicationServices
{
   public static partial class AsyncDocumentCollectionExtensions
   {
      /// <summary>
      /// A wrapper for ExecuteInCommandContextAsync()
      /// that can be called from any execution context.
      /// 
      /// Executes the given action in the document/command 
      /// context. If called from the application context, the
      /// command executes asynchronously and callers should
      /// not rely on side effects of the action which will not
      /// execute until after the calling code returns.
      /// 
      /// <remarks>
      /// ExecuteInCommandContextAsync() is a highly-volatile API
      /// that can cause AutoCAD to terminate, if is not used with
      /// extreme care. Any exception thrown by the delegate that
      /// is passed to that API if not handled, will cause AutoCAD
      /// to terminate.
      /// 
      /// For this reason, this wrapper catches exceptions thrown 
      /// by the delegate and supresses them. When an exception is 
      /// thrown by the delegate, the standard .NET exception dialog 
      /// is displayed. However, the caller of this method cannot 
      /// trap an exception thrown by the delegate at the calling 
      /// level because this method executes synchronously after the 
      /// call returns and control is returned to AutoCAD.
      /// </remarks>
      /// </summary>
      /// <param name="docs">The DocumentCollection</param>
      /// <param name="action">A delegate that takes a Document as a
      /// parameter, represeting the active document at the point when
      /// the delgate executes.</param>
      /// <exception cref="ArgumentNullException"></exception>

      public static void InvokeAsCommand(this DocumentCollection docs,
         Action<Document> action,
         bool showExceptionDialog = true)
      {
         if(docs == null)
            throw new ArgumentNullException(nameof(docs));
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         Document doc = docs.MdiActiveDocument;
         if(doc == null)
            throw new AcRx.Exception(AcRx.ErrorStatus.NoDocument);
         if(docs.IsApplicationContext)
         {
            docs.ExecuteInCommandContextAsync(delegate (object o)
            {
               try
               {
                  action(doc);
                  return Task.CompletedTask;
               }
               catch(System.Exception ex)
               {
                  if(showExceptionDialog)
                     UnhandledExceptionFilter.CerOrShowExceptionDialog(ex);
                  return Task.FromException(ex);
               }
            }, null);
         }
         else
         {
            action(doc);
         }
      }



      /// <summary>
      /// An asynchronous / awaitable version of InvokeAsCommand()
      /// 
      /// This method can be awaited so that code that follows the
      /// awaited call does not execute until the delegate passed to
      /// this method has executed and returned. 
      /// 
      /// Use this method when there is code that is dependent on 
      /// side-effects of the delegate, and that code must execute 
      /// in the application context.
      /// 
      /// <remarks>
      /// 
      /// Handling Exceptions:
      /// 
      /// This API mitigates a significant problem associated with
      /// the use of async/await in AutoCAD. If you try the example
      /// code that's included, you'll see that problem, which is
      /// that exceptions that are thrown by delegates passed to an
      /// awaited call to ExecuteInCommandContextAsync() cannot be 
      /// caught by the calling code. In fact, that problem is not
      /// specific to ExecuteInCommandContextAsync(). It applies to
      /// any use of await in AutoCAD managed code, where a delegate
      /// is passed to an asynchrnous awaited method.
      /// 
      /// In addition to the problem of not being able to handle an
      /// exception thrown by a delegate, exceptions that are thrown
      /// by continuations that follow an awaited call to any async
      /// method will terminate AutoCAD if the exception isn't caught
      /// and handled by an enclosing try/catch.
      /// 
      /// The InvokeAsCommandAsync() wrapper solves that problem by
      /// propagating exceptions thrown by the delegate passed to it, 
      /// back to the caller, which can easily catch and handle them 
      /// using try/catch.
      /// 
      /// For that reason, the code that calls InvokeAsCommandAsync()
      /// must always enclose calls to that method within a try block, 
      /// followed by a catch() block that handles all exceptions and 
      /// does _not_ re-throw them. Failure to do that will result in
      /// AutoCAD terminating if an exception is thrown in either the
      /// delegate, or the continuation that follows an awaited call
      /// to this method.
      /// 
      /// The required try block should contain the awaited call to 
      /// this method, along with any continuation statements that
      /// are to execute after the delegate has executed. Exceptions
      /// thrown by the delegate or by a continuation statement must
      /// be handled by the catch() block, and the catch() block must
      /// NOT re-throw any exceptions that are caught.
      /// 
      /// Minimal example:
      /// 
      /// <code>
      /// 
      ///   public static async void MyMethod()
      ///   {
      ///      try                   // this is NOT optional
      ///      {
      ///         await docs.InvokeAsCommandAsync(doc => 
      ///            doc.Editor.Command("._REGEN"));
      ///            
      ///         // do stuff here after the REGEN command has completed.
      ///      }
      ///      catch(System.Exception ex)
      ///      {
      ///         // deal with the exception and do NOT re-throw it!!!
      ///      }
      ///   }
      ///   
      /// </code>
      /// 
      /// In the above example, if an exception is thrown by the delegate, or
      /// by the code that follows the awaited call to InvokeAsCommandAsync(),
      /// it will be caught by the catch() block, allowing the caller to deal
      /// with it accordingly. The catch() block must not re-throw exceptions,
      /// which is essentially the same as not having a try/catch block at all.
      /// 
      /// InvokeAsCommandAsync() serves several useful purposes:
      /// 
      /// 1.  It can be called from either the application or document 
      ///     context with indifference (ExecuteInDocumentContextAsync() 
      ///     can only be called from the application context).
      ///     
      /// 2.  It handles exceptions thrown by the delegate and propagates
      ///     them back to its caller, which ExecuteInCommandContextAsync()
      ///     fails to do, causing AutoCAD to terminate.
      ///     
      /// 3.  If an exception is thrown by the delegate, any continuation
      ///     that follows an awaited call does not execute, which is the 
      ///     proper behavior.
      ///    
      /// </remarks>
      /// </summary>
      /// <param name="docs">The DocumentCollection</param>
      /// <param name="action">A delegate that takes a Document as a
      /// parameter, represeting the active document at the point when
      /// the delgate executes.</param>
      /// <returns>A task representing the the asynchronous operation</returns>
      /// <exception cref="Autodesk.AutoCAD.Runtime.Exception">There is no
      /// active document</exception>
      /// <exception cref="ArgumentNullException">A required parameter was null</exception>

      public static async Task InvokeAsCommandAsync(this DocumentCollection docs,
         Action<Document> action)
      {
         if(docs == null)
            throw new ArgumentNullException(nameof(docs));
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         Document doc = docs.MdiActiveDocument;
         if(doc == null)
            throw new AcRx.Exception(AcRx.ErrorStatus.NoDocument);
         if(docs.IsApplicationContext)
         {
            Task result = Task.CompletedTask;
            await docs.ExecuteInCommandContextAsync(delegate (object o)
            {
               try
               {
                  action(doc);
                  return result;
               }
               catch(System.Exception ex)
               {
                  return result = Task.FromException(ex);
               }
            }, null);
            if(result.IsFaulted && result.Exception != null)
               throw result.Exception;
         }
         else
         {
            action(doc);
         }
      }

   }

}
