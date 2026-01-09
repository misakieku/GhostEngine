using Ghost.Core;

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
}