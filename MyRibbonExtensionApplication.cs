/// MyRibbonExtensionApplication Example class
/// ActivistInvestor / Tony T
/// 
/// <summary>
/// Using the RibbonExtensionApplication class is easy. You
/// you do not have to modify that class, you only need to 
/// derive a new class from it, like the example shown below, 
/// and override/implement the InitializeRibbon() method, and    
/// optionally the Initialize(), and/or Terminate() methods.
/// 
/// The example below can be used as a template for building 
/// your own derived type.
/// 
/// Disclaimer: Use of RibbonExtensionApplication does not
/// require modification of that class, it only requires 
/// deriving a new class from it, like the example shown
/// below. 
/// 
/// If you instead use the base class by modifying it, you're
/// on your own, and please do not ask for help or support, 
/// because you're not using the code in the manner that it
/// was indended to be used.
/// 
/// You can post comments, issues, flames or whatever in the
/// repo discussion at:
/// 
///    https://github.com/ActivistInvestor/AcMgdUtility/discussions
///    
/// </summary>

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
