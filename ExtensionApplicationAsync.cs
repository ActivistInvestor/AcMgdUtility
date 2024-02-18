using System;
using Autodesk.AutoCAD.ApplicationServices;

/// ExtensionApplicationAsync
/// ActivistInvestor

namespace Autodesk.AutoCAD.Runtime
{
   /// <summary>
   /// This is an abstract base type for implementing 
   /// IExtensionApplications, that mitigates several 
   /// issues related to its use.
   /// 
   /// Initialization is asynchronous and deferred until 
   /// the first idle event is raised, and there is an 
   /// open document, serving several goals:
   /// 
   ///   1. Ensures that initialization always occurs in the 
   ///      Application execution context, regardless of how 
   ///      the extension is loaded (e.g., via NETLOAD or via
   ///      auto-loading at startup).
   ///      
   ///   2. Prevents AutoCAD's managed runtime from supressing
   ///      exceptions that occur in the Initialize() method
   ///      of IExtensionApplication.
   ///
   /// Usage:
   ///
   /// This class should not be altered. Just include it in 
   /// a project and derive a new class from this class, and 
   /// implement/override the Initialize() method to do one-time 
   /// initialization at startup, and optionally, override the 
   /// Terminate() method to do finalization tasks at shutdown.
   /// 
   /// See the example derived MyApplication class included 
   /// in the file MyApplication.cs.
   /// </summary>

   public abstract class ExtensionApplicationAsync : IExtensionApplication
   {
      void IExtensionApplication.Initialize()
      {
         this.Initialize();
      }

      protected abstract void Initialize();
      protected virtual void Terminate()
      {
      }

      void OnIdle(object sender, EventArgs e)
      {
         if(Application.DocumentManager.MdiActiveDocument != null)
         {
            Application.Idle -= OnIdle;
            try
            {
               this.Initialize();
            }
            catch(System.Exception ex)
            {
               Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                  ex.ToString());
            }
         }
      }

      void IExtensionApplication.Terminate()
      {
         this.Terminate();
      }
   }

}

