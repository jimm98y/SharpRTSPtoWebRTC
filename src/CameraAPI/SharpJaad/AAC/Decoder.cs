using SharpJaad.AAC.Filterbank;
using SharpJaad.AAC.Syntax;
using SharpJaad.AAC.Transport;
using System;
using System.ComponentModel;

namespace SharpJaad.AAC
{
    /// <summary>
    /// AAC Decoder ported from JAAD: https://sourceforge.net/projects/jaadec/
    /// </summary>
    public class Decoder
    {
        /*
        static Decoder()
		{
			foreach (Handler h in LOGGER.getHandlers())
			{
				LOGGER.removeHandler(h);
			}
			LOGGER.setLevel(Level.WARNING);

			ConsoleHandler h = new ConsoleHandler();
			h.setLevel(Level.ALL);
			LOGGER.addHandler(h);
		}
		*/

        private DecoderConfig _config;
        private SyntacticElements _syntacticElements;
        private FilterBank _filterBank;
        private BitStream _input;
        private ADIFHeader _adifHeader;

        /// <summary>
        /// The methods returns true, if a profile is supported by the decoder.
        /// </summary>
        /// <param name="profile">An AAC profile.</param>
        /// <returns>true if the specified profile can be decoded</returns>
        public static bool CanDecode(Profile profile)
        {
            return profile.IsDecodingSupported();
        }

        /// <summary>
        /// Initializes the decoder with a MP4 decoder specific info. After this the MP4 frames can be passed to the decodeFrame(byte[], SampleBuffer) method to decode them.
        /// </summary>
        /// <param name="decoderSpecificInfo">A byte array containing the decoder specific info from an MP4 container.</param>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        /// <exception cref="AACException">If the specified profile is not supported.</exception>
        public Decoder(byte[] decoderSpecificInfo)
        {
            _config = DecoderConfig.ParseMP4DecoderSpecificInfo(decoderSpecificInfo);
            if (_config == null) throw new InvalidEnumArgumentException("illegal MP4 decoder specific info");

            if (!CanDecode(_config.GetProfile())) throw new AACException("unsupported profile: " + _config.GetProfile());

            _syntacticElements = new SyntacticElements(_config);
            _filterBank = new FilterBank(_config.IsSmallFrameUsed(), (int)_config.GetChannelConfiguration());

            _input = new BitStream();

            //LOGGER.log(Level.FINE, "profile: {0}", config.getProfile());
            //LOGGER.log(Level.FINE, "sf: {0}", config.getSampleFrequency().getFrequency());
            //LOGGER.log(Level.FINE, "channels: {0}", config.getChannelConfiguration().getDescription());
        }

        public Decoder(DecoderConfig cfg)
        {
            _config = cfg ?? throw new InvalidEnumArgumentException("illegal MP4 decoder specific info");

            if (!CanDecode(_config.GetProfile())) throw new AACException("unsupported profile: " + _config.GetProfile());

            _syntacticElements = new SyntacticElements(_config);
            _filterBank = new FilterBank(_config.IsSmallFrameUsed(), (int)_config.GetChannelConfiguration());

            _input = new BitStream();

            //LOGGER.log(Level.FINE, "profile: {0}", config.getProfile());
            //LOGGER.log(Level.FINE, "sf: {0}", config.getSampleFrequency().getFrequency());
            //LOGGER.log(Level.FINE, "channels: {0}", config.getChannelConfiguration().getDescription());
        }

        public DecoderConfig GetConfig()
        {
            return _config;
        }

        /// <summary>
        /// Decodes one frame of AAC data in frame mode and returns the raw PCM.
        /// </summary>
        /// <param name="frame">The AAC frame.</param>
        /// <param name="buffer">A buffer to hold the decoded PCM data.</param>
		/// <exception cref="AACException">if decoding fails</exception>
        public void DecodeFrame(byte[] frame, SampleBuffer buffer)
        {
            if (frame != null) _input.SetData(frame);
            try
            {
                Decode(buffer);
            }
            catch (AACException e)
            {
                if (!e.IsEndOfStream) throw e;
                //else LOGGER.warning("unexpected end of frame");
            }
        }

        private void Decode(SampleBuffer buffer)
        {
            if (ADIFHeader.IsPresent(_input))
            {
                _adifHeader = ADIFHeader.ReadHeader(_input);
                PCE pce = _adifHeader.GetFirstPCE();
                _config.SetProfile(pce.GetProfile());
                _config.SetSampleFrequency(pce.GetSampleFrequency());
                _config.SetChannelConfiguration((ChannelConfiguration)pce.GetChannelCount());
            }

            if (!CanDecode(_config.GetProfile())) throw new AACException("unsupported profile: " + _config.GetProfile());

            _syntacticElements.StartNewFrame();

            try
            {
                //1: bitstream parsing and noiseless coding
                _syntacticElements.Decode(_input);
                //2: spectral processing
                _syntacticElements.Process(_filterBank);
                //3: send to output buffer
                _syntacticElements.SendToOutput(buffer);
            }
            catch (AACException e)
            {
                buffer.SetData(new byte[0], 0, 0, 0, 0);
                throw e;
            }
            catch (Exception e)
            {
                buffer.SetData(new byte[0], 0, 0, 0, 0);
                throw new AACException(e.Message);
            }
        }
    }
}
