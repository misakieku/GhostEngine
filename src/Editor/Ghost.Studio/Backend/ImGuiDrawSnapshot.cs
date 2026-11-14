using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Hexa.NET.ImGui;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Studio.Backend;

internal struct ImGuiDrawCommandRecord
{
    public uint vertexByteOffset;
    public uint indexStartOffset;
    public uint elemCount;
    public uint textureId;
    public float4 clipRect;
}

internal struct ImGuiTextureSyncOp
{
    public uint textureKey;
    public uint width;
    public uint height;
    public uint pixelDataOffset;
    public uint pixelDataSize;
    public byte isDestroy;
}

internal sealed unsafe class ImGuiDrawSnapshot : IDisposable
{
    private UnsafeList<byte> _vertexBytes;
    private UnsafeList<ushort> _indices;
    private UnsafeList<ImGuiDrawCommandRecord> _commands;
    private UnsafeList<byte> _texturePixelBytes;
    private UnsafeList<ImGuiTextureSyncOp> _textureOps;

    public float2 DisplayPos { get; private set; }
    public float2 DisplaySize { get; private set; }
    public int TotalVertexCount { get; private set; }
    public int TotalIndexCount { get; private set; }
    public Handle<GPUTexture> ExternalTexture { get; private set; }
    public bool HasData => _commands.Count > 0 && _vertexBytes.Count > 0 && _indices.Count > 0;

    public ReadOnlySpan<byte> VertexBytes => _vertexBytes.AsSpan();
    public ReadOnlySpan<ushort> Indices => _indices.AsSpan();
    public ReadOnlySpan<ImGuiDrawCommandRecord> Commands => _commands.AsSpan();
    public ReadOnlySpan<byte> TexturePixelBytes => _texturePixelBytes.AsSpan();
    public ReadOnlySpan<ImGuiTextureSyncOp> TextureOps => _textureOps.AsSpan();

    public ImGuiDrawSnapshot()
    {
        _vertexBytes = new UnsafeList<byte>(4096, AllocationHandle.Persistent);
        _indices = new UnsafeList<ushort>(4096, AllocationHandle.Persistent);
        _commands = new UnsafeList<ImGuiDrawCommandRecord>(128, AllocationHandle.Persistent);
        _texturePixelBytes = new UnsafeList<byte>(1024, AllocationHandle.Persistent);
        _textureOps = new UnsafeList<ImGuiTextureSyncOp>(4, AllocationHandle.Persistent);
    }

