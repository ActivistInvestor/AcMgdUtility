/// MyRibbonManager class
/// ActivistInvestor / Tony T
/// 
/// Example concrete implementation of the
/// RibbonExtensionApplication, demonstrating
/// how that class is used.
/// 
/// Original source location:
/// 
/// https://github.com/ActivistInvestor/AcMgdUtility/blob/main/RibbonExtensionApplication/MyRibbonManager.cs
///

using Autodesk.AutoCAD.Runtime.AIUtils;
using Autodesk.Windows;
using System.Collections.Generic;

/// TODO: Uncomment and modify the attribute parameter to be 
/// the type of the class derived from RibbonExtensionApplication:

// [assembly: ExtensionApplication(typeof(MyRibbonManagerExample.MyRibbonManager))]

namespace MyRibbonManagerExample
{
   /// <summary>
   /// Demonstrates advanced usage of RibbonExtensionApplication,
   /// including automating creation and caching of content, and
   /// adding content to the ribbon.
   /// 
   /// Notice that unlike the MyRibbonExtensionApplication example,
   /// there is no override of InitializeRibbon(). Instead, this
   /// example overrides two other methods that are reponsible for
   /// two distnct tasks:
   /// 
   ///   1. Creating the ribbon content.
   ///   2. Adding the content to the ribbon.
   ///   
   /// There are two distinct tasks because one (1) can be done 
   /// only once, while the other (2) must be done numerous times.
   /// 
   /// Everything else that's required, including caching the ribbon 
   /// content, is handled by the RibbonExtensionApplication class. 
   /// 
   /// This example shows how ribbon content should be handled, 
   /// which is by creating the content only once, and adding it 
   /// to the ribbon as many times as necessary.
   /// /// </summary>

   public class MyRibbonManager : RibbonExtensionApplication
   {
      /// <summary>
      /// In this override, we do nothing except create and return
      /// the ribbon content. The base class will cache it, and use
      /// it whenever the content needs to be added to the ribbon.
      /// 
      /// This example creates and returns a list containing 3 empty 
      /// RibbonTabs that will be added to the ribbon by the second 
      /// overload below.
      /// </summary>

      protected override object GetRibbonContent(RibbonControl ribbon, RibbonState state)
      {
         return new List<RibbonTab>()
         { 
            new RibbonTab() { Id = "MyTab001", Name = "MyTab1" },
            new RibbonTab() { Id = "MyTab002", Name = "MyTab2" },
            new RibbonTab() { Id = "MyTab003", Name = "MyTab3" }
         };
      }

      /// <summary>
      /// In this override, we do nothing except add the content 
      /// to the ribbon. The content to be added is passed in the 
      /// content argument.
      /// </summary>

      protected override void AddContentToRibbon(RibbonControl ribbon, object content, RibbonState state)
      {
         if(content is List<RibbonTab> list)
         {
            var tabs = ribbon.Tabs;
            foreach(RibbonTab tab in list)
            {
               tabs.Add(tab);
            }
         }
      }
   }

}
