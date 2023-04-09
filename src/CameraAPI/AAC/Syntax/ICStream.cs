using CameraAPI.AAC.Error;
using CameraAPI.AAC.Gain;
using CameraAPI.AAC.Huffman;
using CameraAPI.AAC.Tools;
using System;

namespace CameraAPI.AAC.Syntax
{
    public class ICStream : Constants
    {
		private const int SF_DELTA = 60;
		private const int SF_OFFSET = 200;
		private static int randomState = 0x1F2E3D4C;
		private int frameLength;
		//always needed
		private ICSInfo info;
		private int[] sfbCB;
		private int[] sectEnd;
		private float[] data;
		private float[] scaleFactors;
		private int globalGain;
		private bool pulseDataPresent, tnsDataPresent, gainControlPresent;
		//only allocated if needed
		private TNS tns;
		private GainControl gainControl;
		private int[] pulseOffset, pulseAmp;
		private int pulseCount;
		private int pulseStartSWB;
		//error resilience
		private bool noiseUsed;
		private int reorderedSpectralDataLen, longestCodewordLen;
		private RVLC rvlc;

		public ICStream(int frameLength) {
			this.frameLength = frameLength;
			info = new ICSInfo(frameLength);
			sfbCB = new int[MAX_SECTIONS];
			sectEnd = new int[MAX_SECTIONS];
			data = new float[frameLength];
			scaleFactors = new float[MAX_SECTIONS];
		}

		/* ========= decoding ========== */
		public void decode(BitStream input, bool commonWindow, DecoderConfig conf) {
			if(conf.isScalefactorResilienceUsed()&&rvlc==null) rvlc = new RVLC();
			bool er = conf.getProfile().IsErrorResilientProfile();

			globalGain = input.readBits(8);

			if(!commonWindow) info.decode(input, conf, commonWindow);

			decodeSectionData(input, conf.isSectionDataResilienceUsed());

			//if(conf.isScalefactorResilienceUsed()) rvlc.decode(in, this, scaleFactors);
			/*else*/ decodeScaleFactors(input);

			pulseDataPresent = input.readBool();
			if(pulseDataPresent) {
				if(info.isEightShortFrame()) throw new AACException("pulse data not allowed for short frames");
				//LOGGER.log(Level.FINE, "PULSE");
				decodePulseData(input);
			}

			tnsDataPresent = input.readBool();
			if(tnsDataPresent&&!er) {
				if(tns==null) tns = new TNS();
				tns.decode(input, info);
			}

			gainControlPresent = input.readBool();
			if(gainControlPresent) {
				if(gainControl==null) gainControl = new GainControl(frameLength);
				//LOGGER.log(Level.FINE, "GAIN");
				gainControl.Decode(input, info.getWindowSequence());
			}

			//RVLC spectral data
			//if(conf.isScalefactorResilienceUsed()) rvlc.decodeScalefactors(this, in, scaleFactors);

			if(conf.isSpectralDataResilienceUsed()) {
				int max = (conf.getChannelConfiguration()==ChannelConfiguration.CHANNEL_CONFIG_STEREO) ? 6144 : 12288;
				reorderedSpectralDataLen = Math.Max(input.readBits(14), max);
				longestCodewordLen = Math.Max(input.readBits(6), 49);
				//HCR.decodeReorderedSpectralData(this, in, data, conf.isSectionDataResilienceUsed());
			}
			else decodeSpectralData(input);
		}

		public void decodeSectionData(BitStream input, bool sectionDataResilienceUsed) {
			Arrays.Fill(sfbCB, 0);
			Arrays.Fill(sectEnd, 0);
			int bits = info.isEightShortFrame() ? 3 : 5;
			int escVal = (1<<bits)-1;

			int windowGroupCount = info.getWindowGroupCount();
			int maxSFB = info.getMaxSFB();

			int end, cb, incr;
			int idx = 0;

			for(int g = 0; g<windowGroupCount; g++) {
				int k = 0;
				while(k<maxSFB) {
					end = k;
					cb = input.readBits(4);
					if(cb==12) throw new AACException("invalid huffman codebook: 12");
					while((incr = input.readBits(bits))==escVal) {
						end += incr;
					}
					end += incr;
					if(end>maxSFB) throw new AACException("too many bands: "+end+", allowed: "+maxSFB);
					for(; k<end; k++) {
						sfbCB[idx] = cb;
						sectEnd[idx++] = end;
					}
				}
			}
		}

		private void decodePulseData(BitStream input) {
			pulseCount = input.readBits(2)+1;
			pulseStartSWB = input.readBits(6);
			if(pulseStartSWB>=info.getSWBCount()) throw new AACException("pulse SWB out of range: "+pulseStartSWB+" > "+info.getSWBCount());

			if(pulseOffset==null||pulseCount!=pulseOffset.Length) {
				//only reallocate if needed
				pulseOffset = new int[pulseCount];
				pulseAmp = new int[pulseCount];
			}

			pulseOffset[0] = info.getSWBOffsets()[pulseStartSWB];
			pulseOffset[0] += input.readBits(5);
			pulseAmp[0] = input.readBits(4);
			for(int i = 1; i<pulseCount; i++) {
				pulseOffset[i] = input.readBits(5)+pulseOffset[i-1];
				if(pulseOffset[i]>1023) throw new AACException("pulse offset out of range: "+pulseOffset[0]);
				pulseAmp[i] = input.readBits(4);
			}
		}

