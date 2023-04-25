using SharpJaad.MP4.Boxes.Impl.SampleGroupEntries;

namespace SharpJaad.MP4.Boxes.Impl
{
	/**
	 * This description table gives information about the characteristics of sample
	 * groups. The descriptive information is any other information needed to define
	 * or characterize the sample group.
	 *
	 * There may be multiple instances of this box if there is more than one sample
	 * grouping for the samples in a track. Each instance of the
	 * SampleGroupDescriptionBox has a type code that distinguishes different sample
	 * groupings. Within a track, there shall be at most one instance of this box
	 * with a particular grouping type. The associated SampleToGroupBox shall
	 * indicate the same value for the grouping type.
	 *
	 * The information is stored in the sample group description box after the
	 * entry-count. An abstract entry type is defined and sample groupings shall
	 * define derived types to represent the description of each sample group. For
	 * video tracks, an abstract VisualSampleGroupEntry is used with similar types
	 * for audio and hint tracks.
	 *
	 * @author in-somnia
	 */
	public class SampleGroupDescriptionBox : FullBox
	{
		private long _groupingType, _defaultLength, _descriptionLength;
		private SampleGroupDescriptionEntry[] _entries;

		public SampleGroupDescriptionBox() : base("Sample Group Description Box")
		{ }

		public override void decode(MP4InputStream input)
		{
			base.decode(input);

			_groupingType = input.readBytes(4);
			_defaultLength = (version == 1) ? input.readBytes(4) : 0;

			int entryCount = (int)input.readBytes(4);

			//TODO!
			/*final HandlerBox hdlr = (HandlerBox) parent.getParent().getParent().getChild(BoxTypes.HANDLER_BOX);
			final int handlerType = (int) hdlr.getHandlerType();

			final Class<? extends BoxImpl> boxClass;
			switch(handlerType) {
			case HandlerBox.TYPE_VIDEO:
			boxClass = VisualSampleGroupEntry.class;
			break;
			case HandlerBox.TYPE_SOUND:
			boxClass = AudioSampleGroupEntry.class;
			break;
			case HandlerBox.TYPE_HINT:
			boxClass = HintSampleGroupEntry.class;
			break;
			default:
			boxClass = null;
			}

			for(int i = 1; i<entryCount; i++) {
			if(version==1&&defaultLength==0) {
			descriptionLength = in.readBytes(4);
			left -= 4;
			}
			if(boxClass!=null) {
			entries[i] = (SampleGroupDescriptionEntry) BoxFactory.parseBox(in, boxClass);
			if(entries[i]!=null) left -= entries[i].getSize();
			}
			}*/
		}

		/**
		 * The grouping type is an integer that identifies the SampleToGroup box
		 * that is associated with this sample group description.
		 */
		public long GetGroupingType()
		{
			return _groupingType;
		}

		/**
		 * The default length indicates the length of every group entry (if the
		 * length is constant), or zero (0) if it is variable.
		 */
		public long GetDefaultLength()
		{
			return _defaultLength;
		}

		/**
		 * The description length indicates the length of an individual group entry,
		 * in the case it varies from entry to entry and default length is therefore 0.
		 */
		public long GetDescriptionLength()
		{
			return _descriptionLength;
		}
	}
}
