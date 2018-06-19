//
// Copyright (c) 2018 Rodrigo Zechin Rosauro
//
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace JiraKanbanMetrics.Core
{
    /// <summary>
    /// Very simple encryption mechanism
    /// </summary>
    internal static class Crypto
    {
        // Super-secure keys
        // Yes, I know this is not high security.
        // Users will be warned to not store passwords if they want security
        private static readonly byte[] Key = {79, 0, 170, 233, 227, 59, 196, 117};
        private static readonly byte[] Iv = {195, 140, 22, 78, 176, 173, 104, 92};

        /// <summary>
        /// Encrypts the value of a .NET SecureString using our super-secure keys
        /// </summary>
        /// <param name="value">the string</param>
        /// <returns>encrytpted data, in base64</returns>
        public static string Encrypt(SecureString value)
        {
            var algorithm = DES.Create();
            var transform = algorithm.CreateEncryptor(Key, Iv);
            var inputbuffer = Encoding.Unicode.GetBytes(GetStr(value));
            var outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
            return Convert.ToBase64String(outputBuffer);
        }

        /// <summary>
        /// Decrypts a content previously encrypted by <see cref="Encrypt"/>
        /// </summary>
        /// <param name="base64Encrypted">the content</param>
        /// <returns>decrypted SecureString</returns>
        public static SecureString Decrypt(string base64Encrypted)
        {
            var algorithm = DES.Create();
            var transform = algorithm.CreateDecryptor(Key, Iv);
            var inputbuffer = Convert.FromBase64String(base64Encrypted);
            var outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
            var s = new SecureString();
            foreach (var c in Encoding.Unicode.GetString(outputBuffer))
                s.AppendChar(c);
            return s;
        }

        /// <summary>
        /// Retrieves the raw contents of a SecureString
        /// </summary>
        /// <param name="value">the SecureString</param>
        /// <returns>raw contents</returns>
        public static string GetStr(SecureString value)
        {
            var valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}