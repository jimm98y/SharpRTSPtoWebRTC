using System.Collections.Generic;

namespace SharpJaad.MP4.Boxes.Impl.FD
{
    public class GroupIDToNameBox : FullBox
    {
        private readonly Dictionary<long, string> _map;

        public GroupIDToNameBox() : base("Group ID To Name Box")
        {
            _map = new Dictionary<long, string>();
        }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            int entryCount = (int)input.ReadBytes(2);
            long id;
            string name;
            for (int i = 0; i < entryCount; i++)
            {
                id = input.ReadBytes(4);
                name = input.ReadUTFString((int)GetLeft(input), MP4InputStream.UTF8);
                _map.Add(id, name);
            }
        }

        /**
         * Returns the map that contains the ID-name-pairs for all groups.
         *
         * @return the ID to name map
         */
        public Dictionary<long, string> GetMap()
        {
            return _map;
        }
    }
}
