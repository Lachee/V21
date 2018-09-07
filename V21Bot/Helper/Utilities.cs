using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace V21Bot.Helper
{
	static class Utilities
	{

		public static string EncodeToBase64(string s)
		{
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(s);
			return System.Convert.ToBase64String(bytes);
		}

		public static string Hash(string s)
		{
			// step 1, calculate MD5 hash from input
			MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.Unicode.GetBytes(s);
			byte[] hash = md5.ComputeHash(inputBytes);

			// step 2, convert byte array to hex string
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hash.Length; i++)
				sb.Append(hash[i].ToString("X2"));

			return sb.ToString();			
		}

		public static string GenerateSlider(int length, float value, float min = 0, float max = 1)
		{
			StringBuilder builder = new StringBuilder();
			float percent = (value - min) / (max - min);
			int charindex = (int) Math.Round(length * percent);
			
			for (int i = 0; i < length; i++)
				builder.Append(i == charindex ? 'O' : '-');

			if (charindex == length)
				builder.Append('O');

			return builder.ToString();
		}
	}
}
