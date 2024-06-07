
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
///   https://github.com/ActivistInvestor/AcMgdUtility/blob/main/RibbonExtensionApplication/Examples/RibbonEventManagerExample.cs
///

/// TODO: Modify argument to be the name of 
/// the actual IExtensionApplication-based class:

[assembly: ExtensionApplication(typeof(Namespace1.MyApplication))]

namespace Namespace1
{
   public class MyApplication : IExtensionApplication
   {
      static RibbonTab myRibbonTab;

      public void Initialize()
      {
         /// Create a RibbonTab and assign it to a 
         /// static member variable. 
         /// 
         /// All ribbon content should be created before 
         /// adding a handler to the InitializeRibbon event:

         myRibbonTab = new RibbonTab();
         myRibbonTab.Id = "IDMyTab001";
         myRibbonTab.Name = "MyRibbonTab";
         myRibbonTab.Title = "MyRibbonTab";

         /// Add a handler to the InitializeRibbon event.
         /// This should be done after ribbon content has
         /// been created because the handler may be called
         /// immediately after adding it to the event, if
         /// the ribbon exists.

         RibbonEventManager.InitializeRibbon += initializeRibbonHandler;
      }

      /// <summary>
      /// Handler for the InitializeRibbon event:
      /// 
      /// This event handler will be called when:
      /// 
      ///   1. The handler is added to the 
      ///      InitializeRibbonEvent while
      ///      the ribbon exists.
      ///      
      ///   2. The ribbon is created at any
      ///      point after the handler is added 
      ///      to the InitializeRibbon event, 
      ///      while the ribbon does not exist.
      ///      
      ///   3. A workspace was loaded/reloaded.
      ///   
      /// The State property of the event arguments
      /// indicates which of these conditions triggered 
      /// the event.
      ///   
      /// The following handler for the InitializeRibbon
      /// event uses the AddRibbonTabs() method of the 
      /// event arguments to conditionally add a tab to
      /// the ribbon, if it is not already present.
      /// </summary>

      private void initializeRibbonHandler(object sender, RibbonStateEventArgs e)
      {
         e.AddRibbonTabs(myRibbonTab);
      }

      public void Terminate()
      {
      }
   }

}