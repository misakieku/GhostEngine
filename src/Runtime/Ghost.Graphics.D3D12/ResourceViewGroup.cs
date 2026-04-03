using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.D3D12;

internal readonly struct RTVDescriptor;
internal readonly struct DSVDescriptor;
internal readonly struct CbvSrvUavDescriptor;
internal readonly struct SamplerDescriptor;

internal struct ResourceViewGroup
{
    public Identifier<RTVDescriptor> rtv;
    public Identifier<DSVDescriptor> dsv;
    public Identifier<CbvSrvUavDescriptor> srv;
    public Identifier<CbvSrvUavDescriptor> cbv;
    public Identifier<CbvSrvUavDescriptor> uav;
    public Identifier<SamplerDescriptor> sampler;

    public static ResourceViewGroup Invalid => new()
    {
        rtv = Identifier<RTVDescriptor>.Invalid,
        dsv = Identifier<DSVDescriptor>.Invalid,
        srv = Identifier<CbvSrvUavDescriptor>.Invalid,
        cbv = Identifier<CbvSrvUavDescriptor>.Invalid,
        uav = Identifier<CbvSrvUavDescriptor>.Invalid,
        sampler = Identifier<SamplerDescriptor>.Invalid,
    };

    public readonly TextureUsage GetTextureUsage()
    {
        var usage = TextureUsage.None;
        if (rtv.IsValid)
        {
            usage |= TextureUsage.RenderTarget;
        }
        
        if (dsv.IsValid)
        {
            usage |= TextureUsage.DepthStencil;
        }
        
        if (srv.IsValid)
        {
            usage |= TextureUsage.ShaderResource;
        }
        
        if (uav.IsValid)
        {
            usage |= TextureUsage.UnorderedAccess;
        }

        return usage;
    }

    public readonly BufferUsage GetBufferUsage()
    {
        var usage = BufferUsage.None;
        
        if (cbv.IsValid)
        {
            usage |= BufferUsage.Constant;
        }

        if (srv.IsValid)
        {
            usage |= BufferUsage.ShaderResource;
        }

        if (uav.IsValid)
        {
            usage |= BufferUsage.UnorderedAccess;
        }

        return usage;
    }
}