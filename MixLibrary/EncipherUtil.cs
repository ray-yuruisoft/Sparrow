using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace MixLibrary
{
    public static class EncipherUtil
    {
        public static string NewGuid()
        {
            return Guid.NewGuid().ToString("N");
        }
        public static string Md5(this string str)
        {
            return HashAlgorithmBase(MD5.Create(), str, Encoding.UTF8);
        }

        static string Bytes2Str(this IEnumerable<byte> bytes, string formatStr = "{0:x2}")
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.AppendFormat(formatStr, b);
            }
            return sb.ToString();
        }

        static string HashAlgorithmBase(HashAlgorithm hashAlgorithmObj, string str, Encoding encoding)
        {
            byte[] bytes = encoding.GetBytes(str);
            byte[] hashStr = hashAlgorithmObj.ComputeHash(bytes);
            return hashStr.Bytes2Str();
        }

        /// <summary>  
        /// AES Byte类型 加密  
        /// </summary>  
        /// <param name="data">待加密明文</param>  
        /// <param name="keyVal">密钥值（32字节）</param>  
        /// <param name="ivVal">加密辅助向量（16字节）</param>  
        /// <returns></returns>  
        public static byte[] AesEncrypt(this byte[] data, byte[] keyVal, byte[] ivVal)
        {
            byte[] cryptograph;
            Rijndael aes = Rijndael.Create();
            try
            {
                using (MemoryStream mStream = new MemoryStream())
                {
                    using (CryptoStream cStream = new CryptoStream(mStream, aes.CreateEncryptor(keyVal, ivVal), CryptoStreamMode.Write))
                    {
                        cStream.Write(data, 0, data.Length);
                        cStream.FlushFinalBlock();
                        cryptograph = mStream.ToArray();
                    }
                }
            }
            catch(Exception)
            {
                cryptograph = null;
            }
            return cryptograph;
        }

        /// <summary>  
        /// AES Byte类型 解密  
        /// </summary>  
        /// <param name="data">待解密明文</param>  
        /// <param name="keyVal">密钥值（32字节）</param>  
        /// <param name="ivVal">加密辅助向量（16字节）</param> 
        /// <returns></returns>
        public static byte[] AesDecrypt(this byte[] data, byte[] keyVal, byte[] ivVal)
        {
            byte[] original;
            Rijndael aes = Rijndael.Create();
            try
            {
                using (MemoryStream mStream = new MemoryStream(data))
                {
                    using (CryptoStream cStream = new CryptoStream(mStream, aes.CreateDecryptor(keyVal, ivVal), CryptoStreamMode.Read))
                    {
                        using (MemoryStream originalMemory = new MemoryStream())
                        {
                            byte[] buffer = new byte[1024];
                            int readBytes;
                            while ((readBytes = cStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                originalMemory.Write(buffer, 0, readBytes);
                            }

                            original = originalMemory.ToArray();
                        }
                    }
                }
            }
            catch (Exception)
            {
                original = null;
            }
            return original;
        }
    }
}
