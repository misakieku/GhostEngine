namespace Ghost.MeshOptimizer;

[Flags]
public enum SimplifyOptions : uint
{
    LockBorder = 1 << 0,
    Sparse = 1 << 1,
    ErrorAbsolute = 1 << 2,
    Prune = 1 << 3,
    Regularize = 1 << 4,
    Permissive = 1 << 5
}

[Flags]
public enum SimplifyVertexOptions : byte
{
    Lock = 1 << 0,
    Protect = 1 << 1
}

public unsafe partial struct MeshOptApi
{
    public const int VERSION = Api.MESHOPTIMIZER_VERSION;

    /// <summary>
    /// From: <see cref="Api.meshopt_simplify(uint*, uint*, nuint, float*, nuint, nuint, nuint, float, uint, float*)" />
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static nuint Simplify(uint* destination, uint* indices, nuint index_count, float* vertex_positions, nuint vertex_count, nuint vertex_positions_stride, nuint target_index_count, float target_error, SimplifyOptions options, float* result_error)
    {
        return Api.meshopt_simplify(
            destination,
            indices,
            index_count,
            vertex_positions,
            vertex_count,
            vertex_positions_stride,
            target_index_count,
            target_error,
            (uint)options,
            result_error);
    }

    /// <summary>
    /// From: <see cref="Api.meshopt_simplifyWithAttributes(uint*, uint*, nuint, float*, nuint, nuint, float*, nuint, float*, nuint, byte*, nuint, float, uint, float*)" />
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static nuint SimplifyWithAttributes(uint* destination, uint* indices, nuint index_count, float* vertex_positions, nuint vertex_count, nuint vertex_positions_stride, float* vertex_attributes, nuint vertex_attributes_stride, float* attribute_weights, nuint attribute_count, byte* vertex_lock, nuint target_index_count, float target_error, SimplifyOptions options, float* result_error)
    {
        return Api.meshopt_simplifyWithAttributes(
            destination,
            indices,
            index_count,
            vertex_positions,
            vertex_count,
            vertex_positions_stride,
            vertex_attributes,
            vertex_attributes_stride,
            attribute_weights,
            attribute_count,
            vertex_lock,
            target_index_count,
            target_error,
            (uint)options,
            result_error);
    }

    /// <summary>
    /// From: <see cref="Api.meshopt_simplifyWithUpdate(uint*, nuint, float*, nuint, nuint, float*, nuint, float*, nuint, byte*, nuint, float, uint, float*)" />
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static nuint SimplifyWithUpdate(uint* indices, nuint index_count, float* vertex_positions, nuint vertex_count, nuint vertex_positions_stride, float* vertex_attributes, nuint vertex_attributes_stride, float* attribute_weights, nuint attribute_count, byte* vertex_lock, nuint target_index_count, float target_error, SimplifyOptions options, float* result_error)
    {
        return Api.meshopt_simplifyWithUpdate(
            indices,
            index_count,
            vertex_positions,
            vertex_count,
            vertex_positions_stride,
            vertex_attributes,
            vertex_attributes_stride,
            attribute_weights,
            attribute_count,
            vertex_lock,
            target_index_count,
            target_error,
            (uint)options,
            result_error);
    }
}