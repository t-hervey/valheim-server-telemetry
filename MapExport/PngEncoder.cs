using System;
using System.IO;
using System.Text;

namespace ValheimTelemetry.MapExport
{
    internal static class PngEncoder
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        public static byte[] EncodeRgb(int width, int height, byte[] rgb)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (rgb == null) throw new ArgumentNullException(nameof(rgb));
            if (rgb.Length != width * height * 3) throw new ArgumentException("RGB buffer length does not match the image dimensions.", nameof(rgb));

            using (var output = new MemoryStream())
            {
                output.Write(Signature, 0, Signature.Length);
                byte[] header = new byte[13];
                WriteUInt32(header, 0, (uint)width);
                WriteUInt32(header, 4, (uint)height);
                header[8] = 8;
                header[9] = 2;
                WriteChunk(output, "IHDR", header);
                WriteChunk(output, "IDAT", ZlibStore(BuildScanlines(width, height, rgb)));
                WriteChunk(output, "IEND", Array.Empty<byte>());
                return output.ToArray();
            }
        }

        private static byte[] BuildScanlines(int width, int height, byte[] rgb)
        {
            int rowBytes = width * 3;
            byte[] scanlines = new byte[(rowBytes + 1) * height];
            for (int y = 0; y < height; y++)
            {
                int target = y * (rowBytes + 1);
                scanlines[target] = 0;
                Buffer.BlockCopy(rgb, y * rowBytes, scanlines, target + 1, rowBytes);
            }
            return scanlines;
        }

        private static byte[] ZlibStore(byte[] data)
        {
            using (var output = new MemoryStream(data.Length + data.Length / 65535 * 5 + 16))
            {
                output.WriteByte(0x78);
                output.WriteByte(0x01);
                int offset = 0;
                while (offset < data.Length)
                {
                    int length = Math.Min(65535, data.Length - offset);
                    bool final = offset + length == data.Length;
                    output.WriteByte(final ? (byte)1 : (byte)0);
                    output.WriteByte((byte)length);
                    output.WriteByte((byte)(length >> 8));
                    int complement = (~length) & 0xffff;
                    output.WriteByte((byte)complement);
                    output.WriteByte((byte)(complement >> 8));
                    output.Write(data, offset, length);
                    offset += length;
                }
                uint adler = Adler32(data);
                WriteUInt32(output, adler);
                return output.ToArray();
            }
        }

        private static void WriteChunk(Stream output, string type, byte[] data)
        {
            WriteUInt32(output, (uint)data.Length);
            byte[] typeBytes = Encoding.ASCII.GetBytes(type);
            output.Write(typeBytes, 0, typeBytes.Length);
            output.Write(data, 0, data.Length);
            uint crc = 0xffffffffu;
            crc = UpdateCrc(crc, typeBytes);
            crc = UpdateCrc(crc, data) ^ 0xffffffffu;
            WriteUInt32(output, crc);
        }

        private static uint UpdateCrc(uint crc, byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? 0xedb88320u ^ (crc >> 1) : crc >> 1;
            }
            return crc;
        }

        private static uint Adler32(byte[] data)
        {
            const uint modulus = 65521;
            uint a = 1;
            uint b = 0;
            for (int i = 0; i < data.Length; i++)
            {
                a = (a + data[i]) % modulus;
                b = (b + a) % modulus;
            }
            return (b << 16) | a;
        }

        private static void WriteUInt32(Stream output, uint value)
        {
            output.WriteByte((byte)(value >> 24));
            output.WriteByte((byte)(value >> 16));
            output.WriteByte((byte)(value >> 8));
            output.WriteByte((byte)value);
        }

        private static void WriteUInt32(byte[] output, int offset, uint value)
        {
            output[offset] = (byte)(value >> 24);
            output[offset + 1] = (byte)(value >> 16);
            output[offset + 2] = (byte)(value >> 8);
            output[offset + 3] = (byte)value;
        }
    }
}
