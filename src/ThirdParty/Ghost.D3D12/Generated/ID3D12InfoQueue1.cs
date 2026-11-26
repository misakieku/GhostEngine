using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("2852DD88-B484-4C0C-B6B1-67168500E600")]
[NativeTypeName("struct ID3D12InfoQueue1 : ID3D12InfoQueue")]
public unsafe partial struct ID3D12InfoQueue1 : ID3D12InfoQueue1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12InfoQueue1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, uint>)(lpVtbl[1]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, uint>)(lpVtbl[2]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT SetMessageCountLimit([NativeTypeName("UINT64")] ulong MessageCountLimit)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, ulong, int>)(lpVtbl[3]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), MessageCountLimit);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void ClearStoredMessages()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, void>)(lpVtbl[4]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT GetMessageA([NativeTypeName("UINT64")] ulong MessageIndex, D3D12_MESSAGE* pMessage, [NativeTypeName("SIZE_T *")] nuint* pMessageByteLength)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, ulong, D3D12_MESSAGE*, nuint*, int>)(lpVtbl[5]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), MessageIndex, pMessage, pMessageByteLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("UINT64")]
    public ulong GetNumMessagesAllowedByStorageFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, ulong>)(lpVtbl[6]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("UINT64")]
    public ulong GetNumMessagesDeniedByStorageFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, ulong>)(lpVtbl[7]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("UINT64")]
    public ulong GetNumStoredMessages()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, ulong>)(lpVtbl[8]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("UINT64")]
    public ulong GetNumStoredMessagesAllowedByRetrievalFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, ulong>)(lpVtbl[9]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("UINT64")]
    public ulong GetNumMessagesDiscardedByMessageCountLimit()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, ulong>)(lpVtbl[10]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("UINT64")]
    public ulong GetMessageCountLimit()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, ulong>)(lpVtbl[11]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    public HRESULT AddStorageFilterEntries(D3D12_INFO_QUEUE_FILTER* pFilter)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_INFO_QUEUE_FILTER*, int>)(lpVtbl[12]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), pFilter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    public HRESULT GetStorageFilter(D3D12_INFO_QUEUE_FILTER* pFilter, [NativeTypeName("SIZE_T *")] nuint* pFilterByteLength)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_INFO_QUEUE_FILTER*, nuint*, int>)(lpVtbl[13]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), pFilter, pFilterByteLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    public void ClearStorageFilter()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, void>)(lpVtbl[14]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    public HRESULT PushEmptyStorageFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, int>)(lpVtbl[15]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    public HRESULT PushCopyOfStorageFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, int>)(lpVtbl[16]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    public HRESULT PushStorageFilter(D3D12_INFO_QUEUE_FILTER* pFilter)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_INFO_QUEUE_FILTER*, int>)(lpVtbl[17]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), pFilter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    public void PopStorageFilter()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, void>)(lpVtbl[18]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    public uint GetStorageFilterStackSize()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, uint>)(lpVtbl[19]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    public HRESULT AddRetrievalFilterEntries(D3D12_INFO_QUEUE_FILTER* pFilter)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_INFO_QUEUE_FILTER*, int>)(lpVtbl[20]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), pFilter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    public HRESULT GetRetrievalFilter(D3D12_INFO_QUEUE_FILTER* pFilter, [NativeTypeName("SIZE_T *")] nuint* pFilterByteLength)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_INFO_QUEUE_FILTER*, nuint*, int>)(lpVtbl[21]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), pFilter, pFilterByteLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    public void ClearRetrievalFilter()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, void>)(lpVtbl[22]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(23)]
    public HRESULT PushEmptyRetrievalFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, int>)(lpVtbl[23]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(24)]
    public HRESULT PushCopyOfRetrievalFilter()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, int>)(lpVtbl[24]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(25)]
    public HRESULT PushRetrievalFilter(D3D12_INFO_QUEUE_FILTER* pFilter)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_INFO_QUEUE_FILTER*, int>)(lpVtbl[25]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), pFilter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(26)]
    public void PopRetrievalFilter()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, void>)(lpVtbl[26]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(27)]
    public uint GetRetrievalFilterStackSize()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, uint>)(lpVtbl[27]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(28)]
    public HRESULT AddMessage(D3D12_MESSAGE_CATEGORY Category, D3D12_MESSAGE_SEVERITY Severity, D3D12_MESSAGE_ID ID, [NativeTypeName("LPCSTR")] sbyte* pDescription)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_MESSAGE_CATEGORY, D3D12_MESSAGE_SEVERITY, D3D12_MESSAGE_ID, sbyte*, int>)(lpVtbl[28]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), Category, Severity, ID, pDescription);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(29)]
    public HRESULT AddApplicationMessage(D3D12_MESSAGE_SEVERITY Severity, [NativeTypeName("LPCSTR")] sbyte* pDescription)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_MESSAGE_SEVERITY, sbyte*, int>)(lpVtbl[29]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), Severity, pDescription);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(30)]
    public HRESULT SetBreakOnCategory(D3D12_MESSAGE_CATEGORY Category, [NativeTypeName("BOOL")] int bEnable)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_MESSAGE_CATEGORY, int, int>)(lpVtbl[30]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), Category, bEnable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(31)]
    public HRESULT SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY Severity, [NativeTypeName("BOOL")] int bEnable)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_MESSAGE_SEVERITY, int, int>)(lpVtbl[31]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), Severity, bEnable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(32)]
    public HRESULT SetBreakOnID(D3D12_MESSAGE_ID ID, [NativeTypeName("BOOL")] int bEnable)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_MESSAGE_ID, int, int>)(lpVtbl[32]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), ID, bEnable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(33)]
    [return: NativeTypeName("BOOL")]
    public int GetBreakOnCategory(D3D12_MESSAGE_CATEGORY Category)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_MESSAGE_CATEGORY, int>)(lpVtbl[33]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), Category);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(34)]
    [return: NativeTypeName("BOOL")]
    public int GetBreakOnSeverity(D3D12_MESSAGE_SEVERITY Severity)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_MESSAGE_SEVERITY, int>)(lpVtbl[34]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), Severity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(35)]
    [return: NativeTypeName("BOOL")]
    public int GetBreakOnID(D3D12_MESSAGE_ID ID)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, D3D12_MESSAGE_ID, int>)(lpVtbl[35]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), ID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(36)]
    public void SetMuteDebugOutput([NativeTypeName("BOOL")] int bMute)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, int, void>)(lpVtbl[36]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), bMute);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(37)]
    [return: NativeTypeName("BOOL")]
    public int GetMuteDebugOutput()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, int>)(lpVtbl[37]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(38)]
    public HRESULT RegisterMessageCallback([NativeTypeName("D3D12MessageFunc")] delegate* unmanaged[Stdcall]<D3D12_MESSAGE_CATEGORY, D3D12_MESSAGE_SEVERITY, D3D12_MESSAGE_ID, sbyte*, void*, void> CallbackFunc, D3D12_MESSAGE_CALLBACK_FLAGS CallbackFilterFlags, void* pContext, [NativeTypeName("DWORD *")] uint* pCallbackCookie)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, delegate* unmanaged[Stdcall]<D3D12_MESSAGE_CATEGORY, D3D12_MESSAGE_SEVERITY, D3D12_MESSAGE_ID, sbyte*, void*, void>, D3D12_MESSAGE_CALLBACK_FLAGS, void*, uint*, int>)(lpVtbl[38]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), CallbackFunc, CallbackFilterFlags, pContext, pCallbackCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(39)]
    public HRESULT UnregisterMessageCallback([NativeTypeName("DWORD")] uint CallbackCookie)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12InfoQueue1*, uint, int>)(lpVtbl[39]))((ID3D12InfoQueue1*)Unsafe.AsPointer(ref this), CallbackCookie);
    }

    public interface Interface : ID3D12InfoQueue.Interface
    {
        [VtblIndex(38)]
        HRESULT RegisterMessageCallback([NativeTypeName("D3D12MessageFunc")] delegate* unmanaged[Stdcall]<D3D12_MESSAGE_CATEGORY, D3D12_MESSAGE_SEVERITY, D3D12_MESSAGE_ID, sbyte*, void*, void> CallbackFunc, D3D12_MESSAGE_CALLBACK_FLAGS CallbackFilterFlags, void* pContext, [NativeTypeName("DWORD *")] uint* pCallbackCookie);

        [VtblIndex(39)]
        HRESULT UnregisterMessageCallback([NativeTypeName("DWORD")] uint CallbackCookie);
    }
}
