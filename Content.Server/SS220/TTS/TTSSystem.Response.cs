using Content.Shared.SS220.TTS;
using System.Buffers;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public partial class TTSSystem
{
    private static class TTSResponseManager
    {
        private static readonly Stack<TTSResponse> ResponsePool = new();
        private static readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Shared;

        public static TTSResponse Rent()
        {
            if (!ResponsePool.TryPop(out var response))
            {
                response = new();
            }

            return response;
        }

        public static void Return(TTSResponse response)
        {
            FreeBuffer(response);
            ResponsePool.Push(response);
        }

        public static void AllocBuffer(TTSResponse response, int length)
        {
            response.Value = new(ArrayPool.Rent(length), length);
        }

        public static void FreeBuffer(TTSResponse response)
        {
            if (response.Value.Buffer.Length == 0)
                return;
            ArrayPool.Return(response.Value.Buffer);
            response.Value = new();
        }
    }

    private sealed class TTSResponse() : ReferenceCounter<TtsAudioData>(new())
    {
        public Task<bool>? Task;

        protected override void OnHandleDisposed()
        {
            base.OnHandleDisposed();
            if (ReferenceCount == 0)
                TTSResponseManager.Return(this);
        }

        public void Dereference()
        {
            OnHandleDisposed();
        }
    }
}
