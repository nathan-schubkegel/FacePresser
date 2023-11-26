
public static class ImageTypeChecker
{
  private static readonly Dictionary<string, byte[]> _imageHeaders = new Dictionary<string, byte[]>
  {
    { "image/jpeg", new byte[]{ 0xFF, 0xD8 }}, // JPEG
    { "image/bmp", new byte[]{ 0x42, 0x4D }}, // BMP
    { "image/gif", new byte[]{ 0x47, 0x49, 0x46 }}, // GIF
    { "image/png", new byte[]{ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }}, // PNG
  };

  public static string GetImageMimeType(byte[] data)
  {
    foreach ((var imageType, var header) in _imageHeaders)
    {
      if (data.Take(header.Length).SequenceEqual(header))
      {
        return imageType;
      }
    }

    throw new Exception("The given file data does not match any known image type");
  }
}
