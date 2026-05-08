using Ghost.DXC;
using Ghost.TestCore;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.UUID;

namespace Ghost.MicroTest;

internal unsafe class DXCBindingTest : ITest
{
    private static ReadOnlySpan<byte> ShaderCode => @"
struct VSInput
{
    float3 position : POSITION;
    float3 normal : NORMAL;
};

struct PSInput
{
    float4 position : SV_POSITION;
    float3 normal : NORMAL;
};

PSInput main(VSInput input)
{
    PSInput output;
    output.position = float4(input.position, 1.0);
    output.normal = input.normal;
    return output;
}"u8;

    private IDxcCompiler3* _compiler;
    private IDxcUtils* _utils;

    public void Setup()
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

        _compiler = pCompiler;
        _utils = pUtils;
    }

    private static List<string> GetCompilerArguments()
    {
        return new List<string>
        {
            "-T", "vs_6_6",   // Target profile (vs_6_6 for vertex shader)
            "-E", "main",     // Entry point
            "-HV", "2021",    // HLSL version 2021
            "-enable-16bit-types" // Enable 16-bit types
        };
    }

    public void Run()
    {
        IDxcIncludeHandler* includeHandler = default;
        IDxcBlobEncoding* sourceBlob = default;
        try
        {
            var hr = _utils->CreateDefaultIncludeHandler(&includeHandler);
            Assert.AreEqual(0, hr, $"Failed to create default include handler. HRESULT: 0x{hr:X8}");

            fixed (byte* pCode = ShaderCode)
            {
                hr = _utils->CreateBlobFromPinned(pCode, (uint)ShaderCode.Length, Api.DXC_CP_UTF8, &sourceBlob);
                Assert.AreEqual(0, hr, $"Failed to create blob from shader code. HRESULT: 0x{hr:X8}");
            }

            var argsArray = GetCompilerArguments();
            var argPtrs = stackalloc char*[argsArray.Count];
            for (var i = 0; i < argsArray.Count; i++)
            {
                argPtrs[i] = (char*)Marshal.StringToHGlobalUni(argsArray[i]);
            }

            IDxcResult* result = default;
            IDxcBlob* bytecodeBlob = default;

            try
            {
                // Compile shader
                var buffer = new DxcBuffer
                {
                    Ptr = sourceBlob->GetBufferPointer(),
                    Size = sourceBlob->GetBufferSize(),
                    Encoding = Api.DXC_CP_UTF8
                };

                hr = _compiler->Compile(&buffer, argPtrs, (uint)argsArray.Count, includeHandler, __uuidof(result), (void**)&result);
                Assert.AreEqual(0, hr, $"Failed to compile shader. HRESULT: 0x{hr:X8}");

                // Check compilation result
                int hrStatus;
                result->GetStatus(&hrStatus);
                if (hrStatus < 0)
                {
                    // Get error messages
                    IDxcBlobEncoding* pErrorBlob = default;
                    result->GetErrorBuffer(&pErrorBlob);

                    if (pErrorBlob != null)
                    {
                        var errorMessage = Marshal.PtrToStringUTF8((IntPtr)pErrorBlob->GetBufferPointer());
                        pErrorBlob->Release();

                        throw new InvalidOperationException($"DXC shader compilation failed:\n{errorMessage}");
                    }
                    else
                    {
                        throw new InvalidOperationException("DXC shader compilation failed with unknown error.");
                    }
                }

                // Get compiled bytecode
                hr = result->GetResult(&bytecodeBlob);
                Assert.AreEqual(0, hr, $"Failed to get compiled shader bytecode. HRESULT: 0x{hr:X8}");

                var bytecodeSize = bytecodeBlob->GetBufferSize();
                Assert.IsTrue(bytecodeSize > 0, "Compiled shader bytecode is empty.");
            }
            finally
            {
                if (result != null)
                {
                    result->Release();
                }

                if (bytecodeBlob != null)
                {
                    bytecodeBlob->Release();
                }

                for (var i = 0; i < argsArray.Count; i++)
                {
                    Marshal.FreeHGlobal((nint)argPtrs[i]);
                }
            }
        }
        finally
        {
            if (includeHandler != null)
            {
                includeHandler->Release();
            }

            if (sourceBlob != null)
            {
                sourceBlob->Release();
            }
        }
    }

    public void Cleanup()
    {
        _compiler->Release();
        _utils->Release();
    }
}
