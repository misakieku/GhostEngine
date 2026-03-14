using System.Runtime.InteropServices;

namespace Ghost.Ufbx;

public unsafe partial class LoadOpts
{
    public partial cstring ObjMtlPath
    {
        get => _objMtlPath;
        set
        {
            _objMtlPath.Dispose();
            _objMtlPath = new cstring(value);
            _ptr->obj_mtl_path = new ufbx_string
            {
                data = (sbyte*)_objMtlPath.ptr,
                length = (nuint)_objMtlPath.length,
            };
        }
    }

    public partial cstring Filename
    {
        get => _filename;
        set
        {
            _filename.Dispose();
            _filename = new cstring(value);
            _ptr->filename = new ufbx_string
            {
                data = (sbyte*)_filename.ptr,
                length = (nuint)_filename.length,
            };
        }
    }

    public partial void Dispose()
    {
        _objMtlPath.Dispose();
        _filename.Dispose();
        if (_csAlloc && _ptr != null)
        {
            NativeMemory.Free(_ptr);
            _ptr = null;
        }
    }
}
