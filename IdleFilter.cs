using System;

/// IdleFilter.cs  
/// ActivistInvestor / Tony Tanzillo
/// Distributed under the MIT license.
/// 
/// <summary>
/// This class is designed to 'filter' AutoCAD's
/// Application.Idle events both by frequency and
/// by the quiescent state of the drawing editor.
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
      TimeSpan duration;
      bool enabled = false;
      DateTime last = DateTime.MinValue;

      /// <param name="duration">The minimum freqency at which
      /// idle notifications are sent</param>
      /// <param name="quiescent">Specifies if the idle
      /// notification can/cannot be sent when the drawing
      /// editor is in a quiescent state (default = true)</param>
      /// <param name="disabled">Specifies the initial enabled
      /// state of the instance (default is enabled).</param>
      /// <remarks>
      /// This type implements IDisposable, which allows it to
      /// disable itself when it is no-longer needed. An enabled 
      /// instance is always disabled when it is disposed.
      /// </remarks>

      public IdleFilter(TimeSpan duration, bool quiescent = true, bool disabled = false)
      {
         this.duration = duration;
         this.Quiescent = quiescent;
         this.Enabled = !disabled;
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
               OnEnabledChanged(value);
            }
         }
      }

      /// <summary>
      /// Allows derived types to act when the instance is
      /// enabled or disabled.
      /// </summary>
      /// <param name="value">a value indicating if the
      /// instance is being enabled or disabled</param>

      protected virtual void OnEnabledChanged(bool value)
      {
      }

      /// <summary>
      /// Specifies the miniumum amount of time since the
      /// last notification that must elapse before another 
      /// notification is sent. To disable filtering of Idle
      /// notifications by frequency, specify TimeSpan.Zero.
      /// </summary>

      public TimeSpan Duration
      {
         get
         {
            return duration;
         }
         set
         {
            duration = value;
            last = DateTime.Now;
         }
      }

      /// <summary>
      /// Resets the internal timer to the specified duration,
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
      /// document.
      /// </summary>

      public bool Quiescent { get; set; }

      /// <summary>
      /// Raises the Idle event or invokes an action in 
      /// the included derived types.
      /// 
      /// Custom types derived from this class can override
      /// this to handle the notification.
      /// </summary>
      /// <param name="duration">The amount of time that has 
      /// elapsed since this method was last invoked.</param>
      /// <returns>A value indicating if the instance should
      /// be enabled</returns>

      protected virtual bool OnIdle(TimeSpan duration)
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
            return enabled && Application.DocumentManager.MdiActiveDocument != null
               && !Quiescent || IsQuiescent;
         }
      }

      void idle(object sender, EventArgs e)
      {
         if(CanInvoke)
         {
            var elapsed = DateTime.Now - last;
            if(elapsed > duration)
            {
               this.Enabled = OnIdle(elapsed);
               last = DateTime.Now;
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
         this.Dispose(true);
      }

      protected virtual void Dispose(bool disposing)
      {
         Enabled = false;
      }
   }

   /// <summary>
   /// Exposes the functionality of the IdleFilter to 
   /// consumers by allowing them to supply an fuction 
   /// to handle notifications.
   /// </summary>

   public class IdleAction : IdleFilter
   {
      Func<TimeSpan, bool> handler;
      bool single;

      /// <summary>
      /// Creates an instance with the specified parameters
      /// </summary>
      /// <param name="action">A function that takes
      /// a TimeSpan, and returns a bool indicating
      /// if further notifications are to be sent</param>
      /// <param name="duration">The minimum amount of
      /// time that must elapse since the most-recent
      /// notification</param>
      /// <param name="quiescent">A value indicating if
      /// notifications should only be sent when the
      /// editor is in a quiescent state</param>
      /// <param name="disabled">A value indicating if
      /// the instance is disabled by default (false).</param>
      /// <exception cref="ArgumentNullException">The
      /// action is null</exception>

      public IdleAction(Func<TimeSpan, bool> action, 
            TimeSpan duration, 
            bool quiescent = true, 
            bool disabled = false)
         : base(duration, quiescent, disabled)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         this.handler = action;
      }

      protected override bool OnIdle(TimeSpan duration)
      {
         base.OnIdle(duration);
         return handler(duration);
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
      /// <param name="delay">Minimum number of milliseconds to 
      /// wait before invoking the action. The default invokes 
      /// the action immediately on the next raising of the
      /// Idle event, without delay.</param>

      public static void OnNextIdle(Action action, 
         bool quiescent = true,
         long delay = 0L)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         using(new IdleAction((unused) =>
         {
            action();
            return false;
         }, TimeSpan.FromMilliseconds(delay), quiescent)) ;
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
      /// always be true, but can be set to false if 
      /// further notifications are not desired.
      /// </summary>

      public bool Enabled { get; private set; }
   }

   public delegate void IdleEventHandler(object sender, IdleEventArgs e);


   /// <summary>
   /// Exposes the functionality of the base type to 
   /// consumers through an event.
   /// 
   /// See the included IdleEventObserverExample.cs for 
   /// an examnple showing how to consume this class.
   /// </summary>

   public class IdleEventFilter : IdleFilter
   {
      event IdleEventHandler idle;

      /// <summary>
      /// Creates a new instance.
      /// </summary>
      /// <param name="duration">The minimum freqency at which
      /// idle notifications are sent</param>
      /// <param name="quiescent">Specifies if the idle
      /// notification can/cannot be sent when the editor
      /// is in a quiescent state (default = true)</param>

      public IdleEventFilter(TimeSpan duration, bool quiescent = true)
         : base(duration, quiescent, true)
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
      /// first handler is added, the instance becomes enabled. When
      /// the last handler is removed, the instance becomes disabled.
      /// 
      /// Hence, it isn't necessary to manipulate the Enabled property
      /// manually when adding or removing handlers from the event.
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
         base.OnIdle(elapsed);
         if(idle != null)
         {
            var args = new IdleEventArgs(elapsed);
            idle(this, args);
            return args.Enabled;
         }
         return false;
      }
   }
}

