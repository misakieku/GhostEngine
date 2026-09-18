#ifndef GHOST_RASTERIZER_HLSL
#define GHOST_RASTERIZER_HLSL

#include "EngineResources/Shaders/Includes/Utilities/Math.hlsl"

struct RasterTriangle
{
    int2 minPixel;
    int2 maxPixel;

    float2 edge01;
    float2 edge12;
    float2 edge20;

    float c0;
    float c1;
    float c2;

    float3 depthPlane;
    float3 invW;

    float3 barycentrics_dx;
    float3 barycentrics_dy;

    bool isValid;
    bool backface;
};

template<uint subpixelSamples, bool backfaceCull>
RasterTriangle SetupTriangle(int4 scissorRect, float4 verts[3])
{
    RasterTriangle tri;
    tri.isValid = true;
    tri.invW = float3(verts[0].w, verts[1].w, verts[2].w);

	// 16.8 fixed point
    float2 vert0 = verts[0].xy;
    float2 vert1 = verts[1].xy;
    float2 vert2 = verts[2].xy;

	// 4.8 fixed point
    tri.edge01 = vert0 - vert1;
    tri.edge12 = vert1 - vert2;
    tri.edge20 = vert2 - vert0;

    float detXY = tri.edge01.y * tri.edge20.x - tri.edge01.x * tri.edge20.y;
    tri.backface = (detXY >= 0.0f);

    if (backfaceCull)
        tri.isValid = !tri.backface;

    if (!backfaceCull && tri.backface)
    {
		// Swap winding order
        tri.edge01 *= -1.0f;
        tri.edge12 *= -1.0f;
        tri.edge20 *= -1.0f;
    }

	// Bounding rect
    const float2 MinSubpixel = min3(vert0, vert1, vert2);
    const float2 MaxSubpixel = max3(vert0, vert1, vert2);

	// Round to nearest pixel
    tri.minPixel = (int2) floor((MinSubpixel + (subpixelSamples / 2) - 1) * (1.0 / subpixelSamples));
    tri.maxPixel = (int2) floor((MaxSubpixel - (subpixelSamples / 2) - 1) * (1.0 / subpixelSamples)); // inclusive!

	// Scissor
    tri.minPixel = max(tri.minPixel, scissorRect.xy);
    tri.maxPixel = min(tri.maxPixel, scissorRect.zw - 1);
	
	// Limit the rasterizer bounds to a sensible max.
    tri.maxPixel = min(tri.maxPixel, tri.minPixel + 63);

	// Cull when no pixels covered
    if (any(tri.minPixel > tri.maxPixel))
        tri.isValid = false;
	
	// Rebase off minPixel with half pixel offset
	// 4.8 fixed point
	// Max triangle size should only be 7x7 pixels. Not sure why this works for larger triangles.
    const float2 baseSubpixel = (float2) tri.minPixel * subpixelSamples + (subpixelSamples / 2);
    vert0 -= baseSubpixel;
    vert1 -= baseSubpixel;
    vert2 -= baseSubpixel;

	// Half-edge constants
	// 8.16 fixed point
    tri.c0 = tri.edge12.y * vert1.x - tri.edge12.x * vert1.y;
    tri.c1 = tri.edge20.y * vert2.x - tri.edge20.x * vert2.y;
    tri.c2 = tri.edge01.y * vert0.x - tri.edge01.x * vert0.y;

	// Sum c before nudging for fill convention. Afterwards it could be zero.
    const float scaleToUnit = subpixelSamples / (tri.c0 + tri.c1 + tri.c2);

	// Correct for fill convention
	// Top left rule for CCW
#if 1
    tri.c0 -= saturate(tri.edge12.y + saturate(1.0f - tri.edge12.x));
    tri.c1 -= saturate(tri.edge20.y + saturate(1.0f - tri.edge20.x));
    tri.c2 -= saturate(tri.edge01.y + saturate(1.0f - tri.edge01.x));
#else
	tri.c0 -= ( tri.edge12.y < 0 || ( tri.edge12.y == 0 && tri.edge12.x > 0 ) ) ? 0 : 1;
	tri.c1 -= ( tri.edge20.y < 0 || ( tri.edge20.y == 0 && tri.edge20.x > 0 ) ) ? 0 : 1;
	tri.c2 -= ( tri.edge01.y < 0 || ( tri.edge01.y == 0 && tri.edge01.x > 0 ) ) ? 0 : 1;
#endif

#if 0
	// Step in pixel increments
	// 8.16 fixed point
	tri.edge01 *= subpixelSamples;
	tri.edge12 *= subpixelSamples;
	tri.edge20 *= subpixelSamples;
#else
	// Scale c0/c1/c2 down by subpixelSamples instead of scaling edge01/edge12/edge20 up. Lossless because subpixelSamples is a power of two.
    tri.c0 *= (1.0f / subpixelSamples);
    tri.c1 *= (1.0f / subpixelSamples);
    tri.c2 *= (1.0f / subpixelSamples);
#endif

    tri.barycentrics_dx = float3(-tri.edge12.y, -tri.edge20.y, -tri.edge01.y) * scaleToUnit;
    tri.barycentrics_dy = float3(tri.edge12.x, tri.edge20.x, tri.edge01.x) * scaleToUnit;

    tri.depthPlane.x = verts[0].z;
    tri.depthPlane.y = verts[1].z - verts[0].z;
    tri.depthPlane.z = verts[2].z - verts[0].z;
    tri.depthPlane.yz *= scaleToUnit;

    return tri;
}

