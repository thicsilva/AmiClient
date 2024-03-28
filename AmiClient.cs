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
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Reactive.Linq;
    using ByteArrayExtensions;

    public sealed partial class AmiClient : IDisposable
    {
        private readonly ConcurrentDictionary<IObserver<AmiMessage>, Subscription> _observers;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<AmiMessage>> _inFlight;

        public AmiClient()
        {
            _observers = new ConcurrentDictionary<IObserver<AmiMessage>, Subscription>(
                Environment.ProcessorCount,
                65536);

            _inFlight = new ConcurrentDictionary<string, TaskCompletionSource<AmiMessage>>(
                Environment.ProcessorCount,
                16384,
                StringComparer.OrdinalIgnoreCase);
        }

        private Stream _stream;

        [Obsolete("use Start() method")]
        public AmiClient(Stream stream) : this()
        {
            Stream = stream;
        }

        [Obsolete("use Start() method")]
        public Stream Stream
        {
            get => _stream;
            set => Start(value).Wait();
        }

        public async Task<Task> Start(Stream stream)
        {
            AmiClient amiClient = this;
            if (amiClient._stream != null)
                throw new AmiException("client has already been started");
            if (!stream.CanRead)
                throw new ArgumentException("stream does not support reading", nameof(stream));
            amiClient._stream = stream.CanWrite
                ? stream
                : throw new ArgumentException("stream does not support writing", nameof(stream));
            try
            {
                byte[] numArray = await amiClient.LineObserver.ToObservable<byte[]>().Take<byte[]>(1);
            }
            catch (Exception ex)
            {
                throw new AmiException("protocol handshake failed (is this an Asterisk server?)", ex);
            }

            return Task.Factory.StartNew<Task>(new Func<Task>(amiClient.WorkerMain),
                TaskCreationOptions.LongRunning);
        }

        public void Stop() => Stop(null);

        private void Stop(Exception ex)
        {
            if (_stream == null)
                return;

            foreach (IObserver<AmiMessage> observer in _observers.Keys)
            {
                if (ex != null)
                    observer.OnError(ex);
                else
                    observer.OnCompleted();

                _observers.TryRemove(observer, out _);
            }

            foreach (KeyValuePair<string, TaskCompletionSource<AmiMessage>> kvp in _inFlight)
            {
                if (ex != null)
                    kvp.Value.TrySetException(ex);
                else
                    kvp.Value.TrySetCanceled();

                _inFlight.TryRemove(kvp.Key, out _);
            }

            _stream = null;

            Stopped?.Invoke(this, new LifecycleEventArgs(ex));
        }

        public void Dispose() => Stop();

        public async Task<AmiMessage> Publish(AmiMessage action)
        {
            AmiClient sender = this;
            if (sender._stream == null)
                throw new InvalidOperationException("client is not started");

            TaskCompletionSource<AmiMessage> tcs = new TaskCompletionSource<AmiMessage>(TaskCreationOptions.AttachedToParent);

            if (!sender._inFlight.TryAdd(action["ActionID"], tcs))
                throw new AmiException("a message with the same ActionID is already in flight");

            AmiMessage task;
            try
            {
                byte[] buffer = action.ToBytes();
                lock (_stream)
                    _stream.Write(buffer, 0, buffer.Length);
                DataSent?.Invoke(this, new DataEventArgs(buffer));

                task = await tcs.Task;
            }
            catch (Exception ex)
            {
                sender.Stop(ex);
                throw;
            }
            finally
            {
                sender._inFlight.TryRemove(action["ActionID"], out _);
            }

            return task;
        }

        private byte[] _readBuffer = new byte[0];

        private IEnumerable<byte[]> LineObserver
        {
            get
            {
                AmiClient sender = this;
                while (true)
                {
                    int num = sender._readBuffer.Find(AmiMessage.TerminatorBytes, 0, sender._readBuffer.Length);
                    if (num >= 0)
                    {
                        byte[] numArray = sender._readBuffer.Slice(0, num + AmiMessage.TerminatorBytes.Length);
                        sender._readBuffer = sender._readBuffer.Slice(num + AmiMessage.TerminatorBytes.Length);
                        yield return numArray;
                    }
                    else
                    {
                        try
                        {
                            byte[] numArray = new byte[4096];
                            int end = sender._stream.Read(numArray, 0, numArray.Length);
                            if (end == 0)
                                break;
                            sender._readBuffer = sender._readBuffer.Append(numArray.Slice(0, end));
                            sender.DataReceived?.Invoke(sender, new DataEventArgs(numArray.Slice(0, end)));
                        }
                        catch (SocketException ex)
                        {
                            if (ex.SocketErrorCode != SocketError.Interrupted)
                            {
                                if (ex.SocketErrorCode != SocketError.TimedOut)
                                    throw;
                            }
                        }
                    }
                }
            }
        }

        private async Task WorkerMain()
        {
            IObservable<byte[]> lineObserver = LineObserver.ToObservable();

            try
            {
                while (_stream != null)
                {
                    byte[] payload = new byte[0];

                    await lineObserver
                        .TakeUntil(line => line.SequenceEqual(AmiMessage.TerminatorBytes))
                        .Do(line => payload = payload.Append(line))
                        .DefaultIfEmpty();

                    if (payload.Length == 0)
                        break;

                    AmiMessage message = AmiMessage.FromBytes(payload);
                    if (message.Fields.FirstOrDefault().Key == "Response" &&
                        _inFlight.TryGetValue(message["ActionID"], out TaskCompletionSource<AmiMessage> tcs))
                        tcs.SetResult(message);
                    else
                        Dispatch(message);
                }
            }
            catch (Exception ex)
            {
                Stop(ex);
                lineObserver = null;
                return;
            }

            Stop();
            lineObserver = null;
        }
    }
}