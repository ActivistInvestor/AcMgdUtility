/// CommandExtensionExamples.cs  ActivistInvestor / Tony T.
/// 
/// Example code for using the types in CommandExtensions.cs
/// 
/// Distributed under the terms of the MIT license.
/// 
///
/// Source location:
/// 
///     https://github.com/ActivistInvestor/AcMgdUtility/blob/main/CommandExtensionExamples.cs
///     

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace CommandExtensionExamples
{
   using Autodesk.AutoCAD.ApplicationServices.EditorExtensions;

   public static class CommandExtensionExamples
   {
      /// <summary>
      /// Issues the HATCHGENERATEBOUNDARY command and collects
      /// all of the objects created by it and sets them to the 
      /// pickfirst selection set.
      /// </summary>

      [CommandMethod("SELECTHATCHBOUNDARY", CommandFlags.Redraw)]
      public static void MyCommand()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Editor ed = doc.Editor;
         var peo = new PromptEntityOptions("\nSelect hatch: ");
         peo.AddAllowedClass(typeof(Hatch), true);
         var per = ed.GetEntity(peo);
         if(per.Status != PromptStatus.OK)
            return;
         ObjectId hatchId = per.ObjectId;
         var newIds = new ObjectIdCollection();
         ed.Command<Entity>(newIds, "HATCHGENERATEBOUNDARY", hatchId, "");
         if(newIds.Count > 0)
         {
            ed.SetImpliedSelection(newIds.ToArray());
         }
         else
         {
            ed.WriteMessage("\nFailed to capture hatch boundary object(s).");
         }
      }
   }


}
