/// MyRibbonExtensionApplication
/// ActivistInvestor / Tony T
/// 
/// Example concrete implementation of the
/// RibbonExtensionApplication, demonstrating
/// how that class is used.
///
/// Disclaimer: Use of RibbonExtensionApplication does not
/// require modification of that class, it only requires 
/// deriving a new class from it, and overriding one, two,
/// or three virtual methods. If you instead use the base 
/// class by modifying it, you are on your own, and please 
/// do not ask for help or support, because you're not using 
/// the code as it was indended to be used.

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
