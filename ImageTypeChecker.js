const imageHeaders = {
  "image/jpeg": new Uint8Array([0xFF, 0xD8]), // JPEG
  "image/bmp": new Uint8Array([0x42, 0x4D]), // BMP
  "image/gif": new Uint8Array([0x47, 0x49, 0x46]), // GIF
  "image/png": new Uint8Array([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]), // PNG
};

function isEqualArray(a, b) 
{
  if (a.length !== b.length)
  {
    return false;
  }
  for (let i = 0; i < a.length; i++) 
  {
    if (a[i] !== b[i])
    {
      return false;
    }
  }
  return true;
}

class ImageTypeChecker {}

// data is expected to be javascript UInt8Array or NodeJS Buffer class
// returns string mimetype like "image/png"
// TODO: return null if image type not supported
ImageTypeChecker.GetImageMimeType = (data) =>
{
  for (const [key, value] of Object.entries(imageHeaders))
  {
    if (isEqualArray(data.slice(0, value.length), value))
    {
      return key;
    }
  }
  throw "The given file data does not match any known image type";
};

module.exports = {
   ImageTypeChecker: ImageTypeChecker
}