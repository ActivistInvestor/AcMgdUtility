/// ssedit2.cs  (c)2012 Tony Tanzillo
/// 
/// An updated version of ssedit.cs, that was originally 
/// published on the Autodesk discussion group server in 
/// 2012.
/// 
/// A revision was made to eliminate a distracting message
/// from appearing when the EditSelection() extension method
/// is called ("nnn found"). that appeared before the initial
/// "Select objects: " prompt was displayed.
/// 
/// Zoom support:
/// 
/// Support for automatically zooming to the extents of the 
/// selection set to be edited has been added but has not 
/// been tested thoroughly.
/// 
/// The optional third argument to the EditSelection() extension 
/// method specifies if the editor is zoomed to the extents of 
/// the selection set to be edited. The value of this argument 
/// is false by default.
/// 


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AcMgdLib.Overrules.Examples;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;

namespace Autodesk.AutoCAD.EditorInput
{

   /// <summary>
   /// Adds the EditSelection() extension method to the
   /// Editor class, that provides access to the included
   /// SelectionSetEditor class that enables interactive
   /// editing of the contents of an existing selection 
   /// set.
   /// 
   /// When called and passed an existing selection set,
   /// the objects in that selection set are highlighted
   /// and the user can add and/or remove entities to/from
   /// the selection. Optionally, the editor can be zoomed
   /// to the extents of the existing selection set.
   /// 
   /// Usage notes:
   /// 
   /// When this method is called, some or all entities
   /// in the existing selection set may not be visible
   /// in the current view. So, depending on the use case,
   /// it may be necessary or appropriate to zoom the view 
   /// to the extents of the entities in the selection set
   /// just before this method is called, or the optional
   /// ZOOM argument can be specified. However, if all of
   /// the objects in the selection set are in the current
   /// view, the view will still be zoomed, which may not
   /// be desirable.
   /// 
   /// A future update to this method may attempt to avoid
   /// zooming to the extents of the objects if they are all
   /// already visible in the current view, however doing 
   /// that is non-trivial.
   /// 
   /// Selection metadata:
   /// 
   /// Graphical selection information from objects in the 
   /// selection set to be edited is not preserved when 
   /// editing it. The resulting selection set will contain 
   /// only metadata describing how objects that were added 
   /// to the selection set were selected.
   /// 
   /// </summary>

   public static partial class EditorExtensionMethods
   {
      /// <summary>
      /// Allows interactive editing of the contents of an 
      /// existing selection set, returning a new selection 
      /// set containg the result, which can include none, 
      /// some, or all entities in the existing selection 
      /// set, along with other entities that were added.
      /// </summary>
      /// <param name="ed">The Editor of the active document</param>
      /// <param name="ss">The selection set to be edited, which 
      /// can be either a selection set or an IEnumerable<ObjectId>
      /// </param>
      /// <param name="options">The PromptSelectionOptions to be used
      /// in the editing operation, or null to use the default options</param>
      /// <param name="zoom">A value indicating if the current view
      /// should be zoomed to the extents of the selection set that
      /// is to be edited. The default value is false (no zoom)</param>
      /// <returns></returns>
      
      public static PromptSelectionResult EditSelection(this Editor ed, SelectionSet ss, PromptSelectionOptions options = null, bool zoom = false)
      {
         return SelectionSetEditor.Edit(ed, ss, options, zoom);
      }

      public static PromptSelectionResult EditSelection(this Editor ed, IEnumerable<ObjectId> ids, PromptSelectionOptions options = null, bool zoom = false)
      {
         return SelectionSetEditor.Edit(ed, ids, options, zoom);
      }
   }

   /// <summary>
   /// A class that allows a selection of objects to be 
   /// edited interactively, allowing the user to remove
   /// and/or add objects from/to the selection.
   /// </summary>

   class SelectionSetEditor
   {
      Editor ed = null;
      PromptSelectionOptions options = null;
      ObjectId[] selection = null;
      const string keyword = "SSEDIT";

      public SelectionSetEditor(Editor editor, SelectionSet ss, PromptSelectionOptions options = null)
         : this(editor, ss?.GetObjectIds(), options)
      {
      }

      public SelectionSetEditor(Editor editor, IEnumerable<ObjectId> ss, PromptSelectionOptions options = null)
      {
         if(editor is null)
            throw new ArgumentNullException("editor");
         if(ss is null)
            throw new ArgumentNullException("ss");
         this.ed = editor;
         this.selection = ss as ObjectId[] ?? ss.ToArray();
         if(selection.Length > 0)
         {
            this.options = options ?? new PromptSelectionOptions();
            this.options.Keywords.Add(keyword, keyword, keyword, false, true);
         }
      }

      void keywordInput(object sender, SelectionTextInputEventArgs e)
      {
         options.KeywordInput -= keywordInput;
         if(selection is not null && selection.Length > 0)
         {
            e.AddObjects(selection);
            Application.SetSystemVariable("NOMUTT", 1);
            Application.Idle += idle;
         }
         selection = null;
      }

      /// <summary>
      /// Added to supress the "nnn added" message that
      /// appears after the above code adds the initial
      /// objects to the selection. 
      /// 
      /// This handler merely resets the NOMUTT sysvar 
      /// to 0 and displays the "Select objects: " prompt 
      /// that is also supressed by NOMUTT:
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>

