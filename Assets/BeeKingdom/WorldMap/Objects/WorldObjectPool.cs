using System;
using System.Collections.Generic;

namespace BeeKingdom.WorldMap
{
    // Vue rendue d'un objet du monde (handle graphique). Le pool ne connait que cette
    // abstraction : les implementations concretes (sprites, maillages, UI) viendront
    // dans les sprints de contenu.
    public interface IWorldObjectView
    {
        void Attach(WorldObject owner);
        void Detach(WorldObject owner);
        void SetWorldPosition(WorldPosition position);
        void SetVisible(bool visible);
    }

    // Pool generique de vues d'objets, par cle (future cle de prefab/type de vue).
    // Rent/Return reutilisent les instances ; la capacite maximale par cle est
    // configurable (0 = illimite). Rent retourne null si la capacite est epuisee.
    public sealed class WorldObjectPool
    {
        private readonly PoolSettings settings;
        private readonly Dictionary<string, Stack<IWorldObjectView>> available = new Dictionary<string, Stack<IWorldObjectView>>();
        private readonly Dictionary<string, Func<IWorldObjectView>> factories = new Dictionary<string, Func<IWorldObjectView>>();
        private readonly Dictionary<string, int> outstanding = new Dictionary<string, int>();

        public int Created { get; private set; }
        public int RentedOutstanding { get; private set; }

        public WorldObjectPool(PoolSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void RegisterFactory(string key, Func<IWorldObjectView> factory)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A view key is required.", nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            factories[key] = factory;
        }

        public bool HasFactory(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && factories.ContainsKey(key);
        }

        public void Prewarm(string key, int count)
        {
            if (count <= 0)
            {
                return;
            }

            EnsureStack(key);
            int warmup = settings.MaxPerKey > 0 ? Math.Min(count, settings.MaxPerKey) : count;
            for (int i = 0; i < warmup; i++)
            {
                available[key].Push(factories[key]());
                Created++;
            }
        }

        public IWorldObjectView Rent(string key)
        {
            if (!factories.TryGetValue(key, out Func<IWorldObjectView> factory))
            {
                throw new InvalidOperationException("No view factory is registered for key '" + key + "'.");
            }

            if (settings.MaxPerKey > 0 && Outstanding(key) >= settings.MaxPerKey)
            {
                return null;
            }

            EnsureStack(key);
            IWorldObjectView view;
            bool created = false;
            if (available[key].Count > 0)
            {
                view = available[key].Pop();
            }
            else
            {
                view = factory();
                created = true;
            }

            if (view == null)
            {
                throw new InvalidOperationException("The view factory for key '" + key + "' returned null.");
            }

            outstanding[key] = (outstanding.TryGetValue(key, out int outstandingCount) ? outstandingCount : 0) + 1;
            RentedOutstanding++;
            if (created)
            {
                Created++;
            }

            return view;
        }

        public void Return(string key, IWorldObjectView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (!outstanding.TryGetValue(key, out int count) || count <= 0)
            {
                throw new InvalidOperationException("The view was not rented from this pool under key '" + key + "'.");
            }

            outstanding[key] = count - 1;
            RentedOutstanding--;
            EnsureStack(key);
            available[key].Push(view);
        }

        public int Outstanding(string key)
        {
            return outstanding.TryGetValue(key, out int count) ? count : 0;
        }

        public int Available(string key)
        {
            return available.TryGetValue(key, out Stack<IWorldObjectView> stack) ? stack.Count : 0;
        }

        private void EnsureStack(string key)
        {
            if (!available.TryGetValue(key, out Stack<IWorldObjectView> stack))
            {
                stack = new Stack<IWorldObjectView>();
                available.Add(key, stack);
            }
        }
    }
}
