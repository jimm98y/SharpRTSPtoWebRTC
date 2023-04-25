namespace SharpJaad.MP4.OD
{
	/**
	* The <code>InitialObjectDescriptor</code> is a variation of the
	* <code>ObjectDescriptor</code> that shall be used to gain initial access to
	* content.
	*
	* @author in-somnia
	*/
	public class InitialObjectDescriptor : Descriptor
	{
		private int _objectDescriptorID;
		private bool _urlPresent, _includeInlineProfiles;
		private string _url;
		private int _odProfile, _sceneProfile, _audioProfile, _visualProfile, _graphicsProfile;

		public override void decode(MP4InputStream input)
		{
			//10 bits objectDescriptorID, 1 bit url flag, 1 bit
			//includeInlineProfiles flag, 4 bits reserved
			int x = (int)input.readBytes(2);
			_objectDescriptorID = (x >> 6) & 0x3FF;
			_urlPresent = ((x >> 5) & 1) == 1;
			_includeInlineProfiles = ((x >> 4) & 1) == 1;

			if (_urlPresent) _url = input.readString(_size - 2);
			else
			{
				_odProfile = input.read();
				_sceneProfile = input.read();
				_audioProfile = input.read();
				_visualProfile = input.read();
				_graphicsProfile = input.read();
			}

			ReadChildren(input);
		}

		/**
		 * The ID uniquely identifies this ObjectDescriptor within its name scope.
		 * It should be within 0 and 1023 exclusively. The value 0 is forbidden and
		 * the value 1023 is reserved.
		 *
		 * @return this ObjectDescriptor's ID
		 */
		public int GetObjectDescriptorID()
		{
			return _objectDescriptorID;
		}

		/**
		 * A flag that, if set, indicates that the subsequent profile indications
		 * take into account the resources needed to process any content that may
		 * be inlined.
		 *
		 * @return true if this ObjectDescriptor includes inline profiles
		 */
		public bool IncludesInlineProfiles()
		{
			return _includeInlineProfiles;
		}

		/**
		 * A flag that indicates the presence of a URL. If set, no profiles are
		 * present.
		 *
		 * @return true if a URL is present
		 */
		public bool IsURLPresent()
		{
			return _urlPresent;
		}

		/**
		 * A URL String that shall point to another InitialObjectDescriptor. If no
		 * URL is present (if <code>isURLPresent()</code> returns false) this method
		 * returns null.
		 *
		 * @return a URL String or null if none is present
		 */
		public string GetURL()
		{
			return _url;
		}

		/**
		 * A flag that indicates the presence of profiles. If set, no URL is
		 * present.
		 *
		 * @return true if profiles are present
		 */
		public bool AreProfilesPresent()
		{
			return !_urlPresent;
		}

		//TODO: javadoc
		public int GetODProfile()
		{
			return _odProfile;
		}

		/**
		 * An indication of the scene description profile required to process the
		 * content associated with this InitialObjectDescriptor.<br />
		 * The value should be one of the following:
		 * 0x00: reserved for ISO use
		 * 0x01: ISO 14496-1 XXXX profile
		 * 0x02-0x7F: reserved for ISO use
		 * 0x80-0xFD: user private
		 * 0xFE: no scene description profile specified
		 * 0xFF: no scene description capability required
		 *
		 * @return the scene profile
		 */
		public int GetSceneProfile()
		{
			return _sceneProfile;
		}

		/**
		 * An indication of the audio profile required to process the content
		 * associated with this InitialObjectDescriptor.<br />
		 * The value should be one of the following:
		 * 0x00: reserved for ISO use
		 * 0x01: ISO 14496-3 XXXX profile
		 * 0x02-0x7F: reserved for ISO use
		 * 0x80-0xFD: user private
		 * 0xFE: no audio profile specified
		 * 0xFF: no audio capability required
		 *
		 * @return the audio profile
		 */
		public int GetAudioProfile()
		{
			return _audioProfile;
		}

		/**
		 * An indication of the visual profile required to process the content
		 * associated with this InitialObjectDescriptor.<br />
		 * The value should be one of the following:
		 * 0x00: reserved for ISO use
		 * 0x01: ISO 14496-2 XXXX profile
		 * 0x02-0x7F: reserved for ISO use
		 * 0x80-0xFD: user private
		 * 0xFE: no visual profile specified
		 * 0xFF: no visual capability required
		 *
		 * @return the visual profile
		 */
		public int GetVisualProfile()
		{
			return _visualProfile;
		}

		/**
		 * An indication of the graphics profile required to process the content
		 * associated with this InitialObjectDescriptor.<br />
		 * The value should be one of the following:
		 * 0x00: reserved for ISO use
		 * 0x01: ISO 14496-1 XXXX profile
		 * 0x02-0x7F: reserved for ISO use
		 * 0x80-0xFD: user private
		 * 0xFE: no graphics profile specified
		 * 0xFF: no graphics capability required
		 *
		 * @return the graphics profile
		 */
		public int GetGraphicsProfile()
		{
			return _graphicsProfile;
		}
	}
}