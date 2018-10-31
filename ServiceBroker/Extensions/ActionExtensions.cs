using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public static class ActionExtensions
    {
        public static Action Debounce(this Action func, TimeSpan delay)
        {
            int requested = 0;
            return () =>
            {
                int alreadyRequested = Interlocked.Exchange(ref requested, 1);

                if (alreadyRequested == 1)
                    return;

                Task.Delay(delay).ContinueWith(task =>
                {
                    Interlocked.Exchange(ref requested, 0);

                    func();
                    task.Dispose();
                });
            };
        }

        public static Action<T> Debounce<T>(this Action<T> func, TimeSpan delay)
        {
            int requested = 0;
            return (arg) =>
            {
                int alreadyRequested = Interlocked.Exchange(ref requested, 1);

                if (alreadyRequested == 1)
                    return;

                Task.Delay(delay).ContinueWith(task =>
                {
                    Interlocked.Exchange(ref requested, 0);

                    func(arg);
                    task.Dispose();
                });
            };
        }
    }
}
