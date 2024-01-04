/// ExtensionApplicationAsync
/// ActivistInvestor

/// Uncomment the following #define if your
/// project uses the Ribbon and/or references
/// AcWindows.dll

// #define USES_RIBBON

using System;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;
#if(USES_RIBBON)
using Autodesk.AutoCAD.Ribbon;
#endif

/// <summary>
/// A class that provides an implementation of
/// IExtensionApplication that uses asynchronous
/// initialization, which aids in ensuring that
/// all required APIs and dependent objects are 
/// available from the Initialize() method (for
/// example, the Ribbon Control), and diagnosing
/// exceptions that can occur in code within or
/// called from the Initilalize method().
/// 
/// To use this class with an existing class tbat
/// implements IExtensionApplication:
/// 
///   1. In the ancestry list of the existing
///      class, replace 'IExtensionApplication'
///      with 'ExtensionApplicationAsync'.
///      
///   2. Change the signaature of the Initialize()
///      method from 'public void' to 
///      'protected override void'.
///      
///   3. Change the signature of the Terminate()
///      method from 'public void' to 
///      'protected override void'.
///      
/// Example:
///
///  What follows is a before/after example showing how to
///  adapt an existing IExtensionApplication to make use of
///  the ExtensionApplicationAsync class
///
///  <code>
///  // The existing class that implements IExtensionApplication:
///  
///  [Assembly:ExtensionApplication(typeof(MyApplication))]
///  
///  public class MyApplication : IExtensionApplication
///  {
///     public void Initialize()
///     {
///        WriteMessage("\nHello");
///     }
///  
///     public void Terminate()
///     {
///        WriteMessage("\nGoodbye");
///     }
///  
///     static void WriteMessage(string msg, params object[] args)
///     {
///        var doc = Application.DocumentManager.MdiActiveDocument;
///        if(doc != null)
///           doc.Editor.WriteMessage(msg, args);
///     }
///     
///  }
///  
///  // The same class after the required changes were 
///  // made to make use of ExtensionApplicationAsync:
///  
///  [Assembly: ExtensionApplication(typeof(MyApplication))]
///  
///  public class MyApplication : ExtensionApplicationAsync  // <- ancestor changed
///  {
///     protected override void Initialize()  // <- signaure changed
///     {
///        WriteMessage("\nHello");
///     }
///  
///     protected override void Terminate()  // <- signaure changed
///     {
///        WriteMessage("\nGoodbye");
///     }
///     
///     static void WriteMessage(string msg, params object[] args)
///     {
///        var doc = Application.DocumentManager.MdiActiveDocument;
///        if(doc != null)
///           doc.Editor.WriteMessage(msg, args);
///     }
///  }
/// 
/// The ExtensionApplicationAsync class is an abstract base 
/// class that is used by deriving a new class from it, and
/// overridding its virtual methods (Initialize(), Terminate(),
/// and InitializeRibbon()), and that no changes need to be made 
/// to this base class.
/// </summary>

namespace Autodesk.AutoCAD.Runtime.AIUtils
{
   public abstract class ExtensionApplicationAsync : IExtensionApplication
   {
      bool usesRibbon = false;

      public ExtensionApplicationAsync()
      {
#if(USES_RIBBON)
      usesRibbon = HasOverride((Action<RibbonPaletteSet>)this.InitializeRibbon);
#endif

      }

      /// <summary>
      /// Override and add code that implements
      /// IExtensionApplication.Initialize()
      /// </summary>

      protected abstract void Initialize();

      /// <summary>
      /// Override and add code that implements
      /// IExtensionApplication.Terminate()
      /// 
      /// Overriding this method is optional.
      /// </summary>

      protected virtual void Terminate()
      {
      }

      /// <summary>
      /// When overridden in a derived type, it will be called 
      /// at startup if the ribbon exists, otherwise it will be 
      /// called if/when the ribbon is created.
      /// </summary>

#if(USES_RIBBON)
   protected virtual void InitializeRibbon(RibbonPaletteSet ribbon)
   {
   }
#endif

      /// <summary>
      /// Override to indicate if Initialize() can be
      /// safely called. Default implementation waits
      /// for an active document. 
      /// 
      /// Overriding this method is optional and only 
      /// required in highly-specialized use cases.
      /// </summary>

      protected virtual bool CanInitialize
      {
         get
         {
            return Application.DocumentManager.MdiActiveDocument != null;
         }
      }

      void IExtensionApplication.Initialize()
      {
         Application.Idle += idle;
      }

      void IExtensionApplication.Terminate()
      {
         try
         {
            this.Terminate();
         }
         catch
         {
         }
      }

      void idle(object sender, EventArgs e)
      {
         if(CanInitialize)
         {
            Application.Idle -= idle;
            this.Initialize();
#if(USES_RIBBON)
         if(usesRibbon)
         {
            if(RibbonServices.RibbonPaletteSet != null)
               InitializeRibbon(RibbonServices.RibbonPaletteSet);
            else
               RibbonServices.RibbonPaletteSetCreated += ribbonCreated;
         }
#endif
         }
      }

#if(USES_RIBBON)

   void ribbonCreated(object sender, EventArgs e)
   {
      RibbonServices.RibbonPaletteSetCreated -= ribbonCreated;
      InitializeRibbon(RibbonServices.RibbonPaletteSet);
   }

#endif

      bool HasOverride(Delegate del)
      {
         return del.Method != del.Method.GetBaseDefinition();
      }

   }
}