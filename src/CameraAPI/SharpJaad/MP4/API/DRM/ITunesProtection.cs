using SharpJaad.MP4.Boxes;
using SharpJaad.MP4.Boxes.Impl.DRM;
using System;
using System.Linq;
using System.Text;

namespace SharpJaad.MP4.API.DRM
{
    public class ITunesProtection : Protection
    {
        private readonly string _userID, _userName, _userKey;
	    private readonly byte[] _privateKey, _initializationVector;

        public ITunesProtection(Box sinf): base(sinf)
        {
            Box schi = sinf.GetChild(BoxTypes.SCHEME_INFORMATION_BOX);
            _userID = Encoding.UTF8.GetString(((FairPlayDataBox)schi.GetChild(BoxTypes.FAIRPLAY_USER_ID_BOX)).GetData());

            //user name box is filled with 0
            byte[] b = ((FairPlayDataBox)schi.GetChild(BoxTypes.FAIRPLAY_USER_NAME_BOX)).GetData();
            int i = 0;
            while (b[i] != 0)
            {
                i++;
            }
            _userName = Encoding.UTF8.GetString(b.Take(i - 1).ToArray());

            _userKey = Encoding.UTF8.GetString(((FairPlayDataBox)schi.GetChild(BoxTypes.FAIRPLAY_USER_KEY_BOX)).GetData());
            _privateKey = ((FairPlayDataBox)schi.GetChild(BoxTypes.FAIRPLAY_PRIVATE_KEY_BOX)).GetData();
            _initializationVector = ((FairPlayDataBox)schi.GetChild(BoxTypes.FAIRPLAY_IV_BOX)).GetData();
        }

        public override Scheme GetScheme()
        {
            return Scheme.ITUNES_FAIR_PLAY;
        }

        public string GetUserID()
        {
            return _userID;
        }

        public string GetUserName()
        {
            return _userName;
        }

        public string GetUserKey()
        {
            return _userKey;
        }

        public byte[] GetPrivateKey()
        {
            return _privateKey;
        }

        public byte[] GetInitializationVector()
        {
            return _initializationVector;
        }
    }
}