      private void idle(object sender, EventArgs e)
      {
         Application.Idle -= idle;
         Application.SetSystemVariable("NOMUTT", 0);
         string msg = options.MessageForAdding;
         if(string.IsNullOrWhiteSpace(msg))
            msg = "\nSelect objects: ";          // Localization required
         ed.WriteMessage(msg);
      }

      public static PromptSelectionResult Edit(Editor editor,
         SelectionSet selectionSet,
         PromptSelectionOptions options = null,
         bool zoom = false)
      {
         if(editor is null)
            throw new ArgumentNullException(nameof(editor));
         if(selectionSet is null)
            throw new ArgumentNullException(nameof(selectionSet));
         return new SelectionSetEditor(editor, selectionSet, options)
            .EditSelection(zoom);
      }

      public static PromptSelectionResult Edit(Editor editor,
         IEnumerable<ObjectId> selectionSet,
         PromptSelectionOptions options = null,
         bool zoom = false)
      {
         if(editor is null)
            throw new ArgumentNullException(nameof(editor));
         if(selectionSet is null)
            throw new ArgumentNullException(nameof(selectionSet));
         return new SelectionSetEditor(editor, selectionSet, options)
            .EditSelection(zoom);
      }

      PromptSelectionResult EditSelection(bool zoom)
      {
         if(zoom)
            ZoomObjects(ed, this.selection, 0.95);
         if(selection is not null && selection.Length > 0)
         {
            options.KeywordInput += keywordInput;
            ed.Document.SendStringToExecute($"{keyword}\n", true, false, false);
         }
         return ed.GetSelection(options);
      }

      static void ZoomObjects(Editor editor, ObjectId[] ids, double factor = 1.0, bool restorePickfirst = true)
      {
         if(editor is null)
            throw new ArgumentNullException(nameof(editor));
         if(ids is null || ids.Length == 0)
            return;
         SelectionSet pickfirst = null;
         if(restorePickfirst)
         {
            var psr = editor.SelectImplied();
            if(psr.Status == PromptStatus.OK && psr.Value?.Count > 0)
               pickfirst = psr.Value;
         }
         using(new SysVar("GRIPS", 0))
         using(new SysVar("GTAUTO", 0))
         {
            editor.SetImpliedSelection(ids);
            try
            {
               using(var view = editor.GetCurrentView())
               {
                  try
                  {
                     Utils.ZoomObjects(view.PerspectiveEnabled);
                     if(factor != 1.0)
                        ViewUtil.GsLensLength *= factor;
                  }
                  catch(FileLoadException)
                  {
                  }
               }
            }
            finally
            {
               if(restorePickfirst)
                  editor.SetImpliedSelection(pickfirst);
            }
         }
      }
   }

   /// <summary>
   /// Lifted from Common/SystemVariable.cs
   /// </summary>

   class SysVar : IDisposable
   {
      object oldvalue = null;
      string name;

      public SysVar((string Name, object Value) item)
         : this(item.Name, item.Value)
      {
      }

      public SysVar(string name, object value)
      {
         if(string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(nameof(name));
         this.name = name;
         object current = Application.GetSystemVariable(name);
         if(!object.Equals(current, value))
         {
            oldvalue = current;
            Application.SetSystemVariable(name, value);
         }
      }

      public void Dispose()
      {
         if(oldvalue != null)
         {
            Application.SetSystemVariable(name, oldvalue);
            oldvalue = null;
         }
      }
   }

   /// <summary>
   /// An example showing how the SelectionSetEditor class is used:
   /// </summary>

   public static class SSEditExampleCommands
   {
      static SelectionSet currentSelection;

      /// <summary>
      /// A command to define the selection set to be edited.
      /// </summary>

      [CommandMethod("SSEDITSELECT")]
      public static void SSEditSelect()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;

         /// Get an initial selection set from the
         /// user that we will then allow them to edit:

         Editor ed = doc.Editor;
         ed.WriteMessage("\nSpecify initial selection set to edit,");
         var psr = ed.GetSelection();
         if(psr.Status != PromptStatus.OK)
            return;
         ed.WriteMessage($"\nInitial selection count = {psr.Value.Count}");
         currentSelection = psr.Value;
      }


      [CommandMethod("SSEDIT", CommandFlags.Redraw)]
      public static void SSEdit()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Editor ed = doc.Editor;
         try
         {
            if(currentSelection == null)
            {
               SSEditSelect();
               if(currentSelection == null)
                  return;
            }

            /// Allow the user to edit the selection set
            /// acquired using the SSEDITSELECT command:

            ed.WriteMessage("\nSpecify objects to add " +
               "to/remove from original selection,");

            PromptSelectionResult result = ed.EditSelection(currentSelection);

            if(result != null && result.Value != null)
            {
               var ids = result.Value.GetObjectIds();
               ed.SetImpliedSelection(result.Value.GetObjectIds());
               doc.Editor.WriteMessage(
                  $"\nEdited selection Count = {result.Value.Count}");
            }
            currentSelection = null;
         }
         catch(System.Exception ex)
         {
            ed.WriteMessage($"Error: {ex.ToString()}");
         }
      }
   }
}
