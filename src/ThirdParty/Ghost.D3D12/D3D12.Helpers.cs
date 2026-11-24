using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12_RESOURCE_DIMENSION;
using static Ghost.D3D12.D3D12_TEXTURE_COPY_TYPE;
using static Ghost.Win32.UUID;

namespace Ghost.D3D12;

public static unsafe partial class D3D12
{
    public static ulong GetRequiredIntermediateSize(ID3D12Resource* pDestinationResource, uint FirstSubresource, uint NumSubresources)
    {
        var desc = pDestinationResource->GetDesc();
        ulong requiredSize = 0;

        ID3D12Device* pDevice = null;
        pDestinationResource->GetDevice(__uuidof<ID3D12Device>(), (void**)&pDevice);
        pDevice->GetCopyableFootprints(&desc, FirstSubresource, NumSubresources, 0, null, null, null, &requiredSize);
        pDevice->Release();

        return requiredSize;
    }

    public static ulong UpdateSubresources(ID3D12GraphicsCommandList* pCmdList, ID3D12Resource* pDestinationResource, ID3D12Resource* pIntermediate, ulong IntermediateOffset, uint FirstSubresource, uint NumSubresources, D3D12_SUBRESOURCE_DATA* pSrcData)
    {
        var memToAlloc = (nuint)(sizeof(D3D12_PLACED_SUBRESOURCE_FOOTPRINT) + sizeof(uint) + sizeof(ulong)) * NumSubresources;
        if (memToAlloc > (nuint)sizeof(D3D12_PLACED_SUBRESOURCE_FOOTPRINT) * 65536u)
        {
            return 0;
        }

        var pMem = NativeMemory.Alloc(memToAlloc);
        try
        {
            var pLayouts = (D3D12_PLACED_SUBRESOURCE_FOOTPRINT*)pMem;
            var pRowSizesInBytes = (ulong*)(pLayouts + NumSubresources);
            var pNumRows = (uint*)(pRowSizesInBytes + NumSubresources);

            var desc = pDestinationResource->GetDesc();
            ulong requiredSize = 0;

            ID3D12Device* pDevice = null;
            pDestinationResource->GetDevice(__uuidof<ID3D12Device>(), (void**)&pDevice);
            pDevice->GetCopyableFootprints(&desc, FirstSubresource, NumSubresources, IntermediateOffset, pLayouts, pNumRows, pRowSizesInBytes, &requiredSize);
            pDevice->Release();

            return UpdateSubresources(pCmdList, pDestinationResource, pIntermediate, FirstSubresource, NumSubresources, requiredSize, pLayouts, pNumRows, pRowSizesInBytes, pSrcData);
        }
        finally
        {
            NativeMemory.Free(pMem);
        }
    }

    public static ulong UpdateSubresources(ID3D12GraphicsCommandList* pCmdList, ID3D12Resource* pDestinationResource, ID3D12Resource* pIntermediate, uint FirstSubresource, uint NumSubresources, ulong RequiredSize, D3D12_PLACED_SUBRESOURCE_FOOTPRINT* pLayouts, uint* pNumRows, ulong* pRowSizesInBytes, D3D12_SUBRESOURCE_DATA* pSrcData)
    {
        var intermediateDesc = pIntermediate->GetDesc();
        var destinationDesc = pDestinationResource->GetDesc();
        if (intermediateDesc.Dimension != D3D12_RESOURCE_DIMENSION_BUFFER ||
            intermediateDesc.Width < RequiredSize + pLayouts[0].Offset ||
            RequiredSize > nuint.MaxValue ||
            (destinationDesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER && (FirstSubresource != 0 || NumSubresources != 1)))
        {
            return 0;
        }

        byte* pData = null;
        if (pIntermediate->Map(0, null, (void**)&pData).FAILED)
        {
            return 0;
        }

        for (uint i = 0; i < NumSubresources; i++)
        {
            if (pRowSizesInBytes[i] > nuint.MaxValue)
            {
                return 0;
            }

            var destData = new D3D12_MEMCPY_DEST
            {
                pData = pData + pLayouts[i].Offset,
                RowPitch = (nuint)pLayouts[i].Footprint.RowPitch,
                SlicePitch = (nuint)pLayouts[i].Footprint.RowPitch * pNumRows[i]
            };

            MemcpySubresource(&destData, &pSrcData[i], (nuint)pRowSizesInBytes[i], pNumRows[i], pLayouts[i].Footprint.Depth);
        }

        pIntermediate->Unmap(0, null);

        if (destinationDesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
        {
            pCmdList->CopyBufferRegion(pDestinationResource, 0, pIntermediate, pLayouts[0].Offset, pLayouts[0].Footprint.Width);
        }
        else
        {
            for (uint i = 0; i < NumSubresources; i++)
            {
                var dst = new D3D12_TEXTURE_COPY_LOCATION
                {
                    pResource = pDestinationResource,
                    Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
                    Anonymous = new D3D12_TEXTURE_COPY_LOCATION._Anonymous_e__Union { SubresourceIndex = i + FirstSubresource }
                };

                var src = new D3D12_TEXTURE_COPY_LOCATION
                {
                    pResource = pIntermediate,
                    Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
                    Anonymous = new D3D12_TEXTURE_COPY_LOCATION._Anonymous_e__Union { PlacedFootprint = pLayouts[i] }
                };

                pCmdList->CopyTextureRegion(&dst, 0, 0, 0, &src, null);
            }
        }

        return RequiredSize;
    }

    private static void MemcpySubresource(D3D12_MEMCPY_DEST* pDest, D3D12_SUBRESOURCE_DATA* pSrc, nuint rowSizeInBytes, uint numRows, uint numSlices)
    {
        for (uint z = 0; z < numSlices; z++)
        {
            var pDestSlice = (byte*)pDest->pData + pDest->SlicePitch * z;
            var pSrcSlice = (byte*)pSrc->pData + (nuint)pSrc->SlicePitch * z;

            for (uint y = 0; y < numRows; y++)
            {
                NativeMemory.Copy(pSrcSlice + (nuint)pSrc->RowPitch * y, pDestSlice + pDest->RowPitch * y, rowSizeInBytes);
            }
        }
    }
}
