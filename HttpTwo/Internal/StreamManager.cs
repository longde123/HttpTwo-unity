using System;
using System.Collections.Generic;
using System.Threading;

namespace HttpTwo.Internal
{
    public delegate void StreamGetCallback(Http2Stream stream);
    public interface IStreamManager
    {
        uint GetNextIdentifier ();
        void Get(uint streamIdentifier, StreamGetCallback cb);
        void Get(StreamGetCallback cb);
        Http2Stream Get();
        Http2Stream Get(uint streamIdentifier);
        void Cleanup (uint streamIdentifier);
    }

    public class StreamManager : IStreamManager
    {
        const uint STREAM_ID_MAX_VALUE = 1073741823;

        uint nextStreamId = 1;

        public uint GetNextIdentifier ()
        {
            var nextId = nextStreamId;

            // Increment for next use, by 2, must always be odd if initiated from client
            nextStreamId += 2;

            // Wrap around if we hit max
            if (nextStreamId > STREAM_ID_MAX_VALUE) {
                // TODO: Disconnect so we can reset the stream id
            }

            return nextId;
        }

        public StreamManager (IFlowControlManager flowControlManager)
        {
            this.flowControlManager = flowControlManager;

            streams = new Dictionary<uint, Http2Stream> ();

            // Add special stream '0' to act as connection level
            streams.Add (0, new Http2Stream (this.flowControlManager, 0));
        }

        IFlowControlManager flowControlManager;

        Dictionary<uint, Http2Stream> streams;

        readonly Semaphore lockStreams = new Semaphore(1, 100);

        public Http2Stream Get(uint streamIdentifier)
        {
            return GetWithIdentifier(streamIdentifier, null);
        }

        public void Get(uint streamIdentifier, StreamGetCallback cb)
        {
            new Thread(() => GetWithIdentifier(streamIdentifier, cb)).Start();
        }

        private Http2Stream GetWithIdentifier(uint streamIdentifier, StreamGetCallback cb)
        {
            lockStreams.WaitOne();

            Http2Stream stream = null;

            if (!streams.ContainsKey(streamIdentifier))
            {
                stream = new Http2Stream(flowControlManager, streamIdentifier);
                streams.Add(streamIdentifier, stream);
            }
            else
            {
                stream = streams[streamIdentifier];
            }

            lockStreams.Release();

            cb?.Invoke(stream);
            return stream;
        }

        public Http2Stream Get()
        {
            return GetImpl(null);
        }

        public void Get(StreamGetCallback cb)
        {
            new Thread(() => GetImpl(cb)).Start();
        }

        private Http2Stream GetImpl(StreamGetCallback cb)
        {
            lockStreams.WaitOne();

            var stream = new Http2Stream(flowControlManager, GetNextIdentifier());

            streams.Add(stream.StreamIdentifer, stream);

            lockStreams.Release();

            cb?.Invoke(stream);
            return stream;
        }

        public void Cleanup (uint streamIdentifier)
        {
            lockStreams.WaitOne();

            if (streams.ContainsKey (streamIdentifier))
                streams.Remove (streamIdentifier);

            lockStreams.Release ();
        }
    }
}