static inline void WritePixel(uint2 pixelPose, float3 c3, RasterTriangle tri, uint renderWidth, RWByteAddressBuffer vbuffer, uint pixelValue)
{
    uint byteAddress = (pixelPose.y * renderWidth + pixelPose.x) * 16;
    float depth = tri.depthPlane.x + tri.depthPlane.y * c3.y + tri.depthPlane.z * c3.z;
    uint64_t packedValue = (((uint64_t) asuint(depth)) << 32) | (uint64_t) pixelValue;
    vbuffer.InterlockedMax64(byteAddress, packedValue);
}

void RasterizeTri_Rect(RasterTriangle tri, uint renderWidth, RWByteAddressBuffer vbuffer, uint pixelValue)
{
    float cy0 = tri.c0;
    float cy1 = tri.c1;
    float cy2 = tri.c2;

    int y = tri.minPixel.y;
    while (true)
    {
        int x = tri.minPixel.x;
        if (min3(cy0, cy1, cy2) >= 0)
        {
            WritePixel(uint2(x, y), float3(cy0, cy1, cy2), tri, renderWidth, vbuffer, pixelValue);
        }

        if (x < tri.maxPixel.x)
        {
            float cx0 = cy0 - tri.edge12.y;
            float cx1 = cy1 - tri.edge20.y;
            float cx3 = cy2 - tri.edge01.y;
            x++;

            while (true)
            {
                if (min3(cx0, cx1, cx3) >= 0)
                {
                    WritePixel(uint2(x, y), float3(cx0, cx1, cx3), tri, renderWidth, vbuffer, pixelValue);
                }

                if (x >= tri.maxPixel.x)
                {
                    break;
                }

                cx0 -= tri.edge12.y;
                cx1 -= tri.edge20.y;
                cx3 -= tri.edge01.y;
                x++;
            }
        }

        if (y >= tri.maxPixel.y)
        {
            break;
        }

        cy0 += tri.edge12.x;
        cy1 += tri.edge20.x;
        cy2 += tri.edge01.x;
        y++;
    }
}

void RasterizeTri_Scanline(RasterTriangle tri, uint renderWidth, RWByteAddressBuffer vbuffer, uint pixelValue)
{
    float cy0 = tri.c0;
    float cy1 = tri.c1;
    float cy2 = tri.c2;

    float3 edge012 = { tri.edge12.y, tri.edge20.y, tri.edge01.y };
    bool3 openEdge = edge012 < 0;
    float3 invEdge012 = select(edge012 == 0, 1e8, rcp(edge012));

    int y = tri.minPixel.y;
    while (true)
    {
		//float cx0 = cy0 - edge12.y * (x - minPixel.x);
		// edge12.y * (x - minPixel.x) <= cy0;

		/*
		if( edge12.y > 0 )
			x <= cy0 / edge12.y + minPixel.x;	// Closing edge
		else
			x >= cy0 / edge12.y + minPixel.x;	// Opening edge
		*/
			
		// No longer fixed point
        float3 crossX = float3(cy0, cy1, cy2) * invEdge012;

        float3 minX = select(openEdge, crossX, 0.0);
        float3 maxX = select(openEdge, tri.maxPixel.x - tri.minPixel.x, crossX);

        float x0 = ceil(max3(minX.x, minX.y, minX.z));
        float x1 = min3(maxX.x, maxX.y, maxX.z);
		
        float cx0 = cy0 - x0 * tri.edge12.y;
        float cx1 = cy1 - x0 * tri.edge20.y;
        float cx3 = cy2 - x0 * tri.edge01.y;

        x0 += tri.minPixel.x;
        x1 += tri.minPixel.x;

		// NOTE: In some cases x0 > x1 and we need to avoid writing pixels in those situations
		// or else artifacts can appear, particularly in VSM near page edges.
        for (float x = x0; x <= x1; x++)
        {
            if (min3(cx0, cx1, cx3) >= 0)
            {
                WritePixel(uint2(x, y), float3(cx0, cx1, cx3), tri, renderWidth, vbuffer, pixelValue);
            }

            cx0 -= tri.edge12.y;
            cx1 -= tri.edge20.y;
            cx3 -= tri.edge01.y;
        }

        if (y >= tri.maxPixel.y)
        {
            break;
        }

        cy0 += tri.edge12.x;
        cy1 += tri.edge20.x;
        cy2 += tri.edge01.x;
        y++;
    }
}

// false for now
#define MESHLET_PIXEL_PROGRAMMABLE false

void RasterizeTri_Adaptive(RasterTriangle tri, uint renderWidth, RWByteAddressBuffer vbuffer, uint pixelValue)
{
    bool scanline = MESHLET_PIXEL_PROGRAMMABLE || WaveActiveAnyTrue(tri.maxPixel.x - tri.minPixel.x > 4);

    if (scanline)
    {
        RasterizeTri_Scanline(tri, renderWidth, vbuffer, pixelValue);
    }
    else
    {
        RasterizeTri_Rect(tri, renderWidth, vbuffer, pixelValue);
    }
}

#endif // GHOST_RASTERIZER_HLSL
