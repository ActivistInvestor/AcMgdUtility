using System;
using System.Collections;
using System.Collections.Generic;

/// IdleFilter.cs  
/// ActivistInvestor / TT
/// Distributed under the MIT license.
/// 
/// <summary>
/// This class is designed to 'filter' AutoCAD's
/// Application.Idle events both by frequency, the
/// the quiescent state of the drawing editor, and
/// if there is an active document. 
/// 
/// The notification exposed by this class will be
/// deferred until the following conditions are met:
/// 
///   1. The DocumentRequired property is false, or
///      there is an active document in the editor.
///      
///   2. The Quiescent property is false, or the
///      editor is in a quiescent state.
///    
///   3. The amount of time elapsed since the point 
///      when the last notification was sent is greater 
///      than the value of the Frequency property.
/// 
/// This base type is used by two concrete derived
/// types included below. Use either of those to
/// access the functionality of this type, or derive
/// a new type from this type to implement custom
/// or specialized logic.
/// </summary>

namespace Autodesk.AutoCAD.ApplicationServices
{
   public abstract class IdleFilter : IDisposable
   {
      TimeSpan frequency;
      bool enabled = false;
      bool notifying = false;
      bool documentRequired = true;
      DateTime last = DateTime.MinValue;

      /// <param name="frequency">The minimum frequency at which
      /// idle notifications are sent</param>
      /// <param name="quiescent">Specifies if the idle
      /// notification can/cannot be sent when the drawing
      /// editor is in a quiescent state (default = true)</param>
      /// <param name="disabled">Specifies the initial enabled
      /// state of the instance (default is enabled).</param>
      /// <param name="document">Specifies if notifications
      /// can only be sent when there is an active document 
      /// (default = true)</param>
      /// <remarks>
      /// This type implements IDisposable, which allows it to
      /// disable itself when it is no-longer needed. An enabled 
      /// instance is always disabled when it is disposed.
      /// </remarks>

      public IdleFilter(TimeSpan frequency, 
         bool quiescent = true, 
         bool disabled = false,
         bool document = true)
      {
         this.frequency = frequency;
         this.Quiescent = quiescent;
         this.Enabled = !disabled;
         this.documentRequired = document;
      }

      /// <summary>
      /// Specifies if Idle notifications are currently enabled.
      /// </summary>

      public bool Enabled
      {
         get
         {
            return enabled;
         }
         set
         {
            if(value ^ enabled)
            {
               if(value)
               {
                  Application.Idle += idle;
                  this.last = DateTime.Now;
               }
               else
               {
                  Application.Idle -= idle;
               }
               enabled = value;
               OnEnabledChanged(enabled);
            }
         }
      }

      /// <summary>
      /// Allows derived types to act when the instance is
      /// enabled or disabled.
      /// </summary>
      /// <param name="value">a value indicating if the
      /// instance is being enabled or disabled</param>

      protected virtual void OnEnabledChanged(bool enabled)
      {
      }

      /// <summary>
      /// Specifies the miniumum amount of time since the
      /// last notification that must elapse before another 
      /// notification is sent. To disable filtering of Idle
      /// notifications by frequency, specify TimeSpan.Zero.
      /// </summary>

      public TimeSpan Frequency
      {
         get
         {
            return frequency;
         }
         set
         {
            frequency = value;
            last = DateTime.Now;
         }
      }

      /// <summary>
      /// Specifies that notifications are deferred if
      /// there is no active document in the editor. 
      /// If this value true, notifications will be
      /// deferred until there is an active document
      /// in the editor. If this property is false,
      /// notifications are not deferred regardless of
      /// if there's an active document or not.
      /// </summary>

      public bool DocumentRequired 
      {
         get => documentRequired;
         set => documentRequired = value;
      }

      /// <summary>
      /// Resets the internal timer to the specified frequency.
      /// </summary>

      public void Reset()
      {
         last = DateTime.Now;
      }

      /// <summary>
      /// Gets/sets a value indicating if notifications should 
      /// be sent only when the editor is in a quiescent state.
      /// 
      /// If this is true, notifications are deferred until the
      /// editor is in a quiescent state, or there is no active 
      /// document. Note that the active document may not be the
      /// same between the point when the source idle event is 
      /// raised and the point when a deferred notification is 
      /// sent.
      /// </summary>

