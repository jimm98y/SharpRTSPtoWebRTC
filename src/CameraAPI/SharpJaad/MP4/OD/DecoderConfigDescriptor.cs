namespace SharpJaad.MP4.OD
{
	/**
	 * The <code>DecoderConfigDescriptor</code> provides information about the
	 * decoder type and the required decoder resources needed for the associates
	 * elementary stream. This is needed at the receiver to determine whether it is
	 * able to decode the elementary stream. A stream type identifies the category
	 * of the stream while the optional decoder specific information contains stream
	 * specific information for the set up of the decoder in a stream specific
	 * format that is opaque to this layer.
	 *
	 * @author in-somnia
	 */
	public class DecoderConfigDescriptor : Descriptor
	{
		private int _objectProfile, _streamType, _decodingBufferSize;
		private bool _upstream;
		private long _maxBitRate, _averageBitRate;

		void decode(MP4InputStream input)
		{
			_objectProfile = input.read();
			//6 bits stream type, 1 bit upstream flag, 1 bit reserved
			int x = input.read();
			_streamType = (x >> 2) & 0x3F;
			_upstream = ((x >> 1) & 1) == 1;

			_decodingBufferSize = (int)input.readBytes(3);
			_maxBitRate = input.readBytes(4);
			_averageBitRate = input.readBytes(4);

			readChildren(input);
		}

		/**
		 * An indication for the object profile (or scene description profile if
		 * streamType==scene description stream), that needs to be supported by the
		 * decoder for this elementary stream as per the following table. For
		 * streamType==object descriptor stream the value 0xFF ('no profile
		 * specified') shall be used.
		 * 
		 * <table>
		 * <tr><th>Value</th><th>Description</th></tr>
		 * <tr>0x00<td></td><td>Reserved for ISO use</td></tr>
		 * <tr>0x01<td></td><td>ISO 14496-1:Systems Simple Scene Description</td></tr>
		 * <tr>0x02<td></td><td>ISO 14496-1:Systems 2D Scene Description</td></tr>
		 * <tr>0x03<td></td><td>ISO 14496-1:Systems VRML Scene Description</td></tr>
		 * <tr>0x04<td></td><td>ISO 14496-1:Systems Audio Scene Description</td></tr>
		 * <tr>0x05<td></td><td>ISO 14496-1:Systems Complete Scene Description</td></tr>
		 * <tr>0x06-0x1F<td></td><td>reserved for ISO use</td></tr>
		 * <tr>0x20<td></td><td>ISO 14496-2:Visual Simple Profile</td></tr>
		 * <tr>0x21<td></td><td>ISO 14496-2:Visual Core Profile</td></tr>
		 * <tr>0x22<td></td><td>ISO 14496-2:Visual Main Profile</td></tr>
		 * <tr>0x23<td></td><td>ISO 14496-2:Visual Simple Scalable Profile</td></tr>
		 * <tr>0x24<td></td><td>ISO 14496-2:Visual 12 Bit</td></tr>
		 * <tr>0x25<td></td><td>ISO 14496-2:Visual Basic Anim. 2D Texture</td></tr>
		 * <tr>0x26<td></td><td>ISO 14496-2:Visual Anim. 2D Mesh</td></tr>
		 * <tr>0x27<td></td><td>ISO 14496-2:Visual Simple Face</td></tr>
		 * <tr>0x28<td></td><td>ISO 14496-2:Visual Simple Scalable Texture</td></tr>
		 * <tr>0x29<td></td><td>ISO 14496-2:Visual Core Scalable Texture</td></tr>
		 * <tr>0x2A-0x3F<td></td><td>reserved for ISO use</td></tr>
		 * <tr>0x40<td></td><td>ISO 14496-3:Audio AAC Main</td></tr>
		 * <tr>0x41<td></td><td>ISO 14496-3:Audio AAC LC</td></tr>
		 * <tr>0x42<td></td><td>ISO 14496-3:Audio T/F</td></tr>
		 * <tr>0x43<td></td><td>ISO 14496-3:Audio T/F Main Scalable</td></tr>
		 * <tr>0x44<td></td><td>ISO 14496-3:Audio T/F LC Scalable</td></tr>
		 * <tr>0x45<td></td><td>ISO 14496-3:Audio TwinVQ Core</td></tr>
		 * <tr>0x46<td></td><td>ISO 14496-3:Audio CELP</td></tr>
		 * <tr>0x47<td></td><td>ISO 14496-3:Audio HVXC</td></tr>
		 * <tr>0x48<td></td><td>ISO 14496-3:Audio HILN</td></tr>
		 * <tr>0x49<td></td><td>ISO 14496-3:Audio TTSI</td></tr>
		 * <tr>0x4A<td></td><td>ISO 14496-3:Audio Main Synthetic</td></tr>
		 * <tr>0x4B<td></td><td>ISO 14496-3:Audio Wavetable Synthetic</td></tr>
		 * <tr>0x4C-0x5F<td></td><td>reserved for ISO use</td></tr>
		 * <tr>0x60<td></td><td>ISO 13818-2:Visual Simple Profile</td></tr>
		 * <tr>0x61<td></td><td>ISO 13818-2:Visual Main Profile</td></tr>
		 * <tr>0x62<td></td><td>ISO 13818-2:Visual SNR Profile</td></tr>
		 * <tr>0x63<td></td><td>ISO 13818-2:Visual Spatial Profile</td></tr>
		 * <tr>0x64<td></td><td>ISO 13818-2:Visual High Profile</td></tr>
		 * <tr>0x65<td></td><td>ISO 13818-2:Visual 442 Profile</td></tr>
		 * <tr>0x66<td></td><td>ISO 13818-7:Audio (MPEG-2 AAC)</td></tr>
		 * <tr>0x67<td></td><td>ISO 13818-3:Audio (MPEG-2 layer 1/2/3)</td></tr>
		 * <tr>0x68<td></td><td>ISO 11172-2:Visual (MPEG-1)</td></tr>
		 * <tr>0x69<td></td><td>ISO 11172-3:Audio (MPEG-1)</td></tr>
		 * <tr>0x6A-0xBF<td></td><td>reserved for ISO use</td></tr>
		 * <tr>0xC0-0xFE<td></td><td>user private</td></tr>
		 * <tr>0xFF<td></td><td>no profile specified</td></tr>
		 * </table>
		 *
		 * @return the object profile
		 */
		public int GetObjectProfile()
		{
			return _objectProfile;
		}

		/**
		 * The stream type conveys the type of this elementary stream as per the
		 * following table:
		 * <table>
		 * <tr><th>Value</th><th>Description</th></tr>
		 * <tr>0x00<td></td><td>reserved for ISO use</td></tr>
		 * <tr>0x01<td></td><td>ObjectDescriptorStream</td></tr>
		 * <tr>0x02<td></td><td>ClockReferenceStream</td></tr>
		 * <tr>0x03<td></td><td>SceneDescriptionStream</td></tr>
		 * <tr>0x04<td></td><td>VisualStream</td></tr>
		 * <tr>0x05<td></td><td>AudioStream</td></tr>
		 * <tr>0x06<td></td><td>MPEG-7 Stream</td></tr>
		 * <tr>0x07-0x09<td></td><td>reserved for ISO use</td></tr>
		 * <tr>0x0A<td></td><td>ObjectContentInfoStream</td></tr>
		 * <tr>0x0B-0x1F<td></td><td>reserved for ISO use</td></tr>
		 * <tr>0x20-0x3F<td></td><td>user private</td></tr>
		 * </table>
		 *
		 * @return the stream type
		 */
		public int GetStreamType()
		{
			return _streamType;
		}

		/**
		 * Indicates if this stream is used for upstream information.
		 *
		 * @return true if this stream is used for upstream information
		 */
		public bool IsUpstream()
		{
			return _upstream;
		}

		/**
		 * The size of the decoding buffer for this elementary stream in byte.
		 *
		 * @return the decoding buffer size
		 */
		public int GetDecodingBufferSize()
		{
			return _decodingBufferSize;
		}

		/**
		 * The maximum bitrate of this elementary stream in any time window of one
		 * second duration.
		 *
		 * @return the maximum bitrate
		 */
		public long GetMaxBitRate()
		{
			return _maxBitRate;
		}

		/**
		 * The average bitrate of the elementary stream. For streams with variable
		 * bitrates this value shall be set to zero.
		 *
		 * @return the average bitrate or 0 if the stream has a variable bitrate
		 */
		public long GetAverageBitRate()
		{
			return _averageBitRate;
		}
	}
}