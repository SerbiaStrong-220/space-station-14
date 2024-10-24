using System.Text;

namespace Content.Shared.SS220.Utility;

/// <summary>
/// Provides mutable string functionality to achieve zero memory re-allocations. Not the safest thing to work with, use under supervision!
/// </summary>
public struct StringBuffer
{
    public readonly ReadOnlyMemory<char> Memory => _buffer?.AsMemory().Slice(0, _length) ?? new ReadOnlyMemory<char>();
    private const int DefaultStartCapacity = 8;

    // Its not allowed to use [ThreadStatic], so... dont use this in multi-theaded scenario, I guess 
    private static StringBuilder? _stringBuilder;

    private readonly int? _startCapacityOverride;
    private char[]? _buffer;
    private int _length;

    public StringBuffer(int capacity)
    {
        _startCapacityOverride = capacity;
    }

    // THE SAFEST API EVER!
    /// <summary>
    /// Clears underlying memory and provides reference to <see cref="StringBuilder"/> for formatting.
    /// You should call <see cref="EndFormat"/> after you done and not use provided <see cref="StringBuilder"/> anymore.
    /// </summary>
    public StringBuilder BeginFormat()
    {
        _stringBuilder ??= new();
        _stringBuilder.Clear();
        return _stringBuilder;
    }

    /// <summary>
    /// Call this to write formatted string to buffer.
    /// </summary>
    public void EndFormat()
    {
        if (_stringBuilder is null)
            return;
        FlushBuilder();
        _stringBuilder.Clear();
    }

    public static implicit operator ReadOnlyMemory<char>(StringBuffer buffer) => buffer.Memory;

    private void FlushBuilder()
    {
        if (_stringBuilder is null)
            return;
        if (_buffer is null || _buffer.Length < _stringBuilder.Length)
        {
            var currentLength = _buffer?.Length;
            var newLength = currentLength ?? _startCapacityOverride ?? DefaultStartCapacity;
            while (newLength < _stringBuilder.Length)
            {
                newLength = MathHelper.NextPowerOfTwo(newLength);
            }
            _buffer = new char[newLength];
        }
        _length = _stringBuilder.Length;
        _stringBuilder.CopyTo(0, _buffer, _length);
    }
}
