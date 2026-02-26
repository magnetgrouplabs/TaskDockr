using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TaskDockr.Utils
{
    public class IconBackgroundProcessor
    {
        private readonly ConcurrentQueue<IconProcessRequest> _processQueue;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<ImageSource>> _pendingRequests;
        private readonly object _lockObject = new object();
        private bool _isProcessing;

        public IconBackgroundProcessor()
        {
            _processQueue    = new ConcurrentQueue<IconProcessRequest>();
            _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<ImageSource>>();
        }

        public Task<ImageSource> QueueIconProcessAsync(string source, Func<string, Task<ImageSource>> processFunction)
        {
            var requestId = $"{source.GetHashCode():X8}";
            if (_pendingRequests.TryGetValue(requestId, out var existing))
                return existing.Task;

            var tcs = new TaskCompletionSource<ImageSource>();
            _pendingRequests[requestId] = tcs;
            _processQueue.Enqueue(new IconProcessRequest
            {
                Id               = requestId,
                Source           = source,
                ProcessFunction  = processFunction,
                CompletionSource = tcs
            });
            StartProcessing();
            return tcs.Task;
        }

        public void CancelAllRequests()
        {
            lock (_lockObject)
            {
                while (_processQueue.TryDequeue(out var r))
                    r.CompletionSource.TrySetCanceled();
                foreach (var kvp in _pendingRequests)
                    kvp.Value.TrySetCanceled();
                _pendingRequests.Clear();
                _isProcessing = false;
            }
        }

        public int GetQueueLength()          => _processQueue.Count;
        public int GetPendingRequestCount()  => _pendingRequests.Count;

        private void StartProcessing()
        {
            lock (_lockObject)
            {
                if (_isProcessing) return;
                _isProcessing = true;
            }
            Task.Run(ProcessQueueAsync);
        }

        private async Task ProcessQueueAsync()
        {
            while (_processQueue.TryDequeue(out var request))
            {
                try
                {
                    var result = await request.ProcessFunction(request.Source);
                    request.CompletionSource.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    request.CompletionSource.TrySetException(ex);
                }
                finally
                {
                    _pendingRequests.TryRemove(request.Id, out _);
                }
                await Task.Delay(10);
            }
            lock (_lockObject)
            {
                _isProcessing = false;
                if (!_processQueue.IsEmpty) StartProcessing();
            }
        }
    }

    internal class IconProcessRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public Func<string, Task<ImageSource>> ProcessFunction { get; set; } = null!;
        public TaskCompletionSource<ImageSource> CompletionSource { get; set; } = null!;
    }
}
