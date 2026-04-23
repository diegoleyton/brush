using System;
using System.Collections.Generic;

namespace Flowbit.Utilities.Core.Events
{
    /// <summary>
    /// Dispatches events to global listeners and sender-specific listeners.
    /// </summary>
    public sealed class EventDispatcher
    {
        private readonly Dictionary<Type, Delegate> eventTable_;
        private readonly Dictionary<(Type, object), Delegate> eventTableBySender_;

        /// <summary>
        /// Creates a new event dispatcher.
        /// </summary>
        public EventDispatcher()
        {
            eventTable_ = new Dictionary<Type, Delegate>();
            eventTableBySender_ = new Dictionary<(Type, object), Delegate>();
        }

        /// <summary>
        /// Subscribes a global listener to the given event type.
        /// </summary>
        public void Subscribe<T>(Action<T> listener) where T : IEvent
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            Type eventType = typeof(T);

            if (eventTable_.TryGetValue(eventType, out Delegate existingDelegate))
            {
                eventTable_[eventType] = Delegate.Combine(existingDelegate, listener);
                return;
            }

            eventTable_.Add(eventType, listener);
        }

        /// <summary>
        /// Subscribes a listener to the given event type for a specific sender.
        /// </summary>
        public void Subscribe<T>(object sender, Action<T> listener) where T : IEvent
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            (Type, object) key = (typeof(T), sender);

            if (eventTableBySender_.TryGetValue(key, out Delegate existingDelegate))
            {
                eventTableBySender_[key] = Delegate.Combine(existingDelegate, listener);
                return;
            }

            eventTableBySender_.Add(key, listener);
        }

        /// <summary>
        /// Unsubscribes a global listener from the given event type.
        /// </summary>
        public void Unsubscribe<T>(Action<T> listener) where T : IEvent
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            Type eventType = typeof(T);

            if (!eventTable_.TryGetValue(eventType, out Delegate existingDelegate))
            {
                return;
            }

            Delegate updatedDelegate = Delegate.Remove(existingDelegate, listener);

            if (updatedDelegate == null)
            {
                eventTable_.Remove(eventType);
                return;
            }

            eventTable_[eventType] = updatedDelegate;
        }

        /// <summary>
        /// Unsubscribes a sender-specific listener from the given event type.
        /// </summary>
        public void Unsubscribe<T>(object sender, Action<T> listener) where T : IEvent
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            (Type, object) key = (typeof(T), sender);

            if (!eventTableBySender_.TryGetValue(key, out Delegate existingDelegate))
            {
                return;
            }

            Delegate updatedDelegate = Delegate.Remove(existingDelegate, listener);

            if (updatedDelegate == null)
            {
                eventTableBySender_.Remove(key);
                return;
            }

            eventTableBySender_[key] = updatedDelegate;
        }

        /// <summary>
        /// Sends an event to all global listeners of its type.
        /// </summary>
        public void Send<T>(T eventData) where T : IEvent
        {
            Type eventType = typeof(T);

            if (!eventTable_.TryGetValue(eventType, out Delegate existingDelegate))
            {
                return;
            }

            ((Action<T>)existingDelegate)?.Invoke(eventData);
        }

        /// <summary>
        /// Sends an event to all global listeners and sender-specific listeners.
        /// </summary>
        public void Send<T>(object sender, T eventData) where T : IEvent
        {
            Type eventType = typeof(T);

            if (eventTable_.TryGetValue(eventType, out Delegate globalDelegate))
            {
                ((Action<T>)globalDelegate)?.Invoke(eventData);
            }

            if (sender == null)
            {
                return;
            }

            (Type, object) key = (eventType, sender);

            if (eventTableBySender_.TryGetValue(key, out Delegate senderDelegate))
            {
                ((Action<T>)senderDelegate)?.Invoke(eventData);
            }
        }

        /// <summary>
        /// Removes all registered listeners.
        /// </summary>
        public void Clear()
        {
            eventTable_.Clear();
            eventTableBySender_.Clear();
        }
    }
}