      public bool Quiescent { get; set; }

      /// <summary>
      /// Raises the Idle event or invokes an action in 
      /// the included derived types.
      /// 
      /// Custom types derived from this class can override
      /// this to handle the notification.
      /// </summary>
      /// <param name="frequency">The amount of time that has 
      /// elapsed since this method was last invoked.</param>
      /// <returns>A value indicating if the instance should
      /// be enabled</returns>

      protected virtual bool OnIdle(TimeSpan frequency)
      {
         return true;
      }

      /// <summary>
      /// This can be overridden in a derived type to
      /// specify if an idle notification can be sent,
      /// exclusive of filtering by frequency.
      /// </summary>

      protected virtual bool CanInvoke
      {
         get
         {
            return enabled && ! notifying 
               && (!documentRequired || Application.DocumentManager.MdiActiveDocument != null)
               && !Quiescent || IsQuiescent;
         }
      }

      void idle(object sender, EventArgs e)
      {
         if(CanInvoke)
         {
            var elapsed = DateTime.Now - last;
            if(elapsed > frequency)
            {
               notifying = true;
               bool flag = false;
               try
               {
                  flag = OnIdle(elapsed);
                  last = DateTime.Now;
               }
               finally
               {
                  this.Enabled = flag;
                  notifying = false;
               }
            }
         }
      }

      // Note that this returns true if there is no document

      bool IsQuiescent
      {
         get
         {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            return doc == null || doc.Editor.IsQuiescent;
         }
      }

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
         Enabled = false;
      }

