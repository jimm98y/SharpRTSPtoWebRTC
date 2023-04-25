using System.Collections.Generic;
using System.Linq;

namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The Track Reference Box provides a reference from the containing track to
     * another track in the presentation. These references are typed. A 'hint'
     * reference links from the containing hint track to the media data that it
     * hints. A content description reference 'cdsc' links a descriptive or
     * metadata track to the content which it describes.
     *
     * Exactly one Track Reference Box can be contained within the Track Box.
     *
     * If this box is not present, the track is not referencing any other track in
     * any way. The reference array is sized to fill the reference type box.
     * @author in-somnia
     */
    public class TrackReferenceBox : BoxImpl
    {
        private string _referenceType;
        private List<long> _trackIDs;

        public TrackReferenceBox() : base("Track Reference Box")
        {
            _trackIDs = new List<long>();
        }

        public override void Decode(MP4InputStream input)
        {
            _referenceType = input.ReadString(4);

            while (GetLeft(input) > 3)
            {
                _trackIDs.Add(input.ReadBytes(4));
            }
        }

        /**
         * The reference type shall be set to one of the following values: 
         * <ul>
         * <li>'hint': the referenced track(s) contain the original media for this 
         * hint track.</li>
         * <li>'cdsc': this track describes the referenced track.</li>
         * <li>'hind': this track depends on the referenced hint track, i.e., it 
         * should only be used if the referenced hint track is used.</li>
         * @return the reference type
         */
        public string GetReferenceType()
        {
            return _referenceType;
        }

        /**
         * The track IDs are integers that provide a reference from the containing
         * track to other tracks in the presentation. Track IDs are never re-used
         * and cannot be equal to zero.
         * @return the track IDs this box refers to
         */
        public List<long> GetTrackIDs()
        {
            return _trackIDs.ToList();
        }
    }
}
