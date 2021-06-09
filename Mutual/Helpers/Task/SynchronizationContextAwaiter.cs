using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mutual.Helpers
{
    public struct SynchronizationContextAwaiter : INotifyCompletion
    {
        private static readonly SendOrPostCallback _postCallback = state => ((Action) state)();

        private readonly SynchronizationContext _context;

        public SynchronizationContextAwaiter(SynchronizationContext context)
        {
            _context = context;
        }

        public bool IsCompleted => _context == SynchronizationContext.Current;

        public void OnCompleted(Action continuation) => _context.Post(_postCallback, continuation);

        public void GetResult()
        {
        }
    }

    public static class SynchronizationContextAwaiterExt
    {
        public static SynchronizationContextAwaiter GetAwaiter(this SynchronizationContext context)
        {
            return new SynchronizationContextAwaiter(context);
        }
        
        public static async Task Sync(this SynchronizationContext context)
        {
            await context;
        }
        
    }		
}