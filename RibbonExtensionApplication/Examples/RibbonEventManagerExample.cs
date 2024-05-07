
using Autodesk.AutoCAD.Runtime;

/// RibbonEventManagerExample.cs
/// ActivistInvestor / Tony T
/// 
/// An example showing the use of the 
/// RibbonEventManager class.
/// 
/// Source:
///   
///   https://github.com/ActivistInvestor/AcMgdUtility/blob/main/RibbonExtensionApplication/Examples/RibbonEventManagerExample.cs
///

/// TODO: Modify argument to be the name of 
/// the actual IExtensionApplication-based class:

/// [assembly: ExtensionApplication(typeof(Namespace1.MyRibbonEventManagerApplication))]

using Autodesk.AutoCAD.ApplicationServices.AIUtils;
using Autodesk.Windows;

namespace Namespace1
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
         /// This should be done after ribbon content has
         /// been created because the handler may be called
         /// immediately after adding it to the event, if
         /// the ribbon exists.
         
         RibbonEventManager.InitializeRibbon += LoadRibbonContent;
      }

      /// <summary>
      /// 
      /// This event handler will be called when:
      /// 
      ///   1. A handler is added to the 
      ///      InitializeRibbonEvent and
      ///      the ribbon exists.
      ///      
      ///   2. The ribbon is created at any
      ///      point after a handler is added 
      ///      to the InitializeRibbon event.
      ///      
      ///   3. A workspace is loaded.
      ///   
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>

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