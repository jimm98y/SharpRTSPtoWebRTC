using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpJaad.MP4.Boxes.Impl.OMA
{
	//TODO: add remaining javadoc
	public class OMACommonHeadersBox : FullBox
	{
		private int _encryptionMethod, _paddingScheme;
		private long _plaintextLength;
		private byte[] _contentID, _rightsIssuerURL;
		private Dictionary<string, string> _textualHeaders;

		public OMACommonHeadersBox() : base("OMA DRM Common Header Box")
		{  }

		public override void Decode(MP4InputStream input)
		{
			base.Decode(input);

			_encryptionMethod = input.Read();
			_paddingScheme = input.Read();
			_plaintextLength = input.ReadBytes(8);
			int contentIDLength = (int)input.ReadBytes(2);
			int rightsIssuerURLLength = (int)input.ReadBytes(2);
			int textualHeadersLength = (int)input.ReadBytes(2);

			_contentID = new byte[contentIDLength];
			input.ReadBytes(_contentID);
			_rightsIssuerURL = new byte[rightsIssuerURLLength];
			input.ReadBytes(_rightsIssuerURL);

			_textualHeaders = new Dictionary<string, string>();
			string key, value;
			while (textualHeadersLength > 0)
			{
				key = Encoding.UTF8.GetString(input.ReadTerminated((int)GetLeft(input), ':'));
				value = Encoding.UTF8.GetString(input.ReadTerminated((int)GetLeft(input), 0));
				_textualHeaders.Add(key, value);

				textualHeadersLength -= key.Count() + value.Count() + 2;
			}

			ReadChildren(input);
		}

		/**
		 * The encryption method defines how the encrypted content can be decrypted.
		 * Values for the field are defined in the following table:
		 * 
		 * <table>
		 * <tr><th>Value</th><th>Algorithm</th></tr>
		 * <tr><td>0</td><td>no encryption used</td></tr>
		 * <tr><td>1</td><td>AES_128_CBC:<br />AES symmetric encryption as defined 
		 * by NIST. 128 bit keys, Cipher block chaining mode (CBC). For the first 
		 * block a 128-bit initialisation vector (IV) is used. For DCF files, the IV
		 * is included in the OMADRMData as a prefix of the encrypted data. For 
		 * non-streamable PDCF files, the IV is included in the IV field of the 
		 * OMAAUHeader and the IVLength field in the OMAAUFormatBox MUST be set to
		 * 16. Padding according to RFC 2630</td></tr>
		 * <tr><td>2</td><td>AES_128_CTR:<br />AES symmetric encryption as defined 
		 * by NIST. 128 bit keys, Counter mode (CTR). The counter block has a length
		 * of 128 bits. For DCF files, the initial counter value is included in the 
		 * OMADRMData as a prefix of the encrypted data. For non-streamable PDCF 
		 * files, the initial counter value is included in the IV field of the 
		 * OMAAUHeader  and the IVLength field in the OMAAUFormatBox MUST be set to 
		 * 16. For each cipherblock the counter is incremented by 1 (modulo 2128). 
		 * No padding.</td></tr>
		 * </table>
		 * 
		 * @return the encryption method
		 */
		public int GetEncryptionMethod()
		{
			return _encryptionMethod;
		}

		/**
		 * The padding scheme defines how the last block of ciphertext is padded. 
		 * Values of the padding scheme field are defined in the following table:
		 * 
		 * <table>
		 * <tr><th>Value</th><th>Padding scheme</th></tr>
		 * <tr><td>0</td><td>No padding (e.g. when using NULL or CTR algorithm)</td></tr>
		 * <tr><td>1</td><td>Padding according to RFC 2630</td></tr>
		 * </table>
		 * 
		 * @return the padding scheme
		 */
		public int GetPaddingScheme()
		{
			return _paddingScheme;
		}

		public long GetPlaintextLength()
		{
			return _plaintextLength;
		}

		public byte[] GetContentID()
		{
			return _contentID;
		}

		public byte[] GetRightsIssuerURL()
		{
			return _rightsIssuerURL;
		}

		public Dictionary<string, string> GetTextualHeaders()
		{
			return _textualHeaders;
		}
	}
}
