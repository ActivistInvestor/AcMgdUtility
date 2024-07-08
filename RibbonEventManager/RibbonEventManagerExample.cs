
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;

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
      /// Ribbon content should be assigned 
      /// to a static member variable:
      
      static RibbonTab myRibbonTab;

      /// <summary>
      /// IExtensionApplication.Initialize
      /// 
      /// Note: When using the RibbonEventManager,
      /// there is no need to defer execution of
      /// code until the Application.Idle event is
      /// raised, as the RibbonEventManager already
      /// does that for the programmer.
      /// 
      /// The handler for the InitializeRibbon event
      /// will not be called until the next Idle event 
      /// is raised.
      /// 
      /// </summary>
      
      public void Initialize()
      {
         /// Add a handler to the InitializeRibbon event.

         RibbonEventManager.InitializeRibbon += LoadMyRibbonContent;
      }

      /// <summary>
      /// Handler for the InitializeRibbon event.
      /// 
      /// This handler can be called multiple times,
      /// such as when a workspace is loaded. See the
      /// docs for RibbonEventManager for details on
      /// when/why this event handler will be called.
      /// </summary>

      private void LoadMyRibbonContent(object sender, RibbonStateEventArgs e)
      {
         /// Create the ribbon content if it has
         /// not already been created:

         if(myRibbonTab == null)
         {
            myRibbonTab = new RibbonTab();
            myRibbonTab.Id = "IDMyTab001";
            myRibbonTab.Name = "MyRibbonTab";
            myRibbonTab.Title = "MyRibbonTab";
         }
         
         /// Add the content to the ribbon:
         
         e.RibbonControl.Tabs.Add(myRibbonTab);

      }

      public void Terminate()
      {
      }
   }

}