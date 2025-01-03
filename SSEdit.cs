/// ssedit.cs  (c)2012 Tony Tanzillo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace Autodesk.AutoCAD.EditorInput.MyExtensions
{
   public static class EditorExtensionMethods
   {
      public static PromptSelectionResult EditSelection(this Editor ed, SelectionSet ss, PromptSelectionOptions options = null)
      {
         return new SelectionSetEditor(ed, ss, options).EditSelection();
      }

      public static PromptSelectionResult EditSelection(this Editor ed, IEnumerable<ObjectId> ids, PromptSelectionOptions options = null)
      {
         return new SelectionSetEditor(ed, ids, options).EditSelection();
      }
   }

   class SelectionSetEditor
   {
      Editor ed = null;
      PromptSelectionOptions options = null;
      IEnumerable<ObjectId> selection = null;

      public SelectionSetEditor(Editor editor, SelectionSet ss, PromptSelectionOptions options = null)
         : this(editor, ss != null ? ss.GetObjectIds() : null, options)
      {
      }

      public SelectionSetEditor(Editor editor, IEnumerable<ObjectId> ss, PromptSelectionOptions options = null)
      {
         if(editor == null)
            throw new ArgumentNullException("editor");
         if(ss == null)
            throw new ArgumentNullException("ss");
         this.ed = editor;
         this.options = options ?? new PromptSelectionOptions();
         this.options.Keywords.Add("Edit", "Edit", "Edit", false, true);
         this.options.KeywordInput += keywordInput;
         this.selection = ss;
      }

      private void keywordInput(object sender, SelectionTextInputEventArgs e)
      {
         options.KeywordInput -= keywordInput;
         e.AddObjects(selection.ToArray());
      }

      public PromptSelectionResult EditSelection()
      {
         if(this.selection.Any())
         {
            /// A kludge that triggers a call to the keywordInput() method:
            ed.Document.SendStringToExecute("Edit\n", true, false, false);
         }
         return ed.GetSelection(options);
      }
   }

   public static class SSEditExampleCommands
   {
      [CommandMethod("SSEDIT")]
      public static void SSEdit()
      {
         try
         {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            /// Get an initial selection set from the
            /// user that we will then allow them to edit:
           
            Editor ed = doc.Editor;
            ed.WriteMessage("\nSpecify initial selection set to edit,");
            var psr = ed.GetSelection();
            if(psr.Status != PromptStatus.OK)
               return;

            /// Allow the user to edit the selection set
            /// acquired above:

            PromptSelectionResult result = ed.EditSelection(psr.Value);

            if(result != null && result.Value != null)
               doc.Editor.WriteMessage(
                  "\nEdited selection Count = {0}",
                  result.Value.Count);
         }
         catch(System.Exception ex)
         {
            throw ex;
         }
      }
   }
}
