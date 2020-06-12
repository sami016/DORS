using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DORS.Shared
{
    public delegate void SubscribeDisposer();
    public delegate void MessageHandler<T>(T message);

    /// <summary>
    /// Class allowing subsription to messages based on type.
    /// </summary>
    public class PolymorphicDispatcher
    {
        private IDictionary<Type, IList<MessageHandler<object>>> _typeHandlers = new Dictionary<Type, IList<MessageHandler<object>>>();
        private readonly object _lock = new object();

        /// <summary>
        /// Subscribe to all messages of type T. 
        /// </summary>
        /// <typeparam name="T">type</typeparam>
        /// <param name="handler">handler</param>
        /// <returns></returns>
        public SubscribeDisposer Subscribe<T>(MessageHandler<T> handler)
            where T : class
        {
            var type = typeof(T);
            lock (_lock) {
                if (!_typeHandlers.ContainsKey(type))
                {
                    _typeHandlers[type] = new List<MessageHandler<object>>();
                }
            }
            lock (_typeHandlers[type])
            {
                MessageHandler<object> objectHandler = obj => handler(obj as T);
                _typeHandlers[type].Add(objectHandler);
                return () => RemoveSubscription<T>(objectHandler);
            }
        }

        public void RemoveSubscription<T>(MessageHandler<object> handler)
        {
            var type = typeof(T);
            IList<MessageHandler<object>> handlers;
            lock (_lock)
            {
                if (!_typeHandlers.TryGetValue(type, out handlers))
                {
                    return;
                }
            }
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }

        public void Dispatch(object message)
        {
            var type = message.GetType();
            IList<MessageHandler<object>> handlers;
            lock (_lock)
            {
                if (!_typeHandlers.TryGetValue(type, out handlers))
                {
                    return;
                }
                handlers = handlers.ToArray();
            }

            foreach (var handler in handlers)
            {
                handler(message);
            }
        }
    }
}
