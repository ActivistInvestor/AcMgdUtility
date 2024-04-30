/// MyRibbonManager class
/// ActivistInvestor / Tony T
/// 
/// Example concrete implementation of the
/// RibbonExtensionApplication, demonstrating
/// how that class is used.
/// 
/// Original source location:
/// 
/// https://github.com/ActivistInvestor/AcMgdUtility/blob/main/MyRibbonManager.cs
///

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Runtime.AIUtils;
using Autodesk.Windows;
using System.Collections.Generic;

/// TODO: Modify the attribute parameter to be the type
/// of the class derived from RibbonExtensionApplication:

[assembly: ExtensionApplication(typeof(Example.MyRibbonManager))]

namespace Example
{
   /// <summary>
   /// Demonstrates advanced usage of RibbonExtensionApplication,
   /// including automating creation and caching of content, and
   /// adding content to the ribbon.
   /// </summary>

   public class MyRibbonManager : RibbonExtensionApplication
   {
      /// <summary>
      /// In this override, we do nothing except create and return
      /// the ribbon content. The base class will cache it, and use
      /// it whenever the content needs to be added to the ribbon.
      /// This example simply adds 2 empty Tabs to the ribbon.
      /// </summary>

      protected override object CreateRibbonContent(RibbonState state)
      {
         var tabs = new List<RibbonTab>();
         tabs.Add(new RibbonTab() { Id = "MyTab001", Name = "MyTab1" });
         tabs.Add(new RibbonTab() { Id = "MyTab002", Name = "MyTab2" });
         return tabs;
      }

      /// <summary>
      /// In this override, we do nothing except add the content to
      /// the ribbon. 
      /// 
      /// The content to be added is passed in the content argument.
      /// </summary>

      protected override void AddContentToRibbon(RibbonControl ribbon, object content, RibbonState state)
      {
         if(content is List<RibbonTab> tabs)
         {
            foreach(RibbonTab tab in tabs)
            {
               ribbon.Tabs.Add(tab);
            }
         }
      }
   }

}