/// ComInterop.cs
///
/// ActivistInvestor / Tony T
/// 
/// Distributed under the terms of the MIT license.


/// Helper classes for obtaining instances of AutoCAD
/// COM objects for use with .NET Core, which does not
/// support Marshal.GetActiveObject().
/// 
/// This code takes a brute-force approach to finding
/// running instances of AutoCAD and AutoCAD documents,
/// mainly because progid-based access is problematic.
/// 
/// Use the GetActiveAcadApp() method to get an active
/// IAcadApplication object. See the docs below for more
/// on using this with verticals.
/// 
/// Known Issues:
/// 
/// If AutoCAD is started from a shortcut to acad.exe
/// that has the /nologo switch (which suppresses the
/// splash screen), it does not register anything in 
/// the ROT, and this code will fail. Any use of the
/// /nologo switch in a DOS command line will have the
/// same effect (it seems that the splash screen code
/// is doing the COM registration).
 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Autodesk.AutoCAD.InteropHelpers
{

   public static class COMInterop
   {
      [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
      internal static extern int CLSIDFromProgID(string lpszProgID, out Guid lpclsid);
      [DllImport("ole32")]
      private static extern int CLSIDFromProgIDEx([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid lpclsid);
      [DllImport("oleaut32.dll")]
      private static extern int GetActiveObject([MarshalAs(UnmanagedType.LPStruct)] Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
      [DllImport("ole32.dll")]
      private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
      [DllImport("ole32.dll")]

      static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);
      public static object GetActiveObject(string progId) => GetActiveObject(progId, true);

      public static object GetActiveObject(string progId, bool throwOnError = true)
      {
         if(progId == null)
            throw new ArgumentNullException(nameof(progId));

         var hr = CLSIDFromProgID(progId, out var clsid);
         if(hr < 0)
         {
            if(throwOnError)
               System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hr);

            return null;
         }

         hr = GetActiveObject(clsid, IntPtr.Zero, out var obj);
         if(hr < 0)
         {
            if(throwOnError)
               System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hr);
            return null;
         }
         return obj;
      }

      /// <summary>
      /// Returns the first running instance of an
      /// AutoCAD.Application object found, or null
      /// if no running instance was found.
      /// </summary>
      /// <param name="name">The value to match against
      /// the object's Name property. For AutoCAD, the
      /// default value can be used. For AutoCAD vertical
      /// applications, the value should be whatever the
      /// application object's Name property starts with.
      /// 
      /// Running the following LISP on the command
      /// line will return the required value:
      /// 
      ///    (vl-load-com)
      ///    (vla-get-Name (vlax-get-acad-object))
      ///    
      /// No additional checks are done to ensure the
      /// object is actually an AcadApplication verses
      /// some other COM object having a Name property
      /// whose value matches the target string.
      /// 
      /// </param>
      /// <returns></returns>

      public static object GetActiveAcadApp(string name = "AutoCAD")
      {
         foreach(object comObject in GetActiveObjects())
         {
            try
            {
               string appname = ((dynamic)comObject).Name;
               if(appname?.StartsWith(name) == true)
                  return comObject;
            }
            catch
            {
            }
         }
         return null;
      }

      /// <summary>
      /// Returns an enumerable sequence of AcadDocument
      /// objects from the rot.
      /// 
      /// Issue:
      /// 
      /// This method is only returning the first open document 
      /// in the only instance of AutoCAD found in the ROT.
      /// 
      /// It would appear that not all open documents are being
      /// registered with COM.
      /// </summary>
      /// <returns></returns>

      public static IEnumerable<object> GetActiveAcadDocuments()
      {
         foreach(object comObject in GetActiveObjects())
         {
            string name = string.Empty;
            try
            {
               name = ((dynamic)comObject).Name;
            }
            catch(System.Exception ex)
            {
               Debug.WriteLine(ex.ToString());
               continue;
            }
            if(name.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase) == true)
               yield return comObject;
         }
      }

      /// <summary>
      /// Gets an enumeration of all objects in the ROT
      /// </summary>
      
      public static IEnumerable<object> GetActiveObjects()
      {
         IRunningObjectTable rot;
         if(GetRunningObjectTable(0, out rot) == 0)
         {
            IEnumMoniker enumMoniker;
            rot.EnumRunning(out enumMoniker);
            IntPtr fetched = IntPtr.Zero;
            IMoniker[] monikers = new IMoniker[1];
            while(enumMoniker.Next(1, monikers, fetched) == 0)
            {
               IBindCtx bindCtx;
               CreateBindCtx(0, out bindCtx);
               object comObject = null;
               try
               {
                  if(rot.GetObject(monikers[0], out comObject) != 0 || comObject == null)
                     continue;
               }
               catch(System.Exception ex)
               {
                  Console.WriteLine($"exception: {ex.ToString()}");
               }
               yield return comObject;
            }
         }
      }

   }
}