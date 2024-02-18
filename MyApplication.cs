using Autodesk.AutoCAD.Runtime;

/// ExtensionApplicationAsync
/// ActivistInvestor

/// Add assemtly attribute telling AutoCAD that 
/// MyApplication is an IExtensionApplication.
/// This is optional, but helps AutoCAD find the
/// extension application faster.

[assembly: ExtensionApplication(typeof(Example.MyApplication))]

namespace Example
{

   /// <summary>
   /// Example use of ExtensionApplicationAsync.
   ///
   /// The following is a minimal impelementation
   /// of an extension application that uses the
   /// ExtensionApplicationAsync base type.
   /// 
   /// Note: Do not add IExtensionApplication to the 
   /// inheretance list of MyApplication, as it is 
   /// already added to the inheritance list of the 
   /// base class.
   /// </summary>

   public class MyApplication : ExtensionApplicationAsync
   {
      protected override void Initialize()
      {
         /// TODO: Initialize your application here
      }
   }
}

