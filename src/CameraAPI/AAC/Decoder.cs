using CameraAPI.AAC.Filterbank;
using CameraAPI.AAC.Syntax;
using CameraAPI.AAC.Transport;
using System;
using System.ComponentModel;

namespace CameraAPI.AAC
{
    public class Decoder
    {
        static Decoder()
		{
			//foreach(Handler h in LOGGER.getHandlers()) {
			//	LOGGER.removeHandler(h);
			//}
			//LOGGER.setLevel(Level.WARNING);

			//ConsoleHandler h = new ConsoleHandler();
			//h.setLevel(Level.ALL);
			//LOGGER.addHandler(h);
		}

		private DecoderConfig config;
		private SyntacticElements syntacticElements;
		private FilterBank filterBank;
		private BitStream input;
		private ADIFHeader adifHeader;

		/**
		 * The methods returns true, if a profile is supported by the decoder.
		 * @param profile an AAC profile
		 * @return true if the specified profile can be decoded
		 * @see Profile#isDecodingSupported()
		 */
		public static bool canDecode(Profile profile) {
			return profile.IsDecodingSupported();
		}

		/**
		 * Initializes the decoder with a MP4 decoder specific info.
		 *
		 * After this the MP4 frames can be passed to the
		 * <code>decodeFrame(byte[], SampleBuffer)</code> method to decode them.
		 * 
		 * @param decoderSpecificInfo a byte array containing the decoder specific info from an MP4 container
		 * @throws AACException if the specified profile is not supported
		 */
		public Decoder(byte[] decoderSpecificInfo) {
			config = DecoderConfig.parseMP4DecoderSpecificInfo(decoderSpecificInfo);
			if(config==null) throw new InvalidEnumArgumentException("illegal MP4 decoder specific info");

			if(!canDecode(config.getProfile())) throw new AACException("unsupported profile: " + config.getProfile());

			syntacticElements = new SyntacticElements(config);
			filterBank = new FilterBank(config.isSmallFrameUsed(), (int)config.getChannelConfiguration());

			input = new BitStream();

			//LOGGER.log(Level.FINE, "profile: {0}", config.getProfile());
			//LOGGER.log(Level.FINE, "sf: {0}", config.getSampleFrequency().getFrequency());
			//LOGGER.log(Level.FINE, "channels: {0}", config.getChannelConfiguration().getDescription());
		}

		public DecoderConfig getConfig() {
			return config;
		}

		/**
		 * Decodes one frame of AAC data in frame mode and returns the raw PCM
		 * data.
		 * @param frame the AAC frame
		 * @param buffer a buffer to hold the decoded PCM data
		 * @throws AACException if decoding fails
		 */
		public void decodeFrame(byte[] frame, SampleBuffer buffer) {
			if(frame!=null) input.setData(frame);
			try {
				decode(buffer);
			}
			catch(AACException e) {
				if(!e.IsEndOfStream) throw e;
				//else LOGGER.warning("unexpected end of frame");
			}
		}

		private void decode(SampleBuffer buffer) {
			if(ADIFHeader.isPresent(input)) {
				adifHeader = ADIFHeader.readHeader(input);
				PCE pce = adifHeader.getFirstPCE();
				config.setProfile(pce.getProfile());
				config.setSampleFrequency(pce.getSampleFrequency());
				config.setChannelConfiguration((ChannelConfiguration)(pce.getChannelCount()));
			}

			if(!canDecode(config.getProfile())) throw new AACException("unsupported profile: "+config.getProfile());

			syntacticElements.startNewFrame();

			try {
				//1: bitstream parsing and noiseless coding
				syntacticElements.decode(input);
				//2: spectral processing
				syntacticElements.process(filterBank);
				//3: send to output buffer
				syntacticElements.sendToOutput(buffer);
			}
			catch(AACException e) {
				buffer.SetData(new byte[0], 0, 0, 0, 0);
				throw e;
			}
			catch(Exception e) {
				buffer.SetData(new byte[0], 0, 0, 0, 0);
				throw new AACException(e.Message);
			}
		}
    }
}
