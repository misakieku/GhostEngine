using Ghost.DXC;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Ghost.DXC.UUID;

namespace Ghost.Engine.RenderPipeline;

internal static unsafe class RuntimeShaderCompiler
{
    public static byte[] CompileShader(string hlslSource, string entryPoint, string profile)
    {
        IDxcCompiler3* pCompiler = default;
        IDxcUtils* pUtils = default;
        var hr = Api.DxcCreateInstance((Guid*)Unsafe.AsPointer(in Api.CLSID_DxcCompiler), __uuidof(pCompiler), (void**)&pCompiler);
        if (hr < 0)
        {
            throw new InvalidOperationException($"Failed to create DXC compiler instance. HRESULT: 0x{hr:X8}");
        }

        hr = Api.DxcCreateInstance((Guid*)Unsafe.AsPointer(in Api.CLSID_DxcUtils), __uuidof(pUtils), (void**)&pUtils);
        if (hr < 0)
        {
            pCompiler->Release();
            throw new InvalidOperationException($"Failed to create DXC utils instance. HRESULT: 0x{hr:X8}");
        }

        try
        {
            var sourceBytes = Encoding.UTF8.GetBytes(hlslSource);
            fixed (byte* pCode = sourceBytes)
            {
                IDxcBlobEncoding* sourceBlob = default;
                hr = pUtils->CreateBlobFromPinned(pCode, (uint)sourceBytes.Length, Api.DXC_CP_UTF8, &sourceBlob);
                if (hr < 0)
                {
                    throw new InvalidOperationException($"Failed to create blob from shader code. HRESULT: 0x{hr:X8}");
                }

                var args = new string[] { "-T", profile, "-E", entryPoint, "-HV", "2021", "-O3", "-Qstrip_debug" };
                var argPtrs = stackalloc char*[args.Length];
                for (var i = 0; i < args.Length; i++)
                {
                    argPtrs[i] = (char*)Marshal.StringToHGlobalUni(args[i]);
                }

                try
                {
                    var buffer = new DxcBuffer
                    {
                        Ptr = sourceBlob->GetBufferPointer(),
                        Size = sourceBlob->GetBufferSize(),
                        Encoding = Api.DXC_CP_UTF8
                    };

                    IDxcResult* result = default;
                    hr = pCompiler->Compile(&buffer, argPtrs, (uint)args.Length, null, __uuidof(result), (void**)&result);
                    if (hr < 0)
                    {
                        throw new InvalidOperationException($"DXC Compile call failed. HRESULT: 0x{hr:X8}");
                    }

                    try
                    {
                        int hrStatus;
                        result->GetStatus(&hrStatus);
                        if (hrStatus < 0)
                        {
                            IDxcBlobEncoding* pErrorBlob = default;
                            result->GetErrorBuffer(&pErrorBlob);
                            var errorMsg = pErrorBlob != null ? Marshal.PtrToStringUTF8((nint)pErrorBlob->GetBufferPointer()) : "Unknown error";
                            if (pErrorBlob != null) pErrorBlob->Release();
                            throw new InvalidOperationException($"DXC shader compilation failed:\n{errorMsg}");
                        }

                        IDxcBlob* bytecodeBlob = default;
                        hr = result->GetResult(&bytecodeBlob);
                        if (hr < 0)
                        {
                            throw new InvalidOperationException($"Failed to get shader bytecode. HRESULT: 0x{hr:X8}");
                        }

                        var bytecode = new byte[bytecodeBlob->GetBufferSize()];
                        Marshal.Copy((nint)bytecodeBlob->GetBufferPointer(), bytecode, 0, bytecode.Length);
                        bytecodeBlob->Release();
                        return bytecode;
                    }
                    finally
                    {
                        if (result != null)
                        {
                            result->Release();
                        }
                    }
                }
                finally
                {
                    if (sourceBlob != null)
                    {
                        sourceBlob->Release();
                    }

                    for (var i = 0; i < args.Length; i++)
                    {
                        Marshal.FreeHGlobal((nint)argPtrs[i]);
                    }
                }
            }
        }
        finally
        {
            pCompiler->Release();
            pUtils->Release();
        }
    }

    public static byte[] CompileComputeShader(string hlslSource, string entryPoint = "CSMain")
    {
        return CompileShader(hlslSource, entryPoint, "cs_6_6");
    }
}
