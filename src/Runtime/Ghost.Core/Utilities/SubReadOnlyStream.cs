using System.Diagnostics;

namespace Ghost.Core.Utilities;

internal sealed class SubReadOnlyStream : Stream
{
    private readonly Stream _superStream;
    private readonly long _startInSuperStream;
    private readonly long _endInSuperStream;
    private readonly bool _canSeek;
    private readonly bool _leaveOpen;

    private long _positionInSuperStream;
    private bool _isDisposed;

    public SubReadOnlyStream(Stream superStream, long maxLength, bool leaveOpen = false)
    {
        _startInSuperStream = -1;
        _positionInSuperStream = 0;
        _endInSuperStream = maxLength;
        _superStream = superStream;
        _canSeek = false;
        _leaveOpen = leaveOpen;
        _isDisposed = false;
    }

    public SubReadOnlyStream(Stream superStream, long startPosition, long maxLength, bool leaveOpen = false)
    {
        _startInSuperStream = startPosition;
        _positionInSuperStream = startPosition;
        _endInSuperStream = startPosition + maxLength;
        _superStream = superStream;
        _canSeek = superStream.CanSeek;
        _leaveOpen = leaveOpen;
        _isDisposed = false;
    }

    public override long Length
    {
        get
        {
            ThrowIfDisposed();

            if (!_canSeek)
            {
                throw new NotSupportedException();
            }

            return _endInSuperStream - _startInSuperStream;
        }
    }

    public override long Position
    {
        get
        {
            ThrowIfDisposed();

            if (!_canSeek)
            {
                throw new NotSupportedException();
            }

            return _positionInSuperStream - _startInSuperStream;
        }
        set
        {
            ThrowIfDisposed();

            throw new NotSupportedException();
        }
    }

    public override bool CanRead => _superStream.CanRead && !_isDisposed;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_isDisposed, this);

    void ThrowIfCantRead()
    {
        if (!CanRead)
        {
            throw new NotSupportedException();
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        // parameter validation sent to _superStream.Read
        var origCount = count;

        ThrowIfDisposed();
        ThrowIfCantRead();

        if (_canSeek && _superStream.Position != _positionInSuperStream)
        {
            _superStream.Seek(_positionInSuperStream, SeekOrigin.Begin);
        }

        if (_positionInSuperStream > _endInSuperStream - count)
        {
            count = (int)(_endInSuperStream - _positionInSuperStream);
        }

        Debug.Assert(count >= 0);
        Debug.Assert(count <= origCount);

        var ret = _superStream.Read(buffer, offset, count);

        _positionInSuperStream += ret;
        return ret;
    }

    public override int Read(Span<byte> destination)
    {
        // parameter validation sent to _superStream.Read
        var origCount = destination.Length;
        var count = destination.Length;

        ThrowIfDisposed();
        ThrowIfCantRead();

        if (_canSeek && _superStream.Position != _positionInSuperStream)
        {
            _superStream.Seek(_positionInSuperStream, SeekOrigin.Begin);
        }

        if (_positionInSuperStream + count > _endInSuperStream)
        {
            count = (int)(_endInSuperStream - _positionInSuperStream);
        }

        Debug.Assert(count >= 0);
        Debug.Assert(count <= origCount);

        var ret = _superStream.Read(destination.Slice(0, count));

        _positionInSuperStream += ret;
        return ret;
    }

    public override int ReadByte()
    {
        Span<byte> b = stackalloc byte[1];
        return Read(b) == 1 ? b[0] : -1;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);
        return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfCantRead();
        if (_canSeek && _superStream.Position != _positionInSuperStream)
        {
            _superStream.Seek(_positionInSuperStream, SeekOrigin.Begin);
        }

        if (_positionInSuperStream > _endInSuperStream - buffer.Length)
        {
            buffer = buffer.Slice(0, (int)(_endInSuperStream - _positionInSuperStream));
        }

        return Core(buffer, cancellationToken);

        async ValueTask<int> Core(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var ret = await _superStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            _positionInSuperStream += ret;
            return ret;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        throw new NotSupportedException();
    }

    // Close the stream for reading.  Note that this does NOT close the superStream (since
    // the substream is just 'a chunk' of the super-stream
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            if (!_leaveOpen)
            {
                _superStream.Dispose();
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}