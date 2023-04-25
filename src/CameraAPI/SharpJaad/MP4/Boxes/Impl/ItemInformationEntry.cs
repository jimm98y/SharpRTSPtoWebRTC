namespace SharpJaad.MP4.Boxes.Impl
{
    public class ItemInformationEntry : FullBox
    {
        private int _itemID, _itemProtectionIndex;
        private string _itemName, _contentType, _contentEncoding;
        private long _extensionType;
        private Extension _extension;

        public ItemInformationEntry() : base("Item Information Entry")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            if ((_version == 0) || (_version == 1))
            {
                _itemID = (int)input.ReadBytes(2);
                _itemProtectionIndex = (int)input.ReadBytes(2);
                _itemName = input.ReadUTFString((int)GetLeft(input), MP4InputStream.UTF8);
                _contentType = input.ReadUTFString((int)GetLeft(input), MP4InputStream.UTF8);
                _contentEncoding = input.ReadUTFString((int)GetLeft(input), MP4InputStream.UTF8); //optional
            }
            if (_version == 1 && GetLeft(input) > 0)
            {
                //optional
                _extensionType = input.ReadBytes(4);
                if (GetLeft(input) > 0)
                {
                    _extension = Extension.ForType((int)_extensionType);
                    if (_extension != null) _extension.decode(input);
                }
            }
        }

        /**
         * The item ID contains either 0 for the primary resource (e.g., the XML
         * contained in an 'xml ' box) or the ID of the item for which the following
         * information is defined.
         *
         * @return the item ID
         */
        public int GetItemID()
        {
            return _itemID;
        }

        /**
         * The item protection index contains either 0 for an unprotected item, or
         * the one-based index into the item protection box defining the protection
         * applied to this item (the first box in the item protection box has the
         * index 1).
         *
         * @return the item protection index
         */
        public int GetItemProtectionIndex()
        {
            return _itemProtectionIndex;
        }

        /**
         * The item name is a String containing a symbolic name of the item (source
         * file for file delivery transmissions).
         *
         * @return the item name
         */
        public string GetItemName()
        {
            return _itemName;
        }

        /**
         * The content type is a String with the MIME type of the item. If the item 
         * is content encoded (see below), then the content type refers to the item 
         * after content decoding.
         * 
         * @return the content type
         */
        public string GetContentType()
        {
            return _contentType;
        }

        /**
         * The content encoding is an optional String used to indicate that the
         * binary file is encoded and needs to be decoded before interpreted. The
         * values are as defined for Content-Encoding for HTTP/1.1. Some possible
         * values are "gzip", "compress" and "deflate". An empty string indicates no
         * content encoding. Note that the item is stored after the content encoding
         * has been applied.
         *
         * @return the content encoding
         */
        public string GetContentEncoding()
        {
            return _contentEncoding;
        }

        /**
         * The extension type is a printable four-character code that identifies the
         * extension fields of version 1 with respect to version 0 of the item 
         * information entry.
         * 
         * @return the extension type
         */
        public long GetExtensionType()
        {
            return _extensionType;
        }

        /**
         * Returns the extension.
         */
        public Extension GetExtension()
        {
            return _extension;
        }

        public abstract class Extension
        {
            private const int TYPE_FDEL = 1717855596; //fdel

            public static Extension ForType(int type)
            {
                Extension ext;
                switch (type)
                {
                    case Extension.TYPE_FDEL:
                        ext = new FDExtension();
                        break;
                    default:
                        ext = null;
                        break;
                }
                return ext;
            }

            public abstract void decode(MP4InputStream input);
        }

        public class FDExtension : Extension
        {
            private string _contentLocation, _contentMD5;
            private long _contentLength, _transferLength;
            private long[] _groupID;

            public override void decode(MP4InputStream input)
            {
                _contentLocation = input.ReadUTFString(100, MP4InputStream.UTF8);
                _contentMD5 = input.ReadUTFString(100, MP4InputStream.UTF8);

                _contentLength = input.ReadBytes(8);
                _transferLength = input.ReadBytes(8);

                int entryCount = input.Read();
                _groupID = new long[entryCount];
                for (int i = 0; i < entryCount; i++)
                {
                    _groupID[i] = input.ReadBytes(4);
                }
            }

            /**
             * The content location is a String in containing the URI of the file as
             * defined in HTTP/1.1 (RFC 2616).
             *
             * @return the content location
             */
            public string GetContentLocation()
            {
                return _contentLocation;
            }

            /**
             * The content MD5 is a string containing an MD5 digest of the file. See
             * HTTP/1.1 (RFC 2616) and RFC 1864.
             *
             * @return the content MD5
             */
            public string GetContentMD5()
            {
                return _contentMD5;
            }

            /**
             * The total length (in bytes) of the (un-encoded) file.
             *
             * @return the content length
             */
            public long GetContentLength()
            {
                return _contentLength;
            }

            /**
             * The transfer length is the total length (in bytes) of the (encoded)
             * file. Note that transfer length is equal to content length if no
             * content encoding is applied (see above).
             *
             * @return the transfer length
             */
            public long GetTransferLength()
            {
                return _transferLength;
            }

            /**
             * The group ID indicates a file group to which the file item (source
             * file) belongs. See 3GPP TS 26.346 for more details on file groups.
             *
             * @return the group IDs
             */
            public long[] GetGroupID()
            {
                return _groupID;
            }
        }
    }
}