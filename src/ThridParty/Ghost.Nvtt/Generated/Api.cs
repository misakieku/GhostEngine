using System.Reflection;
using System.Runtime.InteropServices;

namespace Ghost.Nvtt
{
    public static unsafe partial class Api
    {
        static Api()
        {
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), (libraryName, assembly, searchPath) =>
            {
                var platform = OperatingSystem.IsWindows() ? "win" :
                                OperatingSystem.IsLinux() ? "linux" :
                                OperatingSystem.IsMacOS() ? "osx" : "unknown";
                var ext = OperatingSystem.IsWindows() ? ".dll" :
                            OperatingSystem.IsLinux() ? ".so" :
                            OperatingSystem.IsMacOS() ? ".dylib" : "";

                var arch = Environment.Is64BitProcess ? "x64" : "x86";
                var nativeDllDir = Path.Combine("./runtimes", platform + "-" + arch, "native");

                return NativeLibrary.Load(Path.Combine(nativeDllDir, libraryName + ext));
            });
        }

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttIsCudaSupported();

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttUseCurrentDevice();

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttCPUInputBuffer* nvttCreateCPUInputBuffer([NativeTypeName("const NvttRefImage *")] NvttRefImage* images, NvttValueType value_type, int numImages, int tile_w, int tile_h, float WeightR, float WeightG, float WeightB, float WeightA, NvttTimingContext* tc, [NativeTypeName("unsigned int *")] uint* num_tiles);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttDestroyCPUInputBuffer(NvttCPUInputBuffer* input);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttCPUInputBufferNumTiles([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttCPUInputBufferTileSize([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, int* tile_w, int* tile_h);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttValueType nvttCPUInputBufferType([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttGPUInputBuffer* nvttCreateGPUInputBuffer([NativeTypeName("const NvttRefImage *")] NvttRefImage* images, NvttValueType value_type, int numImages, int tile_w, int tile_h, float WeightR, float WeightG, float WeightB, float WeightA, NvttTimingContext* tc, [NativeTypeName("unsigned int *")] uint* num_tiles);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttDestroyGPUInputBuffer(NvttGPUInputBuffer* input);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttGPUInputBufferNumTiles([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttGPUInputBufferTileSize([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, int* tile_w, int* tile_h);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttValueType nvttGPUInputBufferType([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttEncodeCPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, void* output, [NativeTypeName("const NvttEncodeSettings *")] NvttEncodeSettings* settings);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttEncodeGPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, void* output, [NativeTypeName("const NvttEncodeSettings *")] NvttEncodeSettings* settings);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC1CPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, NvttBoolean fast_mode, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC1GPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, NvttBoolean fast_mode, void* output, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC1ACPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, NvttBoolean fast_mode, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC1AGPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, void* output, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC2CPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, NvttBoolean fast_mode, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC2GPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, void* output, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC3CPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, NvttBoolean fast_mode, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC3GPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, void* output, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC3NCPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, int qualityLevel, void* output, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC3RGBMCPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC4CPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, NvttBoolean slow_mode, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC4GPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, void* output, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC4SCPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, NvttBoolean slow_mode, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC4SGPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, void* output, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeATI2CPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, NvttBoolean slow_mode, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeATI2GPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, void* output, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC5CPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, NvttBoolean slow_mode, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC5GPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, void* output, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC5SCPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, NvttBoolean slow_mode, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC5SGPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, void* output, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC6HCPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, NvttBoolean slow_mode, NvttBoolean is_signed, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC6HGPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, NvttBoolean is_signed, void* output, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC7CPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, NvttBoolean slow_mode, NvttBoolean imageHasAlpha, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeBC7GPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, NvttBoolean imageHasAlpha, void* output, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeASTCCPU([NativeTypeName("const NvttCPUInputBuffer *")] NvttCPUInputBuffer* input, int qualityLevel, NvttBoolean imageHasAlpha, void* output, NvttBoolean useGpu, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttEncodeASTCGPU([NativeTypeName("const NvttGPUInputBuffer *")] NvttGPUInputBuffer* input, int qualityLevel, NvttBoolean imageHasAlpha, void* output, NvttBoolean to_device_mem, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttCompressionOptions* nvttCreateCompressionOptions();

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttDestroyCompressionOptions(NvttCompressionOptions* compressionOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttResetCompressionOptions(NvttCompressionOptions* compressionOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetCompressionOptionsFormat(NvttCompressionOptions* compressionOptions, NvttFormat format);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetCompressionOptionsQuality(NvttCompressionOptions* compressionOptions, NvttQuality quality);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetCompressionOptionsColorWeights(NvttCompressionOptions* compressionOptions, float red, float green, float blue, float alpha);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetCompressionOptionsPixelFormat(NvttCompressionOptions* compressionOptions, [NativeTypeName("unsigned int")] uint bitcount, [NativeTypeName("unsigned int")] uint rmask, [NativeTypeName("unsigned int")] uint gmask, [NativeTypeName("unsigned int")] uint bmask, [NativeTypeName("unsigned int")] uint amask);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetCompressionOptionsPixelType(NvttCompressionOptions* compressionOptions, NvttPixelType pixelType);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetCompressionOptionsPitchAlignment(NvttCompressionOptions* compressionOptions, int pitchAlignment);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetCompressionOptionsQuantization(NvttCompressionOptions* compressionOptions, NvttBoolean colorDithering, NvttBoolean alphaDithering, NvttBoolean binaryAlpha, int alphaThreshold);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint nvttGetCompressionOptionsD3D9Format([NativeTypeName("const NvttCompressionOptions *")] NvttCompressionOptions* compressionOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttOutputOptions* nvttCreateOutputOptions();

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttDestroyOutputOptions(NvttOutputOptions* outputOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttResetOutputOptions(NvttOutputOptions* outputOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetOutputOptionsFileName(NvttOutputOptions* outputOptions, [NativeTypeName("const char *")] sbyte* fileName);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetOutputOptionsFileHandle(NvttOutputOptions* outputOptions, void* fp);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetOutputOptionsOutputHandler(NvttOutputOptions* outputOptions, [NativeTypeName("nvttBeginImageHandler")] delegate* unmanaged[Cdecl]<int, int, int, int, int, int, void> beginImageHandler, [NativeTypeName("nvttOutputHandler")] delegate* unmanaged[Cdecl]<void*, int, NvttBoolean> outputHandler, [NativeTypeName("nvttEndImageHandler")] IntPtr endImageHandler);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetOutputOptionsErrorHandler(NvttOutputOptions* outputOptions, [NativeTypeName("nvttErrorHandler")] delegate* unmanaged[Cdecl]<NvttError, void> errorHandler);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetOutputOptionsOutputHeader(NvttOutputOptions* outputOptions, NvttBoolean b);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetOutputOptionsContainer(NvttOutputOptions* outputOptions, NvttContainer container);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetOutputOptionsUserVersion(NvttOutputOptions* outputOptions, int version);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetOutputOptionsSrgbFlag(NvttOutputOptions* outputOptions, NvttBoolean b);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttContext* nvttCreateContext();

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttDestroyContext(NvttContext* context);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetContextCudaAcceleration(NvttContext* context, NvttBoolean enable);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttContextIsCudaAccelerationEnabled([NativeTypeName("const NvttContext *")] NvttContext* context);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttContextOutputHeader([NativeTypeName("const NvttContext *")] NvttContext* context, [NativeTypeName("const NvttSurface *")] NvttSurface* img, int mipmapCount, [NativeTypeName("const NvttCompressionOptions *")] NvttCompressionOptions* compressionOptions, [NativeTypeName("const NvttOutputOptions *")] NvttOutputOptions* outputOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttContextCompress([NativeTypeName("const NvttContext *")] NvttContext* context, [NativeTypeName("const NvttSurface *")] NvttSurface* img, int face, int mipmap, [NativeTypeName("const NvttCompressionOptions *")] NvttCompressionOptions* compressionOptions, [NativeTypeName("const NvttOutputOptions *")] NvttOutputOptions* outputOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttContextEstimateSize([NativeTypeName("const NvttContext *")] NvttContext* context, [NativeTypeName("const NvttSurface *")] NvttSurface* img, int mipmapCount, [NativeTypeName("const NvttCompressionOptions *")] NvttCompressionOptions* compressionOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttContextQuantize([NativeTypeName("const NvttContext *")] NvttContext* context, NvttSurface* tex, [NativeTypeName("const NvttCompressionOptions *")] NvttCompressionOptions* compressionOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttContextOutputHeaderCube([NativeTypeName("const NvttContext *")] NvttContext* context, [NativeTypeName("const NvttCubeSurface *")] NvttCubeSurface* img, int mipmapCount, [NativeTypeName("const NvttCompressionOptions *")] NvttCompressionOptions* compressionOptions, [NativeTypeName("const NvttOutputOptions *")] NvttOutputOptions* outputOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttContextCompressCube([NativeTypeName("const NvttContext *")] NvttContext* context, [NativeTypeName("const NvttCubeSurface *")] NvttCubeSurface* img, int mipmap, [NativeTypeName("const NvttCompressionOptions *")] NvttCompressionOptions* compressionOptions, [NativeTypeName("const NvttOutputOptions *")] NvttOutputOptions* outputOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttContextEstimateSizeCube([NativeTypeName("const NvttContext *")] NvttContext* context, [NativeTypeName("const NvttCubeSurface *")] NvttCubeSurface* img, int mipmapCount, [NativeTypeName("const NvttCompressionOptions *")] NvttCompressionOptions* compressionOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttContextOutputHeaderData([NativeTypeName("const NvttContext *")] NvttContext* context, NvttTextureType type, int w, int h, int d, int mipmapCount, NvttBoolean isNormalMap, [NativeTypeName("const NvttCompressionOptions *")] NvttCompressionOptions* compressionOptions, [NativeTypeName("const NvttOutputOptions *")] NvttOutputOptions* outputOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttContextCompressData([NativeTypeName("const NvttContext *")] NvttContext* context, int w, int h, int d, int face, int mipmap, [NativeTypeName("const float *")] float* rgba, [NativeTypeName("const NvttCompressionOptions *")] NvttCompressionOptions* compressionOptions, [NativeTypeName("const NvttOutputOptions *")] NvttOutputOptions* outputOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttContextEstimateSizeData([NativeTypeName("const NvttContext *")] NvttContext* context, int w, int h, int d, int mipmapCount, [NativeTypeName("const NvttCompressionOptions *")] NvttCompressionOptions* compressionOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttContextCompressBatch([NativeTypeName("const NvttContext *")] NvttContext* context, [NativeTypeName("const NvttBatchList *")] NvttBatchList* lst, [NativeTypeName("const NvttCompressionOptions *")] NvttCompressionOptions* compressionOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttContextEnableTiming(NvttContext* context, NvttBoolean enable, int detailLevel);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttTimingContext* nvttContextGetTimingContext(NvttContext* context);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttSurface* nvttCreateSurface();

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttDestroySurface(NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttSurface* nvttSurfaceClone([NativeTypeName("const NvttSurface *")] NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetSurfaceWrapMode(NvttSurface* surface, NvttWrapMode mode);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetSurfaceAlphaMode(NvttSurface* surface, NvttAlphaMode alphaMode);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSetSurfaceNormalMap(NvttSurface* surface, NvttBoolean isNormalMap);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceIsNull([NativeTypeName("const NvttSurface *")] NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttSurfaceWidth([NativeTypeName("const NvttSurface *")] NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttSurfaceHeight([NativeTypeName("const NvttSurface *")] NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttSurfaceDepth([NativeTypeName("const NvttSurface *")] NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttTextureType nvttSurfaceType([NativeTypeName("const NvttSurface *")] NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttWrapMode nvttSurfaceWrapMode([NativeTypeName("const NvttSurface *")] NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttAlphaMode nvttSurfaceAlphaMode([NativeTypeName("const NvttSurface *")] NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceIsNormalMap([NativeTypeName("const NvttSurface *")] NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttSurfaceCountMipmaps([NativeTypeName("const NvttSurface *")] NvttSurface* surface, int min_size);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float nvttSurfaceAlphaTestCoverage([NativeTypeName("const NvttSurface *")] NvttSurface* surface, float alphaRef, int alpha_channel);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float nvttSurfaceAverage([NativeTypeName("const NvttSurface *")] NvttSurface* surface, int channel, int alpha_channel, float gamma);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float* nvttSurfaceData(NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float* nvttSurfaceChannel(NvttSurface* surface, int i);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceHistogram([NativeTypeName("const NvttSurface *")] NvttSurface* surface, int channel, float rangeMin, float rangeMax, int binCount, int* binPtr, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceRange([NativeTypeName("const NvttSurface *")] NvttSurface* surface, int channel, float* rangeMin, float* rangeMax, int alpha_channel, float alpha_ref, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceLoad(NvttSurface* surface, [NativeTypeName("const char *")] sbyte* filename, NvttBoolean* hasAlpha, NvttBoolean expectSigned, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceLoadFromMemory(NvttSurface* surface, [NativeTypeName("const void *")] void* data, [NativeTypeName("unsigned long long")] ulong sizeInBytes, NvttBoolean* hasAlpha, NvttBoolean expectSigned, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceSave([NativeTypeName("const NvttSurface *")] NvttSurface* surface, [NativeTypeName("const char *")] sbyte* fileName, NvttBoolean hasAlpha, NvttBoolean hdr, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceSetImage(NvttSurface* surface, int w, int h, int d, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceSetImageData(NvttSurface* surface, NvttInputFormat format, int w, int h, int d, [NativeTypeName("const void *")] void* data, NvttBoolean unsignedToSigned, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceSetImageRGBA(NvttSurface* surface, NvttInputFormat format, int w, int h, int d, [NativeTypeName("const void *")] void* r, [NativeTypeName("const void *")] void* g, [NativeTypeName("const void *")] void* b, [NativeTypeName("const void *")] void* a, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceSetImage2D(NvttSurface* surface, NvttFormat format, int w, int h, [NativeTypeName("const void *")] void* data, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceSetImage3D(NvttSurface* surface, NvttFormat format, int w, int h, int d, [NativeTypeName("const void *")] void* data, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceResize(NvttSurface* surface, int w, int h, int d, NvttResizeFilter filter, float filterWidth, [NativeTypeName("const float *")] float* @params, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceResizeMax(NvttSurface* surface, int maxExtent, NvttRoundMode mode, NvttResizeFilter filter, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceResizeMaxParams(NvttSurface* surface, int maxExtent, NvttRoundMode mode, NvttResizeFilter filter, float filterWidth, [NativeTypeName("const float *")] float* @params, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceResizeMakeSquare(NvttSurface* surface, int maxExtent, NvttRoundMode mode, NvttResizeFilter filter, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceBuildNextMipmap(NvttSurface* surface, NvttMipmapFilter filter, float filterWidth, [NativeTypeName("const float *")] float* @params, int min_size, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceBuildNextMipmapDefaults(NvttSurface* surface, NvttMipmapFilter filter, int min_size, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceBuildNextMipmapSolidColor(NvttSurface* surface, [NativeTypeName("const float *const")] float* color_components, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceCanvasSize(NvttSurface* surface, int w, int h, int d, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceCanMakeNextMipmap(NvttSurface* surface, int min_size);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToLinear(NvttSurface* surface, float gamma, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToGamma(NvttSurface* surface, float gamma, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToLinearChannel(NvttSurface* surface, int channel, float gamma, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToGammaChannel(NvttSurface* surface, int channel, float gamma, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToSrgb(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToSrgbUnclamped(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToLinearFromSrgb(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToLinearFromSrgbUnclamped(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToXenonSrgb(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToLinearFromXenonSrgb(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceTransform(NvttSurface* surface, [NativeTypeName("const float[4]")] float* w0, [NativeTypeName("const float[4]")] float* w1, [NativeTypeName("const float[4]")] float* w2, [NativeTypeName("const float[4]")] float* w3, [NativeTypeName("const float[4]")] float* offset, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceSwizzle(NvttSurface* surface, int r, int g, int b, int a, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceScaleBias(NvttSurface* surface, int channel, float scale, float bias, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceClamp(NvttSurface* surface, int channel, float low, float high, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceBlend(NvttSurface* surface, float r, float g, float b, float a, float t, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfacePremultiplyAlpha(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceDemultiplyAlpha(NvttSurface* surface, float epsilon, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToGreyScale(NvttSurface* surface, float redScale, float greenScale, float blueScale, float alphaScale, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceSetBorder(NvttSurface* surface, float r, float g, float b, float a, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceFill(NvttSurface* surface, float r, float g, float b, float a, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceScaleAlphaToCoverage(NvttSurface* surface, float coverage, float alphaRef, int alpha_channel, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToRGBM(NvttSurface* surface, float range, float threshold, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceFromRGBM(NvttSurface* surface, float range, float threshold, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToLM(NvttSurface* surface, float range, float threshold, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToRGBE(NvttSurface* surface, int mantissaBits, int exponentBits, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceFromRGBE(NvttSurface* surface, int mantissaBits, int exponentBits, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToYCoCg(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceBlockScaleCoCg(NvttSurface* surface, int bits, float threshold, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceFromYCoCg(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToLUVW(NvttSurface* surface, float range, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceFromLUVW(NvttSurface* surface, float range, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceAbs(NvttSurface* surface, int channel, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceConvolve(NvttSurface* surface, int channel, int kernelSize, float* kernelData, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToLogScale(NvttSurface* surface, int channel, float @base, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceFromLogScale(NvttSurface* surface, int channel, float @base, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceSetAtlasBorder(NvttSurface* surface, int w, int h, float r, float g, float b, float a, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToneMap(NvttSurface* surface, NvttToneMapper tm, float* parameters, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceBinarize(NvttSurface* surface, int channel, float threshold, NvttBoolean dither, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceQuantize(NvttSurface* surface, int channel, int bits, NvttBoolean exactEndPoints, NvttBoolean dither, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToNormalMap(NvttSurface* surface, float sm, float medium, float big, float large, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceNormalizeNormalMap(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceTransformNormals(NvttSurface* surface, NvttNormalTransform xform, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceReconstructNormals(NvttSurface* surface, NvttNormalTransform xform, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToCleanNormalMap(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfacePackNormals(NvttSurface* surface, float scale, float bias, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceExpandNormals(NvttSurface* surface, float scale, float bias, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttSurface* nvttSurfaceCreateToksvigMap([NativeTypeName("const NvttSurface *")] NvttSurface* surface, float power, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttSurface* nvttSurfaceCreateCleanMap([NativeTypeName("const NvttSurface *")] NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceFlipX(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceFlipY(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceFlipZ(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttSurface* nvttSurfaceCreateSubImage([NativeTypeName("const NvttSurface *")] NvttSurface* surface, int x0, int x1, int y0, int y1, int z0, int z1, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceCopyChannel(NvttSurface* surface, [NativeTypeName("const NvttSurface *")] NvttSurface* srcImage, int srcChannel, int dstChannel, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceAddChannel(NvttSurface* surface, [NativeTypeName("const NvttSurface *")] NvttSurface* srcImage, int srcChannel, int dstChannel, float scale, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceCopy(NvttSurface* surface, [NativeTypeName("const NvttSurface *")] NvttSurface* srcImage, int xsrc, int ysrc, int zsrc, int xsize, int ysize, int zsize, int xdst, int ydst, int zdst, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToGPU(NvttSurface* surface, NvttBoolean performCopy, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttSurfaceToCPU(NvttSurface* surface, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const float *")]
        public static extern float* nvttSurfaceGPUData([NativeTypeName("const NvttSurface *")] NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float* nvttSurfaceGPUDataMutable(NvttSurface* surface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttSurfaceSet* nvttCreateSurfaceSet();

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttDestroySurfaceSet(NvttSurfaceSet* surfaceSet);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttResetSurfaceSet(NvttSurfaceSet* surfaceSet);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttTextureType nvttSurfaceSetGetTextureType(NvttSurfaceSet* surfaceSet);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttSurfaceSetGetFaceCount(NvttSurfaceSet* surfaceSet);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttSurfaceSetGetMipmapCount(NvttSurfaceSet* surfaceSet);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttSurfaceSetGetWidth(NvttSurfaceSet* surfaceSet);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttSurfaceSetGetHeight(NvttSurfaceSet* surfaceSet);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttSurfaceSetGetDepth(NvttSurfaceSet* surfaceSet);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttSurface* nvttSurfaceSetGetSurface(NvttSurfaceSet* surfaceSet, int faceId, int mipId, NvttBoolean expectSigned);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceSetLoadDDS(NvttSurfaceSet* surfaceSet, [NativeTypeName("const char *")] sbyte* fileName, NvttBoolean forcenormal);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceSetLoadDDSFromMemory(NvttSurfaceSet* surfaceSet, [NativeTypeName("const void *")] void* data, [NativeTypeName("unsigned long long")] ulong sizeInBytes, NvttBoolean forcenormal);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSurfaceSetSaveImage(NvttSurfaceSet* surfaceSet, [NativeTypeName("const char *")] sbyte* fileName, int faceId, int mipId);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttCubeSurface* nvttCreateCubeSurface();

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttDestroyCubeSurface(NvttCubeSurface* cubeSurface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttCubeSurfaceIsNull([NativeTypeName("const NvttCubeSurface *")] NvttCubeSurface* cubeSurface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttCubeSurfaceEdgeLength([NativeTypeName("const NvttCubeSurface *")] NvttCubeSurface* cubeSurface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttCubeSurfaceCountMipmaps([NativeTypeName("const NvttCubeSurface *")] NvttCubeSurface* cubeSurface);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttCubeSurfaceLoad(NvttCubeSurface* cubeSurface, [NativeTypeName("const char *")] sbyte* fileName, int mipmap);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttCubeSurfaceLoadFromMemory(NvttCubeSurface* cubeSurface, [NativeTypeName("const void *")] void* data, [NativeTypeName("unsigned long long")] ulong sizeInBytes, int mipmap);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttCubeSurfaceSave(NvttCubeSurface* cubeSurface, [NativeTypeName("const char *")] sbyte* fileName);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttSurface* nvttCubeSurfaceFace(NvttCubeSurface* cubeSurface, int face);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttCubeSurfaceFold(NvttCubeSurface* cubeSurface, [NativeTypeName("const NvttSurface *")] NvttSurface* img, NvttCubeLayout layout);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttSurface* nvttCubeSurfaceUnfold([NativeTypeName("const NvttCubeSurface *")] NvttCubeSurface* cubeSurface, NvttCubeLayout layout);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float nvttCubeSurfaceAverage(NvttCubeSurface* cubeSurface, int channel);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttCubeSurfaceRange([NativeTypeName("const NvttCubeSurface *")] NvttCubeSurface* cubeSurface, int channel, float* minimum_ptr, float* maximum_ptr);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttCubeSurfaceClamp(NvttCubeSurface* cubeSurface, int channel, float low, float high);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttCubeSurface* nvttCubeSurfaceIrradianceFilter([NativeTypeName("const NvttCubeSurface *")] NvttCubeSurface* cubeSurface, int size, [NativeTypeName("NvttEdgeFixup")] EdgeFixup fixupMethod);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttCubeSurface* nvttCubeSurfaceCosinePowerFilter([NativeTypeName("const NvttCubeSurface *")] NvttCubeSurface* cubeSurface, int size, float cosinePower, [NativeTypeName("NvttEdgeFixup")] EdgeFixup fixupMethod);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttCubeSurface* nvttCubeSurfaceFastResample([NativeTypeName("const NvttCubeSurface *")] NvttCubeSurface* cubeSurface, int size, [NativeTypeName("NvttEdgeFixup")] EdgeFixup fixupMethod);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttCubeSurfaceToLinear(NvttCubeSurface* cubeSurface, float gamma);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttCubeSurfaceToGamma(NvttCubeSurface* cubeSurface, float gamma);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBatchList* nvttCreateBatchList();

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttDestroyBatchList(NvttBatchList* batchList);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttBatchListClear(NvttBatchList* batchList);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttBatchListAppend(NvttBatchList* batchList, [NativeTypeName("const NvttSurface *")] NvttSurface* pImg, int face, int mipmap, [NativeTypeName("const NvttOutputOptions *")] NvttOutputOptions* outputOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint nvttBatchListGetSize([NativeTypeName("const NvttBatchList *")] NvttBatchList* batchList);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttBatchListGetItem([NativeTypeName("const NvttBatchList *")] NvttBatchList* batchList, [NativeTypeName("unsigned int")] uint i, [NativeTypeName("const NvttSurface **")] NvttSurface** pImg, int* face, int* mipmap, [NativeTypeName("const NvttOutputOptions **")] NvttOutputOptions** outputOptions);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* nvttErrorString(NvttError e);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint nvttVersion();

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttBoolean nvttSetMessageCallback([NativeTypeName("nvttMessageCallback")] delegate* unmanaged[Cdecl]<NvttSeverity, NvttError, sbyte*, void*, void> callback, [NativeTypeName("const void *")] void* userData);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float nvttRmsError([NativeTypeName("const NvttSurface *")] NvttSurface* reference, [NativeTypeName("const NvttSurface *")] NvttSurface* img, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float nvttRmsAlphaError([NativeTypeName("const NvttSurface *")] NvttSurface* reference, [NativeTypeName("const NvttSurface *")] NvttSurface* img, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float nvttRmsCIELabError([NativeTypeName("const NvttSurface *")] NvttSurface* reference, [NativeTypeName("const NvttSurface *")] NvttSurface* img, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float nvttAngularError([NativeTypeName("const NvttSurface *")] NvttSurface* reference, [NativeTypeName("const NvttSurface *")] NvttSurface* img, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttSurface* nvttDiff([NativeTypeName("const NvttSurface *")] NvttSurface* reference, [NativeTypeName("const NvttSurface *")] NvttSurface* img, float scale, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float nvttRmsToneMappedError([NativeTypeName("const NvttSurface *")] NvttSurface* reference, [NativeTypeName("const NvttSurface *")] NvttSurface* img, float exposure, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttSurface* nvttHistogram([NativeTypeName("const NvttSurface *")] NvttSurface* img, int width, int height, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttSurface* nvttHistogramRange([NativeTypeName("const NvttSurface *")] NvttSurface* img, float minRange, float maxRange, int width, int height, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttGetTargetExtent(int* width, int* height, int* depth, int maxExtent, NvttRoundMode roundMode, NvttTextureType textureType, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttCountMipmaps(int w, int h, int d, NvttTimingContext* tc);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NvttTimingContext* nvttCreateTimingContext(int detailLevel);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttDestroyTimingContext(NvttTimingContext* timingContext);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttTimingContextSetDetailLevel(NvttTimingContext* timingContext, int detailLevel);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int nvttTimingContextGetRecordCount(NvttTimingContext* timingContext);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttTimingContextGetRecord(NvttTimingContext* timingContext, int i, [NativeTypeName("char *")] sbyte* description, double* seconds);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint nvttTimingContextGetRecordSafe(NvttTimingContext* timingContext, int i, [NativeTypeName("char *")] sbyte* outDescription, [NativeTypeName("size_t")] nuint outDescriptionSize, double* seconds);

        [DllImport("nvtt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void nvttTimingContextPrintRecords(NvttTimingContext* timingContext);

        [NativeTypeName("#define NVTT_VERSION 30203")]
        public const int NVTT_VERSION = 30203;

        [NativeTypeName("#define NVTT_EncodeSettings_Version_1 1")]
        public const int NVTT_EncodeSettings_Version_1 = 1;
    }
}