		public void decodeScaleFactors(BitStream input) {
			int windowGroups = info.getWindowGroupCount();
			int maxSFB = info.getMaxSFB();
			//0: spectrum, 1: noise, 2: intensity
			int[] offset = {globalGain, globalGain-90, 0};

			int tmp;
			bool noiseFlag = true;

			int sfb, idx = 0;
			for(int g = 0; g<windowGroups; g++) {
				for(sfb = 0; sfb<maxSFB;) {
					int end = sectEnd[idx];
					switch(sfbCB[idx]) {
						case HCB.ZERO_HCB:
							for(; sfb<end; sfb++, idx++) {
								scaleFactors[idx] = 0;
							}
							break;
						case HCB.INTENSITY_HCB:
						case HCB.INTENSITY_HCB2:
							for(; sfb<end; sfb++, idx++) {
								offset[2] += Huffman.Huffman.DecodeScaleFactor(input) -SF_DELTA;
								tmp = Math.Min(Math.Max(offset[2], -155), 100);
								scaleFactors[idx] = ScaleFactorTable.SCALEFACTOR_TABLE[-tmp+SF_OFFSET];
							}
							break;
						case HCB.NOISE_HCB:
							for(; sfb<end; sfb++, idx++) {
								if(noiseFlag) {
									offset[1] += input.readBits(9)-256;
									noiseFlag = false;
								}
								else offset[1] += Huffman.Huffman.DecodeScaleFactor(input) -SF_DELTA;
								tmp = Math.Min(Math.Max(offset[1], -100), 155);
								scaleFactors[idx] = -ScaleFactorTable.SCALEFACTOR_TABLE[tmp+SF_OFFSET];
							}
							break;
						default:
							for(; sfb<end; sfb++, idx++) {
								offset[0] += Huffman.Huffman.DecodeScaleFactor(input) -SF_DELTA;
								if(offset[0]>255) throw new AACException("scalefactor out of range: "+offset[0]);
								scaleFactors[idx] = ScaleFactorTable.SCALEFACTOR_TABLE[offset[0]-100+SF_OFFSET];
							}
							break;
					}
				}
			}
		}

		private void decodeSpectralData(BitStream input) {
			Arrays.Fill(data, 0);
			int maxSFB = info.getMaxSFB();
			int windowGroups = info.getWindowGroupCount();
			int[] offsets = info.getSWBOffsets();
			int[] buf = new int[4];

			int sfb, j, k, w, hcb, off, width, num;
			int groupOff = 0, idx = 0;
			for(int g = 0; g<windowGroups; g++) {
				int groupLen = info.getWindowGroupLength(g);

				for(sfb = 0; sfb<maxSFB; sfb++, idx++) {
					hcb = sfbCB[idx];
					off = groupOff+offsets[sfb];
					width = offsets[sfb+1]-offsets[sfb];
					if(hcb== HCB.ZERO_HCB || hcb== HCB.INTENSITY_HCB || hcb== HCB.INTENSITY_HCB2) {
						for(w = 0; w<groupLen; w++, off += 128) {
							Arrays.Fill(data, off, off+width, 0);
						}
					}
					else if(hcb== HCB.NOISE_HCB) {
						//apply PNS: fill with random values
						for(w = 0; w<groupLen; w++, off += 128) {
							float energy = 0;

							for(k = 0; k<width; k++) {
								randomState *= 1664525+1013904223;
								data[off+k] = randomState;
								energy += data[off+k]*data[off+k];
							}

							float scale = (float) (scaleFactors[idx]/Math.Sqrt(energy));
							for(k = 0; k<width; k++) {
								data[off+k] *= scale;
							}
						}
					}
					else {
						for(w = 0; w<groupLen; w++, off += 128) {
							num = (hcb>= HCB.FIRST_PAIR_HCB) ? 2 : 4;
							for(k = 0; k<width; k += num) {
								Huffman.Huffman.DecodeSpectralData(input, hcb, buf, 0);

								//inverse quantization & scaling
								for(j = 0; j<num; j++) {
									data[off+k+j] = (buf[j]>0) ? IQTable.IQ_TABLE[buf[j]] : -IQTable.IQ_TABLE[-buf[j]];
									data[off+k+j] *= scaleFactors[idx];
								}
							}
						}
					}
				}
				groupOff += groupLen<<7;
			}
		}

		/* =========== gets ============ */
		/**
		 * Does inverse quantization and applies the scale factors on the decoded
		 * data. After this the noiseless decoding is finished and the decoded data
		 * is returned.
		 * @return the inverse quantized and scaled data
		 */
		public float[] getInvQuantData() {
			return data;
		}

		public ICSInfo getInfo() {
			return info;
		}

		public int[] getSectEnd() {
			return sectEnd;
		}

		public int[] getSfbCB() {
			return sfbCB;
		}

		public float[] getScaleFactors() {
			return scaleFactors;
		}

		public bool isTNSDataPresent() {
			return tnsDataPresent;
		}

		public TNS getTNS() {
			return tns;
		}

		public int getGlobalGain() {
			return globalGain;
		}

		public bool isNoiseUsed() {
			return noiseUsed;
		}

		public int getLongestCodewordLength() {
			return longestCodewordLen;
		}

		public int getReorderedSpectralDataLength() {
			return reorderedSpectralDataLen;
		}

		public bool isGainControlPresent() {
			return gainControlPresent;
		}

		public GainControl getGainControl() {
			return gainControl;
		}
    }
}
