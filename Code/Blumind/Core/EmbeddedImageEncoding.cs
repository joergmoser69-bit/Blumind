using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Blumind.Core
{
    // GDI+ can round translucent RGB channels when re-encoding PNGs. Preserve the original
    // compressed data while the decoded bitmap is unchanged, without keeping images alive.
    static class EmbeddedImageEncoding
    {
        sealed record EncodedImage(byte[] Bytes, byte[] PixelHash);
        static readonly ConditionalWeakTable<Image, EncodedImage> Originals = new();

        public static Image Decode(byte[] bytes)
        {
            using var input = new MemoryStream(bytes);
            using var decoded = Image.FromStream(input);
            var image = (Image)decoded.Clone();
            if (image is Bitmap bitmap)
                Originals.Add(image, new EncodedImage(bytes, Hash(bitmap)));
            return image;
        }

        public static bool TryGetOriginal(Image image, out byte[] bytes)
        {
            bytes = null;
            if (image is Bitmap bitmap && Originals.TryGetValue(image, out var original) &&
                CryptographicOperations.FixedTimeEquals(original.PixelHash, Hash(bitmap)))
            {
                bytes = original.Bytes;
                return true;
            }
            return false;
        }

        static byte[] Hash(Bitmap bitmap)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            hash.AppendData(BitConverter.GetBytes(bitmap.Width));
            hash.AppendData(BitConverter.GetBytes(bitmap.Height));
            var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                byte[] row = new byte[checked(bitmap.Width * 4)];
                for (int y = 0; y < bitmap.Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(data.Scan0, checked(y * data.Stride)), row, 0, row.Length);
                    hash.AppendData(row);
                }
            }
            finally { bitmap.UnlockBits(data); }
            return hash.GetHashAndReset();
        }
    }
}
