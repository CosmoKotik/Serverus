using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Serverus.Api
{
    public class Deserialize
    {
        private byte[] _key;
        private byte[] _IV;

        public Deserialize(byte[] key, byte[] IV)
        {
            this._key = key;
            this._IV = IV;
        }

        private string Decrypt(byte[] text)
        {
            byte[] Key = this._key;
            byte[] IV = this._IV;

            // Check arguments.
            if (text == null || text.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(text))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    // Read the decrypted bytes from the decrypting stream
                    // and place them in a string.
                    plaintext = srDecrypt.ReadToEnd();
                }
            }

            return plaintext;
        }

        public void DeserializeProperty(byte[] msg)
        {
            string decrypted = Decrypt(msg);

            Data deserializedData = JsonConvert.DeserializeObject<Data>(decrypted);
        }
    }
}
