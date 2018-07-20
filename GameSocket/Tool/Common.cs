using System;
using System.Security.Cryptography;
using System.Text;

namespace GameSocket
{
    internal class Common
    {
        public static byte[] Int32ToBytes(int n)
        {
            byte[] b = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                b[i] = (byte)(n >> (24 - i * 8));
            }
            return b;
        }

        public static int BytesToInt32(byte[] src, int offset)
        {
            int value;
            value = ((src[offset] & 0xFF) << 24)
                    | ((src[offset + 1] & 0xFF) << 16)
                    | ((src[offset + 2] & 0xFF) << 8)
                    | (src[offset + 3] & 0xFF);
            return value;
        }

        /// <summary>
        /// 字节数组转化为字符串
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static string BytesToString(byte[] src)
        {
            return Encoding.UTF8.GetString(src);
        }

        /// <summary>
        /// 字符串转化为字节数组
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] StringToBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="encryptStr">明文</param>
        /// <param name="key">密钥</param>
        /// <returns></returns>
        public static string AESEncrypt(string encryptStr, string key)
        {
            if (!String.IsNullOrEmpty(key))
            {
                byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
                byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(encryptStr);
                RijndaelManaged rDel = new RijndaelManaged
                {
                    Key = keyArray,
                    Mode = CipherMode.ECB,
                    Padding = PaddingMode.PKCS7
                };
                ICryptoTransform cTransform = rDel.CreateEncryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
                return Convert.ToBase64String(resultArray, 0, resultArray.Length);
            }
            else
                return encryptStr;
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="decryptStr">密文</param>
        /// <param name="key">密钥</param>
        /// <returns></returns>
        public static string AESDecrypt(string decryptStr, string key)
        {
            if (!String.IsNullOrEmpty(key))
            {
                byte[] keyArray = Encoding.UTF8.GetBytes(key);
                byte[] toEncryptArray = Convert.FromBase64String(decryptStr);
                RijndaelManaged rDel = new RijndaelManaged
                {
                    Key = keyArray,
                    Mode = CipherMode.ECB,
                    Padding = PaddingMode.PKCS7
                };
                ICryptoTransform cTransform = rDel.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
                return Encoding.UTF8.GetString(resultArray);
            }
            else
                return decryptStr;
        }
    }
}
