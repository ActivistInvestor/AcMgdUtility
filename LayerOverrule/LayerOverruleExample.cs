using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using AcMdgLib.Common;

namespace AcMgdLib.Overrules.Examples
{
   /// <summary>
   /// Example showing how to use the LayerOverrule
   /// base type (included in LayerOverrule.cs).
   /// 
   /// This example prevents layers from being locked
   /// only if their name starts with "NOLOCK", which
   /// is just an example. Real-world application of
   /// this class can involve realistic criteria.
   /// 
   /// Issue:
   /// 
   /// The Layer properties manager does not update to
   /// display the correct locked/unlocked state of a
   /// layer which the user tried to lock via the layer
   /// manager dialog. 
   /// 
   /// So, if the user tries to lock a layer that this
   /// overrule does not allow them to lock, the layer
   /// properties manager will show the layer as being
   /// locked, even thought it is not locked.
   /// 
   /// The LayerManagerThunk class included in the code
   /// file required by this example addresses that issue,
   /// by forcing the LayerManager control to update when
   /// the user tries to lock a layer that can't be locked.
   /// 
   /// </summary>

   public class MyNoLockLayerOverrule : LayerOverrule
   {
      public MyNoLockLayerOverrule() : base("NOLOCK*")
      {
      }

      /// <summary>
      /// The base type filters the layers by their names
      /// according to the string passed to its constructor
      /// above, so this method will only be called if the 
      /// layer's name matches the wildcard pattern "NOLOCK*".
      /// 
      /// You can change the pattern to meet your needs or
      /// you can override the IsMatch() method to directly
      /// specify what layers the overrule should act on.
      /// </summary>

      protected override void OnLayerModified(LayerTableRecord layer)
      {
         if(layer.IsLocked)
         {
            layer.IsLocked = false;
            LayerManagerThunk.UpdateLayerManager(true);
            Async.WriteLine($"\nALERT: Layer {layer.Name} cannot be locked!");
         }
      }
   }

   /// <summary>
   /// Implementing the above example in a managed extension
   /// requires it to be initialized in the Initialize() method
   /// of an IExtensionApplication. The containing assembly
   /// must also have the assembly:ExtensionApplication()
   /// attribute to tell the runtime that the class should
   /// initialized when the assembly is loaded. 
   /// 
   /// If there is an existing IExtensionApplication, the call 
   /// to the DisposableSinglton<T>.Enabled property below can 
   /// be added to an existing IExtensionApplication's Initlialize() 
   /// method, and this class can be omitted.
   /// </summary>

   ///  public class MyApplication : IExtensionApplication
   ///  {
   ///     public void Initialize()
   ///     {
   ///        Singleton<MyNoLockLayerOverrule>.Enabled = true;
   ///     }
   ///  
   ///     public void Terminate()
   ///     {
   ///     }
   ///  }

   /// A test class that can be used to toggle
   /// the example overrule on/off via a command:

   public static class NoLockLayersCommand
   {
      [CommandMethod("NOLOCKLAYERS")]
      public static void NoLockLayers()
      {
         Singleton<MyNoLockLayerOverrule>.Enabled ^= true;
      }
   }


}

