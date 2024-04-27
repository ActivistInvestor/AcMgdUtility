/// MyRibbonExtensionApplication
/// ActivistInvestor / Tony T
/// 
/// Example concrete implementation of the
/// RibbonExtensionApplication, demonstrating
/// how that class is used.

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Runtime.AIUtils;
using Autodesk.Windows;

[assembly: ExtensionApplication(typeof(Example.MyRibbonExtensionApplication))]

namespace Example
{
   public class MyRibbonExtensionApplication : RibbonExtensionApplication
   {
      protected override void Initialize()
      {
         /// TODO: Initialize application
      }

      protected override void InitializeRibbon(RibbonControl ribbon, RibbonState context)
      {
         /// TODO: Create ribbon content if not already created.
         /// TODO: Add ribbon content to ribbon.
      }
   }

}