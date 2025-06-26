using System;
using System.IO;
using System.Security.Cryptography;

namespace Plexity.Utility
{
    public static class MD5Hash
    {
        public static string FromBytes(byte[] data)
        {
            using MD5 md5 = MD5.Create();
            return Stringify(md5.ComputeHash(data));
        }

        public static string FromStream(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin); // Reset stream position to ensure correct hash
            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(stream);
            return Stringify(hash);
        }

        public static string FromFile(string filename)
        {
            using FileStream stream = File.OpenRead(filename);
            return FromStream(stream); // Stream is passed to FromStream, which handles hashing
        }

        public static string Stringify(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        internal static string Stringify(object value)
        {
            throw new NotImplementedException();
        }
    }
}
