using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Astroid.Core
{
	public static class ACrypto
	{
		private const string Key = "+/*?|1453|?*/+";

		public static string Encrypt(string text, bool useHashing = true)
		{
			var keyArray = GetKeyArray(useHashing);

			using var aes = new AesCryptoServiceProvider
			{
				Key = keyArray,
				Mode = CipherMode.ECB,
				Padding = PaddingMode.PKCS7
			};
			var enc = aes.CreateEncryptor();
			var textBytes = Encoding.UTF8.GetBytes(text);
			var resultArray = enc.TransformFinalBlock(textBytes, 0, textBytes.Length);
			aes.Clear();
			var base64 = Convert.ToBase64String(resultArray, 0, resultArray.Length);

			return base64;
		}

		public static string Decrypt(string text, bool useHashing = true)
		{
			try
			{
				var keyArray = GetKeyArray(useHashing);

				using var aes = new AesCryptoServiceProvider
				{
					Key = keyArray,
					Mode = CipherMode.ECB,
					Padding = PaddingMode.PKCS7
				};
				var toEncryptArray = Convert.FromBase64String(text);
				var dec = aes.CreateDecryptor();
				var resultArray = dec.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
				aes.Clear();

				return Encoding.UTF8.GetString(resultArray);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Decryption Error", ex);
			}
		}

		private static byte[] GetKeyArray(bool useHashing)
		{
			byte[] keyArray;

			if (useHashing)
			{
				using var hash = new MD5CryptoServiceProvider();
				keyArray = hash.ComputeHash(Encoding.UTF8.GetBytes(Key));
				hash.Clear();
			}
			else keyArray = Encoding.UTF8.GetBytes(Key);

			return keyArray;
		}

		public static string Hash(string text, string hashKey, HashAlgorithm? algorithm = default)
		{
			const int saltSize = 1024;
			algorithm ??= SHA512.Create();
			text ??= string.Empty;
			hashKey ??= string.Empty;
			text = $"[ClearText:{text}][HashKey:{hashKey}]";

			var encoding = new UTF8Encoding();
			var binarySaltValue = new byte[saltSize];

			binarySaltValue[0] = byte.Parse(hashKey.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture.NumberFormat);
			binarySaltValue[1] = byte.Parse(hashKey.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture.NumberFormat);
			binarySaltValue[2] = byte.Parse(hashKey.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture.NumberFormat);
			binarySaltValue[3] = byte.Parse(hashKey.AsSpan(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture.NumberFormat);

			var valueToHash = new byte[saltSize + encoding.GetByteCount(text)];
			var binaryPassword = encoding.GetBytes(text);

			binarySaltValue.CopyTo(valueToHash, 0);
			binaryPassword.CopyTo(valueToHash, saltSize);

			var hashValue = algorithm.ComputeHash(valueToHash);
			var hashedValue = hashValue.Aggregate(string.Empty, (current, digit) => current + digit.ToString("X2", CultureInfo.InvariantCulture.NumberFormat));

			return hashedValue;
		}

		private const string EncryptionKey = "[SSO]['S~u8'8XaCPLCEAXcEQ!321av'&<5]";

		// This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
		// This size of the IV (in bytes) must = (KeySize / 8).  Default KeySize is 256, so the IV must be
		// 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
		private static readonly byte[] InitVectorBytes = Encoding.ASCII.GetBytes("fqrqdj@8HrvfWG*v");

		// This constant is used to determine the KeySize of the encryption algorithm.
		private const int KeySize = 256;

		public static string EncryptWithKey(string plainText, string passPhrase = null)
		{
			passPhrase = !string.IsNullOrEmpty(passPhrase) ? $"{EncryptionKey}+|+[{passPhrase}]" : EncryptionKey;
			var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

			using var rfcDb = new Rfc2898DeriveBytes(passPhrase, InitVectorBytes);
			var keyBytes = rfcDb.GetBytes(KeySize / 8);

			using var symmetricKey = Aes.Create("AesManaged");
			symmetricKey.Mode = CipherMode.CBC;
			symmetricKey.Key = rfcDb.GetBytes(32);
			symmetricKey.IV = rfcDb.GetBytes(16);

			using var encrypt = symmetricKey.CreateEncryptor(keyBytes, InitVectorBytes);
			using var memoryStream = new MemoryStream();
			using var cryptoStream = new CryptoStream(memoryStream, encrypt, CryptoStreamMode.Write);
			cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
			cryptoStream.FlushFinalBlock();
			var cipherTextBytes = memoryStream.ToArray();

			return Convert.ToBase64String(cipherTextBytes);
		}

		public static string DecryptWithKey(string cipherText, string passPhrase = null)
		{
			passPhrase = !string.IsNullOrEmpty(passPhrase) ? $"{EncryptionKey}+|+[{passPhrase}]" : EncryptionKey;
			var cipherTextBytes = Convert.FromBase64String(cipherText);

			using var rfcDb = new Rfc2898DeriveBytes(passPhrase, InitVectorBytes);
			var keyBytes = rfcDb.GetBytes(KeySize / 8);

			using var symmetricKey = Aes.Create("AesManaged");
			symmetricKey.Mode = CipherMode.CBC;
			symmetricKey.Key = rfcDb.GetBytes(32);
			symmetricKey.IV = rfcDb.GetBytes(16);

			using var decrypt = symmetricKey.CreateDecryptor(keyBytes, InitVectorBytes);
			using var memoryStream = new MemoryStream(cipherTextBytes);
			using var cryptoStream = new CryptoStream(memoryStream, decrypt, CryptoStreamMode.Read);
			var plainTextBytes = new byte[cipherTextBytes.Length];
			var readBytes = 0;

			while (readBytes < plainTextBytes.Length)
			{
				var n = cryptoStream.Read(plainTextBytes, readBytes, plainTextBytes.Length - readBytes);
				if (n == 0) break;
				readBytes += n;
			}

			return Encoding.UTF8.GetString(plainTextBytes, 0, readBytes);
		}
	}
}
