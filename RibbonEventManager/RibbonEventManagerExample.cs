
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices.AIUtils;
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;


/// RibbonEventManagerExample.cs
/// ActivistInvestor / Tony T
/// 
/// An example showing the use of the 
/// RibbonEventManager class.
/// 
/// Source:
/// 
///   https://github.com/ActivistInvestor/AcMgdUtility/blob/main/RibbonEventManager/RibbonEventManagerExample.cs
///

/// TODO: Modify the argument to be the name of 
/// the actual IExtensionApplication-based class:

[assembly: ExtensionApplication(typeof(Namespace1.MyApplication))]

namespace Namespace1
{
   public class MyApplication : IExtensionApplication
   {
      static RibbonTab myRibbonTab;

      public void Initialize()
      {
         /// Add a handler to the InitializeRibbon event.

         RibbonEventManager.InitializeRibbon += initializeRibbon;
      }

      /// <summary>
      /// Handler for the InitializeRibbon event:
      /// </summary>

      private void initializeRibbon(object sender, RibbonStateEventArgs e)
      {
         /// Create a ribbon tab on the 
         /// first call to this method:

         if(myRibbonTab == null)
         {
            myRibbonTab = new RibbonTab();
            myRibbonTab.Id = "IDMyTab001";
            myRibbonTab.Name = "MyRibbonTab";
            myRibbonTab.Title = "MyRibbonTab";
         }
         
         /// Add the tab to the ribbon:
         
         e.RibbonControl.Tabs.Add(myRibbonTab);
      }

      public void Terminate()
      {
      }
   }

}