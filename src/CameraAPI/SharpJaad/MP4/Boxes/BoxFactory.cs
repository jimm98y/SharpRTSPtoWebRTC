using SharpJaad.MP4.Boxes.Impl;
using SharpJaad.MP4.Boxes.Impl.DRM;
using SharpJaad.MP4.Boxes.Impl.FD;
using SharpJaad.MP4.Boxes.Impl.Meta;
using SharpJaad.MP4.Boxes.Impl.OMA;
using SharpJaad.MP4.Boxes.Impl.SampleEntries;
using SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpJaad.MP4.Boxes
{
    public class BoxFactory
    {
        private static readonly Dictionary<long, Type> BOX_CLASSES = new Dictionary<long, Type>();
        private static readonly Dictionary<long, Type[]> BOX_MULTIPLE_CLASSES = new Dictionary<long, Type[]>();
        private static readonly Dictionary<long, string[]> PARAMETER = new Dictionary<long, string[]>();

        static BoxFactory()
        {
            //classes
            BOX_CLASSES.Add(BoxTypes.ADDITIONAL_METADATA_CONTAINER_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.APPLE_LOSSLESS_BOX, typeof(AppleLosslessBox));
            BOX_CLASSES.Add(BoxTypes.BINARY_XML_BOX, typeof(BinaryXMLBox));
            BOX_CLASSES.Add(BoxTypes.BIT_RATE_BOX, typeof(BitRateBox));
            BOX_CLASSES.Add(BoxTypes.CHAPTER_BOX, typeof(ChapterBox));
            BOX_CLASSES.Add(BoxTypes.CHUNK_OFFSET_BOX, typeof(ChunkOffsetBox));
            BOX_CLASSES.Add(BoxTypes.CHUNK_LARGE_OFFSET_BOX, typeof(ChunkOffsetBox));
            BOX_CLASSES.Add(BoxTypes.CLEAN_APERTURE_BOX, typeof(CleanApertureBox));
            BOX_CLASSES.Add(BoxTypes.COMPACT_SAMPLE_SIZE_BOX, typeof(SampleSizeBox));
            BOX_CLASSES.Add(BoxTypes.COMPOSITION_TIME_TO_SAMPLE_BOX, typeof(CompositionTimeToSampleBox));
            BOX_CLASSES.Add(BoxTypes.COPYRIGHT_BOX, typeof(CopyrightBox));
            BOX_CLASSES.Add(BoxTypes.DATA_ENTRY_URN_BOX, typeof(DataEntryUrnBox));
            BOX_CLASSES.Add(BoxTypes.DATA_ENTRY_URL_BOX, typeof(DataEntryUrlBox));
            BOX_CLASSES.Add(BoxTypes.DATA_INFORMATION_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.DATA_REFERENCE_BOX, typeof(DataReferenceBox));
            BOX_CLASSES.Add(BoxTypes.DECODING_TIME_TO_SAMPLE_BOX, typeof(DecodingTimeToSampleBox));
            BOX_CLASSES.Add(BoxTypes.DEGRADATION_PRIORITY_BOX, typeof(DegradationPriorityBox));
            BOX_CLASSES.Add(BoxTypes.EDIT_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.EDIT_LIST_BOX, typeof(EditListBox));
            BOX_CLASSES.Add(BoxTypes.FD_ITEM_INFORMATION_BOX, typeof(FDItemInformationBox));
            BOX_CLASSES.Add(BoxTypes.FD_SESSION_GROUP_BOX, typeof(FDSessionGroupBox));
            BOX_CLASSES.Add(BoxTypes.FEC_RESERVOIR_BOX, typeof(FECReservoirBox));
            BOX_CLASSES.Add(BoxTypes.FILE_PARTITION_BOX, typeof(FilePartitionBox));
            BOX_CLASSES.Add(BoxTypes.FILE_TYPE_BOX, typeof(FileTypeBox));
            BOX_CLASSES.Add(BoxTypes.FREE_SPACE_BOX, typeof(FreeSpaceBox));
            BOX_CLASSES.Add(BoxTypes.GROUP_ID_TO_NAME_BOX, typeof(GroupIDToNameBox));
            BOX_CLASSES.Add(BoxTypes.HANDLER_BOX, typeof(HandlerBox));
            BOX_CLASSES.Add(BoxTypes.HINT_MEDIA_HEADER_BOX, typeof(HintMediaHeaderBox));
            BOX_CLASSES.Add(BoxTypes.IPMP_CONTROL_BOX, typeof(IPMPControlBox));
            BOX_CLASSES.Add(BoxTypes.IPMP_INFO_BOX, typeof(IPMPInfoBox));
            BOX_CLASSES.Add(BoxTypes.ITEM_INFORMATION_BOX, typeof(ItemInformationBox));
            BOX_CLASSES.Add(BoxTypes.ITEM_INFORMATION_ENTRY, typeof(ItemInformationEntry));
            BOX_CLASSES.Add(BoxTypes.ITEM_LOCATION_BOX, typeof(ItemLocationBox));
            BOX_CLASSES.Add(BoxTypes.ITEM_PROTECTION_BOX, typeof(ItemProtectionBox));
            BOX_CLASSES.Add(BoxTypes.MEDIA_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.MEDIA_DATA_BOX, typeof(MediaDataBox));
            BOX_CLASSES.Add(BoxTypes.MEDIA_HEADER_BOX, typeof(MediaHeaderBox));
            BOX_CLASSES.Add(BoxTypes.MEDIA_INFORMATION_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.META_BOX, typeof(MetaBox));
            BOX_CLASSES.Add(BoxTypes.META_BOX_RELATION_BOX, typeof(MetaBoxRelationBox));
            BOX_CLASSES.Add(BoxTypes.MOVIE_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.MOVIE_EXTENDS_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.MOVIE_EXTENDS_HEADER_BOX, typeof(MovieExtendsHeaderBox));
            BOX_CLASSES.Add(BoxTypes.MOVIE_FRAGMENT_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.MOVIE_FRAGMENT_HEADER_BOX, typeof(MovieFragmentHeaderBox));
            BOX_CLASSES.Add(BoxTypes.MOVIE_FRAGMENT_RANDOM_ACCESS_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.MOVIE_FRAGMENT_RANDOM_ACCESS_OFFSET_BOX, typeof(MovieFragmentRandomAccessOffsetBox));
            BOX_CLASSES.Add(BoxTypes.MOVIE_HEADER_BOX, typeof(MovieHeaderBox));
            BOX_CLASSES.Add(BoxTypes.NERO_METADATA_TAGS_BOX, typeof(NeroMetadataTagsBox));
            BOX_CLASSES.Add(BoxTypes.NULL_MEDIA_HEADER_BOX, typeof(FullBox));
            BOX_CLASSES.Add(BoxTypes.ORIGINAL_FORMAT_BOX, typeof(OriginalFormatBox));
            BOX_CLASSES.Add(BoxTypes.PADDING_BIT_BOX, typeof(PaddingBitBox));
            BOX_CLASSES.Add(BoxTypes.PARTITION_ENTRY, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.PIXEL_ASPECT_RATIO_BOX, typeof(PixelAspectRatioBox));
            BOX_CLASSES.Add(BoxTypes.PRIMARY_ITEM_BOX, typeof(PrimaryItemBox));
            BOX_CLASSES.Add(BoxTypes.PROGRESSIVE_DOWNLOAD_INFORMATION_BOX, typeof(ProgressiveDownloadInformationBox));
            BOX_CLASSES.Add(BoxTypes.PROTECTION_SCHEME_INFORMATION_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.SAMPLE_DEPENDENCY_TYPE_BOX, typeof(SampleDependencyTypeBox));
            BOX_CLASSES.Add(BoxTypes.SAMPLE_DESCRIPTION_BOX, typeof(SampleDescriptionBox));
            BOX_CLASSES.Add(BoxTypes.SAMPLE_GROUP_DESCRIPTION_BOX, typeof(SampleGroupDescriptionBox));
            BOX_CLASSES.Add(BoxTypes.SAMPLE_SCALE_BOX, typeof(SampleScaleBox));
            BOX_CLASSES.Add(BoxTypes.SAMPLE_SIZE_BOX, typeof(SampleSizeBox));
            BOX_CLASSES.Add(BoxTypes.SAMPLE_TABLE_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.SAMPLE_TO_CHUNK_BOX, typeof(SampleToChunkBox));
            BOX_CLASSES.Add(BoxTypes.SAMPLE_TO_GROUP_BOX, typeof(SampleToGroupBox));
            BOX_CLASSES.Add(BoxTypes.SCHEME_TYPE_BOX, typeof(SchemeTypeBox));
            BOX_CLASSES.Add(BoxTypes.SCHEME_INFORMATION_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.SHADOW_SYNC_SAMPLE_BOX, typeof(ShadowSyncSampleBox));
            BOX_CLASSES.Add(BoxTypes.SKIP_BOX, typeof(FreeSpaceBox));
            BOX_CLASSES.Add(BoxTypes.SOUND_MEDIA_HEADER_BOX, typeof(SoundMediaHeaderBox));
            BOX_CLASSES.Add(BoxTypes.SUB_SAMPLE_INFORMATION_BOX, typeof(SubSampleInformationBox));
            BOX_CLASSES.Add(BoxTypes.SYNC_SAMPLE_BOX, typeof(SyncSampleBox));
            BOX_CLASSES.Add(BoxTypes.TRACK_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.TRACK_EXTENDS_BOX, typeof(TrackExtendsBox));
            BOX_CLASSES.Add(BoxTypes.TRACK_FRAGMENT_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.TRACK_FRAGMENT_HEADER_BOX, typeof(TrackFragmentHeaderBox));
            BOX_CLASSES.Add(BoxTypes.TRACK_FRAGMENT_RANDOM_ACCESS_BOX, typeof(TrackFragmentRandomAccessBox));
            BOX_CLASSES.Add(BoxTypes.TRACK_FRAGMENT_RUN_BOX, typeof(TrackFragmentRunBox));
            BOX_CLASSES.Add(BoxTypes.TRACK_HEADER_BOX, typeof(TrackHeaderBox));
            BOX_CLASSES.Add(BoxTypes.TRACK_REFERENCE_BOX, typeof(TrackReferenceBox));
            BOX_CLASSES.Add(BoxTypes.TRACK_SELECTION_BOX, typeof(TrackSelectionBox));
            BOX_CLASSES.Add(BoxTypes.USER_DATA_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.VIDEO_MEDIA_HEADER_BOX, typeof(VideoMediaHeaderBox));
            BOX_CLASSES.Add(BoxTypes.WIDE_BOX, typeof(FreeSpaceBox));
            BOX_CLASSES.Add(BoxTypes.XML_BOX, typeof(XMLBox));
            BOX_CLASSES.Add(BoxTypes.OBJECT_DESCRIPTOR_BOX, typeof(ObjectDescriptorBox));
            BOX_CLASSES.Add(BoxTypes.SAMPLE_DEPENDENCY_BOX, typeof(SampleDependencyBox));
            BOX_CLASSES.Add(BoxTypes.ID3_TAG_BOX, typeof(ID3TagBox));
            BOX_CLASSES.Add(BoxTypes.ITUNES_META_LIST_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.CUSTOM_ITUNES_METADATA_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.ITUNES_METADATA_BOX, typeof(ITunesMetadataBox));
            BOX_CLASSES.Add(BoxTypes.ITUNES_METADATA_NAME_BOX, typeof(ITunesMetadataNameBox));
            BOX_CLASSES.Add(BoxTypes.ITUNES_METADATA_MEAN_BOX, typeof(ITunesMetadataMeanBox));
            BOX_CLASSES.Add(BoxTypes.ALBUM_ARTIST_NAME_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.ALBUM_ARTIST_SORT_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.ALBUM_NAME_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.ALBUM_SORT_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.ARTIST_NAME_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.ARTIST_SORT_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.CATEGORY_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.COMMENTS_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.COMPILATION_PART_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.COMPOSER_NAME_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.COMPOSER_SORT_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.COVER_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.CUSTOM_GENRE_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.DESCRIPTION_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.DISK_NUMBER_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.ENCODER_NAME_BOX, typeof(EncoderBox));
            BOX_CLASSES.Add(BoxTypes.ENCODER_TOOL_BOX, typeof(EncoderBox));
            BOX_CLASSES.Add(BoxTypes.EPISODE_GLOBAL_UNIQUE_ID_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.GAPLESS_PLAYBACK_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.GENRE_BOX, typeof(GenreBox));
            BOX_CLASSES.Add(BoxTypes.GROUPING_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.HD_VIDEO_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.ITUNES_PURCHASE_ACCOUNT_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.ITUNES_ACCOUNT_TYPE_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.ITUNES_CATALOGUE_ID_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.ITUNES_COUNTRY_CODE_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.KEYWORD_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.LONG_DESCRIPTION_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.LYRICS_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.META_TYPE_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.PODCAST_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.PODCAST_URL_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.PURCHASE_DATE_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.RATING_BOX, typeof(RatingBox));
            BOX_CLASSES.Add(BoxTypes.RELEASE_DATE_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.REQUIREMENT_BOX, typeof(RequirementBox));
            BOX_CLASSES.Add(BoxTypes.TEMPO_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.TRACK_NAME_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.TRACK_NUMBER_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.TRACK_SORT_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.TV_EPISODE_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.TV_EPISODE_NUMBER_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.TV_NETWORK_NAME_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.TV_SEASON_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.TV_SHOW_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.TV_SHOW_SORT_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.THREE_GPP_ALBUM_BOX, typeof(ThreeGPPAlbumBox));
            BOX_CLASSES.Add(BoxTypes.THREE_GPP_AUTHOR_BOX, typeof(ThreeGPPMetadataBox));
            BOX_CLASSES.Add(BoxTypes.THREE_GPP_CLASSIFICATION_BOX, typeof(ThreeGPPMetadataBox));
            BOX_CLASSES.Add(BoxTypes.THREE_GPP_DESCRIPTION_BOX, typeof(ThreeGPPMetadataBox));
            BOX_CLASSES.Add(BoxTypes.THREE_GPP_KEYWORDS_BOX, typeof(ThreeGPPKeywordsBox));
            BOX_CLASSES.Add(BoxTypes.THREE_GPP_LOCATION_INFORMATION_BOX, typeof(ThreeGPPLocationBox));
            BOX_CLASSES.Add(BoxTypes.THREE_GPP_PERFORMER_BOX, typeof(ThreeGPPMetadataBox));
            BOX_CLASSES.Add(BoxTypes.THREE_GPP_RECORDING_YEAR_BOX, typeof(ThreeGPPRecordingYearBox));
            BOX_CLASSES.Add(BoxTypes.THREE_GPP_TITLE_BOX, typeof(ThreeGPPMetadataBox));
            BOX_CLASSES.Add(BoxTypes.GOOGLE_HOST_HEADER_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.GOOGLE_PING_MESSAGE_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.GOOGLE_PING_URL_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.GOOGLE_SOURCE_DATA_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.GOOGLE_START_TIME_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.GOOGLE_TRACK_DURATION_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.MP4V_SAMPLE_ENTRY, typeof(VideoSampleEntry));
            BOX_CLASSES.Add(BoxTypes.H263_SAMPLE_ENTRY, typeof(VideoSampleEntry));
            BOX_CLASSES.Add(BoxTypes.ENCRYPTED_VIDEO_SAMPLE_ENTRY, typeof(VideoSampleEntry));
            BOX_CLASSES.Add(BoxTypes.AVC_SAMPLE_ENTRY, typeof(VideoSampleEntry));
            BOX_CLASSES.Add(BoxTypes.MP4A_SAMPLE_ENTRY, typeof(AudioSampleEntry));
            BOX_CLASSES.Add(BoxTypes.AC3_SAMPLE_ENTRY, typeof(AudioSampleEntry));
            BOX_CLASSES.Add(BoxTypes.EAC3_SAMPLE_ENTRY, typeof(AudioSampleEntry));
            BOX_CLASSES.Add(BoxTypes.DRMS_SAMPLE_ENTRY, typeof(AudioSampleEntry));
            BOX_CLASSES.Add(BoxTypes.AMR_SAMPLE_ENTRY, typeof(AudioSampleEntry));
            BOX_CLASSES.Add(BoxTypes.AMR_WB_SAMPLE_ENTRY, typeof(AudioSampleEntry));
            BOX_CLASSES.Add(BoxTypes.EVRC_SAMPLE_ENTRY, typeof(AudioSampleEntry));
            BOX_CLASSES.Add(BoxTypes.QCELP_SAMPLE_ENTRY, typeof(AudioSampleEntry));
            BOX_CLASSES.Add(BoxTypes.SMV_SAMPLE_ENTRY, typeof(AudioSampleEntry));
            BOX_CLASSES.Add(BoxTypes.ENCRYPTED_AUDIO_SAMPLE_ENTRY, typeof(AudioSampleEntry));
            BOX_CLASSES.Add(BoxTypes.MPEG_SAMPLE_ENTRY, typeof(MPEGSampleEntry));
            BOX_CLASSES.Add(BoxTypes.TEXT_METADATA_SAMPLE_ENTRY, typeof(TextMetadataSampleEntry));
            BOX_CLASSES.Add(BoxTypes.XML_METADATA_SAMPLE_ENTRY, typeof(XMLMetadataSampleEntry));
            BOX_CLASSES.Add(BoxTypes.RTP_HINT_SAMPLE_ENTRY, typeof(RTPHintSampleEntry));
            BOX_CLASSES.Add(BoxTypes.FD_HINT_SAMPLE_ENTRY, typeof(FDHintSampleEntry));
            BOX_CLASSES.Add(BoxTypes.ESD_BOX, typeof(ESDBox));
            BOX_CLASSES.Add(BoxTypes.H263_SPECIFIC_BOX, typeof(H263SpecificBox));
            BOX_CLASSES.Add(BoxTypes.AVC_SPECIFIC_BOX, typeof(AVCSpecificBox));
            BOX_CLASSES.Add(BoxTypes.AC3_SPECIFIC_BOX, typeof(AC3SpecificBox));
            BOX_CLASSES.Add(BoxTypes.EAC3_SPECIFIC_BOX, typeof(EAC3SpecificBox));
            BOX_CLASSES.Add(BoxTypes.AMR_SPECIFIC_BOX, typeof(AMRSpecificBox));
            BOX_CLASSES.Add(BoxTypes.EVRC_SPECIFIC_BOX, typeof(EVRCSpecificBox));
            BOX_CLASSES.Add(BoxTypes.QCELP_SPECIFIC_BOX, typeof(QCELPSpecificBox));
            BOX_CLASSES.Add(BoxTypes.SMV_SPECIFIC_BOX, typeof(SMVSpecificBox));
            BOX_CLASSES.Add(BoxTypes.OMA_ACCESS_UNIT_FORMAT_BOX, typeof(OMAAccessUnitFormatBox));
            BOX_CLASSES.Add(BoxTypes.OMA_COMMON_HEADERS_BOX, typeof(OMACommonHeadersBox));
            BOX_CLASSES.Add(BoxTypes.OMA_CONTENT_ID_BOX, typeof(OMAContentIDBox));
            BOX_CLASSES.Add(BoxTypes.OMA_CONTENT_OBJECT_BOX, typeof(OMAContentObjectBox));
            BOX_CLASSES.Add(BoxTypes.OMA_COVER_URI_BOX, typeof(OMAURLBox));
            BOX_CLASSES.Add(BoxTypes.OMA_DISCRETE_MEDIA_HEADERS_BOX, typeof(OMADiscreteMediaHeadersBox));
            BOX_CLASSES.Add(BoxTypes.OMA_DRM_CONTAINER_BOX, typeof(FullBox));
            BOX_CLASSES.Add(BoxTypes.OMA_ICON_URI_BOX, typeof(OMAURLBox));
            BOX_CLASSES.Add(BoxTypes.OMA_INFO_URL_BOX, typeof(OMAURLBox));
            BOX_CLASSES.Add(BoxTypes.OMA_LYRICS_URI_BOX, typeof(OMAURLBox));
            BOX_CLASSES.Add(BoxTypes.OMA_MUTABLE_DRM_INFORMATION_BOX, typeof(BoxImpl));
            BOX_CLASSES.Add(BoxTypes.OMA_KEY_MANAGEMENT_BOX, typeof(FullBox));
            BOX_CLASSES.Add(BoxTypes.OMA_RIGHTS_OBJECT_BOX, typeof(OMARightsObjectBox));
            BOX_CLASSES.Add(BoxTypes.OMA_TRANSACTION_TRACKING_BOX, typeof(OMATransactionTrackingBox));
            BOX_CLASSES.Add(BoxTypes.FAIRPLAY_USER_ID_BOX, typeof(FairPlayDataBox));
            BOX_CLASSES.Add(BoxTypes.FAIRPLAY_USER_NAME_BOX, typeof(FairPlayDataBox));
            BOX_CLASSES.Add(BoxTypes.FAIRPLAY_USER_KEY_BOX, typeof(FairPlayDataBox));
            BOX_CLASSES.Add(BoxTypes.FAIRPLAY_IV_BOX, typeof(FairPlayDataBox));
            BOX_CLASSES.Add(BoxTypes.FAIRPLAY_PRIVATE_KEY_BOX, typeof(FairPlayDataBox));
            //parameter
            PARAMETER.Add(BoxTypes.ADDITIONAL_METADATA_CONTAINER_BOX, new string[] { "Additional Metadata Container Box" });
            PARAMETER.Add(BoxTypes.DATA_INFORMATION_BOX, new string[] { "Data Information Box" });
            PARAMETER.Add(BoxTypes.EDIT_BOX, new string[] { "Edit Box" });
            PARAMETER.Add(BoxTypes.MEDIA_BOX, new string[] { "Media Box" });
            PARAMETER.Add(BoxTypes.MEDIA_INFORMATION_BOX, new string[] { "Media Information Box" });
            PARAMETER.Add(BoxTypes.MOVIE_BOX, new string[] { "Movie Box" });
            PARAMETER.Add(BoxTypes.MOVIE_EXTENDS_BOX, new string[] { "Movie Extends Box" });
            PARAMETER.Add(BoxTypes.MOVIE_FRAGMENT_BOX, new string[] { "Movie Fragment Box" });
            PARAMETER.Add(BoxTypes.MOVIE_FRAGMENT_RANDOM_ACCESS_BOX, new string[] { "Movie Fragment Random Access Box" });
            PARAMETER.Add(BoxTypes.NULL_MEDIA_HEADER_BOX, new string[] { "Null Media Header Box" });
            PARAMETER.Add(BoxTypes.PARTITION_ENTRY, new string[] { "Partition Entry" });
            PARAMETER.Add(BoxTypes.PROTECTION_SCHEME_INFORMATION_BOX, new string[] { "Protection Scheme Information Box" });
            PARAMETER.Add(BoxTypes.SAMPLE_TABLE_BOX, new string[] { "Sample Table Box" });
            PARAMETER.Add(BoxTypes.SCHEME_INFORMATION_BOX, new string[] { "Scheme Information Box" });
            PARAMETER.Add(BoxTypes.TRACK_BOX, new string[] { "Track Box" });
            PARAMETER.Add(BoxTypes.TRACK_FRAGMENT_BOX, new string[] { "Track Fragment Box" });
            PARAMETER.Add(BoxTypes.USER_DATA_BOX, new string[] { "User Data Box" });
            PARAMETER.Add(BoxTypes.ITUNES_META_LIST_BOX, new string[] { "iTunes Meta List Box" });
            PARAMETER.Add(BoxTypes.CUSTOM_ITUNES_METADATA_BOX, new string[] { "Custom iTunes Metadata Box" });
            PARAMETER.Add(BoxTypes.ALBUM_ARTIST_NAME_BOX, new string[] { "Album Artist Name Box" });
            PARAMETER.Add(BoxTypes.ALBUM_ARTIST_SORT_BOX, new string[] { "Album Artist Sort Box" });
            PARAMETER.Add(BoxTypes.ALBUM_NAME_BOX, new string[] { "Album Name Box" });
            PARAMETER.Add(BoxTypes.ALBUM_SORT_BOX, new string[] { "Album Sort Box" });
            PARAMETER.Add(BoxTypes.ARTIST_NAME_BOX, new string[] { "Artist Name Box" });
            PARAMETER.Add(BoxTypes.ARTIST_SORT_BOX, new string[] { "Artist Sort Box" });
            PARAMETER.Add(BoxTypes.CATEGORY_BOX, new string[] { "Category Box" });
            PARAMETER.Add(BoxTypes.COMMENTS_BOX, new string[] { "Comments Box" });
            PARAMETER.Add(BoxTypes.COMPILATION_PART_BOX, new string[] { "Compilation Part Box" });
            PARAMETER.Add(BoxTypes.COMPOSER_NAME_BOX, new string[] { "Composer Name Box" });
            PARAMETER.Add(BoxTypes.COMPOSER_SORT_BOX, new string[] { "Composer Sort Box" });
            PARAMETER.Add(BoxTypes.COVER_BOX, new string[] { "Cover Box" });
            PARAMETER.Add(BoxTypes.CUSTOM_GENRE_BOX, new string[] { "Custom Genre Box" });
            PARAMETER.Add(BoxTypes.DESCRIPTION_BOX, new string[] { "Description Cover Box" });
            PARAMETER.Add(BoxTypes.DISK_NUMBER_BOX, new string[] { "Disk Number Box" });
            PARAMETER.Add(BoxTypes.EPISODE_GLOBAL_UNIQUE_ID_BOX, new string[] { "Episode Global Unique ID Box" });
            PARAMETER.Add(BoxTypes.GAPLESS_PLAYBACK_BOX, new string[] { "Gapless Playback Box" });
            PARAMETER.Add(BoxTypes.GROUPING_BOX, new string[] { "Grouping Box" });
            PARAMETER.Add(BoxTypes.HD_VIDEO_BOX, new string[] { "HD Video Box" });
            PARAMETER.Add(BoxTypes.ITUNES_PURCHASE_ACCOUNT_BOX, new string[] { "iTunes Purchase Account Box" });
            PARAMETER.Add(BoxTypes.ITUNES_ACCOUNT_TYPE_BOX, new string[] { "iTunes Account Type Box" });
            PARAMETER.Add(BoxTypes.ITUNES_CATALOGUE_ID_BOX, new string[] { "iTunes Catalogue ID Box" });
            PARAMETER.Add(BoxTypes.ITUNES_COUNTRY_CODE_BOX, new string[] { "iTunes Country Code Box" });
            PARAMETER.Add(BoxTypes.KEYWORD_BOX, new string[] { "Keyword Box" });
            PARAMETER.Add(BoxTypes.LONG_DESCRIPTION_BOX, new string[] { "Long Description Box" });
            PARAMETER.Add(BoxTypes.LYRICS_BOX, new string[] { "Lyrics Box" });
            PARAMETER.Add(BoxTypes.META_TYPE_BOX, new string[] { "Meta Type Box" });
            PARAMETER.Add(BoxTypes.PODCAST_BOX, new string[] { "Podcast Box" });
            PARAMETER.Add(BoxTypes.PODCAST_URL_BOX, new string[] { "Podcast URL Box" });
            PARAMETER.Add(BoxTypes.PURCHASE_DATE_BOX, new string[] { "Purchase Date Box" });
            PARAMETER.Add(BoxTypes.RELEASE_DATE_BOX, new string[] { "Release Date Box" });
            PARAMETER.Add(BoxTypes.TEMPO_BOX, new string[] { "Tempo Box" });
            PARAMETER.Add(BoxTypes.TRACK_NAME_BOX, new string[] { "Track Name Box" });
            PARAMETER.Add(BoxTypes.TRACK_NUMBER_BOX, new string[] { "Track Number Box" });
            PARAMETER.Add(BoxTypes.TRACK_SORT_BOX, new string[] { "Track Sort Box" });
            PARAMETER.Add(BoxTypes.TV_EPISODE_BOX, new string[] { "TV Episode Box" });
            PARAMETER.Add(BoxTypes.TV_EPISODE_NUMBER_BOX, new string[] { "TV Episode Number Box" });
            PARAMETER.Add(BoxTypes.TV_NETWORK_NAME_BOX, new string[] { "TV Network Name Box" });
            PARAMETER.Add(BoxTypes.TV_SEASON_BOX, new string[] { "TV Season Box" });
            PARAMETER.Add(BoxTypes.TV_SHOW_BOX, new string[] { "TV Show Box" });
            PARAMETER.Add(BoxTypes.TV_SHOW_SORT_BOX, new string[] { "TV Show Sort Box" });
            PARAMETER.Add(BoxTypes.THREE_GPP_AUTHOR_BOX, new string[] { "3GPP Author Box" });
            PARAMETER.Add(BoxTypes.THREE_GPP_CLASSIFICATION_BOX, new string[] { "3GPP Classification Box" });
            PARAMETER.Add(BoxTypes.THREE_GPP_DESCRIPTION_BOX, new string[] { "3GPP Description Box" });
            PARAMETER.Add(BoxTypes.THREE_GPP_PERFORMER_BOX, new string[] { "3GPP Performer Box" });
            PARAMETER.Add(BoxTypes.THREE_GPP_TITLE_BOX, new string[] { "3GPP Title Box" });
            PARAMETER.Add(BoxTypes.GOOGLE_HOST_HEADER_BOX, new string[] { "Google Host Header Box" });
            PARAMETER.Add(BoxTypes.GOOGLE_PING_MESSAGE_BOX, new string[] { "Google Ping Message Box" });
            PARAMETER.Add(BoxTypes.GOOGLE_PING_URL_BOX, new string[] { "Google Ping URL Box" });
            PARAMETER.Add(BoxTypes.GOOGLE_SOURCE_DATA_BOX, new string[] { "Google Source Data Box" });
            PARAMETER.Add(BoxTypes.GOOGLE_START_TIME_BOX, new string[] { "Google Start Time Box" });
            PARAMETER.Add(BoxTypes.GOOGLE_TRACK_DURATION_BOX, new string[] { "Google Track Duration Box" });
            PARAMETER.Add(BoxTypes.MP4V_SAMPLE_ENTRY, new string[] { "MPEG-4 Video Sample Entry" });
            PARAMETER.Add(BoxTypes.H263_SAMPLE_ENTRY, new string[] { "H263 Video Sample Entry" });
            PARAMETER.Add(BoxTypes.ENCRYPTED_VIDEO_SAMPLE_ENTRY, new string[] { "Encrypted Video Sample Entry" });
            PARAMETER.Add(BoxTypes.AVC_SAMPLE_ENTRY, new string[] { "AVC Video Sample Entry" });
            PARAMETER.Add(BoxTypes.MP4A_SAMPLE_ENTRY, new string[] { "MPEG- 4Audio Sample Entry" });
            PARAMETER.Add(BoxTypes.AC3_SAMPLE_ENTRY, new string[] { "AC-3 Audio Sample Entry" });
            PARAMETER.Add(BoxTypes.EAC3_SAMPLE_ENTRY, new string[] { "Extended AC-3 Audio Sample Entry" });
            PARAMETER.Add(BoxTypes.DRMS_SAMPLE_ENTRY, new string[] { "DRMS Audio Sample Entry" });
            PARAMETER.Add(BoxTypes.AMR_SAMPLE_ENTRY, new string[] { "AMR Audio Sample Entry" });
            PARAMETER.Add(BoxTypes.AMR_WB_SAMPLE_ENTRY, new string[] { "AMR-Wideband Audio Sample Entry" });
            PARAMETER.Add(BoxTypes.EVRC_SAMPLE_ENTRY, new string[] { "EVC Audio Sample Entry" });
            PARAMETER.Add(BoxTypes.QCELP_SAMPLE_ENTRY, new string[] { "QCELP Audio Sample Entry" });
            PARAMETER.Add(BoxTypes.SMV_SAMPLE_ENTRY, new string[] { "SMV Audio Sample Entry" });
            PARAMETER.Add(BoxTypes.ENCRYPTED_AUDIO_SAMPLE_ENTRY, new string[] { "Encrypted Audio Sample Entry" });
            PARAMETER.Add(BoxTypes.OMA_COVER_URI_BOX, new string[] { "OMA DRM Cover URI Box" });
            PARAMETER.Add(BoxTypes.OMA_DRM_CONTAINER_BOX, new string[] { "OMA DRM Container Box" });
            PARAMETER.Add(BoxTypes.OMA_ICON_URI_BOX, new string[] { "OMA DRM Icon URI Box" });
            PARAMETER.Add(BoxTypes.OMA_INFO_URL_BOX, new string[] { "OMA DRM Info URL Box" });
            PARAMETER.Add(BoxTypes.OMA_LYRICS_URI_BOX, new string[] { "OMA DRM Lyrics URI Box" });
            PARAMETER.Add(BoxTypes.OMA_MUTABLE_DRM_INFORMATION_BOX, new string[] { "OMA DRM Mutable DRM Information Box" });
        }

        public static Box ParseBox(Box parent, MP4InputStream input)
        {
            long offset = input.getOffset();

            long size = input.readBytes(4);
            long type = input.readBytes(4);
            if (size == 1) size = input.readBytes(8);
            if (type == BoxTypes.EXTENDED_TYPE) input.skipBytes(16);

            //error protection
            if (parent != null)
            {
                long parentLeft = (parent.GetOffset() + parent.GetSize()) - offset;
                if (size > parentLeft) throw new IOException("error while decoding box '" + TypeToString(type) + "' at offset " + offset + ": box too large for parent");
            }

            //Logger.getLogger("MP4 Boxes").finest(typeToString(type));
            BoxImpl box = ForType(type, input.getOffset());
            box.SetParams(parent, size, type, offset);
            box.decode(input);

            //if box doesn't contain data it only contains children
            Type cl = box.GetType();
            if (cl == typeof(BoxImpl) || cl == typeof(FullBox)) box.ReadChildren(input);

            //check bytes left
            long left = (box.GetOffset() + box.GetSize()) - input.getOffset();
            if (left > 0 && !(box is MediaDataBox)

                            && !(box is UnknownBox)

                            && !(box is FreeSpaceBox))
            {
                //LOGGER.log(Level.INFO, "bytes left after reading box {0}: left: {1}, offset: {2}", new Object[] { typeToString(type), left, input.getOffset() });
            }

            else if (left < 0)
            {
                //LOGGER.log(Level.SEVERE, "box {0} overread: {1} bytes, offset: {2}", new Object[] { typeToString(type), -left, input.getOffset() });
            }

            //if mdat found and no random access, don't skip
            if (box.GetBoxType() != BoxTypes.MEDIA_DATA_BOX || input.hasRandomAccess()) input.skipBytes(left);
            return box;
        }

#warning Review this
        //TODO: remove usages
        public static Box ParseBox(MP4InputStream input, Type boxClass)
        {
            long offset = input.getOffset();

            long size = input.readBytes(4);
            long type = input.readBytes(4);
            if (size == 1) size = input.readBytes(8);
            if (type == BoxTypes.EXTENDED_TYPE) input.skipBytes(16);

            BoxImpl box = null;
            try
            {
                box = (BoxImpl)Activator.CreateInstance(boxClass);
            }
            catch (Exception e)
            {
            }

            if (box != null)
            {
                box.SetParams(null, size, type, offset);
                box.decode(input);
                long left = (box.GetOffset() + box.GetSize()) - input.getOffset();
                input.skipBytes(left);
            }
            return box;
        }

        private static BoxImpl ForType(long type, long offset)
        {
            BoxImpl box = null;

            long l = type;
            if (BOX_CLASSES.ContainsKey(l))
            {
                Type cl = BOX_CLASSES[l];
                if (PARAMETER.ContainsKey(l))
                {
                    string[] s = PARAMETER[l];
                    try
                    {
                        box = (BoxImpl)Activator.CreateInstance(cl, s[0]);
                    }
                    catch (Exception e)
                    {
                        //LOGGER.log(Level.SEVERE, "BoxFactory: could not call constructor for " + typeToString(type), e);
                        box = new UnknownBox();
                    }
                }
                else
                {
                    try
                    {
                        box = (BoxImpl)Activator.CreateInstance(cl);
                    }
                    catch (Exception e)
                    {
                        //LOGGER.log(Level.SEVERE, "BoxFactory: could not instantiate box " + typeToString(type), e);
                    }
                }
            }

            if (box == null)
            {
                //LOGGER.log(Level.INFO, "BoxFactory: unknown box type: {0}; position: {1}", new Object[] { typeToString(type), offset });
                box = new UnknownBox();
            }
            return box;
        }

        public static string TypeToString(long l)
        {
            byte[] b = new byte[4];
            b[0] = (byte)((l >> 24) & 0xFF);
            b[1] = (byte)((l >> 16) & 0xFF);
            b[2] = (byte)((l >> 8) & 0xFF);
            b[3] = (byte)(l & 0xFF);
            return Encoding.ASCII.GetString(b);
        }
    }
}
