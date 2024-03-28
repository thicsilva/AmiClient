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
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Security.Cryptography;

    using ByteArrayExtensions;

    public sealed partial class AmiClient
    {
        public async Task<bool> Login(string username, string secret, bool md5 = true)
        {
            if(username == null)
            {
                throw new ArgumentNullException(nameof(username));
            }

            if(secret == null)
            {
                throw new ArgumentNullException(nameof(secret));
            }

            AmiMessage request, response;

            if(md5)
            {
                request = new AmiMessage
                {
                    { "Action", "Challenge" },
                    { "AuthType", "MD5" },
                };

                response = await Publish(request);

                if(!(response["Response"] ?? string.Empty).Equals("Success", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                byte[] answer = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(response["Challenge"] + secret));

                string key = "";

                for(int i = 0; i < answer.Length; i++)
                {
                    key += answer[i].ToString("x2");
                }

                request = new AmiMessage
                {
                    { "Action", "Login" },
                    { "AuthType", "MD5" },
                    { "Username", username },
                    { "Key", key },
                };

                response = await Publish(request);
            }
            else
            {
                request = new AmiMessage
                {
                    { "Action", "Login" },
                    { "Username", username },
                    { "Secret", secret },
                };

                response = await Publish(request);
            }

            return (response["Response"] ?? string.Empty).Equals("Success", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> Logoff()
        {
            AmiMessage request, response;

            request = new AmiMessage
            {
                { "Action", "Logoff" },
            };

            response = await Publish(request);

            return (response["Response"] ?? string.Empty).Equals("Goodbye", StringComparison.OrdinalIgnoreCase);
        }
    }
}
