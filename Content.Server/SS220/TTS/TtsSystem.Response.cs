using Content.Shared.SS220.TTS;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public partial class TtsSystem
{
    private static class TtsResponseManager
    {
        private static readonly ConcurrentStack<TtsResponse> ResponsePool = new();
        private static readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Shared;

        public static TtsResponse Rent()
        {
            if (!ResponsePool.TryPop(out var response))
                response = new();

            return response;
        }

        public static void Return(TtsResponse response)
        {
            FreeBuffer(response);
            ResponsePool.Push(response);
        }

        public static void AllocBuffer(TtsResponse response, int length)
        {
            response.Value = new(ArrayPool.Rent(length), length);
        }

        public static void FreeBuffer(TtsResponse response)
        {
            if (response.Value.Buffer.Length == 0)
                return;

            ArrayPool.Return(response.Value.Buffer);
            response.Value = new();
        }
    }

    private sealed class TtsResponse() : ReferenceCounter<TtsAudioBufferData>(new())
    {
        public Task<bool>? Task;

        protected override void OnAllReferencesDisposed()
        {
            base.OnAllReferencesDisposed();
            TtsResponseManager.Return(this);
        }

        public void Dereference()
        {
            OnReferenceDisposed();
        }
    }
}
