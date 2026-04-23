using System.Runtime.InteropServices;

namespace Ghost.StbI;

public static unsafe partial class Api
{
    public const int STBI_default = 0;
    public const int STBI_grey = 1;
    public const int STBI_grey_alpha = 2;
    public const int STBI_rgb = 3;
    public const int STBI_rgb_alpha = 4;

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("stbi_uc *")]
    public static extern byte* stbi_load_from_memory([NativeTypeName("const stbi_uc *")] byte* buffer, int len, int* x, int* y, int* channels_in_file, int desired_channels);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("stbi_uc *")]
    public static extern byte* stbi_load_from_callbacks([NativeTypeName("const stbi_io_callbacks *")] stbi_io_callbacks* clbk, void* user, int* x, int* y, int* channels_in_file, int desired_channels);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("stbi_uc *")]
    public static extern byte* stbi_load([NativeTypeName("const char *")] sbyte* filename, int* x, int* y, int* channels_in_file, int desired_channels);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("stbi_uc *")]
    public static extern byte* stbi_load_gif_from_memory([NativeTypeName("const stbi_uc *")] byte* buffer, int len, int** delays, int* x, int* y, int* z, int* comp, int req_comp);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("stbi_us *")]
    public static extern ushort* stbi_load_16_from_memory([NativeTypeName("const stbi_uc *")] byte* buffer, int len, int* x, int* y, int* channels_in_file, int desired_channels);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("stbi_us *")]
    public static extern ushort* stbi_load_16_from_callbacks([NativeTypeName("const stbi_io_callbacks *")] stbi_io_callbacks* clbk, void* user, int* x, int* y, int* channels_in_file, int desired_channels);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("stbi_us *")]
    public static extern ushort* stbi_load_16([NativeTypeName("const char *")] sbyte* filename, int* x, int* y, int* channels_in_file, int desired_channels);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern float* stbi_loadf_from_memory([NativeTypeName("const stbi_uc *")] byte* buffer, int len, int* x, int* y, int* channels_in_file, int desired_channels);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern float* stbi_loadf_from_callbacks([NativeTypeName("const stbi_io_callbacks *")] stbi_io_callbacks* clbk, void* user, int* x, int* y, int* channels_in_file, int desired_channels);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern float* stbi_loadf([NativeTypeName("const char *")] sbyte* filename, int* x, int* y, int* channels_in_file, int desired_channels);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void stbi_hdr_to_ldr_gamma(float gamma);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void stbi_hdr_to_ldr_scale(float scale);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void stbi_ldr_to_hdr_gamma(float gamma);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void stbi_ldr_to_hdr_scale(float scale);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_is_hdr_from_callbacks([NativeTypeName("const stbi_io_callbacks *")] stbi_io_callbacks* clbk, void* user);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_is_hdr_from_memory([NativeTypeName("const stbi_uc *")] byte* buffer, int len);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_is_hdr([NativeTypeName("const char *")] sbyte* filename);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("const char *")]
    public static extern sbyte* stbi_failure_reason();

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void stbi_image_free(void* retval_from_stbi_load);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_info_from_memory([NativeTypeName("const stbi_uc *")] byte* buffer, int len, int* x, int* y, int* comp);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_info_from_callbacks([NativeTypeName("const stbi_io_callbacks *")] stbi_io_callbacks* clbk, void* user, int* x, int* y, int* comp);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_is_16_bit_from_memory([NativeTypeName("const stbi_uc *")] byte* buffer, int len);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_is_16_bit_from_callbacks([NativeTypeName("const stbi_io_callbacks *")] stbi_io_callbacks* clbk, void* user);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_info([NativeTypeName("const char *")] sbyte* filename, int* x, int* y, int* comp);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_is_16_bit([NativeTypeName("const char *")] sbyte* filename);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void stbi_set_unpremultiply_on_load(int flag_true_if_should_unpremultiply);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void stbi_convert_iphone_png_to_rgb(int flag_true_if_should_convert);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void stbi_set_flip_vertically_on_load(int flag_true_if_should_flip);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void stbi_set_unpremultiply_on_load_thread(int flag_true_if_should_unpremultiply);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void stbi_convert_iphone_png_to_rgb_thread(int flag_true_if_should_convert);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void stbi_set_flip_vertically_on_load_thread(int flag_true_if_should_flip);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("char *")]
    public static extern sbyte* stbi_zlib_decode_malloc_guesssize([NativeTypeName("const char *")] sbyte* buffer, int len, int initial_size, int* outlen);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("char *")]
    public static extern sbyte* stbi_zlib_decode_malloc_guesssize_headerflag([NativeTypeName("const char *")] sbyte* buffer, int len, int initial_size, int* outlen, int parse_header);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("char *")]
    public static extern sbyte* stbi_zlib_decode_malloc([NativeTypeName("const char *")] sbyte* buffer, int len, int* outlen);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_zlib_decode_buffer([NativeTypeName("char *")] sbyte* obuffer, int olen, [NativeTypeName("const char *")] sbyte* ibuffer, int ilen);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("char *")]
    public static extern sbyte* stbi_zlib_decode_noheader_malloc([NativeTypeName("const char *")] sbyte* buffer, int len, int* outlen);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_zlib_decode_noheader_buffer([NativeTypeName("char *")] sbyte* obuffer, int olen, [NativeTypeName("const char *")] sbyte* ibuffer, int ilen);

    [NativeTypeName("#define STBI_VERSION 1")]
    public const int STBI_VERSION = 1;

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_write_png([NativeTypeName("const char *")] sbyte* filename, int w, int h, int comp, [NativeTypeName("const void *")] void* data, int stride_in_bytes);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_write_bmp([NativeTypeName("const char *")] sbyte* filename, int w, int h, int comp, [NativeTypeName("const void *")] void* data);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_write_tga([NativeTypeName("const char *")] sbyte* filename, int w, int h, int comp, [NativeTypeName("const void *")] void* data);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_write_hdr([NativeTypeName("const char *")] sbyte* filename, int w, int h, int comp, [NativeTypeName("const float *")] float* data);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_write_jpg([NativeTypeName("const char *")] sbyte* filename, int x, int y, int comp, [NativeTypeName("const void *")] void* data, int quality);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_write_png_to_func([NativeTypeName("stbi_write_func *")] delegate* unmanaged[Cdecl]<void*, void*, int, void> func, void* context, int w, int h, int comp, [NativeTypeName("const void *")] void* data, int stride_in_bytes);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_write_bmp_to_func([NativeTypeName("stbi_write_func *")] delegate* unmanaged[Cdecl]<void*, void*, int, void> func, void* context, int w, int h, int comp, [NativeTypeName("const void *")] void* data);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_write_tga_to_func([NativeTypeName("stbi_write_func *")] delegate* unmanaged[Cdecl]<void*, void*, int, void> func, void* context, int w, int h, int comp, [NativeTypeName("const void *")] void* data);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_write_hdr_to_func([NativeTypeName("stbi_write_func *")] delegate* unmanaged[Cdecl]<void*, void*, int, void> func, void* context, int w, int h, int comp, [NativeTypeName("const float *")] float* data);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int stbi_write_jpg_to_func([NativeTypeName("stbi_write_func *")] delegate* unmanaged[Cdecl]<void*, void*, int, void> func, void* context, int x, int y, int comp, [NativeTypeName("const void *")] void* data, int quality);

    [DllImport("stbi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void stbi_flip_vertically_on_write(int flip_boolean);
}
