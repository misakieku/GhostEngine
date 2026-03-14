namespace Ghost.Ufbx;

public unsafe struct MaterialFeatures
{
    private ufbx_material_features* _ptr;

    internal MaterialFeatures(ufbx_material_features* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public MaterialFeatureInfo Pbr => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->pbr));

    public MaterialFeatureInfo Metalness => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->metalness));

    public MaterialFeatureInfo Diffuse => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->diffuse));

    public MaterialFeatureInfo Specular => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->specular));

    public MaterialFeatureInfo Emission => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->emission));

    public MaterialFeatureInfo Transmission => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission));

    public MaterialFeatureInfo Coat => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->coat));

    public MaterialFeatureInfo Sheen => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->sheen));

    public MaterialFeatureInfo Opacity => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->opacity));

    public MaterialFeatureInfo AmbientOcclusion => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->ambient_occlusion));

    public MaterialFeatureInfo Matte => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->matte));

    public MaterialFeatureInfo Unlit => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->unlit));

    public MaterialFeatureInfo Ior => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->ior));

    public MaterialFeatureInfo DiffuseRoughness => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->diffuse_roughness));

    public MaterialFeatureInfo TransmissionRoughness => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_roughness));

    public MaterialFeatureInfo ThinWalled => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->thin_walled));

    public MaterialFeatureInfo Caustics => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->caustics));

    public MaterialFeatureInfo ExitToBackground => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->exit_to_background));

    public MaterialFeatureInfo InternalReflections => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->internal_reflections));

    public MaterialFeatureInfo DoubleSided => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->double_sided));

    public MaterialFeatureInfo RoughnessAsGlossiness => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->roughness_as_glossiness));

    public MaterialFeatureInfo CoatRoughnessAsGlossiness => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->coat_roughness_as_glossiness));

    public MaterialFeatureInfo TransmissionRoughnessAsGlossiness => new((ufbx_material_feature_info*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_roughness_as_glossiness));

    internal ufbx_material_features* GetUnsafePtr() => _ptr;
}
