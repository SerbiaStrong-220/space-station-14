// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Content.Server.SS220.Investigation;

public sealed partial class InvestigationRecorder
{
    private sealed class JsonlStream
    {
        private readonly StreamWriterThread _thread;
        private readonly StreamWriter _writer;
        private List<string> _buffer = new();

        public JsonlStream(StreamWriterThread thread, StreamWriter writer)
        {
            _thread = thread;
            _writer = writer;
        }

        public void Write(string row) => _buffer.Add(row);

        public bool Flush()
        {
            if (_buffer.Count == 0)
                return true;

            var batch = _buffer;
            _buffer = new List<string>();
            return _thread.Enqueue(_writer, batch);
        }

        public void Close() => _thread.EnqueueClose(_writer);
    }

    /// <remarks>Bounded queue: if the disk cannot keep up, batches are dropped and counted rather than growing the heap.</remarks>
    private sealed class StreamWriterThread
    {
        private const int QueueCapacity = 64;

        private static readonly TimeSpan CloseEnqueueTimeout = TimeSpan.FromSeconds(2);

        private readonly BlockingCollection<Action> _queue = new(QueueCapacity);
        private readonly Thread _thread;
        private readonly ISawmill _sawmill;

        private int _droppedBatches;

        private volatile string? _failure;

        public StreamWriterThread(ISawmill sawmill)
        {
            _sawmill = sawmill;
            _thread = new Thread(Run)
            {
                Name = "InvestigationWriter",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
            };
            _thread.Start();
        }

        public string? Failure => _failure;

        public int DroppedBatches => _droppedBatches;

        public bool Enqueue(StreamWriter writer, List<string> batch)
        {
            return Post(() =>
            {
                foreach (var row in batch)
                {
                    writer.Write(row);
                    writer.Write('\n');
                }

                writer.Flush();
            });
        }

        public void EnqueueClose(StreamWriter writer)
        {
            // Not routed through Post: a dropped close costs the gzip trailer.
            if (_queue.IsAddingCompleted || !_queue.TryAdd(() => writer.Dispose(), CloseEnqueueTimeout))
                _sawmill.Error("Could not queue an investigation stream close; that file will have no gzip trailer.");
        }

        private bool Post(Action work)
        {
            if (_queue.IsAddingCompleted || !_queue.TryAdd(work))
            {
                _droppedBatches++;
                return false;
            }

            return true;
        }

        public void Stop(TimeSpan timeout)
        {
            _queue.CompleteAdding();

            if (!_thread.Join(timeout))
                _sawmill.Error($"Investigation writer thread did not finish within {timeout.TotalSeconds:0}s; bundle may be truncated.");
        }

        private void Run()
        {
            try
            {
                foreach (var work in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        work();
                    }
                    catch (Exception e)
                    {
                        _failure ??= e.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                _failure ??= e.ToString();
            }
        }
    }

}
