using Ghost.MicroTest.Core;
using Ghost.StbI;

namespace Ghost.MicroTest;

internal class StbIBindingTest : ITest
{
    public void Setup()
    {
    }

    public unsafe void Run()
    {
        using var stream = File.OpenRead("C:\\Users\\Misaki\\Downloads\\Screenshot 2024-07-20 035047.png");
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);

        int width, height, channels;
        var buff = StbIApi.LoadFromMemory(bytes, &width, &height, &channels, 4);
        if (buff == null)
        {
            Console.WriteLine("Failed to load image");
            return;
        }

        try
        {
            Console.WriteLine($"Image loaded: {width}x{height}, channels: {channels}");

            var expectedColor = (Span<byte>)stackalloc byte[] { 122, 145, 224, 255 };
            var firstPixel = new Span<byte>(buff, 4);

            Console.WriteLine("First pixel RGBA: " + string.Join(", ", firstPixel.ToArray()));
            Console.WriteLine("Expected RGBA: " + string.Join(", ", expectedColor.ToArray()));

            if (!firstPixel.SequenceEqual(expectedColor))
            {
                Console.WriteLine("First pixel does not match expected color");
            }
            else
            {
                Console.WriteLine("First pixel matches expected color");
            }

            firstPixel.Fill(0xFF);

            int result;
            var newFilePath = "C:\\Users\\Misaki\\Downloads\\ModifiedImage.jpg"u8;
            fixed (byte* pathPtr = newFilePath)
            {
                result = StbIApi.WriteJpg((sbyte*)pathPtr, width, height, 4, buff, 90);
            }

            if (result == 0)
            {
                Console.WriteLine("Failed to write image");
            }
            else
            {
                Console.WriteLine("Image written successfully");
            }
        }
        finally
        {
            StbIApi.ImageFree(buff);
        }
    }

    public void Cleanup()
    {
    }
}
