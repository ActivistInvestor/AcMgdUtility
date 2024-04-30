/// MyRibbonExtensionApplication
/// ActivistInvestor / Tony T
/// 
/// Example concrete implementation of the
/// RibbonExtensionApplication, demonstrating
/// how that class is used.

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Runtime.AIUtils;
using Autodesk.Windows;

// TODO: Modify the attribute parameter to be the type
// of the class derived from RibbonExtensionApplication:

// [assembly: ExtensionApplication(typeof(Example.MyRibbonExtensionApplication))]

namespace Example
{
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

   public class MyRibbonExtensionApplication : RibbonExtensionApplication
   {
      protected override void Initialize()
      {
         /// TODO: Initialize application. 
         /// 
         /// Avoid using any ribbon-dependent code here, 
         /// because the ribbon may not yet exist when
         /// this method is called.
      }

      protected override void InitializeRibbon(RibbonControl ribbon, RibbonState context)
      {
         /// TODO: Create ribbon content if not already created.
         /// TODO: Add ribbon content to ribbon.
      }
   }

   /// <summary>
   /// Demonstrates advanced usage of RibbonExtensionApplication,
   /// including automating the creation, caching, and adding
   /// content to the ribbon.
   /// </summary>

   public class MyRibbonManager : RibbonExtensionApplication
   {
      /// <summary>
      /// In this override, we do nothing except create and return
      /// the ribbon content. The base class will cache it, and use
      /// it whenever the content needs to be added to the ribbon.
      /// This example simplY adds an empty Tab to the ribbon.
      /// </summary>

      protected override object CreateRibbonContent(RibbonState state)
      {
         RibbonTab tab = new RibbonTab();
         tab.Id = "MyTab001";
         tab.Name = "MyTab1";
         return tab;
      }

      /// <summary>
      /// In this override, we do nothing except add the content to
      /// the ribbon. The content passed in as an argument.
      /// </summary>

      protected override void AddContentToRibbon(RibbonControl ribbon, object content, RibbonState state)
      {
         ribbon.Tabs.Add((RibbonTab)content);
      }
   }

}
