using SharpJaad.MP4.Boxes;
using SharpJaad.MP4.Boxes.Impl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpJaad.MP4.API
{
    public class Movie
    {
        private readonly MP4InputStream _input;
        private readonly MovieHeaderBox _mvhd;
	    private readonly List<Track> _tracks;
        private readonly MetaData _metaData;
	    private readonly List<Protection> _protections;

        public Movie(Box moov, MP4InputStream input)
        {
            this._input = input;

            //create tracks
            _mvhd = (MovieHeaderBox)moov.GetChild(BoxTypes.MOVIE_HEADER_BOX);
            List<Box> trackBoxes = moov.GetChildren(BoxTypes.TRACK_BOX);
            _tracks = new List<Track>(trackBoxes.Count);
            Track track;
            for (int i = 0; i < trackBoxes.Count; i++)
            {
                track = CreateTrack(trackBoxes[i]);
                if (track != null) _tracks.Add(track);
            }

            //read metadata: moov.meta/moov.udta.meta
            _metaData = new MetaData();
            if (moov.HasChild(BoxTypes.META_BOX)) _metaData.Parse(null, moov.GetChild(BoxTypes.META_BOX));
            else if (moov.HasChild(BoxTypes.USER_DATA_BOX))
            {
                Box udta = moov.GetChild(BoxTypes.USER_DATA_BOX);
                if (udta.HasChild(BoxTypes.META_BOX)) _metaData.Parse(udta, udta.GetChild(BoxTypes.META_BOX));
            }

            //detect DRM
            _protections = new List<Protection>();
            if (moov.HasChild(BoxTypes.ITEM_PROTECTION_BOX))
            {
                Box ipro = moov.GetChild(BoxTypes.ITEM_PROTECTION_BOX);
                foreach (Box sinf in ipro.GetChildren(BoxTypes.PROTECTION_SCHEME_INFORMATION_BOX))
                {
                    _protections.Add(Protection.Parse(sinf));
                }
            }
        }

        //TODO: support hint and meta
        private Track CreateTrack(Box trak)
        {
            HandlerBox hdlr = (HandlerBox)trak.GetChild(BoxTypes.MEDIA_BOX).GetChild(BoxTypes.HANDLER_BOX);
            Track track;
            switch ((int)hdlr.GetHandlerType())
            {
                case HandlerBox.TYPE_VIDEO:
                    track = new VideoTrack(trak, _input);
                    break;
                case HandlerBox.TYPE_SOUND:
                    track = new AudioTrack(trak, _input);
                    break;
                default:
                    track = null;
                    break;
            }
            return track;
        }

        /**
         * Returns an unmodifiable list of all tracks in this movie. The tracks are
         * ordered as they appeare in the file/stream.
         *
         * @return the tracks contained by this movie
         */
        public List<Track> GetTracks()
        {
            return _tracks.ToList();
        }

        /**
         * Returns an unmodifiable list of all tracks in this movie with the
         * specified type. The tracks are ordered as they appeare in the
         * file/stream.
         *
         * @return the tracks contained by this movie with the passed type
         */
        public List<Track> GetTracks(Type type)
        {
            List<Track> l = new List<Track>();
            foreach (Track t in _tracks)
            {
                if (t.GetTrackType().Equals(type)) l.Add(t);
            }
            return l.ToList();
        }

        /**
         * Returns an unmodifiable list of all tracks in this movie whose samples
         * are encoded with the specified codec. The tracks are ordered as they 
         * appeare in the file/stream.
         *
         * @return the tracks contained by this movie with the passed type
         */
        public List<Track> GetTracks(System.Enum codec)
        {
            List<Track> l = new List<Track>();
            foreach (Track t in _tracks)
            {
                if (t.GetCodec().Equals(codec)) l.Add(t);
            }
            return l.ToList();
        }

        /**
         * Indicates if this movie contains metadata. If false the <code>MetaData</code>
         * object returned by <code>getMetaData()</code> will not contain any field.
         * 
         * @return true if this movie contains any metadata
         */
        public bool ContainsMetaData()
        {
            return _metaData.ContainsMetaData();
        }

        /**
         * Returns the MetaData object for this movie.
         *
         * @return the MetaData for this movie
         */
        public MetaData GetMetaData()
        {
            return _metaData;
        }

        /**
         * Returns the <code>ProtectionInformation</code> objects that contains 
         * details about the DRM systems used. If no protection is present the 
         * returned list will be empty.
         * 
         * @return a list of protection informations
         */
        public List<Protection> GetProtections()
        {
            return _protections.ToList();
        }

        //mvhd
        /**
         * Returns the time this movie was created.
         * @return the creation time
         */
        public DateTime GetCreationTime()
        {
            return Utils.GetDate(_mvhd.GetCreationTime());
        }

        /**
         * Returns the last time this movie was modified.
         * @return the modification time
         */
        public DateTime GetModificationTime()
        {
            return Utils.GetDate(_mvhd.GetModificationTime());
        }

        /**
         * Returns the duration in seconds.
         * @return the duration
         */
        public double GetDuration()
        {
            return (double)_mvhd.GetDuration() / (double)_mvhd.GetTimeScale();
        }

        /**
         * Indicates if there are more frames to be read in this movie.
         *
         * @return true if there is at least one track in this movie that has at least one more frame to read.
         */
        public bool HasMoreFrames()
        {
            foreach (Track track in _tracks)
            {
                if (track.HasMoreFrames()) return true;
            }
            return false;
        }

        /**
         * Reads the next frame from this movie (from one of the contained tracks).
         * The frame is the next in time-order, thus the next for playback. If none
         * of the tracks contains any more frames, null is returned.
         *
         * @return the next frame or null if there are no more frames to read from this movie.
         * @throws IOException if reading fails
         */
        public Frame ReadNextFrame()
        {
            Track track = null;
		    foreach(Track t in _tracks) {
			    if(t.HasMoreFrames()&&(track==null||t.GetNextTimeStamp()<track.GetNextTimeStamp())) track = t;
		    }

		    return (track==null) ? null : track.ReadNextFrame();
	    }
    }
}
