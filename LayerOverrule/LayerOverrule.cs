using System;
using System.Reflection;
using AcMgdLib.Overrules.Examples;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.LayerManager;
using Autodesk.AutoCAD.Runtime;

/// Note: this code requires a reference to AcLayer.dll

namespace AcMgdLib.Overrules
{
   /// <summary>
   /// The following abstract base class is designed to be 
   /// resuable and not application-specific. It can be used 
   /// to deal with modified layers by deriving a class from
   /// it, and implementing application-specific code logic
   /// in an override of the OnLayerModified() method, as is
   /// done in the above example.
   /// </summary>

   public abstract class LayerOverrule : ObjectOverrule
   {
      string namePattern = "*";
      static RXClass rxclass = RXObject.GetClass(typeof(LayerTableRecord));

      public LayerOverrule(string namePattern = "*")
      {
         this.namePattern = namePattern ?? "*";
         AddOverrule(rxclass, this, true);
      }

      public override void Close(DBObject obj)
      {
         if(obj.IsWriteEnabled &&
               obj.IsModified &&
               !obj.IsUndoing &&
               obj.IsReallyClosing &&
               obj is LayerTableRecord layer && IsMatch(layer))
            OnLayerModified(layer);
         base.Close(obj);
      }

      protected virtual bool IsMatch(LayerTableRecord layer)
      {
         bool result = namePattern == "*" || Utils.WcMatchEx(layer.Name, namePattern, true);
         return result;
      }

      protected abstract void OnLayerModified(LayerTableRecord layer);

      protected override void Dispose(bool disposing)
      {
         if(!base.IsDisposed)
            RemoveOverrule(rxclass, this);
         base.Dispose(disposing);
      }
   }
}

/// Bundled Utility and helper classes used by the above code

namespace AcMdgLib.Common
{
   /// <summary>
   /// Helper class to invoke actions asynchronously
   /// on next Application.Idle event:
   /// </summary>

   public class Async
   {
      private Action action;

      Async(Action action)
      {
         this.action = action;
         Application.Idle += idle;
      }

      void idle(object sender, EventArgs e)
      {
         Application.Idle -= idle;
         if(action != null)
            action();
         action = null;
      }

      public static void WriteLine(string msg, params object[] args)
      {
         OnIdle(() => WriteMsg(msg, args));
      }

      static void WriteMsg(string msg, params object[] args)
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         if(doc != null)
            doc.Editor.WriteMessage("\n" + msg, args);
      }

      public static void OnIdle(Action action)
      {
         new Async(action);
      }
   }

   /// <summary>
   /// Helper class to manage singleton instances of
   /// of types that implement IDisposable. The managed
   /// type must have a public default constructor.
   /// </summary>
   /// <typeparam name="T">The type to be managed</typeparam>

   public static class Singleton<T> where T : class, IDisposable, new()
   {
      static T instance = null;

      public static bool Enabled
      {
         get => instance != null;
         set
         {
            if(instance != null ^ value)
            {
               if(value)
                  instance = new T();
               else
               {
                  instance?.Dispose();
                  instance = null;
               }
            }
         }
      }

      public static T Instance => instance;
   }

   /// <summary>
   /// A class that backdoors the LayerManager to 
   /// force it to Update.
   /// </summary>

   public static class LayerManagerThunk
   {
      static LayerManagerControl layerManagerControl;

      public static void UpdateLayerManager(bool async = true)
      {
         if(Initialize() && layerManagerControl.Visible &&
            !layerManagerControl.LayerViewManager.Modal)
         {
            if(!async)
               layerManagerControl.UpdateLayerManager(true);
            else
               Async.OnIdle(() => layerManagerControl.UpdateLayerManager(true));
         }
      }

      static bool Initialize()
      {
         if(layerManagerControl == null)
         {
            FieldInfo field = typeof(PaletteHost).GetField("layerManager_",
               BindingFlags.Static | BindingFlags.NonPublic);
            object rslt = field.GetValue(null);
            if(rslt is LayerManagerControl lmc)
               layerManagerControl = lmc;
         }
         return layerManagerControl != null;
      }
   }

}

