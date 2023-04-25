using SharpJaad.MP4.Boxes;
using SharpJaad.MP4.Boxes.Impl;
using SharpJaad.MP4.OD;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace SharpJaad.MP4.API
{
    /**
     * This class represents a track in a movie.
     *
     * Each track contains either a decoder specific info as a byte array or a
     * <code>DecoderInfo</code> object that contains necessary information for the
     * decoder.
     *
     * @author in-somnia
     */
    //TODO: expand javadoc; use generics for subclasses?
    public abstract class Track
    {
        private readonly MP4InputStream _input;
        protected readonly TrackHeaderBox _tkhd;
        private readonly MediaHeaderBox _mdhd;
        private readonly bool _inFile;
        private readonly List<Frame> _frames;
        private Uri _location;
        private int _currentFrame;
        //info structures
        protected DecoderSpecificInfo _decoderSpecificInfo;
        protected DecoderInfo _decoderInfo;
        protected Protection _protection;

        public Track(Box trak, MP4InputStream input)
        {
            this._input = input;

            _tkhd = (TrackHeaderBox)trak.GetChild(BoxTypes.TRACK_HEADER_BOX);

            Box mdia = trak.GetChild(BoxTypes.MEDIA_BOX);
            _mdhd = (MediaHeaderBox)mdia.GetChild(BoxTypes.MEDIA_HEADER_BOX);
            Box minf = mdia.GetChild(BoxTypes.MEDIA_INFORMATION_BOX);

            Box dinf = minf.GetChild(BoxTypes.DATA_INFORMATION_BOX);
            DataReferenceBox dref = (DataReferenceBox)dinf.GetChild(BoxTypes.DATA_REFERENCE_BOX);
            //TODO: support URNs
            if (dref.HasChild(BoxTypes.DATA_ENTRY_URL_BOX))
            {
                DataEntryUrlBox url = (DataEntryUrlBox)dref.GetChild(BoxTypes.DATA_ENTRY_URL_BOX);
                _inFile = url.IsInFile();
                if (!_inFile)
                {
                    try
                    {
                        _location = new Uri(url.GetLocation());
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Parsing URL-Box failed: {0}, url: {1}", e.ToString(), url.GetLocation());
                        //Logger.getLogger("MP4 API").log(Level.WARNING, "Parsing URL-Box failed: {0}, url: {1}", new String[] { e.toString(), url.getLocation() });
                        _location = null;
                    }
                }
            }
            /*else if(dref.containsChild(BoxTypes.DATA_ENTRY_URN_BOX)) {
            DataEntryUrnBox urn = (DataEntryUrnBox) dref.getChild(BoxTypes.DATA_ENTRY_URN_BOX);
            inFile = urn.isInFile();
            location = urn.getLocation();
            }*/
            else
            {
                _inFile = true;
                _location = null;
            }

            //sample table
            Box stbl = minf.GetChild(BoxTypes.SAMPLE_TABLE_BOX);
            if (stbl.HasChildren())
            {
                _frames = new List<Frame>();
                ParseSampleTable(stbl);
            }
            else _frames = new List<Frame>();
            _currentFrame = 0;
        }

        private void ParseSampleTable(Box stbl)
        {
            double timeScale = _mdhd.GetTimeScale();
            Type type = GetTrackType();

            //sample sizes
            long[] sampleSizes = ((SampleSizeBox)stbl.GetChild(BoxTypes.SAMPLE_SIZE_BOX)).GetSampleSizes();

            //chunk offsets
            ChunkOffsetBox stco;
            if (stbl.HasChild(BoxTypes.CHUNK_OFFSET_BOX)) stco = (ChunkOffsetBox)stbl.GetChild(BoxTypes.CHUNK_OFFSET_BOX);
            else stco = (ChunkOffsetBox)stbl.GetChild(BoxTypes.CHUNK_LARGE_OFFSET_BOX);
            long[] chunkOffsets = stco.GetChunks();

            //samples to chunks
            SampleToChunkBox stsc = ((SampleToChunkBox)stbl.GetChild(BoxTypes.SAMPLE_TO_CHUNK_BOX));
            long[] firstChunks = stsc.GetFirstChunks();
            long[] samplesPerChunk = stsc.GetSamplesPerChunk();

            //sample durations/timestamps
            DecodingTimeToSampleBox stts = (DecodingTimeToSampleBox)stbl.GetChild(BoxTypes.DECODING_TIME_TO_SAMPLE_BOX);
            long[] sampleCounts = stts.GetSampleCounts();
            long[] sampleDeltas = stts.GetSampleDeltas();
            long[] timeOffsets = new long[sampleSizes.Length];
            long tmp = 0;
            int off = 0;
            for (int i = 0; i < sampleCounts.Length; i++)
            {
                for (int j = 0; j < sampleCounts[i]; j++)
                {
                    timeOffsets[off + j] = tmp;
                    tmp += sampleDeltas[i];
                }
                off += (int)sampleCounts[i];
            }

            //create samples
            int current = 0;
            int lastChunk;
            double timeStamp;
            long offset = 0;
            //iterate over all chunk groups
            for (int i = 0; i < firstChunks.Length; i++)
            {
                if (i < firstChunks.Length - 1) lastChunk = (int)firstChunks[i + 1] - 1;
                else lastChunk = chunkOffsets.Length;

                //iterate over all chunks in current group
                for (int j = (int)firstChunks[i] - 1; j < lastChunk; j++)
                {
                    offset = chunkOffsets[j];

                    //iterate over all samples in current chunk
                    for (int k = 0; k < samplesPerChunk[i]; k++)
                    {
                        //create samples
                        timeStamp = ((double)timeOffsets[current]) / timeScale;
                        _frames.Add(new Frame(type, offset, sampleSizes[current], timeStamp));
                        offset += sampleSizes[current];
                        current++;
                    }
                }
            }

            //frames need not to be time-ordered: sort by timestamp
            //TODO: is it possible to add them to the specific position?
            _frames.Sort();
        }

        //TODO: implement other entry descriptors
        protected void FindDecoderSpecificInfo(ESDBox esds)
        {
            Descriptor ed = esds.GetEntryDescriptor();
            List<Descriptor> children = ed.GetChildren();
            List<Descriptor> children2;

            foreach (Descriptor e in children)
            {
                children2 = e.GetChildren();
                foreach (Descriptor e2 in children2)
                {
                    switch (e2.GetDescriptorType())
                    {
                        case Descriptor.TYPE_DECODER_SPECIFIC_INFO:
                            _decoderSpecificInfo = (DecoderSpecificInfo)e2;
                            break;
                    }
                }
            }
        }

        public abstract Type GetTrackType();

        public abstract System.Enum GetCodec();

        //tkhd
        /**
         * Returns true if the track is enabled. A disabled track is treated as if
         * it were not present.
         * @return true if the track is enabled
         */
        public bool IsEnabled()
        {
            return _tkhd.IsTrackEnabled();
        }

        /**
         * Returns true if the track is used in the presentation.
         * @return true if the track is used
         */
        public bool IsUsed()
        {
            return _tkhd.IsTrackInMovie();
        }

        /**
         * Returns true if the track is used in previews.
         * @return true if the track is used in previews
         */
        public bool IsUsedForPreview()
        {
            return _tkhd.IsTrackInPreview();
        }

        /**
         * Returns the time this track was created.
         * @return the creation time
         */
        public DateTime GetCreationTime()
        {
            return Utils.GetDate(_tkhd.GetCreationTime());
        }

        /**
         * Returns the last time this track was modified.
         * @return the modification time
         */
        public DateTime GetModificationTime()
        {
            return Utils.GetDate(_tkhd.GetModificationTime());
        }

        //mdhd
        /**
         * Returns the language for this media.
         * @return the language
         */
        public CultureInfo GetLanguage()
        {
            return new CultureInfo(_mdhd.GetLanguage());
        }

        /**
         * Returns true if the data for this track is present in this file (stream).
         * If not, <code>getLocation()</code> returns the URL where the data can be
         * found.
         * @return true if the data is in this file (stream), false otherwise
         */
        public bool IsInFile()
        {
            return _inFile;
        }

        /**
         * If the data for this track is not present in this file (if
         * <code>isInFile</code> returns false), this method returns the data's
         * location. Else null is returned.
         * @return the data's location or null if the data is in this file
         */
        public Uri GetLocation()
        {
            return _location;
        }

        //info structures
        /**
         * Returns the decoder specific info, if present. It contains configuration
         * data for the decoder. If the decoder specific info is not present, the
         * track contains a <code>DecoderInfo</code>.
         *
         * @see #getDecoderInfo() 
         * @return the decoder specific info
         */
        public byte[] GetDecoderSpecificInfo()
        {
            return _decoderSpecificInfo.GetData();
        }

        /**
         * Returns the <code>DecoderInfo</code>, if present. It contains 
         * configuration information for the decoder. If the structure is not
         * present, the track contains a decoder specific info.
         *
         * @see #getDecoderSpecificInfo()
         * @return the codec specific structure
         */
        public DecoderInfo GetDecoderInfo()
        {
            return _decoderInfo;
        }

        /**
         * Returns the <code>ProtectionInformation</code> object that contains 
         * details about the DRM system used. If no protection is present this 
         * method returns null.
         * 
         * @return a <code>ProtectionInformation</code> object or null if no 
         * protection is used
         */
        public Protection GetProtection()
        {
            return _protection;
        }

        //reading
        /**
         * Indicates if there are more frames to be read in this track.
         * 
         * @return true if there is at least one more frame to read.
         */
        public bool HasMoreFrames()
        {
            return _currentFrame < _frames.Count;
        }

        /**
         * Reads the next frame from this track. If it contains no more frames to
         * read, null is returned.
         * 
         * @return the next frame or null if there are no more frames to read
         * @throws IOException if reading fails
         */
        public Frame ReadNextFrame()
        {
            Frame frame = null;
            if (HasMoreFrames()) {
                frame = _frames[_currentFrame];

                long diff = frame.GetOffset() - _input.GetOffset();
                if (diff > 0) _input.SkipBytes(diff);
                else if (diff < 0) {
                    if (_input.HasRandomAccess()) _input.Seek(frame.GetOffset());
                    else {
                        //Logger.getLogger("MP4 API").log(Level.WARNING, "readNextFrame failed: frame {0} already skipped, offset:{1}, stream:{2}", new Object[]{currentFrame, frame.getOffset(), in.getOffset()});
                        throw new Exception("frame already skipped and no random access");
                    }
                }

                byte[] b = new byte[(int)frame.GetSize()];
                try
                {
                    _input.ReadBytes(b);
                }
                catch (Exception e)
                {
                    //Logger.getLogger("MP4 API").log(Level.WARNING, "readNextFrame failed: tried to read {0} bytes at {1}", new long[] { frame.getSize(), input.getOffset() });
                    throw e;
                }
                frame.SetData(b);
                _currentFrame++;
            }
            return frame;
        }

        /**
         * This method tries to seek to the frame that is nearest to the given
         * timestamp. It returns the timestamp of the frame it seeked to or -1 if
         * none was found.
         * 
         * @param timestamp a timestamp to seek to
         * @return the frame's timestamp that the method seeked to
         */
        public double Seek(double timestamp)
        {
            //find first frame > timestamp
            Frame frame = null;
            for (int i = 0; i < _frames.Count; i++)
            {
                frame = _frames[i++];
                if (frame.GetTime() > timestamp)
                {
                    _currentFrame = i;
                    break;
                }
            }
            return (frame == null) ? -1 : frame.GetTime();
        }

        /**
         * Returns the timestamp of the next frame to be read. This is needed to
         * read frames from a movie that contains multiple tracks.
         *
         * @return the next frame's timestamp
         */
        public double GetNextTimeStamp()
        {
            return _frames[_currentFrame].GetTime();
        }
    }
}
