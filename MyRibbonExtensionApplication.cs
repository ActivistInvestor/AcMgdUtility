/// MyRibbonExtensionApplication
/// ActivistInvestor / Tony T
/// 
/// Example concrete implementation of the
/// RibbonExtensionApplication, demonstrating
/// how that class is used.
/// 
/// Original source location:
/// 
/// https://github.com/ActivistInvestor/AcMgdUtility/blob/main/MyRibbonExtensionApplication.cs
///

using Autodesk.AutoCAD.Runtime.AIUtils;
using Autodesk.Windows;

/// TODO: Modify the attribute parameter to be the type
/// of the class derived from RibbonExtensionApplication:

// [assembly: ExtensionApplication(typeof(Example.MyRibbonExtensionApplication))]

namespace RibbonExtensionApplicationExample1
{
   /// <summary>
   /// Using the RibbonExtensionApplication class is easy. You
   /// you do not have to modify that class, you only need to 
   /// derive a new class from it like the example shown below, 
   /// and override/implement the InitializeRibbon() method, and    
   /// optionally the Initialize() and/or Terminate() methods.
   /// 
   /// The example below can be used as a template for building 
   /// your own derived type.
   /// 
   /// Disclaimer: 
   /// 
   /// Use of RibbonExtensionApplication does not require any
   /// modification of that class, it only requires deriving a 
   /// new class from it, like the example shown below. 
   /// 
   /// If you instead use the base class directly and modify it,
   /// you're on your own and shouldn't expect help or support 
   /// with that, because you're not using the code in the manner 
   /// it was indended to be used.
   /// 
   /// You can post your comments, issues, bug reports, flames 
   /// or whatever in the repo at:
   /// 
   ///   https://github.com/ActivistInvestor/AcMgdUtility/discussions
   ///    
   /// </summary>

   public class MyRibbonExtensionApplication : RibbonExtensionApplication
   {
      /// <summary>
      /// This override is always called when the 
      /// extension is loaded, once and only once.      
      /// 
      /// Avoid using any ribbon-dependent code here, 
      /// because the ribbon may not exist when this 
      /// override is called. 
      /// </summary>

      protected override void Initialize()
      {
         /// TODO: Initialize your extension application. 
      }

      /// <summary>
      /// This override may be called any number of times
      /// (or never), whenever content must be added to 
      /// the ribbon.
      /// 
      /// Do not do unrelated initialization tasks that 
      /// must always happen when an extension is loaded 
      /// in this override, because it may never be called.
      /// </summary>

      protected override void InitializeRibbon(RibbonControl ribbon, RibbonState context)
      {
         /// TODO: Create ribbon content if not already created.
         /// TODO: Add ribbon content to ribbon.
      }
   }
}