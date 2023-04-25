using System;
using System.Collections.Generic;
using System.Text;

namespace SharpJaad.MP4.Boxes
{
    public static class BoxTypes
    {
        public const long EXTENDED_TYPE = 1970628964; //uuid
                                                      //standard boxes (ISO BMFF)
        public const long ADDITIONAL_METADATA_CONTAINER_BOX = 1835361135; //meco
        public const long APPLE_LOSSLESS_BOX = 1634492771; //alac
        public const long BINARY_XML_BOX = 1652059500; //bxml
        public const long BIT_RATE_BOX = 1651798644; //btrt
        public const long CHAPTER_BOX = 1667788908; //chpl
        public const long CHUNK_OFFSET_BOX = 1937007471; //stco
        public const long CHUNK_LARGE_OFFSET_BOX = 1668232756; //co64
        public const long CLEAN_APERTURE_BOX = 1668047216; //clap
        public const long COMPACT_SAMPLE_SIZE_BOX = 1937013298; //stz2
        public const long COMPOSITION_TIME_TO_SAMPLE_BOX = 1668576371; //ctts
        public const long COPYRIGHT_BOX = 1668313716; //cprt
        public const long DATA_ENTRY_URN_BOX = 1970433568; //urn 
        public const long DATA_ENTRY_URL_BOX = 1970433056; //url 
        public const long DATA_INFORMATION_BOX = 1684631142; //dinf
        public const long DATA_REFERENCE_BOX = 1685218662; //dref
        public const long DECODING_TIME_TO_SAMPLE_BOX = 1937011827; //stts
        public const long DEGRADATION_PRIORITY_BOX = 1937007728; //stdp
        public const long EDIT_BOX = 1701082227; //edts
        public const long EDIT_LIST_BOX = 1701606260; //elst
        public const long FD_ITEM_INFORMATION_BOX = 1718184302; //fiin
        public const long FD_SESSION_GROUP_BOX = 1936025458; //segr
        public const long FEC_RESERVOIR_BOX = 1717920626; //fecr
        public const long FILE_PARTITION_BOX = 1718641010; //fpar
        public const long FILE_TYPE_BOX = 1718909296; //ftyp
        public const long FREE_SPACE_BOX = 1718773093; //free
        public const long GROUP_ID_TO_NAME_BOX = 1734964334; //gitn
        public const long HANDLER_BOX = 1751411826; //hdlr
        public const long HINT_MEDIA_HEADER_BOX = 1752000612; //hmhd
        public const long IPMP_CONTROL_BOX = 1768975715; //ipmc
        public const long IPMP_INFO_BOX = 1768778086; //imif
        public const long ITEM_INFORMATION_BOX = 1768517222; //iinf
        public const long ITEM_INFORMATION_ENTRY = 1768842853; //infe
        public const long ITEM_LOCATION_BOX = 1768714083; //iloc
        public const long ITEM_PROTECTION_BOX = 1768977007; //ipro
        public const long MEDIA_BOX = 1835297121; //mdia
        public const long MEDIA_DATA_BOX = 1835295092; //mdat
        public const long MEDIA_HEADER_BOX = 1835296868; //mdhd
        public const long MEDIA_INFORMATION_BOX = 1835626086; //minf
        public const long META_BOX = 1835365473; //meta
        public const long META_BOX_RELATION_BOX = 1835364965; //mere
        public const long MOVIE_BOX = 1836019574; //moov
        public const long MOVIE_EXTENDS_BOX = 1836475768; //mvex
        public const long MOVIE_EXTENDS_HEADER_BOX = 1835362404; //mehd
        public const long MOVIE_FRAGMENT_BOX = 1836019558; //moof
        public const long MOVIE_FRAGMENT_HEADER_BOX = 1835427940; //mfhd
        public const long MOVIE_FRAGMENT_RANDOM_ACCESS_BOX = 1835430497; //mfra
        public const long MOVIE_FRAGMENT_RANDOM_ACCESS_OFFSET_BOX = 1835430511; //mfro
        public const long MOVIE_HEADER_BOX = 1836476516; //mvhd
        public const long NERO_METADATA_TAGS_BOX = 1952540531; //tags
        public const long NULL_MEDIA_HEADER_BOX = 1852663908; //nmhd
        public const long ORIGINAL_FORMAT_BOX = 1718775137; //frma
        public const long PADDING_BIT_BOX = 1885430882; //padb
        public const long PARTITION_ENTRY = 1885431150; //paen
        public const long PIXEL_ASPECT_RATIO_BOX = 1885434736; //pasp
        public const long PRIMARY_ITEM_BOX = 1885959277; //pitm
        public const long PROGRESSIVE_DOWNLOAD_INFORMATION_BOX = 1885628782; //pdin
        public const long PROTECTION_SCHEME_INFORMATION_BOX = 1936289382; //sinf
        public const long SAMPLE_DEPENDENCY_TYPE_BOX = 1935963248; //sdtp
        public const long SAMPLE_DESCRIPTION_BOX = 1937011556; //stsd
        public const long SAMPLE_GROUP_DESCRIPTION_BOX = 1936158820; //sgpd
        public const long SAMPLE_SCALE_BOX = 1937011564; //stsl
        public const long SAMPLE_SIZE_BOX = 1937011578; //stsz
        public const long SAMPLE_TABLE_BOX = 1937007212; //stbl
        public const long SAMPLE_TO_CHUNK_BOX = 1937011555; //stsc
        public const long SAMPLE_TO_GROUP_BOX = 1935828848; //sbgp
        public const long SCHEME_TYPE_BOX = 1935894637; //schm
        public const long SCHEME_INFORMATION_BOX = 1935894633; //schi
        public const long SHADOW_SYNC_SAMPLE_BOX = 1937011560; //stsh
        public const long SKIP_BOX = 1936419184; //skip
        public const long SOUND_MEDIA_HEADER_BOX = 1936549988; //smhd
        public const long SUB_SAMPLE_INFORMATION_BOX = 1937072755; //subs
        public const long SYNC_SAMPLE_BOX = 1937011571; //stss
        public const long TRACK_BOX = 1953653099; //trak
        public const long TRACK_EXTENDS_BOX = 1953654136; //trex
        public const long TRACK_FRAGMENT_BOX = 1953653094; //traf
        public const long TRACK_FRAGMENT_HEADER_BOX = 1952868452; //tfhd
        public const long TRACK_FRAGMENT_RANDOM_ACCESS_BOX = 1952871009; //tfra
        public const long TRACK_FRAGMENT_RUN_BOX = 1953658222; //trun
        public const long TRACK_HEADER_BOX = 1953196132; //tkhd
        public const long TRACK_REFERENCE_BOX = 1953654118; //tref
        public const long TRACK_SELECTION_BOX = 1953719660; //tsel
        public const long USER_DATA_BOX = 1969517665; //udta
        public const long VIDEO_MEDIA_HEADER_BOX = 1986881636; //vmhd
        public const long WIDE_BOX = 2003395685; //wide
        public const long XML_BOX = 2020437024; //xml 
                                                 //mp4 extension
        public const long OBJECT_DESCRIPTOR_BOX = 1768907891; //iods
        public const long SAMPLE_DEPENDENCY_BOX = 1935959408; //sdep
                                                               //metadata: id3
        public const long ID3_TAG_BOX = 1768174386; //id32
                                                     //metadata: itunes
        public const long ITUNES_META_LIST_BOX = 1768715124; //ilst
        public const long CUSTOM_ITUNES_METADATA_BOX = 757935405; //----
        public const long ITUNES_METADATA_BOX = 1684108385; //data
        public const long ITUNES_METADATA_NAME_BOX = 1851878757; //name
        public const long ITUNES_METADATA_MEAN_BOX = 1835360622; //mean
        public const long ALBUM_ARTIST_NAME_BOX = 1631670868; //aART
        public const long ALBUM_ARTIST_SORT_BOX = 1936679265; //soaa 
        public const long ALBUM_NAME_BOX = 2841734242; //©alb
        public const long ALBUM_SORT_BOX = 1936679276; //soal
        public const long ARTIST_NAME_BOX = 2839630420; //©ART
        public const long ARTIST_SORT_BOX = 1936679282; //soar
        public const long CATEGORY_BOX = 1667331175; //catg
        public const long COMMENTS_BOX = 2841865588; //©cmt
        public const long COMPILATION_PART_BOX = 1668311404; //cpil 
        public const long COMPOSER_NAME_BOX = 2843177588; //©wrt
        public const long COMPOSER_SORT_BOX = 1936679791; //soco
        public const long COVER_BOX = 1668249202; //covr
        public const long CUSTOM_GENRE_BOX = 2842125678; //©gen
        public const long DESCRIPTION_BOX = 1684370275; //desc
        public const long DISK_NUMBER_BOX = 1684632427; //disk
        public const long ENCODER_NAME_BOX = 2841996899; //©enc
        public const long ENCODER_TOOL_BOX = 2842980207; //©too
        public const long EPISODE_GLOBAL_UNIQUE_ID_BOX = 1701276004; //egid
        public const long GAPLESS_PLAYBACK_BOX = 1885823344; //pgap
        public const long GENRE_BOX = 1735291493; //gnre
        public const long GROUPING_BOX = 2842129008; //©grp
        public const long HD_VIDEO_BOX = 1751414372; //hdvd
        public const long ITUNES_PURCHASE_ACCOUNT_BOX = 1634748740; //apID
        public const long ITUNES_ACCOUNT_TYPE_BOX = 1634421060; //akID
        public const long ITUNES_CATALOGUE_ID_BOX = 1668172100; //cnID
        public const long ITUNES_COUNTRY_CODE_BOX = 1936083268; //sfID
        public const long KEYWORD_BOX = 1801812343; //keyw
        public const long LONG_DESCRIPTION_BOX = 1818518899; //ldes
        public const long LYRICS_BOX = 2842458482; //©lyr
        public const long META_TYPE_BOX = 1937009003; //stik
        public const long PODCAST_BOX = 1885565812; //pcst
        public const long PODCAST_URL_BOX = 1886745196; //purl
        public const long PURCHASE_DATE_BOX = 1886745188; //purd
        public const long RATING_BOX = 1920233063; //rtng
        public const long RELEASE_DATE_BOX = 2841928057; //©day
        public const long REQUIREMENT_BOX = 2842846577; //©req
        public const long TEMPO_BOX = 1953329263; //tmpo
        public const long TRACK_NAME_BOX = 2842583405; //©nam
        public const long TRACK_NUMBER_BOX = 1953655662; //trkn
        public const long TRACK_SORT_BOX = 1936682605; //sonm
        public const long TV_EPISODE_BOX = 1953916275; //tves
        public const long TV_EPISODE_NUMBER_BOX = 1953916270; //tven
        public const long TV_NETWORK_NAME_BOX = 1953918574; //tvnn
        public const long TV_SEASON_BOX = 1953919854; //tvsn
        public const long TV_SHOW_BOX = 1953919848; //tvsh
        public const long TV_SHOW_SORT_BOX = 1936683886; //sosn
                                           //metadata: 3gpp
        public const long THREE_GPP_ALBUM_BOX = 1634493037; //albm
        public const long THREE_GPP_AUTHOR_BOX = 1635087464; //auth
        public const long THREE_GPP_CLASSIFICATION_BOX = 1668051814; //clsf
        public const long THREE_GPP_DESCRIPTION_BOX = 1685283696; //dscp
        public const long THREE_GPP_KEYWORDS_BOX = 1803122532; //kywd
        public const long THREE_GPP_LOCATION_INFORMATION_BOX = 1819239273; //loci
        public const long THREE_GPP_PERFORMER_BOX = 1885696614; //perf
        public const long THREE_GPP_RECORDING_YEAR_BOX = 2037543523; //yrrc
        public const long THREE_GPP_TITLE_BOX = 1953068140; //titl
                                              //metadata: google/youtube
        public const long GOOGLE_HOST_HEADER_BOX = 1735616616; //gshh
        public const long GOOGLE_PING_MESSAGE_BOX = 1735618669; //gspm
        public const long GOOGLE_PING_URL_BOX = 1735618677; //gspu
        public const long GOOGLE_SOURCE_DATA_BOX = 1735619428; //gssd
        public const long GOOGLE_START_TIME_BOX = 1735619444; //gsst
        public const long GOOGLE_TRACK_DURATION_BOX = 1735619684; //gstd
                                                      //sample entries
        public const long MP4V_SAMPLE_ENTRY = 1836070006; //mp4v
        public const long H263_SAMPLE_ENTRY = 1932670515; //s263
        public const long ENCRYPTED_VIDEO_SAMPLE_ENTRY = 1701733238; //encv
        public const long AVC_SAMPLE_ENTRY = 1635148593; //avc1
        public const long MP4A_SAMPLE_ENTRY = 1836069985; //mp4a
        public const long AC3_SAMPLE_ENTRY = 1633889587; //ac-3
        public const long EAC3_SAMPLE_ENTRY = 1700998451; //ec-3
        public const long DRMS_SAMPLE_ENTRY = 1685220723; //drms
        public const long AMR_SAMPLE_ENTRY = 1935764850; //samr
        public const long AMR_WB_SAMPLE_ENTRY = 1935767394; //sawb
        public const long EVRC_SAMPLE_ENTRY = 1936029283; //sevc
        public const long QCELP_SAMPLE_ENTRY = 1936810864; //sqcp
        public const long SMV_SAMPLE_ENTRY = 1936944502; //ssmv
        public const long ENCRYPTED_AUDIO_SAMPLE_ENTRY = 1701733217; //enca
        public const long MPEG_SAMPLE_ENTRY = 1836070003; //mp4s
        public const long TEXT_METADATA_SAMPLE_ENTRY = 1835365492; //mett
        public const long XML_METADATA_SAMPLE_ENTRY = 1835365496; //metx
        public const long RTP_HINT_SAMPLE_ENTRY = 1920233504; //rtp 
        public const long FD_HINT_SAMPLE_ENTRY = 1717858336; //fdp 
                                                //codec infos
        public const long ESD_BOX = 1702061171; //esds
                                      //video codecs
        public const long H263_SPECIFIC_BOX = 1681012275; //d263
        public const long AVC_SPECIFIC_BOX = 1635148611; //avcC
                                              //audio codecs
        public const long AC3_SPECIFIC_BOX = 1684103987; //dac3
        public const long EAC3_SPECIFIC_BOX = 1684366131; //dec3
        public const long AMR_SPECIFIC_BOX = 1684106610; //damr
        public const long EVRC_SPECIFIC_BOX = 1684371043; //devc
        public const long QCELP_SPECIFIC_BOX = 1685152624; //dqcp
        public const long SMV_SPECIFIC_BOX = 1685286262; //dsmv
                                             //OMA DRM
        public const long OMA_ACCESS_UNIT_FORMAT_BOX = 1868849510; //odaf
        public const long OMA_COMMON_HEADERS_BOX = 1869112434; //ohdr
        public const long OMA_CONTENT_ID_BOX = 1667459428; //ccid
        public const long OMA_CONTENT_OBJECT_BOX = 1868850273; //odda
        public const long OMA_COVER_URI_BOX = 1668706933; //cvru
        public const long OMA_DISCRETE_MEDIA_HEADERS_BOX = 1868851301; //odhe
        public const long OMA_DRM_CONTAINER_BOX = 1868853869; //odrm
        public const long OMA_ICON_URI_BOX = 1768124021; //icnu
        public const long OMA_INFO_URL_BOX = 1768842869; //infu
        public const long OMA_LYRICS_URI_BOX = 1819435893; //lrcu
        public const long OMA_MUTABLE_DRM_INFORMATION_BOX = 1835299433; //mdri
        public const long OMA_KEY_MANAGEMENT_BOX = 1868852077; //odkm
        public const long OMA_RIGHTS_OBJECT_BOX = 1868853858; //odrb
        public const long OMA_TRANSACTION_TRACKING_BOX = 1868854388; //odtt
                                                         //iTunes DRM (FairPlay)
        public const long FAIRPLAY_USER_ID_BOX = 1970496882; //user
        public const long FAIRPLAY_USER_NAME_BOX = 1851878757; //name
        public const long FAIRPLAY_USER_KEY_BOX = 1801812256; //key 
        public const long FAIRPLAY_IV_BOX = 1769367926; //iviv
        public const long FAIRPLAY_PRIVATE_KEY_BOX = 1886546294; //priv
    }
}
