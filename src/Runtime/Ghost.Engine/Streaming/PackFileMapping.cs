using Ghost.Core;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

public sealed unsafe class PackFileMapping : IDisposable
{
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private byte* _basePointer;
    private readonly long _length;

    public byte* BasePointer => _basePointer;
    public long Length => _length;

    public PackFileMapping(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Pack file not found: {filePath}", filePath);
        }

        var length = fileInfo.Length;
        if (length < PackFileHeader.SIZE)
        {
            throw new InvalidDataException($"Pack file '{filePath}' is too small to contain a valid header ({length} bytes).");
        }

        _length = length;
        _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        _accessor = _mmf.CreateViewAccessor(0, length, MemoryMappedFileAccess.Read);
        _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _basePointer);

        ref readonly var header = ref Unsafe.AsRef<PackFileHeader>(_basePointer);
        if (header.magic != PackFileHeader.MAGIC || header.version != PackFileHeader.VERSION)
        {
            Dispose();
            throw new InvalidDataException($"Pack file '{filePath}' has an invalid header (magic 0x{header.magic:X8}, version {header.version}).");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* GetPointer(long offset)
    {
        Logger.DebugAssert(_basePointer != null, "Pack file mapping has been disposed.");
        Logger.DebugAssert(offset >= 0 && offset <= _length, "Offset is out of bounds.");
        return _basePointer + offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetSpan(long offset, int length)
    {
        Logger.DebugAssert(_basePointer != null, "Pack file mapping has been disposed.");
        Logger.DebugAssert(offset >= 0 && length >= 0 && offset + length <= _length, "Range is out of bounds.");
        return new ReadOnlySpan<byte>(_basePointer + offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Read(long offset, Span<byte> destination)
    {
        Logger.DebugAssert(_basePointer != null, "Pack file mapping has been disposed.");
        Logger.DebugAssert(offset >= 0 && offset + destination.Length <= _length, "Range is out of bounds.");
        new ReadOnlySpan<byte>(_basePointer + offset, destination.Length).CopyTo(destination);
    }

    public void Dispose()
    {
        if (_basePointer != null)
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _basePointer = null;
        }

        _accessor?.Dispose();
        _mmf?.Dispose();
        GC.SuppressFinalize(this);
    }
}
