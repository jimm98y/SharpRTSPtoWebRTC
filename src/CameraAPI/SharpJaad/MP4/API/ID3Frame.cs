using System;
using System.Globalization;
using System.Text;

namespace SharpJaad.MP4.API
{
    public class ID3Frame
    {
        public const int ALBUM_TITLE = 1413565506; //TALB
        public const int ALBUM_SORT_ORDER = 1414745921; //TSOA
        public const int ARTIST = 1414546737; //TPE1
        public const int ATTACHED_PICTURE = 1095780675; //APIC
        public const int AUDIO_ENCRYPTION = 1095061059; //AENC
        public const int AUDIO_SEEK_POINT_INDEX = 1095979081; //ASPI
        public const int BAND = 1414546738; //TPE2
        public const int BEATS_PER_MINUTE = 1413632077; //TBPM
        public const int COMMENTS = 1129270605; //COMM
        public const int COMMERCIAL_FRAME = 1129270610; //COMR
        public const int COMMERCIAL_INFORMATION = 1464029005; //WCOM
        public const int COMPOSER = 1413697357; //TCOM
        public const int CONDUCTOR = 1414546739; //TPE3
        public const int CONTENT_GROUP_DESCRIPTION = 1414091825; //TIT1
        public const int CONTENT_TYPE = 1413697358; //TCON
        public const int COPYRIGHT = 1464029008; //WCOP
        public const int COPYRIGHT_MESSAGE = 1413697360; //TCOP
        public const int ENCODED_BY = 1413828163; //TENC
        public const int ENCODING_TIME = 1413760334; //TDEN
        public const int ENCRYPTION_METHOD_REGISTRATION = 1162756946; //ENCR
        public const int EQUALISATION = 1162958130; //EQU2
        public const int EVENT_TIMING_CODES = 1163150159; //ETCO
        public const int FILE_OWNER = 1414485838; //TOWN
        public const int FILE_TYPE = 1413893204; //TFLT
        public const int GENERAL_ENCAPSULATED_OBJECT = 1195724610; //GEOB
        public const int GROUP_IDENTIFICATION_REGISTRATION = 1196575044; //GRID
        public const int INITIAL_KEY = 1414219097; //TKEY
        public const int INTERNET_RADIO_STATION_NAME = 1414681422; //TRSN
        public const int INTERNET_RADIO_STATION_OWNER = 1414681423; //TRSO
        public const int MODIFIED_BY = 1414546740; //TPE4
        public const int INVOLVED_PEOPLE_LIST = 1414090828; //TIPL
        public const int INTERNATIONAL_STANDARD_RECORDING_CODE = 1414746691; //TSRC
        public const int LANGUAGES = 1414283598; //TLAN
        public const int LENGTH = 1414284622; //TLEN
        public const int LINKED_INFORMATION = 1279872587; //LINK
        public const int LYRICIST = 1413830740; //TEXT
        public const int MEDIA_TYPE = 1414350148; //TMED
        public const int MOOD = 1414352719; //TMOO
        public const int MPEG_LOCATION_LOOKUP_TABLE = 1296845908; //MLLT
        public const int MUSICIAN_CREDITS_LIST = 1414349644; //TMCL
        public const int MUSIC_CD_IDENTIFIER = 1296254025; //MCDI
        public const int OFFICIAL_ARTIST_WEBPAGE = 1464811858; //WOAR
        public const int OFFICIAL_AUDIO_FILE_WEBPAGE = 1464811846; //WOAF
        public const int OFFICIAL_AUDIO_SOURCE_WEBPAGE = 1464811859; //WOAS
        public const int OFFICIAL_INTERNET_RADIO_STATION_HOMEPAGE = 1464816211; //WORS
        public const int ORIGINAL_ALBUM_TITLE = 1414480204; //TOAL
        public const int ORIGINAL_ARTIST = 1414484037; //TOPE
        public const int ORIGINAL_FILENAME = 1414481486; //TOFN
        public const int ORIGINAL_LYRICIST = 1414483033; //TOLY
        public const int ORIGINAL_RELEASE_TIME = 1413762898; //TDOR
        public const int OWNERSHIP_FRAME = 1331121733; //OWNE
        public const int PART_OF_A_SET = 1414549331; //TPOS
        public const int PAYMENT = 1464877401; //WPAY
        public const int PERFORMER_SORT_ORDER = 1414745936; //TSOP
        public const int PLAYLIST_DELAY = 1413762137; //TDLY
        public const int PLAY_COUNTER = 1346588244; //PCNT
        public const int POPULARIMETER = 1347375181; //POPM
        public const int POSITION_SYNCHRONISATION_FRAME = 1347375955; //POSS
        public const int PRIVATE_FRAME = 1347570006; //PRIV
        public const int PRODUCED_NOTICE = 1414550095; //TPRO
        public const int PUBLISHER = 1414550850; //TPUB
        public const int PUBLISHERS_OFFICIAL_WEBPAGE = 1464882498; //WPUB
        public const int RECOMMENDED_BUFFER_SIZE = 1380078918; //RBUF
        public const int RECORDING_TIME = 1413763651; //TDRC
        public const int RELATIVE_VOLUME_ADJUSTMENT = 1381384498; //RVA2
        public const int RELEASE_TIME = 1413763660; //TDRL
        public const int REVERB = 1381388866; //RVRB
        public const int SEEK_FRAME = 1397048651; //SEEK
        public const int SET_SUBTITLE = 1414746964; //TSST
        public const int SIGNATURE_FRAME = 1397311310; //SIGN
        public const int ENCODING_TOOLS_AND_SETTINGS = 1414746949; //TSSE
        public const int SUBTITLE = 1414091827; //TIT3
        public const int SYNCHRONISED_LYRIC = 1398361172; //SYLT
        public const int SYNCHRONISED_TEMPO_CODES = 1398363203; //SYTC
        public const int TAGGING_TIME = 1413764167; //TDTG
        public const int TERMS_OF_USE = 1431520594; //USER
        public const int TITLE = 1414091826; //TIT2
        public const int TITLE_SORT_ORDER = 1414745940; //TSOT
        public const int TRACK_NUMBER = 1414677323; //TRCK
        public const int UNIQUE_FILE_IDENTIFIER = 1430669636; //UFID
        public const int UNSYNCHRONISED_LYRIC = 1431522388; //USLT
        public const int USER_DEFINED_TEXT_INFORMATION_FRAME = 1415075928; //TXXX
        public const int USER_DEFINED_URL_LINK_FRAME = 1465407576; //WXXX
        private static string[] TEXT_ENCODINGS = {"ISO-8859-1", "UTF-16"/*BOM*/, "UTF-16", "UTF-8"};
	    private static string[] VALID_TIMESTAMPS = {"yyyy, yyyy-MM", "yyyy-MM-dd", "yyyy-MM-ddTHH", "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"};
	    private static string UNKNOWN_LANGUAGE = "xxx";
	    private long _size;
        private int _id, _flags, _groupID, _encryptionMethod;
        private byte[] _data;

