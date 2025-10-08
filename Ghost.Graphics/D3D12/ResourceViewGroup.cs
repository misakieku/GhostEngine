using Ghost.Core;

namespace Ghost.Graphics.D3D12;

internal readonly struct RTVDesc : IIdentifierType;
internal readonly struct DSVDesc : IIdentifierType;
internal readonly struct CbvSrvUavDesc : IIdentifierType;
internal readonly struct SamplerDesc : IIdentifierType;

internal struct ResourceViewGroup
{
    public Identifier<RTVDesc> rtv;
    public Identifier<DSVDesc> dsv;
    public Identifier<CbvSrvUavDesc> srv;
    public Identifier<CbvSrvUavDesc> cbv;
    public Identifier<CbvSrvUavDesc> uav;
    public Identifier<SamplerDesc> sampler;

    public static ResourceViewGroup Invalid => new()
    {
        rtv = Identifier<RTVDesc>.Invalid,
        dsv = Identifier<DSVDesc>.Invalid,
        srv = Identifier<CbvSrvUavDesc>.Invalid,
        cbv = Identifier<CbvSrvUavDesc>.Invalid,
        uav = Identifier<CbvSrvUavDesc>.Invalid,
        sampler = Identifier<SamplerDesc>.Invalid,
    };
}