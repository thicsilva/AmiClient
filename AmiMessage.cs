﻿/* Copyright © 2017 Alex Forster. All rights reserved.
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

    public sealed partial class AmiMessage : IEnumerable<KeyValuePair<String, String>>
    {
        public readonly List<KeyValuePair<String, String>> Fields = new List<KeyValuePair<String, String>>();

        public DateTimeOffset Timestamp
        {
            get;
            private set;
        }

        public AmiMessage()
        {
            this.Timestamp = DateTimeOffset.Now;
        }

        public String this[String key]
        {
            get
            {
                if(String.IsNullOrWhiteSpace(key))
                {
                    throw new ArgumentException(nameof(key));
                }

                return this.Fields
                           .Where(kvp => kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                           .Select(el => el.Value)
                           .FirstOrDefault();
            }

            private set
            {
                if(String.IsNullOrWhiteSpace(key) || key.IndexOfAny(new[] { '\r', '\n' }) != -1)
                {
                    throw new ArgumentException(nameof(key));
                }

                if(value == null || value.IndexOfAny(new[] { '\r', '\n' }) != -1)
                {
                    throw new ArgumentException(nameof(value));
                }

                key = key.Trim();
                value = value.Trim();

                if(key.Equals("ActionID", StringComparison.OrdinalIgnoreCase))
                {
                    // ActionIDs are overwritten so that the AmiMessage creator can override autogenerated values;
                    // all other keys can be added multiple times (though it is usually invalid to do so)

                    this.Fields.RemoveAll(field => field.Key.Equals("ActionID", StringComparison.OrdinalIgnoreCase));
                }

                this.Fields.Add(new KeyValuePair<String, String>(key, value));

                if(key.Equals("Action", StringComparison.OrdinalIgnoreCase) &&
                   this.Fields.Any(field => field.Key.Equals("ActionID", StringComparison.OrdinalIgnoreCase)) == false)
                {
                    // add an autogenerated ActionID when the Action field is set
                    // only if an ActionID has not already been set

                    this.Fields.Add(new KeyValuePair<String, String>("ActionID", Guid.NewGuid().ToString("D")));
                }
            }
        }

        public IEnumerator<KeyValuePair<String, String>> GetEnumerator()
        {
            return this.Fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Add(String key, String value)
        {
            this[key] = value;
        }
    }
}