        public ID3Frame(DataInputStream input)
        {
            _id = input.ReadInt();
		    _size = ID3Tag.ReadSynch(input);
		    _flags = input.ReadShort();

		    if(IsInGroup()) _groupID = input.ReadByte();
		    if(IsEncrypted()) _encryptionMethod = input.ReadByte();
            //TODO: data length indicator, unsync

            _data = new byte[(int)_size];
            input.ReadFully(_data);
        }

        //header data
        public int GetID()
        {
            return _id;
        }

        public long GetSize()
        {
            return _size;
        }

        public bool IsInGroup()
        {
            return (_flags & 0x40) == 0x40;
        }

        public int GetGroupID()
        {
            return _groupID;
        }

        public bool IsCompressed()
        {
            return (_flags & 8) == 8;
        }

        public bool IsEncrypted()
        {
            return (_flags & 4) == 4;
        }

        public int GetEncryptionMethod()
        {
            return _encryptionMethod;
        }

        //content data
        public byte[] GetData()
        {
            return _data;
        }

        public string GetText()
        {
            return Encoding.GetEncoding(TEXT_ENCODINGS[0]).GetString(_data);
        }

        public string GetEncodedText()
        {
            //first byte indicates encoding
            int enc = _data[0];

            //charsets 0,3 end with '0'; 1,2 end with '00'
            int t = -1;
            for (int i = 1; i < _data.Length && t < 0; i++)
            {
                if (_data[i] == 0 && (enc == 0 || enc == 3 || _data[i + 1] == 0)) t = i;
            }
            return Encoding.GetEncoding(TEXT_ENCODINGS[enc]).GetString(_data).Substring(1, t - 1);
        }

        public int GetNumber()
        {
            return int.Parse(Encoding.UTF8.GetString(_data));
        }

        public int[] GetNumbers()
        {
            //multiple numbers separated by '/'
            string x = Encoding.GetEncoding(TEXT_ENCODINGS[0]).GetString(_data);
            int i = x.IndexOf('/');
            int[] y;
            if (i > 0) y = new int[] { int.Parse(x.Substring(0, i)), int.Parse(x.Substring(i + 1)) };
            else y = new int[] { int.Parse(x) };
            return y;
        }

        public DateTime GetDate()
        {
            //timestamp lengths: 4,7,10,13,16,19
            int i = (int)Math.Floor((float)(_data.Length / 3)) - 1;
            DateTime date;
            if (i >= 0 && i < VALID_TIMESTAMPS.Length)
            {
                date = DateTime.ParseExact(Encoding.UTF8.GetString(_data), VALID_TIMESTAMPS[i], CultureInfo.InvariantCulture);
            }
            else date = DateTime.MinValue; // invalid
            return date;
        }

        public CultureInfo GetLocale()
        {
            string s = Encoding.UTF8.GetString(_data).ToLowerInvariant();
            CultureInfo l;
            if (s.Equals(UNKNOWN_LANGUAGE)) l = null;
            else l = new CultureInfo(s);
            return l;
        }
    }
}
