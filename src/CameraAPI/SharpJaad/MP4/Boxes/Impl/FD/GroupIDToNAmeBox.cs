using System;
using System.Collections.Generic;

namespace SharpJaad.MP4.Boxes.Impl.FD
{
    public class GroupIDToNameBox : FullBox
    {
        private readonly Dictionary<long, String> map;

        public GroupIDToNameBox() : base("Group ID To Name Box")
        {
            map = new Dictionary<long, string>();
        }

        public override void Decode(MP4InputStream input)
        {
            base.decode(input);

            int entryCount = (int)input.readBytes(2);
            long id;
            string name;
            for (int i = 0; i < entryCount; i++)
            {
                id = input.readBytes(4);
                name = input.readUTFString((int)GetLeft(input), MP4InputStream.UTF8);
                map.Add(id, name);
            }
        }

        /**
         * Returns the map that contains the ID-name-pairs for all groups.
         *
         * @return the ID to name map
         */
        public Dictionary<long, string> getMap()
        {
            return map;
        }
    }
}
