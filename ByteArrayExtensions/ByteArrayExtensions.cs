/* Copyright © Alex Forster. All rights reserved.
 * https://github.com/alexforster/AmiClient/
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Ami.ByteArrayExtensions
{
	using System;
	using System.Diagnostics;
	using System.Collections.Generic;

	internal static class ByteArrayExtensions
	{
		[DebuggerStepThrough]
		public static byte[] Prepend(this byte[] bytes, byte[] items)
		{
			byte[] result = new byte[bytes.Length + items.Length];

			Buffer.BlockCopy(items, 0, result, 0, items.Length);
			Buffer.BlockCopy(bytes, 0, result, items.Length, bytes.Length);

			return result;
		}

		[DebuggerStepThrough]
		public static byte[] Append(this byte[] bytes, byte[] items)
		{
			byte[] result = new byte[bytes.Length + items.Length];

			Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
			Buffer.BlockCopy(items, 0, result, bytes.Length, items.Length);

			return result;
		}

		[DebuggerStepThrough]
		public static byte[] Slice(this byte[] bytes, int startPos)
		{
			int start = (startPos < 0) ? (bytes.Length + startPos) : (startPos);
			int end = (bytes.Length);

			return bytes.Slice(start, end);
		}

		[DebuggerStepThrough]
		public static byte[] Slice(this byte[] bytes, int startPos, int endPos)
		{
			int start = (startPos < 0) ? (bytes.Length + startPos) : (startPos);
			int end = (endPos < 0) ? (bytes.Length + endPos) : (endPos);

			if(start < 0 || bytes.Length < start)
			{
				throw new ArgumentOutOfRangeException(nameof(startPos));
			}

			if(end < start || bytes.Length < end)
			{
				throw new ArgumentOutOfRangeException(nameof(endPos));
			}

			byte[] result = new byte[end - start];

			Buffer.BlockCopy(bytes, start, result, 0, result.Length);

			return result;
		}

		[DebuggerStepThrough]
		public static int Find(this byte[] bytes, byte[] needle, int startPos = 0)
		{
			int start = (startPos < 0) ? (bytes.Length + startPos) : (startPos);
			int end = (bytes.Length);

			return bytes.Find(needle, start, end);
		}

		[DebuggerStepThrough]
		public static int Find(this byte[] bytes, byte[] needle, int startPos, int endPos)
		{
			int start = (startPos < 0) ? (bytes.Length + startPos) : (startPos);
			int end = (endPos < 0) ? (bytes.Length + endPos) : (endPos);

			int needlePos = 0;

			for(int i = start; i < end; i++)
			{
				if(bytes[i] == needle[needlePos])
				{
					if(++needlePos == needle.Length)
					{
						return i - needlePos + 1;
					}
				}
				else
				{
					i -= needlePos;
					needlePos = 0;
				}
			}

			return -1;
		}

		[DebuggerStepThrough]
		public static int[] FindAll(this byte[] bytes, byte[] needle, int startPos = 0)
		{
			int start = (startPos < 0) ? (bytes.Length + startPos) : (startPos);
			int end = (bytes.Length);

			return bytes.FindAll(needle, start, end);
		}

		[DebuggerStepThrough]
		public static int[] FindAll(this byte[] bytes, byte[] needle, int startPos, int endPos)
		{
			int start = (startPos < 0) ? (bytes.Length + startPos) : (startPos);
			int end = (endPos < 0) ? (bytes.Length + endPos) : (endPos);

			List<int> matches = new List<int>();

			int needlePos = 0;

			for(int i = start; i < end; i++)
			{
				if(bytes[i] == needle[needlePos])
				{
					if (++needlePos != needle.Length) 
						continue;
					
					matches.Add(i - needlePos + 1);
					needlePos = 0;
				}
				else
				{
					i -= needlePos;
					needlePos = 0;
				}
			}

			return matches.ToArray();
		}
	}
}
