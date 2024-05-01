using Autodesk.AutoCAD.DatabaseServices;
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
///   2. The Quiescent property is false, or there
///      editor is in a quiescent state.
///    
///   3. The amount of time elapsed since the point 
///      when the last notification was sent is less 
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
      bool lockDocument = false;

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

      protected virtual void UpdateEnabled(bool reset = true)
      {
         if(reset)
            Reset();
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
            if(!enabled || notifying)
               return false;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            return doc == null ? !documentRequired 
               : !Quiescent || doc.Editor.IsQuiescent;
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

      // Note that this returns false if there is no document

      bool IsQuiescent
      {
         get
         {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            return doc != null && doc.Editor.IsQuiescent;
         }
      }

      /// <summary>
      /// If true, the document will be automatically 
      /// locked when the Idle event fires, and unlocked
      /// when the event handler returns. 
      /// 
      /// Note that it is up to derived types to enforce
      /// and implement automatic document locking/unlocking
      /// at the point when the consumer gets control from
      /// within the handler of the Idle notification.
      /// </summary>

      public bool LockDocument { get => lockDocument; set => lockDocument = value; }

      public Document ActiveDocument => Application.DocumentManager.MdiActiveDocument;

      protected IDisposable TryLockDocument()
      {
         if(LockDocument && ActiveDocument != null && Application.DocumentManager.IsApplicationContext)
            return ActiveDocument.LockDocument();
         else
            return null;
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

   public class IdleActionFilter : IdleFilter
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

      public IdleActionFilter(Func<TimeSpan, bool> action,
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
         using(TryLockDocument())
         {
            return handler(frequency);
         }
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
         using(new IdleActionFilter(delegate (TimeSpan unused)
         {
            action();
            return false;
         }, TimeSpan.FromMilliseconds(delay), quiescent, false, true)) ;
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
            UpdateEnabled();
         }
         remove
         {
            idle -= value;
            UpdateEnabled();
         }
      }

      protected override void UpdateEnabled(bool reset = true)
      {
         Enabled = idle != null;
         base.UpdateEnabled(reset);
      }

      protected sealed override bool OnIdle(TimeSpan elapsed)
      {
         if(idle != null)
         {
            var args = GetEventArgs(elapsed);
            using(TryLockDocument())
            {
               idle(this, args);
            }
            return args.Enabled;
         }
         return false;
      }

      protected virtual IdleEventArgs GetEventArgs(TimeSpan elapsed)
      {
         return new IdleEventArgs(elapsed);
      }
   }

   public delegate void IdleEventHandler<T>(object sender, IdleEventArgs<T> e);

   public class IdleEventArgs<T> : IdleEventArgs
   {
      IReadOnlyList<T> items;
      public IdleEventArgs(List<T> list, TimeSpan duration) : base(duration)
      {
         this.items = list.AsReadOnly();
      }

      public int Count => Items.Count;

      public IReadOnlyList<T> Items
      {
         get => items;
         private set => items = value;
      }
   }

   /// <summary>
   /// This class works in ways similar to IdleEventFilter, except
   /// that it marshals a list of the generic argument type, and 
   /// will invoke its Idle event at the prescribed time, passing 
   /// in an instance of IdleEventArgs<T>, which provides access to 
   /// the list containing all items added since the last time the 
   /// Idle event was raised. Each time the Idle event is raised,
   /// the contents of the list is cleared.
   /// 
   /// The Add() method is used to add event data to the instance. 
   /// When the Idle event is raised, the Idle event handler is passed 
   /// an event argument type that exposes the list of all items added 
   /// to the instance since the last time the Idle event was raised. 
   /// 
   /// After the handler(s) for the Idle event return, the list is
   /// cleared and the instance is disabled until more items are 
   /// added to it.
   /// 
   /// Note that one way to allow information from different events
   /// to be added to the instance, is to declare the generic argument
   /// type as EventArgs, which allows any event argument that derives
   /// from EventArgs to be added.
   /// </summary>
   /// <typeparam name="T">The type of the list element</typeparam>

   public class EventDataCollection<T> : IdleFilter, IReadOnlyList<T>
   {
      event IdleEventHandler<T> idle;
      protected internal List<T> list = new List<T>();
      HashSet<T> set = null;
      IEqualityComparer<T> comparer = null;
      bool allowDuplicates = true;


      /// <summary>
      /// Creates a new instance that is disabled until a
      /// handler is added to the Idle event and one or
      /// more items are added to the instance.
      /// </summary>
      /// <param name="frequency">The minimum freqency at which
      /// idle notifications are sent</param>
      /// <param name="quiescent">Specifies if the idle
      /// notification can/cannot be sent when the editor
      /// is in a quiescent state (default = true)</param>

      public EventDataCollection(TimeSpan frequency, bool quiescent = true, IEqualityComparer<T> comparer = null)
         : base(frequency, quiescent, true, true)
      {
         this.comparer = comparer ?? EqualityComparer<T>.Default;
      }

      /// <summary>
      /// Creates an instance using the default values of no
      /// delay and to send notifications only when the editor
      /// is quiescent:
      /// </summary>

      public EventDataCollection() : this(TimeSpan.FromSeconds(0), true)
      {
      }

      /// <summary>
      /// Exposes the filtered Application.Idle event to consumers.
      /// </summary>
      /// <remarks>
      /// The instance is automatically enabled or disabled based on
      /// if there are any handlers attached to this event and there
      /// is at least one element added to the instance. 
      /// </remarks>

      public event IdleEventHandler<T> Idle
      {
         add
         {
            idle += value;
            UpdateEnabled();
         }
         remove
         {
            idle -= value;
            UpdateEnabled();
         }
      }

      /// <summary>
      /// Adds one or more elements to the instance.
      /// </summary>
      /// <remarks>
      /// If the AllowDuplicates property is false,
      /// attempts to add the same element multiple
      /// times will be ignored without error.
      /// 
      /// Null values cannot be added to the instance.
      /// </remarks>
      /// <param name="data">One or more items to be 
      /// added to the collection</param>

      public virtual void Add(params T[] data)
      {
         if(data != null && data.Length > 0)
         {
            int cnt = this.Count;
            if(!AllowDuplicates)
            {
               if(set == null)
                  set = new HashSet<T>(this.comparer);
               for(int i = 0; i < data.Length; i++)
               {
                  T item = data[i];
                  if(item == null)
                     throw new ArgumentException("null element");
                  if(set.Add(item))
                     list.Add(item);
               }
            }
            else 
            {
               list.AddRange(data);
            }
            if(cnt != list.Count)
            {
               UpdateEnabled();
               OnListChanged();
            }
         }
      }

      /// <summary>
      /// Clears the internal list of event data, 
      /// causing the instance to become disabled.
      /// </summary>

      public virtual void Clear()
      {
         list.Clear();
         set?.Clear();
         OnListChanged();
         UpdateEnabled();
      }

      protected virtual void OnListChanged()
      {
      }

      public int Count => list.Count;

      public T this[int index] => list[index];

      protected override void UpdateEnabled(bool reset = true)
      {
         Enabled = idle != null && list.Count > 0;
         if(reset)
            Reset();
      }

      /// <summary>
      /// If true, the event data list contents must be
      /// distinct, and may not contain multiple values
      /// that compare as equal. If an attempt is made to
      /// add an element that already exists in the list,
      /// it is ignored and no error is signaled. The
      /// default value is false. 
      /// 
      /// This property must be set prior to the first 
      /// call to Add().
      /// </summary>

      public bool AllowDuplicates 
      { 
         get => allowDuplicates;
         set
         {
            if(list.Count > 0)
               throw new InvalidOperationException("list is not empty");
            allowDuplicates = value;
         }
      }

      protected sealed override bool OnIdle(TimeSpan elapsed)
      {
         if(idle != null && list.Count > 0)
         {
            using(TryLockDocument())
            {
               idle(this, new IdleEventArgs<T>(list, elapsed));
               this.Clear();
            }
         }
         return false;
      }

      public IEnumerator<T> GetEnumerator()
      {
         return list.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return this.GetEnumerator();
      }

   }

   /// <summary>
   /// This class provides same functionality as IdleEventFilter<T>,
   /// except that it enforces uniqueness of event data list elements,
   /// using a function that produces a key for each data item, which
   /// must be unique across all elements. Any attempt to add an item
   /// that produces the same key as an existing item will be ignored.
   /// 
   /// The type of data items can be simple types that will be treated
   /// as a functional set, by simply declaring both generic arguments
   /// to have the same type, and passing a function that returns its
   /// argument. In the following example, the data item marshalled by
   /// the IdleEventFilter are ObjectIds and the intent is to disallow
   /// adding the same ObjectId to the data list more than once:
   /// <code>
   /// 
   /// IdleEventFilter<ObjectId, ObjectId> filter = 
   ///    new IdleEventFilter<ObjectId, ObjectId>(
   ///       TimeSpan.FromSeconds(1),               // frequency
   ///       id => id                               // key selector
   ///    );
   /// 
   /// In the following example, a struct containing two ObjectIds
   /// is marshalled by an IdleEventFilter, where one of the two
   /// ObjectIds must be unique:
   /// 
   /// public struct ObjectIdWithOwner
   /// {
   ///    public ObjectIdWithOwner(ObjectId id, ObjectId ownerId)
   ///    {
   ///       this.Id = id;
   ///       this.OwnerId = ownerId;
   ///    }
   ///    public ObjectId Id;
   ///    public ObjectId OwnerId;
   /// }
   /// 
   /// The function passed to the constructor mandates that the 
   /// Id field of each element must be unique across all elements 
   /// added to the instance:
   /// 
   /// IdleEventFilter<ObjectIdWithOwner> filter = 
   ///    new IdleEventFilter<ObjectIdWithOwner>(arg => arg.id);
   /// 
   /// </code>
   /// </summary>
   /// <typeparam name="T">The type of the event data list element</typeparam>
   /// <typeparam name="TKey">The type of the value to use to compare 
   /// event data list elements for equality</typeparam>

   public class EventDataCollection<T, TKey> : EventDataCollection<T>
   {
      /// <summary>
      /// Creates a new instance that is disabled until a
      /// handler is added to the Idle event.
      /// </summary>
      /// <param name="frequency">The minimum freqency at which
      /// idle notifications are sent</param>
      /// <param name="quiescent">Specifies if the idle
      /// notification can/cannot be sent when the editor
      /// is in a quiescent state (default = true)</param>

      Func<T, TKey> selector;
      HashSet<TKey> keys;

      public EventDataCollection(Func<T, TKey> keySelector)
         : base()
      {
         if(keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
         selector = keySelector;
         base.AllowDuplicates = false;
      }

      public new bool AllowDuplicates { get { return false; } set { } }

      public override void Add(params T[] data)
      {
         int count = base.Count;
         if(data != null)
         {
            for(int i = 0; i < data.Length; i++)
            {
               T item = data[i];
               if(keys.Add(selector(item)))
                  list.Add(item);
            }
            if(count != base.Count)
               UpdateEnabled();
         }
      }

      public override void Clear()
      {
         keys.Clear();
         base.Clear();
      }

   }

   public class MyObjectModifiedObserver : IDisposable
   {
      private Database db;
      EventDataCollection<ObjectId> collection;

      public MyObjectModifiedObserver(Database db)
      {
         this.db = db;
         db.ObjectModified += objectModified;
         collection = new EventDataCollection<ObjectId>();
         collection.Frequency = TimeSpan.FromSeconds(1);
         collection.Quiescent = true;
         collection.DocumentRequired = true;
         collection.AllowDuplicates = false;
         collection.Idle += OnIdle;
      }

      private void OnIdle(object sender, IdleEventArgs<ObjectId> args)
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         IReadOnlyList<ObjectId> list = args.Items;
         for(int i = 0; i < list.Count; i++) 
         {
            doc.Editor.WriteMessage($"\nModified 0x{list[i].Handle.ToString().ToUpper()}");
         }
      }

      private void objectModified(object sender, ObjectEventArgs e)
      {
         collection.Add(e.DBObject.ObjectId);
      }

      public void Dispose()
      {
      }
   }
}

