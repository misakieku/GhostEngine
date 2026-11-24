using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("0742A90B-C387-483F-B946-30A7E4E61458")]
[NativeTypeName("struct ID3D12InfoQueue : IUnknown")]
public unsafe partial struct ID3D12InfoQueue : ID3D12InfoQueue.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12InfoQueue);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, uint>)(lpVtbl[1]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, uint>)(lpVtbl[2]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetMessageCountLimit([NativeTypeName("UINT64")] ulong MessageCountLimit)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, ulong, int>)(lpVtbl[3]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), MessageCountLimit);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void ClearStoredMessages()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, void>)(lpVtbl[4]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetMessageA([NativeTypeName("UINT64")] ulong MessageIndex, D3D12_MESSAGE* pMessage, [NativeTypeName("SIZE_T *")] nuint* pMessageByteLength)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, ulong, D3D12_MESSAGE*, nuint*, int>)(lpVtbl[5]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), MessageIndex, pMessage, pMessageByteLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("UINT64")]
    public ulong GetNumMessagesAllowedByStorageFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, ulong>)(lpVtbl[6]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("UINT64")]
    public ulong GetNumMessagesDeniedByStorageFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, ulong>)(lpVtbl[7]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("UINT64")]
    public ulong GetNumStoredMessages()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, ulong>)(lpVtbl[8]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("UINT64")]
    public ulong GetNumStoredMessagesAllowedByRetrievalFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, ulong>)(lpVtbl[9]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("UINT64")]
    public ulong GetNumMessagesDiscardedByMessageCountLimit()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, ulong>)(lpVtbl[10]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("UINT64")]
    public ulong GetMessageCountLimit()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, ulong>)(lpVtbl[11]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int AddStorageFilterEntries(D3D12_INFO_QUEUE_FILTER* pFilter)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_INFO_QUEUE_FILTER*, int>)(lpVtbl[12]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), pFilter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("HRESULT")]
    public int GetStorageFilter(D3D12_INFO_QUEUE_FILTER* pFilter, [NativeTypeName("SIZE_T *")] nuint* pFilterByteLength)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_INFO_QUEUE_FILTER*, nuint*, int>)(lpVtbl[13]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), pFilter, pFilterByteLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    public void ClearStorageFilter()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, void>)(lpVtbl[14]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int PushEmptyStorageFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, int>)(lpVtbl[15]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    [return: NativeTypeName("HRESULT")]
    public int PushCopyOfStorageFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, int>)(lpVtbl[16]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    [return: NativeTypeName("HRESULT")]
    public int PushStorageFilter(D3D12_INFO_QUEUE_FILTER* pFilter)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_INFO_QUEUE_FILTER*, int>)(lpVtbl[17]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), pFilter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    public void PopStorageFilter()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, void>)(lpVtbl[18]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    public uint GetStorageFilterStackSize()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, uint>)(lpVtbl[19]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    [return: NativeTypeName("HRESULT")]
    public int AddRetrievalFilterEntries(D3D12_INFO_QUEUE_FILTER* pFilter)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_INFO_QUEUE_FILTER*, int>)(lpVtbl[20]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), pFilter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    [return: NativeTypeName("HRESULT")]
    public int GetRetrievalFilter(D3D12_INFO_QUEUE_FILTER* pFilter, [NativeTypeName("SIZE_T *")] nuint* pFilterByteLength)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_INFO_QUEUE_FILTER*, nuint*, int>)(lpVtbl[21]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), pFilter, pFilterByteLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    public void ClearRetrievalFilter()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, void>)(lpVtbl[22]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(23)]
    [return: NativeTypeName("HRESULT")]
    public int PushEmptyRetrievalFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, int>)(lpVtbl[23]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(24)]
    [return: NativeTypeName("HRESULT")]
    public int PushCopyOfRetrievalFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, int>)(lpVtbl[24]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(25)]
    [return: NativeTypeName("HRESULT")]
    public int PushRetrievalFilter(D3D12_INFO_QUEUE_FILTER* pFilter)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_INFO_QUEUE_FILTER*, int>)(lpVtbl[25]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), pFilter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(26)]
    public void PopRetrievalFilter()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, void>)(lpVtbl[26]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(27)]
    public uint GetRetrievalFilterStackSize()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, uint>)(lpVtbl[27]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(28)]
    [return: NativeTypeName("HRESULT")]
    public int AddMessage(D3D12_MESSAGE_CATEGORY Category, D3D12_MESSAGE_SEVERITY Severity, D3D12_MESSAGE_ID ID, [NativeTypeName("LPCSTR")] sbyte* pDescription)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_MESSAGE_CATEGORY, D3D12_MESSAGE_SEVERITY, D3D12_MESSAGE_ID, sbyte*, int>)(lpVtbl[28]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), Category, Severity, ID, pDescription);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(29)]
    [return: NativeTypeName("HRESULT")]
    public int AddApplicationMessage(D3D12_MESSAGE_SEVERITY Severity, [NativeTypeName("LPCSTR")] sbyte* pDescription)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_MESSAGE_SEVERITY, sbyte*, int>)(lpVtbl[29]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), Severity, pDescription);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(30)]
    [return: NativeTypeName("HRESULT")]
    public int SetBreakOnCategory(D3D12_MESSAGE_CATEGORY Category, [NativeTypeName("BOOL")] int bEnable)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_MESSAGE_CATEGORY, int, int>)(lpVtbl[30]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), Category, bEnable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(31)]
    [return: NativeTypeName("HRESULT")]
    public int SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY Severity, [NativeTypeName("BOOL")] int bEnable)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_MESSAGE_SEVERITY, int, int>)(lpVtbl[31]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), Severity, bEnable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(32)]
    [return: NativeTypeName("HRESULT")]
    public int SetBreakOnID(D3D12_MESSAGE_ID ID, [NativeTypeName("BOOL")] int bEnable)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_MESSAGE_ID, int, int>)(lpVtbl[32]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), ID, bEnable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(33)]
    [return: NativeTypeName("BOOL")]
    public int GetBreakOnCategory(D3D12_MESSAGE_CATEGORY Category)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_MESSAGE_CATEGORY, int>)(lpVtbl[33]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), Category);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(34)]
    [return: NativeTypeName("BOOL")]
    public int GetBreakOnSeverity(D3D12_MESSAGE_SEVERITY Severity)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_MESSAGE_SEVERITY, int>)(lpVtbl[34]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), Severity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(35)]
    [return: NativeTypeName("BOOL")]
    public int GetBreakOnID(D3D12_MESSAGE_ID ID)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, D3D12_MESSAGE_ID, int>)(lpVtbl[35]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), ID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(36)]
    public void SetMuteDebugOutput([NativeTypeName("BOOL")] int bMute)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, int, void>)(lpVtbl[36]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this), bMute);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(37)]
    [return: NativeTypeName("BOOL")]
    public int GetMuteDebugOutput()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue*, int>)(lpVtbl[37]))((ID3D12InfoQueue*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int SetMessageCountLimit([NativeTypeName("UINT64")] ulong MessageCountLimit);

        [VtblIndex(4)]
        void ClearStoredMessages();

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetMessageA([NativeTypeName("UINT64")] ulong MessageIndex, D3D12_MESSAGE* pMessage, [NativeTypeName("SIZE_T *")] nuint* pMessageByteLength);

        [VtblIndex(6)]
        [return: NativeTypeName("UINT64")]
        ulong GetNumMessagesAllowedByStorageFilter();

        [VtblIndex(7)]
        [return: NativeTypeName("UINT64")]
        ulong GetNumMessagesDeniedByStorageFilter();

        [VtblIndex(8)]
        [return: NativeTypeName("UINT64")]
        ulong GetNumStoredMessages();

        [VtblIndex(9)]
        [return: NativeTypeName("UINT64")]
        ulong GetNumStoredMessagesAllowedByRetrievalFilter();

        [VtblIndex(10)]
        [return: NativeTypeName("UINT64")]
        ulong GetNumMessagesDiscardedByMessageCountLimit();

        [VtblIndex(11)]
        [return: NativeTypeName("UINT64")]
        ulong GetMessageCountLimit();

        [VtblIndex(12)]
        [return: NativeTypeName("HRESULT")]
        int AddStorageFilterEntries(D3D12_INFO_QUEUE_FILTER* pFilter);

        [VtblIndex(13)]
        [return: NativeTypeName("HRESULT")]
        int GetStorageFilter(D3D12_INFO_QUEUE_FILTER* pFilter, [NativeTypeName("SIZE_T *")] nuint* pFilterByteLength);

        [VtblIndex(14)]
        void ClearStorageFilter();

        [VtblIndex(15)]
        [return: NativeTypeName("HRESULT")]
        int PushEmptyStorageFilter();

        [VtblIndex(16)]
        [return: NativeTypeName("HRESULT")]
        int PushCopyOfStorageFilter();

        [VtblIndex(17)]
        [return: NativeTypeName("HRESULT")]
        int PushStorageFilter(D3D12_INFO_QUEUE_FILTER* pFilter);

        [VtblIndex(18)]
        void PopStorageFilter();

        [VtblIndex(19)]
        uint GetStorageFilterStackSize();

        [VtblIndex(20)]
        [return: NativeTypeName("HRESULT")]
        int AddRetrievalFilterEntries(D3D12_INFO_QUEUE_FILTER* pFilter);

        [VtblIndex(21)]
        [return: NativeTypeName("HRESULT")]
        int GetRetrievalFilter(D3D12_INFO_QUEUE_FILTER* pFilter, [NativeTypeName("SIZE_T *")] nuint* pFilterByteLength);

        [VtblIndex(22)]
        void ClearRetrievalFilter();

        [VtblIndex(23)]
        [return: NativeTypeName("HRESULT")]
        int PushEmptyRetrievalFilter();

        [VtblIndex(24)]
        [return: NativeTypeName("HRESULT")]
        int PushCopyOfRetrievalFilter();

        [VtblIndex(25)]
        [return: NativeTypeName("HRESULT")]
        int PushRetrievalFilter(D3D12_INFO_QUEUE_FILTER* pFilter);

        [VtblIndex(26)]
        void PopRetrievalFilter();

        [VtblIndex(27)]
        uint GetRetrievalFilterStackSize();

        [VtblIndex(28)]
        [return: NativeTypeName("HRESULT")]
        int AddMessage(D3D12_MESSAGE_CATEGORY Category, D3D12_MESSAGE_SEVERITY Severity, D3D12_MESSAGE_ID ID, [NativeTypeName("LPCSTR")] sbyte* pDescription);

        [VtblIndex(29)]
        [return: NativeTypeName("HRESULT")]
        int AddApplicationMessage(D3D12_MESSAGE_SEVERITY Severity, [NativeTypeName("LPCSTR")] sbyte* pDescription);

        [VtblIndex(30)]
        [return: NativeTypeName("HRESULT")]
        int SetBreakOnCategory(D3D12_MESSAGE_CATEGORY Category, [NativeTypeName("BOOL")] int bEnable);

        [VtblIndex(31)]
        [return: NativeTypeName("HRESULT")]
        int SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY Severity, [NativeTypeName("BOOL")] int bEnable);

        [VtblIndex(32)]
        [return: NativeTypeName("HRESULT")]
        int SetBreakOnID(D3D12_MESSAGE_ID ID, [NativeTypeName("BOOL")] int bEnable);

        [VtblIndex(33)]
        [return: NativeTypeName("BOOL")]
        int GetBreakOnCategory(D3D12_MESSAGE_CATEGORY Category);

        [VtblIndex(34)]
        [return: NativeTypeName("BOOL")]
        int GetBreakOnSeverity(D3D12_MESSAGE_SEVERITY Severity);

        [VtblIndex(35)]
        [return: NativeTypeName("BOOL")]
        int GetBreakOnID(D3D12_MESSAGE_ID ID);

        [VtblIndex(36)]
        void SetMuteDebugOutput([NativeTypeName("BOOL")] int bMute);

        [VtblIndex(37)]
        [return: NativeTypeName("BOOL")]
        int GetMuteDebugOutput();
    }
}
