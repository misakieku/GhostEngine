namespace Ghost.Graphics.Utilities;
public class TextureUtility
{
    public static uint CountMips(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return 0;
        }

        uint count = 1;
        while (width > 1 || height > 1)
        {
            width >>= 1;
            height >>= 1;
            count++;
        }

        return count;
    }
}
