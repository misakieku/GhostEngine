using Win32;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.Data;

public class Material : IDisposable
{
    // TODO: Pipeline state should be abstracted that can support multiple graphics APIs.
    private ComPtr<ID3D12PipelineState> _pipelineState;

    public Shader Shader
    {
        get;
        set;
    } = Shader.Empty;

    public void Dispose()
    {
        _pipelineState.Dispose();
    }
}