namespace Ghost.Ufbx;

public unsafe struct MaterialPbrMaps
{
    private ufbx_material_pbr_maps* _ptr;

    internal MaterialPbrMaps(ufbx_material_pbr_maps* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public MaterialMap BaseFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->base_factor));

    public MaterialMap BaseColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->base_color));

    public MaterialMap Roughness => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->roughness));

    public MaterialMap Metalness => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->metalness));

    public MaterialMap DiffuseRoughness => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->diffuse_roughness));

    public MaterialMap SpecularFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->specular_factor));

    public MaterialMap SpecularColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->specular_color));

    public MaterialMap SpecularIor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->specular_ior));

    public MaterialMap SpecularAnisotropy => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->specular_anisotropy));

    public MaterialMap SpecularRotation => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->specular_rotation));

    public MaterialMap TransmissionFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_factor));

    public MaterialMap TransmissionColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_color));

    public MaterialMap TransmissionDepth => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_depth));

    public MaterialMap TransmissionScatter => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_scatter));

    public MaterialMap TransmissionScatterAnisotropy => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_scatter_anisotropy));

    public MaterialMap TransmissionDispersion => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_dispersion));

    public MaterialMap TransmissionRoughness => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_roughness));

    public MaterialMap TransmissionExtraRoughness => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_extra_roughness));

    public MaterialMap TransmissionPriority => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_priority));

    public MaterialMap TransmissionEnableInAov => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_enable_in_aov));

    public MaterialMap SubsurfaceFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->subsurface_factor));

    public MaterialMap SubsurfaceColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->subsurface_color));

    public MaterialMap SubsurfaceRadius => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->subsurface_radius));

    public MaterialMap SubsurfaceScale => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->subsurface_scale));

    public MaterialMap SubsurfaceAnisotropy => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->subsurface_anisotropy));

    public MaterialMap SubsurfaceTintColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->subsurface_tint_color));

    public MaterialMap SubsurfaceType => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->subsurface_type));

    public MaterialMap SheenFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->sheen_factor));

    public MaterialMap SheenColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->sheen_color));

    public MaterialMap SheenRoughness => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->sheen_roughness));

    public MaterialMap CoatFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->coat_factor));

    public MaterialMap CoatColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->coat_color));

    public MaterialMap CoatRoughness => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->coat_roughness));

    public MaterialMap CoatIor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->coat_ior));

    public MaterialMap CoatAnisotropy => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->coat_anisotropy));

    public MaterialMap CoatRotation => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->coat_rotation));

    public MaterialMap CoatNormal => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->coat_normal));

    public MaterialMap CoatAffectBaseColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->coat_affect_base_color));

    public MaterialMap CoatAffectBaseRoughness => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->coat_affect_base_roughness));

    public MaterialMap ThinFilmFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->thin_film_factor));

    public MaterialMap ThinFilmThickness => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->thin_film_thickness));

    public MaterialMap ThinFilmIor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->thin_film_ior));

    public MaterialMap EmissionFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->emission_factor));

    public MaterialMap EmissionColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->emission_color));

    public MaterialMap Opacity => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->opacity));

    public MaterialMap IndirectDiffuse => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->indirect_diffuse));

    public MaterialMap IndirectSpecular => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->indirect_specular));

    public MaterialMap NormalMap => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->normal_map));

    public MaterialMap TangentMap => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->tangent_map));

    public MaterialMap DisplacementMap => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->displacement_map));

    public MaterialMap MatteFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->matte_factor));

    public MaterialMap MatteColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->matte_color));

    public MaterialMap AmbientOcclusion => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->ambient_occlusion));

    public MaterialMap Glossiness => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->glossiness));

    public MaterialMap CoatGlossiness => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->coat_glossiness));

    public MaterialMap TransmissionGlossiness => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transmission_glossiness));

    internal ufbx_material_pbr_maps* GetUnsafePtr() => _ptr;
}
