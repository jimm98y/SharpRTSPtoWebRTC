using SharpJaad.MP4.Boxes;
using SharpJaad.MP4.Boxes.Impl;
using SharpJaad.MP4.Boxes.Impl.Meta;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace SharpJaad.MP4.API
{
    /**
     * This class contains the metadata for a movie. It parses different metadata
     * types (iTunes tags, ID3).
     * The fields can be read via the <code>get(Field)</code> method using one of
     * the predefined <code>Field</code>s.
     *
     * @author in-somnia
     */
    public class MetaData
    {
        public class Field
        {
            public static readonly Field<string> ARTIST = new Field<string>("Artist");
            public static readonly Field<string> TITLE = new Field<string>("Title");
            public static readonly Field<string> ALBUM_ARTIST = new Field<string>("Album Artist");
            public static readonly Field<string> ALBUM = new Field<string>("Album");
            public static readonly Field<int> TRACK_NUMBER = new Field<int>("Track Number");
            public static readonly Field<int> TOTAL_TRACKS = new Field<int>("Total Tracks");
            public static readonly Field<int> DISK_NUMBER = new Field<int>("Disk Number");
            public static readonly Field<int> TOTAL_DISKS = new Field<int>("Total disks");
            public static readonly Field<string> COMPOSER = new Field<string>("Composer");
            public static readonly Field<string> COMMENTS = new Field<string>("Comments");
            public static readonly Field<int> TEMPO = new Field<int>("Tempo");
            public static readonly Field<int> LENGTH_IN_MILLISECONDS = new Field<int>("Length in milliseconds");
            public static readonly Field<DateTime> RELEASE_DATE = new Field<DateTime>("Release Date");
            public static readonly Field<string> GENRE = new Field<string>("Genre");
            public static readonly Field<string> ENCODER_NAME = new Field<string>("Encoder Name");
            public static readonly Field<string> ENCODER_TOOL = new Field<string>("Encoder Tool");
            public static readonly Field<DateTime> ENCODING_DATE = new Field<DateTime>("Encoding Date");
            public static readonly Field<string> COPYRIGHT = new Field<string>("Copyright");
            public static readonly Field<string> PUBLISHER = new Field<string>("Publisher");
            public static readonly Field<bool> COMPILATION = new Field<bool>("Part of compilation");
            public static readonly Field<List<Artwork>> COVER_ARTWORKS = new Field<List<Artwork>>("Cover Artworks");
            public static readonly Field<string> GROUPING = new Field<string>("Grouping");
            public static readonly Field<string> LOCATION = new Field<string>("Location");
            public static readonly Field<string> LYRICS = new Field<string>("Lyrics");
            public static readonly Field<int> RATING = new Field<int>("Rating");
            public static readonly Field<int> PODCAST = new Field<int>("Podcast");
            public static readonly Field<string> PODCAST_URL = new Field<string>("Podcast URL");
            public static readonly Field<string> CATEGORY = new Field<string>("Category");
            public static readonly Field<string> KEYWORDS = new Field<string>("Keywords");
            public static readonly Field<int> EPISODE_GLOBAL_UNIQUE_ID = new Field<int>("Episode Global Unique ID");
            public static readonly Field<string> DESCRIPTION = new Field<string>("Description");
            public static readonly Field<string> TV_SHOW = new Field<string>("TV Show");
            public static readonly Field<string> TV_NETWORK = new Field<string>("TV Network");
            public static readonly Field<string> TV_EPISODE = new Field<string>("TV Episode");
            public static readonly Field<int> TV_EPISODE_NUMBER = new Field<int>("TV Episode Number");
            public static readonly Field<int> TV_SEASON = new Field<int>("TV Season");
            public static readonly Field<string> INTERNET_RADIO_STATION = new Field<string>("Internet Radio Station");
            public static readonly Field<string> PURCHASE_DATE = new Field<string>("Purchase Date");
            public static readonly Field<string> GAPLESS_PLAYBACK = new Field<string>("Gapless Playback");
            public static readonly Field<bool> HD_VIDEO = new Field<bool>("HD Video");
            public static readonly Field<CultureInfo> LANGUAGE = new Field<CultureInfo>("Language");
            //sorting     readonly
            public static readonly Field<string> ARTIST_SORT_TEXT = new Field<string>("Artist Sort Text");
            public static readonly Field<string> TITLE_SORT_TEXT = new Field<string>("Title Sort Text");
            public static readonly Field<string> ALBUM_SORT_TEXT = new Field<string>("Album Sort Text");
        }

        public class Field<T> : Field
        {
            private string name;

            public Field(string name)
            {
                this.name = name;
            }

            public string GetName()
            {
                return name;
            }
        }

        private static string[] STANDARD_GENRES = 
        {
		    "undefined",
		    //IDv1 standard
		    "blues",
		    "classic rock",
		    "country",
		    "dance",
		    "disco",
		    "funk",
		    "grunge",
		    "hip hop",
		    "jazz",
		    "metal",
		    "new age",
		    "oldies",
		    "other",
		    "pop",
		    "r and b",
		    "rap",
		    "reggae",
		    "rock",
		    "techno",
		    "industrial",
		    "alternative",
		    "ska",
		    "death metal",
		    "pranks",
		    "soundtrack",
		    "euro techno",
		    "ambient",
		    "trip hop",
		    "vocal",
		    "jazz funk",
		    "fusion",
		    "trance",
		    "classical",
		    "instrumental",
		    "acid",
		    "house",
		    "game",
		    "sound clip",
		    "gospel",
		    "noise",
		    "alternrock",
		    "bass",
		    "soul",
		    "punk",
		    "space",
		    "meditative",
		    "instrumental pop",
		    "instrumental rock",
		    "ethnic",
		    "gothic",
		    "darkwave",
		    "techno industrial",
		    "electronic",
		    "pop folk",
		    "eurodance",
		    "dream",
		    "southern rock",
		    "comedy",
		    "cult",
		    "gangsta",
		    "top ",
		    "christian rap",
		    "pop funk",
		    "jungle",
		    "native american",
		    "cabaret",
		    "new wave",
		    "psychedelic",
		    "rave",
		    "showtunes",
		    "trailer",
		    "lo fi",
		    "tribal",
		    "acid punk",
		    "acid jazz",
		    "polka",
		    "retro",
		    "musical",
		    "rock and roll",
		    //winamp extension
		    "hard rock",
		    "folk",
		    "folk rock",
		    "national folk",
		    "swing",
		    "fast fusion",
		    "bebob",
		    "latin",
		    "revival",
		    "celtic",
		    "bluegrass",
		    "avantgarde",
		    "gothic rock",
		    "progressive rock",
		    "psychedelic rock",
		    "symphonic rock",
		    "slow rock",
		    "big band",
		    "chorus",
		    "easy listening",
		    "acoustic",
		    "humour",
		    "speech",
		    "chanson",
		    "opera",
		    "chamber music",
		    "sonata",
		    "symphony",
		    "booty bass",
		    "primus",
		    "porn groove",
		    "satire",
		    "slow jam",
		    "club",
		    "tango",
		    "samba",
		    "folklore",
		    "ballad",
		    "power ballad",
		    "rhythmic soul",
		    "freestyle",
		    "duet",
		    "punk rock",
		    "drum solo",
		    "a capella",
		    "euro house",
		    "dance hall"
	    };
	    
        private static readonly string[] NERO_TAGS =
        {
		    "artist", "title", "album", "track", "totaltracks", "year", "genre",
		    "disc", "totaldiscs", "url", "copyright", "comment", "lyrics",
		    "credits", "rating", "label", "composer", "isrc", "mood", "tempo"
	    };

	    private Dictionary<Field, object> _contents;

        public MetaData()
        {
            _contents = new Dictionary<Field, object>();
        }

        /*moov.udta:
         * -3gpp boxes
         * -meta
         * --ilst
         * --tags
         * --meta (no container!)
         * --tseg
         * ---tshd
         */
        public void Parse(Box udta, Box meta)
        {
            //standard boxes
            if (meta.HasChild(BoxTypes.COPYRIGHT_BOX))
            {
                CopyrightBox cprt = (CopyrightBox)meta.GetChild(BoxTypes.COPYRIGHT_BOX);
                Put(Field.LANGUAGE, new CultureInfo(cprt.GetLanguageCode()));
                Put(Field.COPYRIGHT, cprt.GetNotice());
            }
            //3gpp user data
            if (udta != null) Parse3GPPData(udta);
            //id3, TODO: can be present in different languages
            if (meta.HasChild(BoxTypes.ID3_TAG_BOX)) ParseID3((ID3TagBox)meta.GetChild(BoxTypes.ID3_TAG_BOX));
            //itunes
            if (meta.HasChild(BoxTypes.ITUNES_META_LIST_BOX)) ParseITunesMetaData(meta.GetChild(BoxTypes.ITUNES_META_LIST_BOX));
            //nero tags
            if (meta.HasChild(BoxTypes.NERO_METADATA_TAGS_BOX)) ParseNeroTags((NeroMetadataTagsBox)meta.GetChild(BoxTypes.NERO_METADATA_TAGS_BOX));
        }

        //parses specific children of 'udta': 3GPP
        //TODO: handle language codes
        private void Parse3GPPData(Box udta)
        {
            if (udta.HasChild(BoxTypes.THREE_GPP_ALBUM_BOX))
            {
                ThreeGPPAlbumBox albm = (ThreeGPPAlbumBox)udta.GetChild(BoxTypes.THREE_GPP_ALBUM_BOX);
                Put(Field.ALBUM, albm.GetData());
                Put(Field.TRACK_NUMBER, albm.GetTrackNumber());
            }
            //if(udta.hasChild(BoxTypes.THREE_GPP_AUTHOR_BOX));
            //if(udta.hasChild(BoxTypes.THREE_GPP_CLASSIFICATION_BOX));
            if (udta.HasChild(BoxTypes.THREE_GPP_DESCRIPTION_BOX)) Put(Field.DESCRIPTION, ((ThreeGPPMetadataBox)udta.GetChild(BoxTypes.THREE_GPP_DESCRIPTION_BOX)).GetData());
            if (udta.HasChild(BoxTypes.THREE_GPP_KEYWORDS_BOX)) Put(Field.KEYWORDS, ((ThreeGPPMetadataBox)udta.GetChild(BoxTypes.THREE_GPP_KEYWORDS_BOX)).GetData());
            if (udta.HasChild(BoxTypes.THREE_GPP_LOCATION_INFORMATION_BOX)) Put(Field.LOCATION, ((ThreeGPPLocationBox)udta.GetChild(BoxTypes.THREE_GPP_LOCATION_INFORMATION_BOX)).GetPlaceName());
            if (udta.HasChild(BoxTypes.THREE_GPP_PERFORMER_BOX)) Put(Field.ARTIST, ((ThreeGPPMetadataBox)udta.GetChild(BoxTypes.THREE_GPP_PERFORMER_BOX)).GetData());
            if (udta.HasChild(BoxTypes.THREE_GPP_RECORDING_YEAR_BOX))
            {
                string value = ((ThreeGPPMetadataBox)udta.GetChild(BoxTypes.THREE_GPP_RECORDING_YEAR_BOX)).GetData();
                try
                {
                    Put(Field.RELEASE_DATE, new DateTime(int.Parse(value)));
                }
                catch (FormatException)
                {
                    Debug.WriteLine("unable to parse 3GPP metadata: recording year value: {0}", value);
                    //Logger.getLogger("MP4 API").log(Level.INFO, "unable to parse 3GPP metadata: recording year value: {0}", value);
                }
            }
            if (udta.HasChild(BoxTypes.THREE_GPP_TITLE_BOX)) Put(Field.TITLE, ((ThreeGPPMetadataBox)udta.GetChild(BoxTypes.THREE_GPP_TITLE_BOX)).GetData());
        }

        //parses children of 'ilst': iTunes
        private void ParseITunesMetaData(Box ilst)
        {
            List<Box> boxes = ilst.GetChildren();
            long l;
            ITunesMetadataBox data;
            foreach (Box box in boxes)
            {
                l = box.GetBoxType();
                data = (ITunesMetadataBox)box.GetChild(BoxTypes.ITUNES_METADATA_BOX);

                if (l == BoxTypes.ARTIST_NAME_BOX) Put(Field.ARTIST, data.GetText());
                else if (l == BoxTypes.TRACK_NAME_BOX) Put(Field.TITLE, data.GetText());
                else if (l == BoxTypes.ALBUM_ARTIST_NAME_BOX) Put(Field.ALBUM_ARTIST, data.GetText());
                else if (l == BoxTypes.ALBUM_NAME_BOX) Put(Field.ALBUM, data.GetText());
                else if (l == BoxTypes.TRACK_NUMBER_BOX)
                {
                    byte[] b = data.GetData();
                    Put(Field.TRACK_NUMBER, (int)(b[3]));
                    Put(Field.TOTAL_TRACKS, (int)(b[5]));
                }
                else if (l == BoxTypes.DISK_NUMBER_BOX) Put(Field.DISK_NUMBER, data.GetInteger());
                else if (l == BoxTypes.COMPOSER_NAME_BOX) Put(Field.COMPOSER, data.GetText());
                else if (l == BoxTypes.COMMENTS_BOX) Put(Field.COMMENTS, data.GetText());
                else if (l == BoxTypes.TEMPO_BOX) Put(Field.TEMPO, data.GetInteger());
                else if (l == BoxTypes.RELEASE_DATE_BOX) Put(Field.RELEASE_DATE, data.GetDate());
                else if (l == BoxTypes.GENRE_BOX || l == BoxTypes.CUSTOM_GENRE_BOX)
                {
                    String s = null;
                    if (data.GetDataType() == ITunesMetadataBox.DataType.UTF8) s = data.GetText();
                    else
                    {
                        int i = data.GetInteger();
                        if (i > 0 && i < STANDARD_GENRES.Length) s = STANDARD_GENRES[data.GetInteger()];
                    }
                    if (s != null) Put(Field.GENRE, s);
                }
                else if (l == BoxTypes.ENCODER_NAME_BOX) Put(Field.ENCODER_NAME, data.GetText());
                else if (l == BoxTypes.ENCODER_TOOL_BOX) Put(Field.ENCODER_TOOL, data.GetText());
                else if (l == BoxTypes.COPYRIGHT_BOX) Put(Field.COPYRIGHT, data.GetText());
                else if (l == BoxTypes.COMPILATION_PART_BOX) Put(Field.COMPILATION, data.GetBoolean());
                else if (l == BoxTypes.COVER_BOX)
                {
                    Artwork aw = new Artwork(Artwork.ForDataType(data.GetDataType()), data.GetData());
                    if (_contents.ContainsKey(Field.COVER_ARTWORKS))
                    {
                        Get<List<Artwork>>(Field.COVER_ARTWORKS).Add(aw);
                    }
                    else
                    {
                        List<Artwork> list = new List<Artwork>();
                        list.Add(aw);
                        Put(Field.COVER_ARTWORKS, list);
                    }
                }
                else if (l == BoxTypes.GROUPING_BOX) Put(Field.GROUPING, data.GetText());
                else if (l == BoxTypes.LYRICS_BOX) Put(Field.LYRICS, data.GetText());
                else if (l == BoxTypes.RATING_BOX) Put(Field.RATING, data.GetInteger());
                else if (l == BoxTypes.PODCAST_BOX) Put(Field.PODCAST, data.GetInteger());
                else if (l == BoxTypes.PODCAST_URL_BOX) Put(Field.PODCAST_URL, data.GetText());
                else if (l == BoxTypes.CATEGORY_BOX) Put(Field.CATEGORY, data.GetText());
                else if (l == BoxTypes.KEYWORD_BOX) Put(Field.KEYWORDS, data.GetText());
                else if (l == BoxTypes.DESCRIPTION_BOX) Put(Field.DESCRIPTION, data.GetText());
                else if (l == BoxTypes.LONG_DESCRIPTION_BOX) Put(Field.DESCRIPTION, data.GetText());
                else if (l == BoxTypes.TV_SHOW_BOX) Put(Field.TV_SHOW, data.GetText());
                else if (l == BoxTypes.TV_NETWORK_NAME_BOX) Put(Field.TV_NETWORK, data.GetText());
                else if (l == BoxTypes.TV_EPISODE_BOX) Put(Field.TV_EPISODE, data.GetText());
                else if (l == BoxTypes.TV_EPISODE_NUMBER_BOX) Put(Field.TV_EPISODE_NUMBER, data.GetInteger());
                else if (l == BoxTypes.TV_SEASON_BOX) Put(Field.TV_SEASON, data.GetInteger());
                else if (l == BoxTypes.PURCHASE_DATE_BOX) Put(Field.PURCHASE_DATE, data.GetText());
                else if (l == BoxTypes.GAPLESS_PLAYBACK_BOX) Put(Field.GAPLESS_PLAYBACK, data.GetText());
                else if (l == BoxTypes.HD_VIDEO_BOX) Put(Field.HD_VIDEO, data.GetBoolean());
                else if (l == BoxTypes.ARTIST_SORT_BOX) Put(Field.ARTIST_SORT_TEXT, data.GetText());
                else if (l == BoxTypes.TRACK_SORT_BOX) Put(Field.TITLE_SORT_TEXT, data.GetText());
                else if (l == BoxTypes.ALBUM_SORT_BOX) Put(Field.ALBUM_SORT_TEXT, data.GetText());
            }
        }

        //parses children of ID3
        private void ParseID3(ID3TagBox box)
        {
            try
            {
                DataInputStream input = new DataInputStream(new MemoryStream(box.GetID3Data()));
                ID3Tag tag = new ID3Tag(input);
                int[] num;
                foreach (ID3Frame frame in tag.GetFrames())
                {
                    switch (frame.GetID())
                    {
                        case ID3Frame.TITLE:
                            Put(Field.TITLE, frame.GetEncodedText());
                            break;
                        case ID3Frame.ALBUM_TITLE:
                            Put(Field.ALBUM, frame.GetEncodedText());
                            break;
                        case ID3Frame.TRACK_NUMBER:
                            num = frame.GetNumbers();
                            Put(Field.TRACK_NUMBER, num[0]);
                            if (num.Length > 1) Put(Field.TOTAL_TRACKS, num[1]);
                            break;
                        case ID3Frame.ARTIST:
                            Put(Field.ARTIST, frame.GetEncodedText());
                            break;
                        case ID3Frame.COMPOSER:
                            Put(Field.COMPOSER, frame.GetEncodedText());
                            break;
                        case ID3Frame.BEATS_PER_MINUTE:
                            Put(Field.TEMPO, frame.GetNumber());
                            break;
                        case ID3Frame.LENGTH:
                            Put(Field.LENGTH_IN_MILLISECONDS, frame.GetNumber());
                            break;
                        case ID3Frame.LANGUAGES:
                            Put(Field.LANGUAGE, frame.GetLocale());
                            break;
                        case ID3Frame.COPYRIGHT_MESSAGE:
                            Put(Field.COPYRIGHT, frame.GetEncodedText());
                            break;
                        case ID3Frame.PUBLISHER:
                            Put(Field.PUBLISHER, frame.GetEncodedText());
                            break;
                        case ID3Frame.INTERNET_RADIO_STATION_NAME:
                            Put(Field.INTERNET_RADIO_STATION, frame.GetEncodedText());
                            break;
                        case ID3Frame.ENCODING_TIME:
                            Put(Field.ENCODING_DATE, frame.GetDate());
                            break;
                        case ID3Frame.RELEASE_TIME:
                            Put(Field.RELEASE_DATE, frame.GetDate());
                            break;
                        case ID3Frame.ENCODING_TOOLS_AND_SETTINGS:
                            Put(Field.ENCODER_TOOL, frame.GetEncodedText());
                            break;
                        case ID3Frame.PERFORMER_SORT_ORDER:
                            Put(Field.ARTIST_SORT_TEXT, frame.GetEncodedText());
                            break;
                        case ID3Frame.TITLE_SORT_ORDER:
                            Put(Field.TITLE_SORT_TEXT, frame.GetEncodedText());
                            break;
                        case ID3Frame.ALBUM_SORT_ORDER:
                            Put(Field.ALBUM_SORT_TEXT, frame.GetEncodedText());
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception in MetaData.parseID3: {0}", e.ToString());
                //Logger.getLogger("MP4 API").log(Level.SEVERE, "Exception in MetaData.parseID3: {0}", e.toString());
            }
        }

        //parses children of 'tags': Nero
        private void ParseNeroTags(NeroMetadataTagsBox tags)
        {
            Dictionary<string, string> pairs = tags.GetPairs();
            string val;
            foreach (string key in pairs.Keys)
            {
                val = pairs[key];
                try
                {
                    if (key.Equals(NERO_TAGS[0])) Put(Field.ARTIST, val);
                    if (key.Equals(NERO_TAGS[1])) Put(Field.TITLE, val);
                    if (key.Equals(NERO_TAGS[2])) Put(Field.ALBUM, val);
                    if (key.Equals(NERO_TAGS[3])) Put(Field.TRACK_NUMBER, int.Parse(val));
                    if (key.Equals(NERO_TAGS[4])) Put(Field.TOTAL_TRACKS, int.Parse(val));
                    if (key.Equals(NERO_TAGS[5]))
                    {
                        Put(Field.RELEASE_DATE, new DateTime(int.Parse(val), 1, 1));
                    }
                    if (key.Equals(NERO_TAGS[6])) Put(Field.GENRE, val);
                    if (key.Equals(NERO_TAGS[7])) Put(Field.DISK_NUMBER, int.Parse(val));
                    if (key.Equals(NERO_TAGS[8])) Put(Field.TOTAL_DISKS, int.Parse(val));
                    //if (key.Equals(NERO_TAGS[9])) ; //url
                    if (key.Equals(NERO_TAGS[10])) Put(Field.COPYRIGHT, val);
                    if (key.Equals(NERO_TAGS[11])) Put(Field.COMMENTS, val);
                    if (key.Equals(NERO_TAGS[12])) Put(Field.LYRICS, val);
                    //if (key.Equals(NERO_TAGS[13])) ; //credits
                    if (key.Equals(NERO_TAGS[14])) Put(Field.RATING, int.Parse(val));
                    if (key.Equals(NERO_TAGS[15])) Put(Field.PUBLISHER, val);
                    if (key.Equals(NERO_TAGS[16])) Put(Field.COMPOSER, val);
                    //if (key.Equals(NERO_TAGS[17])) ; //isrc
                    //if (key.Equals(NERO_TAGS[18])) ; //mood
                    if (key.Equals(NERO_TAGS[19])) Put(Field.TEMPO, int.Parse(val));
                }
                catch (FormatException e)
                {
                    Debug.WriteLine("Exception in MetaData.parseNeroTags: {0}", e.ToString());
                    //Logger.getLogger("MP4 API").log(Level.SEVERE, "Exception in MetaData.parseNeroTags: {0}", e.toString());
                }
            }
        }

        private void Put<T>(Field field, T value)
        {
            _contents.Add(field, value);
        }

        public bool ContainsMetaData()
        {
            return _contents.Count != 0;
        }

        public T Get<T>(Field field)
        {
            return (T)_contents[field];
        }

        public Dictionary<Field, object> GetAll()
        {
            return new Dictionary<Field, object>(_contents);
        }
    }
}
