using Autodesk.AutoCAD.ApplicationServices;
using System;

public class IdleAction
{
   Action action;
   bool quiescenceRequired = false;
   bool documentRequired = false;
   static DocumentCollection docs = Application.DocumentManager;

   /// <summary>
   /// Creates a new instance of an IdleAction
   /// </summary>
   /// <param name="action">The action to execute on the
   /// next raising of the Application.Idle event</param>
   /// <param name="quiescent">A value indicating if 
   /// invocation of the action is deferred until there 
   /// is an active document and it is in a quiescent state. 
   /// <param name="document">A value indicating if invocation 
   /// of the action should be deferred until there is an 
   /// active document.
   /// If <paramref name="quiescent"/> is true, this condition 
   /// is not evaluated and is effectively true.</param>
   /// <exception cref="ArgumentNullException"></exception>

   public IdleAction(Action action, bool quiescent = false, bool document = true)
   {
      this.action = action;
      if(action != null)
      {
         this.documentRequired = document;
         this.quiescenceRequired = quiescent;
         Application.Idle += idle;
      }
   }

   public static IdleAction OnIdle(Action action, bool quiescent = false, bool document = true)
   {
      return new IdleAction(action);
   }

   public static IdleAction OnIdle(bool quiescent, bool document, Action action)
   {
      return new IdleAction(action, quiescent, document);
   }

   public static IdleAction OnIdle(bool quiescent, Action action)
   {
      return new IdleAction(action, quiescent);
   }

   public static IdleAction Invoke(Action action)
   {
      return new IdleAction(action);
   }

   bool CanInvoke
   {
      get
      {
         Document doc = docs.MdiActiveDocument;
         if(doc == null)
            return !quiescenceRequired && !documentRequired;
         return !quiescenceRequired || doc.Editor.IsQuiescent;
      }
   }

   private void idle(object sender, EventArgs e)
   {
      if(action != null && CanInvoke)
      {
         Application.Idle -= idle;
         try { action(); }
         finally { action = null; }
      }
   }
}
