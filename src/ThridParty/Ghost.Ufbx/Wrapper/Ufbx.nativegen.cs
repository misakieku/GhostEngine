namespace Ghost.Ufbx;

public static unsafe class Ufbx
{
    public static bool CoordinateAxesValid(ufbx_coordinate_axes axes)
    {
        return Api.ufbx_coordinate_axes_valid(axes);
    }

    public static bool DefaultOpenFile(void* user, Stream stream, sbyte* path, nuint pathLen, OpenFileInfo info)
    {
        return Api.ufbx_default_open_file(user, stream.GetUnsafePtr(), path, pathLen, info.GetUnsafePtr());
    }

    public static Misaki.HighPerformance.Mathematics.quaternion EulerToQuat(Misaki.HighPerformance.Mathematics.float3 v, ufbx_rotation_order order)
    {
        return Api.ufbx_euler_to_quat(v, order);
    }

    public static Misaki.HighPerformance.Mathematics.quaternion EvaluateBakedQuat(ufbx_baked_quat_list keyframes, double time)
    {
        return Api.ufbx_evaluate_baked_quat(keyframes, time);
    }

    public static Misaki.HighPerformance.Mathematics.float3 EvaluateBakedVec3(ufbx_baked_vec3_list keyframes, double time)
    {
        return Api.ufbx_evaluate_baked_vec3(keyframes, time);
    }

    public static nuint FormatError(sbyte* dst, nuint dstSize, Error error)
    {
        return Api.ufbx_format_error(dst, dstSize, error.GetUnsafePtr());
    }

    public static nint Inflate(void* dst, nuint dstSize, InflateInput input, InflateRetain retain)
    {
        return Api.ufbx_inflate(dst, dstSize, input.GetUnsafePtr(), retain.GetUnsafePtr());
    }

    public static bool IsThreadSafe()
    {
        return Api.ufbx_is_thread_safe();
    }

    public static float MatrixDeterminant(Misaki.HighPerformance.Mathematics.float3x4* m)
    {
        return Api.ufbx_matrix_determinant(m);
    }

    public static Misaki.HighPerformance.Mathematics.float3x4 MatrixForNormals(Misaki.HighPerformance.Mathematics.float3x4* m)
    {
        return Api.ufbx_matrix_for_normals(m);
    }

    public static Misaki.HighPerformance.Mathematics.float3x4 MatrixInvert(Misaki.HighPerformance.Mathematics.float3x4* m)
    {
        return Api.ufbx_matrix_invert(m);
    }

    public static Misaki.HighPerformance.Mathematics.float3x4 MatrixMul(Misaki.HighPerformance.Mathematics.float3x4* a, Misaki.HighPerformance.Mathematics.float3x4* b)
    {
        return Api.ufbx_matrix_mul(a, b);
    }

    public static ufbx_transform MatrixToTransform(Misaki.HighPerformance.Mathematics.float3x4* m)
    {
        return Api.ufbx_matrix_to_transform(m);
    }

    public static float QuatDot(Misaki.HighPerformance.Mathematics.quaternion a, Misaki.HighPerformance.Mathematics.quaternion b)
    {
        return Api.ufbx_quat_dot(a, b);
    }

    public static Misaki.HighPerformance.Mathematics.quaternion QuatFixAntipodal(Misaki.HighPerformance.Mathematics.quaternion q, Misaki.HighPerformance.Mathematics.quaternion reference)
    {
        return Api.ufbx_quat_fix_antipodal(q, reference);
    }

    public static Misaki.HighPerformance.Mathematics.quaternion QuatMul(Misaki.HighPerformance.Mathematics.quaternion a, Misaki.HighPerformance.Mathematics.quaternion b)
    {
        return Api.ufbx_quat_mul(a, b);
    }

    public static Misaki.HighPerformance.Mathematics.quaternion QuatNormalize(Misaki.HighPerformance.Mathematics.quaternion q)
    {
        return Api.ufbx_quat_normalize(q);
    }

    public static Misaki.HighPerformance.Mathematics.float3 QuatRotateVec3(Misaki.HighPerformance.Mathematics.quaternion q, Misaki.HighPerformance.Mathematics.float3 v)
    {
        return Api.ufbx_quat_rotate_vec3(q, v);
    }

    public static Misaki.HighPerformance.Mathematics.quaternion QuatSlerp(Misaki.HighPerformance.Mathematics.quaternion a, Misaki.HighPerformance.Mathematics.quaternion b, float t)
    {
        return Api.ufbx_quat_slerp(a, b, t);
    }

    public static Misaki.HighPerformance.Mathematics.float3 QuatToEuler(Misaki.HighPerformance.Mathematics.quaternion q, ufbx_rotation_order order)
    {
        return Api.ufbx_quat_to_euler(q, order);
    }

    public static void* ThreadPoolGetUserPtr(nuint ctx)
    {
        return Api.ufbx_thread_pool_get_user_ptr(ctx);
    }

    public static void ThreadPoolRunTask(nuint ctx, uint index)
    {
        Api.ufbx_thread_pool_run_task(ctx, index);
    }

    public static void ThreadPoolSetUserPtr(nuint ctx, void* userPtr)
    {
        Api.ufbx_thread_pool_set_user_ptr(ctx, userPtr);
    }

    public static Misaki.HighPerformance.Mathematics.float3 TransformDirection(Misaki.HighPerformance.Mathematics.float3x4* m, Misaki.HighPerformance.Mathematics.float3 v)
    {
        return Api.ufbx_transform_direction(m, v);
    }

    public static Misaki.HighPerformance.Mathematics.float3 TransformPosition(Misaki.HighPerformance.Mathematics.float3x4* m, Misaki.HighPerformance.Mathematics.float3 v)
    {
        return Api.ufbx_transform_position(m, v);
    }

    public static uint TriangulateFace(uint* indices, nuint numIndices, Mesh mesh, ufbx_face face)
    {
        return Api.ufbx_triangulate_face(indices, numIndices, mesh.GetUnsafePtr(), face);
    }

    public static Misaki.HighPerformance.Mathematics.float3 Vec3Normalize(Misaki.HighPerformance.Mathematics.float3 v)
    {
        return Api.ufbx_vec3_normalize(v);
    }

}
