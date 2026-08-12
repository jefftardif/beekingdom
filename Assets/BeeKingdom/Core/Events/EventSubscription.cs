using System;

namespace BeeKingdom.Core.Events
{
    public sealed class EventSubscription : IDisposable
    {
        private readonly Action unsubscribe;
        private bool isDisposed;

        public bool IsDisposed => isDisposed;

        public EventSubscription(Action unsubscribe)
        {
            this.unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            unsubscribe();
            isDisposed = true;
        }
    }
}