      ~IdleFilter()
      {
         Dispose(false);
      }
   }

   /// <summary>
   /// Exposes the functionality of the IdleFilter to 
   /// consumers by allowing them to supply a delegate
   /// to handle notifications.
   /// </summary>

   public class IdleAction : IdleFilter
   {
      Func<TimeSpan, bool> handler;

      /// <summary>
      /// Creates an instance with the specified parameters
      /// </summary>
      /// <param name="action">A function that takes
      /// a TimeSpan, and returns a bool indicating
      /// if further notifications are to be sent</param>
      /// <param name="frequency">The minimum amount of
      /// time that must elapse since the most-recent
      /// notification</param>
      /// <param name="quiescent">A value indicating if
      /// notifications should only be sent when the
      /// editor is in a quiescent state</param>
      /// <param name="disabled">A value indicating if
      /// the instance is disabled by default (false).</param>
      /// <param name="document">A value indicating if
      /// notifications should only be sent if there is
      /// an active document. Default = true</param>
      /// <exception cref="ArgumentNullException">The
      /// action is null</exception>

      public IdleAction(Func<TimeSpan, bool> action, 
         TimeSpan frequency = default(TimeSpan), 
         bool quiescent = true, 
         bool disabled = false,
         bool document = true) : base(frequency, quiescent, disabled, document)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         this.handler = action;
      }

      protected override bool OnIdle(TimeSpan frequency)
      {
         base.OnIdle(frequency);
         return handler(frequency);
      }

      /// <summary>
      /// Can be used to recieve a single idle notification
      /// that is handled by a given action.
      /// 
      /// The specified action is invoked only once per call 
      /// to this API.
      /// 
      /// This API is typically used to perform one-time
      /// initialization (e.g., an IExtensionApplication)
      /// that must be deferred until the drawing editor is
      /// initialized and there is an active document. The
      /// callback supplied to this API will always execute
      /// in the application execution context.
      /// </summary>
      /// <param name="action">The action to be invoked</param>
      /// <param name="quiescent">True to wait until the editor 
      /// is quiescent (default = true)</param>
      /// <param name="delay">Minimum number of milliseconds 
      /// to wait before invoking the action. The default is to
      /// invoke the action immediately on the next raising of 
      /// the Idle event without delay.</param>

      public static void OnNextIdle(Action action, 
         bool quiescent = true,
         long delay = 0L)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         using(new IdleAction(delegate(TimeSpan unused)
         {
            action();
            return false;
         }, TimeSpan.FromMilliseconds(delay), quiescent, false, true));
      }
   }

   public class IdleEventArgs : EventArgs
   {
      public IdleEventArgs(TimeSpan duration)
      {
         this.Duration = duration;
         this.Enabled = true;
      }

      /// <summary>
      /// The amount of time that has elapsed since 
      /// the most-recent notification was sent:
      /// </summary>

      public TimeSpan Duration { get; private set; }

      /// <summary>
      /// Specifes if subsequent notifications are
      /// to be sent. If set to false, no subsequent
      /// notifications are sent unless the Enabled
      /// property of the sender is subseqently set 
      /// to true. The value of this property will
      /// always be true but can be set to false if 
      /// further notifications are not desired.
      /// </summary>

      public bool Enabled { get; set; }
   }

   public delegate void IdleEventHandler(object sender, IdleEventArgs e);

   /// <summary>
   /// Exposes the functionality of the base type to 
   /// consumers through an event.
   /// 
   /// See the included IdleEventObserverExample.cs for 
   /// examnples showing how to consume this class.
   /// </summary>

   public class IdleEventFilter : IdleFilter
   {
      event IdleEventHandler idle;

      /// <summary>
      /// Creates a new instance that is disabled until a
      /// handler is added to the Idle event.
      /// </summary>
      /// <param name="frequency">The minimum freqency at which
      /// idle notifications are sent</param>
      /// <param name="quiescent">Specifies if the idle
      /// notification can/cannot be sent when the editor
      /// is in a quiescent state (default = true)</param>

      public IdleEventFilter(TimeSpan frequency, bool quiescent = true)
         : base(frequency, quiescent, true)
      {
      }

      /// <summary>
      /// Creates an instance using the default values of no
      /// delay and to send notifications only when the editor
      /// is quiescent:
      /// </summary>

      public IdleEventFilter() : this(TimeSpan.FromSeconds(0), true)
      {
      }

      /// <summary>
      /// Exposes the filtered Application.Idle event to consumers.
      /// </summary>
      /// <remarks>
      /// The instance is automatically enabled or disabled based on
      /// if there are any handlers attached to this event. When the
      /// first handler is added, the instance becomes enabled and
      /// begins listening to the Application's Idle event. When the
      /// last handler is removed, the instance becomes disabled and
      /// no longer listens to the Application's Idle event.
      /// 
      /// Hence, it isn't necessary to manipulate the Enabled property
      /// manually when adding or removing handlers from the event. The
      /// Enabled property can be set to False when there is a handler
      /// in specialized use cases, however.
      /// 
      /// A handler of this event can disable subsequent notifications
      /// by setting the Enabled property of the event args to false.
      /// </remarks>

      public event IdleEventHandler Idle
      {
         add
         {
            idle += value;
            Enabled = true;
         }
         remove
         {
            idle -= value;
            Enabled = idle != null;
         }
      }

      protected sealed override bool OnIdle(TimeSpan elapsed)
      {
         if(idle != null)
         {
            var args = new IdleEventArgs(elapsed);
            idle(this, args);
            return args.Enabled;
         }
         return false;
      }

      /// <summary>
      /// The following types are used by specializations of
      /// IdleFilter that are not included in this distribution.
      /// The main reason for this is due to the possiblity that
      /// the type T could expose references to objects that are
      /// no longer in-scope or usable.
      /// </summary>

      public delegate void IdleEventHandler<T>(object sender, IdleEventArgs<T> e) where T : EventArgs;
      public class EventQueue<T> : Queue<IdleEventHandler<T>> where T:EventArgs { }
     
      public class IdleEventArgs<T> : IdleEventArgs where T:EventArgs
      {
         T sourceEventArgs;
         WeakReference<Object> sender;

         public IdleEventArgs(object sender, T sourceEventArgs, TimeSpan duration) : base(duration)
         {
            this.sourceEventArgs = sourceEventArgs;
            this.sender = new WeakReference<object>(sender);
         }

         public bool IsSenderAlive 
         { 
            get
            {
               return sender.TryGetTarget(out _);
            }
         }

         public object Sender 
         {
            get
            {
               if(sender.TryGetTarget(out var obj))
                  return obj;
               else
                  return null;
            }
            private set 
            { 
               this.sender = new WeakReference<object>(sender); 
            }
         }

         public T SourceEventArgs { get; private set; }
      }


   }
}

