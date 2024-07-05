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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace Autodesk.AutoCAD.InteropHelpers
{
   using System.Runtime.InteropServices;

   public static class COMInterop
   {
      [DllImport("ole32.dll")]
      private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

      [DllImport("ole32.dll")]
      private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);


      /// <summary>
      /// Returns the first running instance of an
      /// AutoCAD.Application object found, or null
      /// if no running instance was found.
      /// </summary>
      /// <returns></returns>
      
      public static object GetActiveAcadApp(string name = "AutoCAD")
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
               object comObject;
               try
               {
                  rot.GetObject(monikers[0], out comObject);
                  if(comObject == null)
                     continue;
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
               catch 
               { 
               }
            }
         }
         return null;
      }

      /// <summary>
      /// Returns a dictionary containing the filenames and 
      /// corresponding AcadDocument objects found. This will
      /// not return AcadDocuments with non-DWG files.
      /// </summary>

      public static Dictionary<string, object> GetActiveAcadDocumentMap()
      {
         Dictionary<string, object> map = new Dictionary<string, object>(
            StringComparer.OrdinalIgnoreCase);
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
               object comObject;
               try
               {
                  rot.GetObject(monikers[0], out comObject);
                  if(comObject == null)
                     continue;
                  try
                  {
                     string fullName = ((dynamic)comObject).FullName;
                     if(fullName?.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase) == true)
                        map[fullName] = comObject;
                  }
                  catch
                  {
                  }
               }
               catch
               {
               }
            }
         }
         return map;
      }

      public static IEnumerable<object> GetActiveAcadDocuments()
      {
         Dictionary<string, object> map = new Dictionary<string, object>(
            StringComparer.OrdinalIgnoreCase);
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
               string fullName = string.Empty;
               try
               {
                  rot.GetObject(monikers[0], out comObject);
                  if(comObject == null)
                     continue;
                  try
                  {
                     fullName = ((dynamic)comObject).FullName;
                  }
                  catch
                  {
                  }
               }
               catch
               {
               }
               if(fullName.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase) == true)
                  yield return comObject;
            }
         }
      }

   }
}