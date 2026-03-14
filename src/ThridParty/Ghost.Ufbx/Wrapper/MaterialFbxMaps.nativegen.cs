namespace Ghost.Ufbx;

public unsafe struct MaterialFbxMaps
{
    private ufbx_material_fbx_maps* _ptr;

    internal MaterialFbxMaps(ufbx_material_fbx_maps* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public MaterialMap DiffuseFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->diffuse_factor));

    public MaterialMap DiffuseColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->diffuse_color));

    public MaterialMap SpecularFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->specular_factor));

    public MaterialMap SpecularColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->specular_color));

    public MaterialMap SpecularExponent => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->specular_exponent));

    public MaterialMap ReflectionFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->reflection_factor));

    public MaterialMap ReflectionColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->reflection_color));

    public MaterialMap TransparencyFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transparency_factor));

    public MaterialMap TransparencyColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transparency_color));

    public MaterialMap EmissionFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->emission_factor));

    public MaterialMap EmissionColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->emission_color));

    public MaterialMap AmbientFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->ambient_factor));

    public MaterialMap AmbientColor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->ambient_color));

    public MaterialMap NormalMap => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->normal_map));

    public MaterialMap Bump => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->bump));

    public MaterialMap BumpFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->bump_factor));

    public MaterialMap DisplacementFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->displacement_factor));

    public MaterialMap Displacement => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->displacement));

    public MaterialMap VectorDisplacementFactor => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vector_displacement_factor));

    public MaterialMap VectorDisplacement => new((ufbx_material_map*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vector_displacement));

    internal ufbx_material_fbx_maps* GetUnsafePtr() => _ptr;
}
