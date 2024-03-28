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

namespace Ami
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using ByteArrayExtensions;

    public sealed partial class AmiMessage
    {
        internal static readonly byte[] TerminatorBytes = { 13, 10 };

        internal static readonly char[] TerminatorChars = { '\r', '\n' };

        public static AmiMessage FromBytes(byte[] bytes)
        {
            AmiMessage amiMessage = new AmiMessage();

            int num = 1;
            while (true)
            {
                int end = bytes.Find(AmiMessage.TerminatorBytes, 0, bytes.Length);
                if (end != -1)
                {
                    string str = Encoding.UTF8.GetString(bytes.Slice(0, end));
                    bytes = bytes.Slice(end + AmiMessage.TerminatorBytes.Length);
                    if (!str.Equals(string.Empty))
                    {
                        string[] strArray = str.Split(new char[1]{ ':' }, 2);
                        if (strArray.Length == 2)
                        {
                            amiMessage.Add(strArray[0], strArray[1]);
                            ++num;
                        }
                        else
                            throw new ArgumentException($"unexpected end of message after {num} line(s)", nameof (bytes));
                    }
                    else
                        throw new ArgumentException($"malformed field on line {num}", nameof (bytes));
                }
                else
                    break;
            }

            return amiMessage;
        }

        public static AmiMessage FromString(string @string)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(@string);

            return FromBytes(bytes);
        }

        public byte[] ToBytes()
        {
            MemoryStream stream = new MemoryStream();

            using(StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                foreach(KeyValuePair<string, string> field in Fields)
                {
                    writer.Write(field.Key);
                    writer.Write(": ");
                    writer.Write(field.Value);
                    writer.Write(TerminatorChars);
                }

                writer.Write(TerminatorChars);
            }

            return stream.ToArray();
        }

        public override string ToString() => Encoding.UTF8.GetString(ToBytes());
    }
}
