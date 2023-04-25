namespace SharpJaad.MP4.Boxes.Impl.FD
{
    /**
 * The FD session group box is optional, although it is mandatory for files
 * containing more than one FD hint track. It contains a list of sessions as
 * well as all file groups and hint tracks that belong to each session. An FD
 * session sends simultaneously over all FD hint tracks (channels) that are
 * listed in the FD session group box for a particular FD session.
 *
 * Only one session group should be processed at any time. The first listed
 * hint track in a session group specifies the base channel. If the server has
 * no preference between the session groups, the default choice should be the
 * first session group. The group IDs of all file groups containing the files
 * referenced by the hint tracks shall be included in the list of file groups.
 * The file group IDs can in turn be translated into file group names (using the
 * group ID to name box) that can be included by the server in FDTs.
 *
 * @author in-somnia
 */
    public class FDSessionGroupBox : FullBox
    {
        private long[][] _groupIDs, _hintTrackIDs;

        public FDSessionGroupBox() : base("FD Session Group Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            int sessionGroups = (int)input.ReadBytes(2);
            _groupIDs = new long[sessionGroups][];
            _hintTrackIDs = new long[sessionGroups][];

            int j, entryCount, channelsInSessionGroup;
            for (int i = 0; i < sessionGroups; i++)
            {
                entryCount = input.Read();
                _groupIDs[i] = new long[entryCount];
                for (j = 0; j < entryCount; j++)
                {
                    _groupIDs[i][j] = input.ReadBytes(4);
                }

                channelsInSessionGroup = (int)input.ReadBytes(2);
                _hintTrackIDs[i] = new long[channelsInSessionGroup];
                for (j = 0; j < channelsInSessionGroup; j++)
                {
                    _hintTrackIDs[i][j] = input.ReadBytes(4);
                }
            }
        }

        /**
         * A group ID indicates a file group that the session group complies with.
         *
         * @return all group IDs for all session groups
         */
        public long[][] GetGroupIDs()
        {
            return _groupIDs;
        }

        /**
         * A hint track ID specifies the track ID of the FD hint track belonging to
         * a particular session group. Note that one FD hint track corresponds to
         * one LCT channel.
         *
         * @return all hint track IDs for all session groups
         */
        public long[][] GetHintTrackIDs()
        {
            return _hintTrackIDs;
        }
    }
}