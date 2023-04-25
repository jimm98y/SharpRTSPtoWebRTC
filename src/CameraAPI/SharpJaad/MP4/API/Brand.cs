namespace SharpJaad.MP4.API
{
    public enum Brand
    {
        UNKNOWN_BRAND,
        ISO_BASE_MEDIA,
        ISO_BASE_MEDIA_2,
        ISO_BASE_MEDIA_3,
        ISO_BASE_MEDIA_4,
        MP4,
        MP4_V2,
        MP4_VIDEO,
        MP4_AUDIO,
        MP4_PROTECTED_AUDIO,
        MP4_AUDIO_BOOK,
        MP4_WITH_MPEG_7,
        AVC,
        JPEG_2000,
        JPEG_2000_COMPOUND_IMAGE,
        JPEG_2000_EXTENDED,
        MOTION_JPEG_2000_SIMPLE_PROFILE,
        MOTION_JPEG_2000_GENERAL_PROFILE,
        DMB_FULL,
        DMB_MPEG_2,
        DMB_MPEG_2_EXTENDED,
        DMB_BSAC,
        DMB_BSAC_EXTENDED,
        DMB_HE_AAC_V2,
        DMB_HE_AAC_V2_EXTENDED,
        DMB_HE_AAC,
        DMB_HE_AAC_EXTENDED,
        DMB_AVC_BSAC,
        DMB_MAF_AVC_BSAC_EXTENDED,
        DMB_MAF_AVC_HE_AAC_V2,
        DMB_MAF_AVC_HE_AAC_V2_EXTENDED,
        DMB_MAF_AVC_HE_AAC,
        DMB_MAF_AVC_HE_AAC_EXTENDED,
        MPEG_21,
        MPPI_PHOTO_PLAYER,
        JPSEARCH,
        //3gpp
        THREE_GPP_RELEASE_1,
        THREE_GPP_RELEASE_2,
        THREE_GPP_RELEASE_3,
        THREE_GPP_RELEASE_4,
        THREE_GPP_RELEASE_5,
        THREE_GPP_RELEASE_6,
        THREE_GPP_RELEASE_6_GENERAL,
        THREE_GPP_RELEASE_6_EXTENDED,
        THREE_GPP_RELEASE_6_PROGRESSIVE_DOWNLOAD,
        THREE_GPP_RELEASE_6_STREAMING,
        THREE_GPP_RELEASE_7,
        THREE_GPP_RELEASE_7_EXTENDED,
        THREE_GPP_RELEASE_7_STREAMING,
        THREE_GPP_RELEASE_8,
        THREE_GPP_RELEASE_8_RECORDING,
        THREE_GPP_RELEASE_9,
        THREE_GPP_RELEASE_9_PROGRESSIVE_DOWNLOAD,
        THREE_GPP_RELEASE_9_EXTENDED,
        THREE_GPP_RELEASE_9_RECORDING,
        THREE_GPP_RELEASE_9_FILE_DELIVERY,
        THREE_GPP_RELEASE_9_ADAPTIVE_STREAMING,
        THREE_GPP_RELEASE_9_MEDIA_SEGMENT,
        THREE_GPP2_A,
        THREE_GPP2_B,
        THREE_GPP2_C,
        THREE_GPP2_KDDI_3G_EZMOVIE,
        MPEG_4_MOBILE_PROFILE_,
        //others
        DIRAC,
        DIGITAL_MEDIA_PROJECT,
        DVB_OVER_RTP,
        DVB_OVER_MPEG_2_TRANSPORT_STREAM,
        SD_MEMORY_CARD_VIDEO,
        //producers
        ADOBE_FLASH_PLAYER_VIDEO,
        ADOBE_FLASH_PLAYER_PROTECTED_VIDEO,
        ADOBE_FLASH_PLAYER_AUDIO,
        ADOBE_FLASH_PLAYER_AUDIO_BOOK,
        APPLE_QUICKTIME,
        APPLE_TV,
        APPLE_IPHONE_VIDEO,
        ARRI_DIGITAL_CAMERA,
        CANON_DIGITAL_CAMERA,
        CASIO_DIGITAL_CAMERA,
        CONVERGENT_DESIGN,
        DECE_COMMON_FILE_FORMAT,
        ISMACRYP_2_ENCRYPTED_FILE,
        NIKON_DIGITAL_CAMERA,
        LEICA_DIGITAL_CAMERA,
        MICROSOFT_PIFF,
        NERO_DIGITAL_AAC_AUDIO,
        NERO_STANDARD_PROFILE,
        NERO_CINEMA_PROFILE,
        NERO_HDTV_PROFILE,
        NERO_MOBILE_PROFILE,
        NERO_PORTABLE_PROFILE,
        NERO_AVC_STANDARD_PROFILE,
        NERO_AVC_CINEMA_PROFILE,
        NERO_AVC_HDTV_PROFILE,
        NERO_AVC_MOBILE_PROFILE,
        NERO_AVC_PORTABLE_PROFILE,
        OMA_DCF_2,
        OMA_PDCF_2_1,
        OMA_PDCF_XBS_EXTENSIONS,
        PANASONIC_DIGITAL_CAMERA,
        ROSS_VIDEO,
        SAMSUNG_STEREOSCOPIC_SINGLE_STREAM,
        SAMSUNG_STEREOSCOPIC_DUAL_STREAM,
        SONY_MOBILE,
        SONY_PSP,
    }

    public static class BrandExtensions
    {
        public static Brand ForID(string id)
        {
            Brand ret = Brand.UNKNOWN_BRAND;
            switch (id)
            {
                case "isom":
                    ret = Brand.ISO_BASE_MEDIA;
                    break;
                case "iso2":
                    ret = Brand.ISO_BASE_MEDIA_2;
                    break;
                case "iso3":
                    ret = Brand.ISO_BASE_MEDIA_3;
                    break;
                case "iso4":
                    ret = Brand.ISO_BASE_MEDIA_4 ;
                    break;
                case "mp41":
                    ret = Brand.MP4;
                    break;
                case "mp42":
                    ret = Brand.MP4_V2;
                    break;
                case "M4V ":
                    ret = Brand.MP4_VIDEO;
                    break;
                case "M4A ":
                    ret = Brand.MP4_AUDIO;
                    break;
                case "M4P ":
                    ret = Brand.MP4_PROTECTED_AUDIO;
                    break;
                case "M4B ":
                    ret = Brand.MP4_AUDIO_BOOK;
                    break;
                case "mp71":
                    ret = Brand.MP4_WITH_MPEG_7;
                    break;
                case "avc1":
                    ret = Brand.AVC;
                    break;
                case "JP2 ":
                    ret = Brand.JPEG_2000;
                    break;
                case "jpm ":
                    ret = Brand.JPEG_2000_COMPOUND_IMAGE;
                    break;
                case "jpx ":
                    ret = Brand.JPEG_2000_EXTENDED;
                    break;
                case "mj2s":
                    ret = Brand.MOTION_JPEG_2000_SIMPLE_PROFILE;
                    break;
                case "mjp2":
                    ret = Brand.MOTION_JPEG_2000_GENERAL_PROFILE;
                    break;
                case "dmb1":
                    ret = Brand.DMB_FULL;
                    break;
                case "da0a":
                    ret = Brand.DMB_MPEG_2;
                    break;
                case "da0b":
                    ret = Brand.DMB_MPEG_2_EXTENDED;
                    break;
                case "da1a":
                    ret = Brand.DMB_BSAC;
                    break;
                case "da1b":
                    ret = Brand.DMB_BSAC_EXTENDED;
                    break;
                case "da2a":
                    ret = Brand.DMB_HE_AAC_V2;
                    break;
                case "da2b":
                    ret = Brand.DMB_HE_AAC_V2_EXTENDED;
                    break;
                case "da3a":
                    ret = Brand.DMB_HE_AAC;
                    break;
                case "da3b":
                    ret = Brand.DMB_HE_AAC_EXTENDED;
                    break;
                case "dv1a":
                    ret = Brand.DMB_AVC_BSAC;
                    break;
                case "dv1b":
                    ret = Brand.DMB_MAF_AVC_BSAC_EXTENDED;
                    break;
                case "dv2a":
                    ret = Brand.DMB_MAF_AVC_HE_AAC_V2;
                    break;
                case "dv2b":
                    ret = Brand.DMB_MAF_AVC_HE_AAC_V2_EXTENDED;
                    break;
                case "dv3a":
                    ret = Brand.DMB_MAF_AVC_HE_AAC;
                    break;
                case "dv3b":
                    ret = Brand.DMB_MAF_AVC_HE_AAC_EXTENDED;
                    break;
                case "mp21":
                    ret = Brand.MPEG_21;
                    break;
                case "MPPI":
                    ret = Brand.MPPI_PHOTO_PLAYER;
                    break;
                case "jpsi":
                    ret = Brand.JPSEARCH;
                    break;
                case "3gp1":
                    ret = Brand.THREE_GPP_RELEASE_1;
                    break;
                case "3gp2":
                    ret = Brand.THREE_GPP_RELEASE_2;
                    break;
                case "3gp3":
                    ret = Brand.THREE_GPP_RELEASE_3;
                    break;
                case "3gp4":
                    ret = Brand.THREE_GPP_RELEASE_4;
                    break;
                case "3gp5":
                    ret = Brand.THREE_GPP_RELEASE_5;
                    break;
                case "3gp6":
                    ret = Brand.THREE_GPP_RELEASE_6;
                    break;
                case "3gg6":
                    ret = Brand.THREE_GPP_RELEASE_6_GENERAL;
                    break;
                case "3ge6":
                    ret = Brand.THREE_GPP_RELEASE_6_EXTENDED;
                    break;
                case "3gr6":
                    ret = Brand.THREE_GPP_RELEASE_6_PROGRESSIVE_DOWNLOAD;
                    break;
                case "3gs6":
                    ret = Brand.THREE_GPP_RELEASE_6_STREAMING;
                    break;
                case "3gp7":
                    ret = Brand.THREE_GPP_RELEASE_7;
                    break;
                case "3ge7":
                    ret = Brand.THREE_GPP_RELEASE_7_EXTENDED;
                    break;
                case "3gs7":
                    ret = Brand.THREE_GPP_RELEASE_7_STREAMING;
                    break;
                //case "3gp7":
                //    ret = Brand.THREE_GPP_RELEASE_8;
                //    break;
                case "3gt8":
                    ret = Brand.THREE_GPP_RELEASE_8_RECORDING;
                    break;
                case "3gs9":
                    ret = Brand.THREE_GPP_RELEASE_9;
                    break;
                case "3gr9":
                    ret = Brand.THREE_GPP_RELEASE_9_PROGRESSIVE_DOWNLOAD;
                    break;
                case "3ge9":
                    ret = Brand.THREE_GPP_RELEASE_9_EXTENDED;
                    break;
                case "3gt9":
                    ret = Brand.THREE_GPP_RELEASE_9_RECORDING;
                    break;
                case "3gf9":
                    ret = Brand.THREE_GPP_RELEASE_9_FILE_DELIVERY;
                    break;
                case "3gh9":
                    ret = Brand.THREE_GPP_RELEASE_9_ADAPTIVE_STREAMING;
                    break;
                case "3gm9":
                    ret = Brand.THREE_GPP_RELEASE_9_MEDIA_SEGMENT;
                    break;
                case "3g2a":
                    ret = Brand.THREE_GPP2_A;
                    break;
                case "3g2b":
                    ret = Brand.THREE_GPP2_B;
                    break;
                case "3g2c":
                    ret = Brand.THREE_GPP2_C;
                    break;
                case "KDDI":
                    ret = Brand.THREE_GPP2_KDDI_3G_EZMOVIE;
                    break;
                case "mmp4":
                    ret = Brand.MPEG_4_MOBILE_PROFILE_;
                    break;
                case "drc1":
                    ret = Brand.DIRAC;
                    break;
                case "dmpf":
                    ret = Brand.DIGITAL_MEDIA_PROJECT;
                    break;
                case "dvr1":
                    ret = Brand.DVB_OVER_RTP;
                    break;
                case "dvt1":
                    ret = Brand.DVB_OVER_MPEG_2_TRANSPORT_STREAM;
                    break;
                case "sdv ":
                    ret = Brand.SD_MEMORY_CARD_VIDEO;
                    break;
                case "F4V ":
                    ret = Brand.ADOBE_FLASH_PLAYER_VIDEO;
                    break;
                case "F4P ":
                    ret = Brand.ADOBE_FLASH_PLAYER_PROTECTED_VIDEO;
                    break;
                case "F4A ":
                    ret = Brand.ADOBE_FLASH_PLAYER_AUDIO;
                    break;
                case "F4B ":
                    ret = Brand.ADOBE_FLASH_PLAYER_AUDIO_BOOK;
                    break;
                case "qt  ":
                    ret = Brand.APPLE_QUICKTIME;
                    break;
                case "M4VH":
                    ret = Brand.APPLE_TV;
                    break;
                case "M4VP":
                    ret = Brand.APPLE_IPHONE_VIDEO;
                    break;
                case "ARRI":
                    ret = Brand.ARRI_DIGITAL_CAMERA;
                    break;
                case "CAEP":
                    ret = Brand.CANON_DIGITAL_CAMERA;
                    break;
                case "caqv":
                    ret = Brand.CASIO_DIGITAL_CAMERA;
                    break;
                case "CDes":
                    ret = Brand.CONVERGENT_DESIGN;
                    break;
                case "ccff":
                    ret = Brand.DECE_COMMON_FILE_FORMAT;
                    break;
                case "isc2":
                    ret = Brand.ISMACRYP_2_ENCRYPTED_FILE;
                    break;
                case "niko":
                    ret = Brand.NIKON_DIGITAL_CAMERA;
                    break;
                case "LCAG":
                    ret = Brand.LEICA_DIGITAL_CAMERA;
                    break;
                case "piff":
                    ret = Brand.MICROSOFT_PIFF;
                    break;
                case "NDAS":
                    ret = Brand.NERO_DIGITAL_AAC_AUDIO;
                    break;
                case "NDSS":
                    ret = Brand.NERO_STANDARD_PROFILE;
                    break;
                case "NDSC":
                    ret = Brand.NERO_CINEMA_PROFILE;
                    break;
                case "NDSH":
                    ret = Brand.NERO_HDTV_PROFILE;
                    break;
                case "NDSM":
                    ret = Brand.NERO_MOBILE_PROFILE;
                    break;
                case "NDSP":
                    ret = Brand.NERO_PORTABLE_PROFILE;
                    break;
                case "NDXS":
                    ret = Brand.NERO_AVC_STANDARD_PROFILE;
                    break;
                case "NDXC":
                    ret = Brand.NERO_AVC_CINEMA_PROFILE;
                    break;
                case "NDXH":
                    ret = Brand.NERO_AVC_HDTV_PROFILE;
                    break;
                case "NDXM":
                    ret = Brand.NERO_AVC_MOBILE_PROFILE;
                    break;
                case "NDXP":
                    ret = Brand.NERO_AVC_PORTABLE_PROFILE;
                    break;
                case "odcf":
                    ret = Brand.OMA_DCF_2;
                    break;
                case "opf2":
                    ret = Brand.OMA_PDCF_2_1;
                    break;
                case "opx2":
                    ret = Brand.OMA_PDCF_XBS_EXTENSIONS;
                    break;
                case "pana":
                    ret = Brand.PANASONIC_DIGITAL_CAMERA;
                    break;
                case "ROSS":
                    ret = Brand.ROSS_VIDEO;
                    break;
                case "ssc1":
                    ret = Brand.SAMSUNG_STEREOSCOPIC_SINGLE_STREAM;
                    break;
                case "ssc2":
                    ret = Brand.SAMSUNG_STEREOSCOPIC_DUAL_STREAM;
                    break;
                case "mqt ":
                    ret = Brand.SONY_MOBILE;
                    break;
                case "MSNV":
                    ret = Brand.SONY_PSP;
                    break;
                default:
                    ret = Brand.UNKNOWN_BRAND;
                    break;
            }

            return ret;
        }

        public static string GetID(this Brand brand)
        {
            string ret = null;

            switch (brand)
            {
                case Brand.ISO_BASE_MEDIA:
                    ret = "isom";
                    break;
                case Brand.ISO_BASE_MEDIA_2:
                    ret = "iso2";
                    break;
                case Brand.ISO_BASE_MEDIA_3:
                    ret = "iso3";
                    break;
                case Brand.ISO_BASE_MEDIA_4:
                    ret = "iso4";
                    break;
                case Brand.MP4:
                    ret = "mp41";
                    break;
                case Brand.MP4_V2:
                    ret = "mp42";
                    break;
                case Brand.MP4_VIDEO:
                    ret = "M4V ";
                    break;
                case Brand.MP4_AUDIO:
                    ret = "M4A ";
                    break;
                case Brand.MP4_PROTECTED_AUDIO:
                    ret = "M4P ";
                    break;
                case Brand.MP4_AUDIO_BOOK:
                    ret = "M4B ";
                    break;
                case Brand.MP4_WITH_MPEG_7:
                    ret = "mp71";
                    break;
                case Brand.AVC:
                    ret = "avc1";
                    break;
                case Brand.JPEG_2000:
                    ret = "JP2 ";
                    break;
                case Brand.JPEG_2000_COMPOUND_IMAGE:
                    ret = "jpm ";
                    break;
                case Brand.JPEG_2000_EXTENDED:
                    ret = "jpx ";
                    break;
                case Brand.MOTION_JPEG_2000_SIMPLE_PROFILE:
                    ret = "mj2s";
                    break;
                case Brand.MOTION_JPEG_2000_GENERAL_PROFILE:
                    ret = "mjp2";
                    break;
                case Brand.DMB_FULL:
                    ret = "dmb1";
                    break;
                case Brand.DMB_MPEG_2:
                    ret = "da0a";
                    break;
                case Brand.DMB_MPEG_2_EXTENDED:
                    ret = "da0b";
                    break;
                case Brand.DMB_BSAC:
                    ret = "da1a";
                    break;
                case Brand.DMB_BSAC_EXTENDED:
                    ret = "da1b";
                    break;
                case Brand.DMB_HE_AAC_V2:
                    ret = "da2a";
                    break;
                case Brand.DMB_HE_AAC_V2_EXTENDED:
                    ret = "da2b";
                    break;
                case Brand.DMB_HE_AAC:
                    ret = "da3a";
                    break;
                case Brand.DMB_HE_AAC_EXTENDED:
                    ret = "da3b";
                    break;
                case Brand.DMB_AVC_BSAC:
                    ret = "dv1a";
                    break;
                case Brand.DMB_MAF_AVC_BSAC_EXTENDED:
                    ret = "dv1b";
                    break;
                case Brand.DMB_MAF_AVC_HE_AAC_V2:
                    ret = "dv2a";
                    break;
                case Brand.DMB_MAF_AVC_HE_AAC_V2_EXTENDED:
                    ret = "dv2b";
                    break;
                case Brand.DMB_MAF_AVC_HE_AAC:
                    ret = "dv3a";
                    break;
                case Brand.DMB_MAF_AVC_HE_AAC_EXTENDED:
                    ret = "dv3b";
                    break;
                case Brand.MPEG_21:
                    ret = "mp21";
                    break;
                case Brand.MPPI_PHOTO_PLAYER:
                    ret = "MPPI";
                    break;
                case Brand.JPSEARCH:
                    ret = "jpsi";
                    break;
                case Brand.THREE_GPP_RELEASE_1:
                    ret = "3gp1";
                    break;
                case Brand.THREE_GPP_RELEASE_2:
                    ret = "3gp2";
                    break;
                case Brand.THREE_GPP_RELEASE_3:
                    ret = "3gp3";
                    break;
                case Brand.THREE_GPP_RELEASE_4:
                    ret = "3gp4";
                    break;
                case Brand.THREE_GPP_RELEASE_5:
                    ret = "3gp5";
                    break;
                case Brand.THREE_GPP_RELEASE_6:
                    ret = "3gp6";
                    break;
                case Brand.THREE_GPP_RELEASE_6_GENERAL:
                    ret = "3gg6";
                    break;
                case Brand.THREE_GPP_RELEASE_6_EXTENDED:
                    ret = "3ge6";
                    break;
                case Brand.THREE_GPP_RELEASE_6_PROGRESSIVE_DOWNLOAD:
                    ret = "3gr6";
                    break;
                case Brand.THREE_GPP_RELEASE_6_STREAMING:
                    ret = "3gs6";
                    break;
                case Brand.THREE_GPP_RELEASE_7:
                    ret = "3gp7";
                    break;
                case Brand.THREE_GPP_RELEASE_7_EXTENDED:
                    ret = "3ge7";
                    break;
                case Brand.THREE_GPP_RELEASE_7_STREAMING:
                    ret = "3gs7";
                    break;
                case Brand.THREE_GPP_RELEASE_8:
                    ret = "3gp7";
                    break;
                case Brand.THREE_GPP_RELEASE_8_RECORDING:
                    ret = "3gt8";
                    break;
                case Brand.THREE_GPP_RELEASE_9:
                    ret = "3gs9";
                    break;
                case Brand.THREE_GPP_RELEASE_9_PROGRESSIVE_DOWNLOAD:
                    ret = "3gr9";
                    break;
                case Brand.THREE_GPP_RELEASE_9_EXTENDED:
                    ret = "3ge9";
                    break;
                case Brand.THREE_GPP_RELEASE_9_RECORDING:
                    ret = "3gt9";
                    break;
                case Brand.THREE_GPP_RELEASE_9_FILE_DELIVERY:
                    ret = "3gf9";
                    break;
                case Brand.THREE_GPP_RELEASE_9_ADAPTIVE_STREAMING:
                    ret = "3gh9";
                    break;
                case Brand.THREE_GPP_RELEASE_9_MEDIA_SEGMENT:
                    ret = "3gm9";
                    break;
                case Brand.THREE_GPP2_A:
                    ret = "3g2a";
                    break;
                case Brand.THREE_GPP2_B:
                    ret = "3g2b";
                    break;
                case Brand.THREE_GPP2_C:
                    ret = "3g2c";
                    break;
                case Brand.THREE_GPP2_KDDI_3G_EZMOVIE:
                    ret = "KDDI";
                    break;
                case Brand.MPEG_4_MOBILE_PROFILE_:
                    ret = "mmp4";
                    break;
                case Brand.DIRAC:
                    ret = "drc1";
                    break;
                case Brand.DIGITAL_MEDIA_PROJECT:
                    ret = "dmpf";
                    break;
                case Brand.DVB_OVER_RTP:
                    ret = "dvr1";
                    break;
                case Brand.DVB_OVER_MPEG_2_TRANSPORT_STREAM:
                    ret = "dvt1";
                    break;
                case Brand.SD_MEMORY_CARD_VIDEO:
                    ret = "sdv ";
                    break;
                case Brand.ADOBE_FLASH_PLAYER_VIDEO:
                    ret = "F4V ";
                    break;
                case Brand.ADOBE_FLASH_PLAYER_PROTECTED_VIDEO:
                    ret = "F4P ";
                    break;
                case Brand.ADOBE_FLASH_PLAYER_AUDIO:
                    ret = "F4A ";
                    break;
                case Brand.ADOBE_FLASH_PLAYER_AUDIO_BOOK:
                    ret = "F4B ";
                    break;
                case Brand.APPLE_QUICKTIME:
                    ret = "qt  ";
                    break;
                case Brand.APPLE_TV:
                    ret = "M4VH";
                    break;
                case Brand.APPLE_IPHONE_VIDEO:
                    ret = "M4VP";
                    break;
                case Brand.ARRI_DIGITAL_CAMERA:
                    ret = "ARRI";
                    break;
                case Brand.CANON_DIGITAL_CAMERA:
                    ret = "CAEP";
                    break;
                case Brand.CASIO_DIGITAL_CAMERA:
                    ret = "caqv";
                    break;
                case Brand.CONVERGENT_DESIGN:
                    ret = "CDes";
                    break;
                case Brand.DECE_COMMON_FILE_FORMAT:
                    ret = "ccff";
                    break;
                case Brand.ISMACRYP_2_ENCRYPTED_FILE:
                    ret = "isc2";
                    break;
                case Brand.NIKON_DIGITAL_CAMERA:
                    ret = "niko";
                    break;
                case Brand.LEICA_DIGITAL_CAMERA:
                    ret = "LCAG";
                    break;
                case Brand.MICROSOFT_PIFF:
                    ret = "piff";
                    break;
                case Brand.NERO_DIGITAL_AAC_AUDIO:
                    ret = "NDAS";
                    break;
                case Brand.NERO_STANDARD_PROFILE:
                    ret = "NDSS";
                    break;
                case Brand.NERO_CINEMA_PROFILE:
                    ret = "NDSC";
                    break;
                case Brand.NERO_HDTV_PROFILE:
                    ret = "NDSH";
                    break;
                case Brand.NERO_MOBILE_PROFILE:
                    ret = "NDSM";
                    break;
                case Brand.NERO_PORTABLE_PROFILE:
                    ret = "NDSP";
                    break;
                case Brand.NERO_AVC_STANDARD_PROFILE:
                    ret = "NDXS";
                    break;
                case Brand.NERO_AVC_CINEMA_PROFILE:
                    ret = "NDXC";
                    break;
                case Brand.NERO_AVC_HDTV_PROFILE:
                    ret = "NDXH";
                    break;
                case Brand.NERO_AVC_MOBILE_PROFILE:
                    ret = "NDXM";
                    break;
                case Brand.NERO_AVC_PORTABLE_PROFILE:
                    ret = "NDXP";
                    break;
                case Brand.OMA_DCF_2:
                    ret = "odcf";
                    break;
                case Brand.OMA_PDCF_2_1:
                    ret = "opf2";
                    break;
                case Brand.OMA_PDCF_XBS_EXTENSIONS:
                    ret = "opx2";
                    break;
                case Brand.PANASONIC_DIGITAL_CAMERA:
                    ret = "pana";
                    break;
                case Brand.ROSS_VIDEO:
                    ret = "ROSS";
                    break;
                case Brand.SAMSUNG_STEREOSCOPIC_SINGLE_STREAM:
                    ret = "ssc1";
                    break;
                case Brand.SAMSUNG_STEREOSCOPIC_DUAL_STREAM:
                    ret = "ssc2";
                    break;
                case Brand.SONY_MOBILE:
                    ret = "mqt ";
                    break;
                case Brand.SONY_PSP:
                    ret = "MSNV";
                    break;
                default:
                    ret = null;
                    break;
            }

            return ret;
        }
    }
}
