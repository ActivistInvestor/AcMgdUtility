using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Autodesk.AutoCAD.ApplicationServices
{
   /// <summary>
   /// An update of the original TypedValueList that was originally
   /// published here:
   /// 
   ///    http://www.theswamp.org/index.php?topic=14495.msg186823#msg186823
   /// 
   /// This class remains only for the purpose of allowing implicit
   /// casts to/from ResultBuffer, TypedValue[] and SelectionFilter;
   /// 
   /// Most functionality that was previously provided by the original
   /// TypedValueList is now implmented as extension methods that can 
   /// target List<TypedValue>, IList<TypedValue>, and TypedValueList.
   /// 
   /// Note that this class targets .NET 5 or later, and will not work
   /// with earlier versions of the .NET framework.
   /// 
   /// See TypedValueListExtensions.cs
   /// </summary>

   public class TypedValueList : List<TypedValue>
   {
      public TypedValueList()
      {
      }

      public TypedValueList(params TypedValue[] args)
      {
         if(args == null)
            throw new ArgumentNullException(nameof(args));
         if(args.Length > 0)
         {
#if(NET5_0_OR_GREATER)
            CollectionsMarshal.SetCount(this, args.Length);
            var span = new ReadOnlySpan<TypedValue>(args);
            span.CopyTo(CollectionsMarshal.AsSpan(this));
#else
            this.AddRange(args);
#endif
         }
      }

      /// <summary>
      /// New: constructors that take ValueTuples instead 
      /// of TypedValues. The first element of the tuple 
      /// can be either a DxfCode, a short, or a LispDataType:
      /// 
      ///   new TypedValueList(
      ///      (DxfCode.Text, "Moe"),
      ///      (DxfCode.Text, "Larry"),
      ///      (DxfCode.Text, "Curly"));
      ///      
      /// or: 
      /// 
      ///   new TypedValueList((1, "Moe"), (1, "Larry"), (1, "Curly"));
      ///   
      /// or: 
      ///  
      ///   new TypedValueList(
      ///      (LispDataType.ListBegin, null),
      ///      (LispDataType.ObjectId, someObjectId),
      ///      (LispDataType.Point3d, new Point3d(0, 0, 0)),
      ///      (LispDataType.ListEnd, null)
      ///   );
      /// 
      /// </summary>

      public TypedValueList(params (short code, object value)[] args)
         : this(FromTuples(args))
      {
      }

      public TypedValueList(params (DxfCode code, object value)[] args)
         : this(FromTuples(args))
      {
      }

      public TypedValueList(params (LispDataType code, object value)[] args)
         : this(FromTuples(args))
      {
      }

      public TypedValueList(IEnumerable<TypedValue> args)
      {
         if(args == null)
            throw new ArgumentNullException(nameof(args));
         AddRange(args);
      }

      static TypedValue[] FromTuples((DxfCode code, object value)[] args)
      {
         if(args == null)
            throw new ArgumentNullException(nameof(args));
         TypedValue[] result = new TypedValue[args.Length];
         for(int i = 0; i < args.Length; i++)
         {
            (DxfCode code, object value) item = args[i];
            result[i] = new TypedValue((short) item.code, item.value);
         }
         return result;
      }

      static TypedValue[] FromTuples((LispDataType code, object value)[] args)
      {
         if(args == null)
            throw new ArgumentNullException(nameof(args));
         TypedValue[] result = new TypedValue[args.Length];
         for(int i = 0; i < args.Length; i++)
         {
            (LispDataType code, object value) item = args[i];
            result[i] = new TypedValue((short)item.code, item.value);
         }
         return result;
      }

      static TypedValue[] FromTuples((short code, object value)[] args)
      {
         if(args == null)
            throw new ArgumentNullException(nameof(args));
         TypedValue[] result = new TypedValue[args.Length];
         for(int i = 0; i < args.Length; i++)
         {
            (short code, object value) item = args[i];
            result[i] = new TypedValue(item.code, item.value);
         }
         return result;
      }


      /// The implicit conversion operators
      /// from the original TypedValueList:

      // Implicit conversion to SelectionFilter
      public static implicit operator SelectionFilter?(TypedValueList src)
      {
         return src != null ? new SelectionFilter(src) : null;
      }

      // Implicit conversion to ResultBuffer
      public static implicit operator ResultBuffer?(TypedValueList src)
      {
         return src != null ? new ResultBuffer(src) : null;
      }

      // Implicit conversion to TypedValue[] 
      public static implicit operator TypedValue[]?(TypedValueList src)
      {
         return src != null ? src.ToArray() : null;
      }

      // Implicit conversion from TypedValue[] 
      public static implicit operator TypedValueList?(TypedValue[] src)
      {
         return src != null ? new TypedValueList(src) : null;
      }

      // Implicit conversion from SelectionFilter
      public static implicit operator TypedValueList?(SelectionFilter src)
      {
         return src != null ? new TypedValueList(src.GetFilter()) : null;
      }

      // Implicit conversion from ResultBuffer
      public static implicit operator TypedValueList?(ResultBuffer src)
      {
         return src != null ? new TypedValueList(src.AsArray()) : null;
      }

      // NEW: Implicit conversion from ValueTuple(short, object)[]
      public static implicit operator TypedValueList((short, object value)[] args)
      {
         return new TypedValueList(args);
      }

      // NEW: Implicit conversion from ValueTuple(DxfCode, object)[]
      public static implicit operator TypedValueList((DxfCode, object value)[] args)
      {
         return new TypedValueList(args);
      }

      // NEW: Implicit conversion from ValueTuple(LispDataType, object)[]
      public static implicit operator TypedValueList((LispDataType, object value)[] args)
      {
         return new TypedValueList(args);
      }

   }



}


