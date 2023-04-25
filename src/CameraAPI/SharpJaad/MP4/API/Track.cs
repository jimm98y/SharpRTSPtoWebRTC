using SharpJaad.MP4.Boxes;
using SharpJaad.MP4.Boxes.Impl;
using SharpJaad.MP4.OD;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        private readonly MP4InputStream input;
        protected readonly TrackHeaderBox tkhd;
        private readonly MediaHeaderBox mdhd;
        private readonly bool inFile;
        private readonly List<Frame> frames;
        private Uri location;
        private int currentFrame;
        //info structures
        protected DecoderSpecificInfo decoderSpecificInfo;
        protected DecoderInfo decoderInfo;
        protected Protection protection;

        public Track(Box trak, MP4InputStream input)
        {
            this.input = input;

            tkhd = (TrackHeaderBox)trak.GetChild(BoxTypes.TRACK_HEADER_BOX);

            Box mdia = trak.GetChild(BoxTypes.MEDIA_BOX);
            mdhd = (MediaHeaderBox)mdia.GetChild(BoxTypes.MEDIA_HEADER_BOX);
            Box minf = mdia.GetChild(BoxTypes.MEDIA_INFORMATION_BOX);

            Box dinf = minf.GetChild(BoxTypes.DATA_INFORMATION_BOX);
            DataReferenceBox dref = (DataReferenceBox)dinf.GetChild(BoxTypes.DATA_REFERENCE_BOX);
            //TODO: support URNs
            if (dref.HasChild(BoxTypes.DATA_ENTRY_URL_BOX))
            {
                DataEntryUrlBox url = (DataEntryUrlBox)dref.GetChild(BoxTypes.DATA_ENTRY_URL_BOX);
                inFile = url.IsInFile();
                if (!inFile)
                {
                    try
                    {
                        location = new Uri(url.GetLocation());
                    }
                    catch (Exception e)
                    {
                        //Logger.getLogger("MP4 API").log(Level.WARNING, "Parsing URL-Box failed: {0}, url: {1}", new String[] { e.toString(), url.getLocation() });
                        location = null;
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
                inFile = true;
                location = null;
            }

            //sample table
            Box stbl = minf.GetChild(BoxTypes.SAMPLE_TABLE_BOX);
            if (stbl.HasChildren())
            {
                frames = new List<Frame>();
                parseSampleTable(stbl);
            }
            else frames = new List<Frame>();
            currentFrame = 0;
        }

        private void parseSampleTable(Box stbl)
        {
            double timeScale = mdhd.GetTimeScale();
            Type type = getType();

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
                        frames.Add(new Frame(type, offset, sampleSizes[current], timeStamp));
                        offset += sampleSizes[current];
                        current++;
                    }
                }
            }

            //frames need not to be time-ordered: sort by timestamp
            //TODO: is it possible to add them to the specific position?
            frames.Sort();
        }

        //TODO: implement other entry descriptors
        protected void findDecoderSpecificInfo(ESDBox esds)
        {
            Descriptor ed = esds.GetEntryDescriptor();
            List<Descriptor> children = ed.GetChildren();
            List<Descriptor> children2;

            foreach (Descriptor e in children)
            {
                children2 = e.GetChildren();
                foreach (Descriptor e2 in children2)
                {
                    switch (e2.getType())
                    {
                        case Descriptor.TYPE_DECODER_SPECIFIC_INFO:
                            decoderSpecificInfo = (DecoderSpecificInfo)e2;
                            break;
                    }
                }
            }
        }

        protected void parseSampleEntry<T>(Box sampleEntry, Class<T> clazz)
        {
            T type;
            try
            {
                type = clazz.newInstance();
                if (sampleEntry.getClass().isInstance(type))
                {
                    Debug.WriteLine("true");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public abstract Type getType();

        public abstract Codec GetCodec();

        //tkhd
        /**
         * Returns true if the track is enabled. A disabled track is treated as if
         * it were not present.
         * @return true if the track is enabled
         */
        public bool isEnabled()
        {
            return tkhd.IsTrackEnabled();
        }

        /**
         * Returns true if the track is used in the presentation.
         * @return true if the track is used
         */
        public bool isUsed()
        {
            return tkhd.IsTrackInMovie();
        }

        /**
         * Returns true if the track is used in previews.
         * @return true if the track is used in previews
         */
        public bool isUsedForPreview()
        {
            return tkhd.IsTrackInPreview();
        }

        /**
         * Returns the time this track was created.
         * @return the creation time
         */
        public DateTime getCreationTime()
        {
            return Utils.getDate(tkhd.GetCreationTime());
        }

        /**
         * Returns the last time this track was modified.
         * @return the modification time
         */
        public DateTime getModificationTime()
        {
            return Utils.getDate(tkhd.GetModificationTime());
        }

        //mdhd
        /**
         * Returns the language for this media.
         * @return the language
         */
        public Locale getLanguage()
        {
            return new Locale(mdhd.GetLanguage());
        }

        /**
         * Returns true if the data for this track is present in this file (stream).
         * If not, <code>getLocation()</code> returns the URL where the data can be
         * found.
         * @return true if the data is in this file (stream), false otherwise
         */
        public bool isInFile()
        {
            return inFile;
        }

        /**
         * If the data for this track is not present in this file (if
         * <code>isInFile</code> returns false), this method returns the data's
         * location. Else null is returned.
         * @return the data's location or null if the data is in this file
         */
        public Uri getLocation()
        {
            return location;
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
        public byte[] getDecoderSpecificInfo()
        {
            return decoderSpecificInfo.GetData();
        }

        /**
         * Returns the <code>DecoderInfo</code>, if present. It contains 
         * configuration information for the decoder. If the structure is not
         * present, the track contains a decoder specific info.
         *
         * @see #getDecoderSpecificInfo()
         * @return the codec specific structure
         */
        public DecoderInfo getDecoderInfo()
        {
            return decoderInfo;
        }

        /**
         * Returns the <code>ProtectionInformation</code> object that contains 
         * details about the DRM system used. If no protection is present this 
         * method returns null.
         * 
         * @return a <code>ProtectionInformation</code> object or null if no 
         * protection is used
         */
        public Protection getProtection()
        {
            return protection;
        }

        //reading
        /**
         * Indicates if there are more frames to be read in this track.
         * 
         * @return true if there is at least one more frame to read.
         */
        public bool hasMoreFrames()
        {
            return currentFrame < frames.Count;
        }

        /**
         * Reads the next frame from this track. If it contains no more frames to
         * read, null is returned.
         * 
         * @return the next frame or null if there are no more frames to read
         * @throws IOException if reading fails
         */
        public Frame readNextFrame()
        {
            Frame frame = null;
            if (hasMoreFrames()) {
                frame = frames[currentFrame];

                long diff = frame.GetOffset() - input.getOffset();
                if (diff > 0) input.skipBytes(diff);
                else if (diff < 0) {
                    if (input.hasRandomAccess()) input.seek(frame.GetOffset());
                    else {
                        //Logger.getLogger("MP4 API").log(Level.WARNING, "readNextFrame failed: frame {0} already skipped, offset:{1}, stream:{2}", new Object[]{currentFrame, frame.getOffset(), in.getOffset()});
                        throw new Exception("frame already skipped and no random access");
                    }
                }

                byte[] b = new byte[(int)frame.GetSize()];
                try
                {
                    input.readBytes(b);
                }
                catch (Exception e)
                {
                    //Logger.getLogger("MP4 API").log(Level.WARNING, "readNextFrame failed: tried to read {0} bytes at {1}", new long[] { frame.getSize(), input.getOffset() });
                    throw e;
                }
                frame.SetData(b);
                currentFrame++;
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
        public double seek(double timestamp)
        {
            //find first frame > timestamp
            Frame frame = null;
            for (int i = 0; i < frames.Count; i++)
            {
                frame = frames[i++];
                if (frame.GetTime() > timestamp)
                {
                    currentFrame = i;
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
        public double getNextTimeStamp()
        {
            return frames[currentFrame].GetTime();
        }
    }
}