    public void Capture(ImDrawDataPtr drawData, Handle<GPUTexture> externalTexture = default)
    {
        ExternalTexture = externalTexture;
        _vertexBytes.Clear();
        _indices.Clear();
        _commands.Clear();
        _texturePixelBytes.Clear();
        _textureOps.Clear();

        CaptureTextureUpdates(drawData);

        if (!drawData.Valid || drawData.TotalVtxCount <= 0 || drawData.TotalIdxCount <= 0)
        {
            TotalVertexCount = 0;
            TotalIndexCount = 0;
            DisplayPos = float2.zero;
            DisplaySize = float2.zero;
            return;
        }

        DisplayPos = new float2(drawData.DisplayPos.X, drawData.DisplayPos.Y);
        DisplaySize = new float2(drawData.DisplaySize.X, drawData.DisplaySize.Y);
        TotalVertexCount = drawData.TotalVtxCount;
        TotalIndexCount = drawData.TotalIdxCount;

        var totalVertexBytes = TotalVertexCount * sizeof(ImDrawVert);
        _vertexBytes.Resize(totalVertexBytes);
        _vertexBytes.UnsafeSetCount(totalVertexBytes);

        _indices.Resize(TotalIndexCount);
        _indices.UnsafeSetCount(TotalIndexCount);

        var pDstVtx = (byte*)_vertexBytes.GetUnsafePtr();
        var pDstIdx = (ushort*)_indices.GetUnsafePtr();

        var vtxWriteCount = 0;
        var idxWriteCount = 0;

        for (var listIdx = 0; listIdx < drawData.CmdListsCount; listIdx++)
        {
            var cmdList = drawData.CmdLists[listIdx];
            var listVtxCount = cmdList.VtxBuffer.Size;
            var listIdxCount = cmdList.IdxBuffer.Size;

            var listVtxBytes = (nuint)(listVtxCount * sizeof(ImDrawVert));
            MemoryUtility.MemCpy(pDstVtx + vtxWriteCount * sizeof(ImDrawVert), cmdList.VtxBuffer.Data, listVtxBytes);
            MemoryUtility.MemCpy(pDstIdx + idxWriteCount, cmdList.IdxBuffer.Data, (nuint)(listIdxCount * sizeof(ushort)));

            for (var cmdIdx = 0; cmdIdx < cmdList.CmdBuffer.Size; cmdIdx++)
            {
                var pCmd = &cmdList.CmdBuffer.Data[cmdIdx];
                if (pCmd->UserCallback != null || pCmd->ElemCount == 0)
                {
                    continue;
                }

                var cmdRecord = new ImGuiDrawCommandRecord
                {
                    vertexByteOffset = (uint)((vtxWriteCount + (int)pCmd->VtxOffset) * sizeof(ImDrawVert)),
                    indexStartOffset = (uint)(idxWriteCount + (int)pCmd->IdxOffset),
                    elemCount = pCmd->ElemCount,
                    textureId = (uint)pCmd->GetTexID().Handle,
                    clipRect = new float4(pCmd->ClipRect.X - DisplayPos.x, pCmd->ClipRect.Y - DisplayPos.y, pCmd->ClipRect.Z - DisplayPos.x, pCmd->ClipRect.W - DisplayPos.y),
                };

                _commands.Add(cmdRecord);
            }

            vtxWriteCount += listVtxCount;
            idxWriteCount += listIdxCount;
        }
    }

    private void CaptureTextureUpdates(ImDrawDataPtr drawData)
    {
        var textures = drawData.Textures;
        for (var i = 0; i < textures.Size; i++)
        {
            var tex = textures.Data[i];
            if (tex.Status == ImTextureStatus.Ok || tex.Status == ImTextureStatus.Destroyed)
            {
                continue;
            }

            var texKey = ImGuiContextManager.IMGUI_MANAGED_TEX_BIT | (uint)tex.UniqueID;

            if (tex.Status == ImTextureStatus.WantCreate || tex.Status == ImTextureStatus.WantUpdates)
            {
                var pixelSize = (uint)(tex.Width * tex.Height * tex.BytesPerPixel);
                var dataOffset = (uint)_texturePixelBytes.Count;
                _texturePixelBytes.Resize((int)(dataOffset + pixelSize));
                _texturePixelBytes.UnsafeSetCount((int)(dataOffset + pixelSize));

                var pDst = (byte*)_texturePixelBytes.GetUnsafePtr() + dataOffset;
                MemoryUtility.MemCpy(pDst, tex.Pixels, pixelSize);

                _textureOps.Add(new ImGuiTextureSyncOp
                {
                    textureKey = texKey,
                    width = (uint)tex.Width,
                    height = (uint)tex.Height,
                    pixelDataOffset = dataOffset,
                    pixelDataSize = pixelSize,
                    isDestroy = 0,
                });

                tex.TexID = new ImTextureID((nint)texKey);
                tex.Status = ImTextureStatus.Ok;
            }
            else if (tex.Status == ImTextureStatus.WantDestroy && tex.UnusedFrames > 0)
            {
                _textureOps.Add(new ImGuiTextureSyncOp
                {
                    textureKey = texKey,
                    isDestroy = 1,
                });

                tex.TexID = ImTextureID.Null;
                tex.Status = ImTextureStatus.Destroyed;
            }
        }
    }

    public void Dispose()
    {
        _vertexBytes.Dispose();
        _indices.Dispose();
        _commands.Dispose();
        _texturePixelBytes.Dispose();
        _textureOps.Dispose();
    }
}
