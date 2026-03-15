namespace Ghost.MeshOptimizer
{
    public enum meshopt_SimplifyOptions
    {
        meshopt_SimplifyLockBorder = 1 << 0,
        meshopt_SimplifySparse = 1 << 1,
        meshopt_SimplifyErrorAbsolute = 1 << 2,
        meshopt_SimplifyPrune = 1 << 3,
        meshopt_SimplifyRegularize = 1 << 4,
        meshopt_SimplifyPermissive = 1 << 5,
    }
}
