/// RibbonEventManager.cs
/// ActivistInvestor / Tony T
/// 
/// A class that provides a simplified means of 
/// initializing and managing application-provided
/// content added to AutoCAD's ribbon.
/// 
/// Source:
///   
///   https://github.com/ActivistInvestor/AcMgdUtility/blob/main/RibbonExtensionApplication/Examples/RibbonEventManagerExample.cs
///

using Autodesk.AutoCAD.ApplicationServices.AIUtils;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;

namespace MyRibbonEventManagerExample
{
   public class MyRibbonEventManagerApplication : IExtensionApplication
   {
      static RibbonTab myRibbonTab;

      public void Initialize()
      {
         /// Create a RibbonTab and store it in a 
         /// static member. The ribbon tab must be 
         /// created before adding a handler to the 
         /// InitializeRibbon event:

         myRibbonTab = new RibbonTab();
         myRibbonTab.Id = "IDMyTab001";
         myRibbonTab.Name = "MyRibbonTab";

         /// Add a handler to the InitializeRibbon event:
         RibbonEventManager.InitializeRibbon += LoadRibbonContent;
      }

      private void LoadRibbonContent(object sender, RibbonStateEventArgs e)
      {
         // Add the RibbonTab to the Ribbon:

         e.RibbonControl.Tabs.Add(myRibbonTab);
      }

      public void Terminate()
      {
      }
   }

}