namespace SharpJaad.MP4.Boxes.Impl
{
	/**
	 * This optional table answers three questions about sample dependency:
	 * <ul>
	 * <li>does this sample depend on others (is it an I-picture)?</li>
	 * <li>do no other samples depend on this one?</li>
	 * <li>does this sample contain multiple (redundant) encodings of the data at
	 * this time-instant (possibly with different dependencies)?</li>
	 * </ul>
	 *
	 * In the absence of this table:
	 * <ul>
	 * <li>the sync sample table answers the first question; in most video codecs,
	 * I-pictures are also sync points</li>
	 * <li>the dependency of other samples on this one is unknown</li>
	 * <li>the existence of redundant coding is unknown</li>
	 * </ul>
	 *
	 * When performing 'trick' modes, such as fast-forward, it is possible to use
	 * the first piece of information to locate independently decodable samples.
	 * Similarly, when performing random access, it may be necessary to locate the
	 * previous sync point or random access recovery point, and roll-forward from
	 * the sync point or the pre-roll starting point of the random access recovery
	 * point to the desired point. While rolling forward, samples on which no others
	 * depend need not be retrieved or decoded.
	 * The value of 'sample is depended on' is independent of the existence of
	 * redundant codings. However, a redundant coding may have different
	 * dependencies from the primary coding; if redundant codings are available, the
	 * value of 'sample depends on' documents only the primary coding.
	 *
	 * A Sample Dependency Box may also occur in the Track Fragment Box.
	 * 
	 * @author in-somnia
	 */
	public class SampleDependencyTypeBox : FullBox
	{
		private int[] _sampleDependsOn, _sampleIsDependedOn, _sampleHasRedundancy;

		public SampleDependencyTypeBox() : base("Sample Dependency Type Box")
		{ }

		public override void decode(MP4InputStream input)
		{
			base.decode(input);

			//get number of samples from SampleSizeBox
			long sampleCount = -1;
			if (parent.HasChild(BoxTypes.SAMPLE_SIZE_BOX)) sampleCount = ((SampleSizeBox)parent.GetChild(BoxTypes.SAMPLE_SIZE_BOX)).GetSampleCount();
			//TODO: uncomment when CompactSampleSizeBox is implemented
			//else if(parent.containsChild(BoxTypes.COMPACT_SAMPLE_SIZE_BOX)) sampleCount = ((CompactSampleSizeBox)parent.getChild(BoxTypes.SAMPLE_SIZE_BOX)).getSampleSize();
			_sampleHasRedundancy = new int[(int)sampleCount];
			_sampleIsDependedOn = new int[(int)sampleCount];
			_sampleDependsOn = new int[(int)sampleCount];

			byte b;
			for (int i = 0; i < sampleCount; i++)
			{
				b = (byte)input.read();
				/* 2 bits reserved
				 * 2 bits sampleDependsOn
				 * 2 bits sampleIsDependedOn
				 * 2 bits sampleHasRedundancy
				 */
				_sampleHasRedundancy[i] = b & 3;
				_sampleIsDependedOn[i] = (b >> 2) & 3;
				_sampleDependsOn[i] = (b >> 4) & 3;
			}
		}

		/**
		 * The 'sample depends on' field takes one of the following four values:
		 * 0: the dependency of this sample is unknown
		 * 1: this sample does depend on others (not an I picture)
		 * 2: this sample does not depend on others (I picture)
		 * 3: reserved
		 *
		 * @return a list of 'sample depends on' values for all samples
		 */
		public int[] GetSampleDependsOn()
		{
			return _sampleDependsOn;
		}

		/**
		 * The 'sample is depended on' field takes one of the following four values:
		 * 0: the dependency of other samples on this sample is unknown
		 * 1: other samples may depend on this one (not disposable)
		 * 2: no other sample depends on this one (disposable)
		 * 3: reserved
		 *
		 * @return a list of 'sample is depended on' values for all samples
		 */
		public int[] GetSampleIsDependedOn()
		{
			return _sampleIsDependedOn;
		}

		/**
		 * The 'sample has redundancy' field takes one of the following four values:
		 * 0: it is unknown whether there is redundant coding in this sample
		 * 1: there is redundant coding in this sample
		 * 2: there is no redundant coding in this sample
		 * 3: reserved
		 * 
		 * @return a list of 'sample has redundancy' values for all samples
		 */
		public int[] GetSampleHasRedundancy()
		{
			return _sampleHasRedundancy;
		}
	}
}
