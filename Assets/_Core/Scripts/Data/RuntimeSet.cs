using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gaman
{
    /// <summary>
    /// A ScriptableObject registry. Objects add themselves on Awake and remove
    /// themselves on OnDestroy; anything that needs them queries the asset —
    /// no scene coupling, no singletons.
    /// </summary>
    [CreateAssetMenu(fileName = "RuntimeSet", menuName = "SDE/Data/RuntimeSet")]
    public class RuntimeSet : ScriptableObject
    {
        private readonly LinkedList<IRuntime> _items = new LinkedList<IRuntime>();

        public LinkedList<IRuntime> List => _items;
        public bool IsEmpty => _items.Count == 0;

        public void Add(IRuntime item) => _items.AddLast(item);
        public void Remove(IRuntime item) => _items.Remove(item);

        // Safe retrieval — returns false instead of throwing
        public bool TryGetFirst<T>(out T result) where T : class, IRuntime
        {
            foreach (IRuntime item in _items)
            {
                if (item is T typed)
                {
                    result = typed;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public bool TryGetLast<T>(out T result) where T : class, IRuntime
        {
            LinkedListNode<IRuntime> node = _items.Last;
            while (node != null)
            {
                if (node.Value is T typed)
                {
                    result = typed;
                    return true;
                }

                node = node.Previous;
            }

            result = null;
            return false;
        }

        // Fast retrieval — throws if not found
        public T GetFirst<T>() where T : class, IRuntime
        {
            if (TryGetFirst<T>(out T result))
            {
                return result;
            }

            throw new InvalidOperationException($"No item of type {typeof(T).Name} found in RuntimeSet '{name}'.");
        }

        public T GetLast<T>() where T : class, IRuntime
        {
            if (TryGetLast<T>(out T result))
            {
                return result;
            }

            throw new InvalidOperationException($"No item of type {typeof(T).Name} found in RuntimeSet '{name}'.");
        }

        // Fire-and-forget — execute an action only if a matching item exists
        public bool TryApplyToFirst<T>(Action<T> action) where T : class, IRuntime
        {
            if (TryGetFirst<T>(out T result))
            {
                action(result);
                return true;
            }

            return false;
        }
    }
}
