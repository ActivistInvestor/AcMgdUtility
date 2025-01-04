/// ssedit.cs  (c)2012 Tony Tanzillo
/// Originally posted on the Autodesk discussion groups in 2012.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace Autodesk.AutoCAD.EditorInput.MyExtensions
{
   public static partial class EditorExtensionMethods
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

   /// <summary>
   /// A class that allows a selection of objects to be 
   /// edited interactively, allowing the user to remove
   /// and/or add objects to the selection.
   /// </summary>
   
   public class SelectionSetEditor
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
         if(editor is null)
            throw new ArgumentNullException("editor");
         if(ss is null)
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
         options.Keywords.Clear();
         e.AddObjects(selection.ToArray());
      }

      public PromptSelectionResult EditSelection()
      {
         if(this.selection.Any())
         {
            ed.Document.SendStringToExecute("Edit\n", true, false, false);
         }
         return ed.GetSelection(options);
      }
   }

   /// <summary>
   /// An example showing how the SelectionSetEditor class is used:
   /// </summary>
   
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
