namespace SharpJaad.MP4.Boxes.Impl
{
	//TODO: 3gpp brands
	public class FileTypeBox : BoxImpl
	{
		public const string BRAND_ISO_BASE_MEDIA = "isom";
		public const string BRAND_ISO_BASE_MEDIA_2 = "iso2";
		public const string BRAND_ISO_BASE_MEDIA_3 = "iso3";
		public const string BRAND_MP4_1 = "mp41";
		public const string BRAND_MP4_2 = "mp42";
		public const string BRAND_MOBILE_MP4 = "mmp4";
		public const string BRAND_QUICKTIME = "qm  ";
		public const string BRAND_AVC = "avc1";
		public const string BRAND_AUDIO = "M4A ";
		public const string BRAND_AUDIO_2 = "M4B ";
		public const string BRAND_AUDIO_ENCRYPTED = "M4P ";
		public const string BRAND_MP7 = "mp71";
		protected string _majorBrand, _minorVersion;
		protected string[] _compatibleBrands;

		public FileTypeBox() : base("File Type Box")
		{ }

		public override void Decode(MP4InputStream input)
		{
			_majorBrand = input.ReadString(4);
			_minorVersion = input.ReadString(4);
			_compatibleBrands = new string[(int)GetLeft(input) / 4];
			for (int i = 0; i < _compatibleBrands.Length; i++)
			{
				_compatibleBrands[i] = input.ReadString(4);
			}
		}

		public string GetMajorBrand()
		{
			return _majorBrand;
		}

		public string GetMinorVersion()
		{
			return _minorVersion;
		}

		public string[] GetCompatibleBrands()
		{
			return _compatibleBrands;
		}
	}
}
