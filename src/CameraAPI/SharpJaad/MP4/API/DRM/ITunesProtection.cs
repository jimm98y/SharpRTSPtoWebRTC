using SharpJaad.MP4.Boxes;
using SharpJaad.MP4.Boxes.Impl.DRM;
using System;
using System.Linq;
using System.Text;

namespace SharpJaad.MP4.API.DRM
{
    public class ITunesProtection : Protection
    {
        private readonly string userID, userName, userKey;
	    private readonly byte[] privateKey, initializationVector;

        public ITunesProtection(Box sinf): base(sinf)
        {
            Box schi = sinf.GetChild(BoxTypes.SCHEME_INFORMATION_BOX);
            userID = ((FairPlayDataBox)schi.GetChild(BoxTypes.FAIRPLAY_USER_ID_BOX)).getData();

            //user name box is filled with 0
            byte[] b = ((FairPlayDataBox)schi.GetChild(BoxTypes.FAIRPLAY_USER_NAME_BOX)).getData();
            int i = 0;
            while (b[i] != 0)
            {
                i++;
            }
            userName = Encoding.ASCII.GetString(b.Take(i - 1).ToArray());

            userKey = new String(((FairPlayDataBox)schi.GetChild(BoxTypes.FAIRPLAY_USER_KEY_BOX)).getData());
            privateKey = ((FairPlayDataBox)schi.GetChild(BoxTypes.FAIRPLAY_PRIVATE_KEY_BOX)).getData();
            initializationVector = ((FairPlayDataBox)schi.GetChild(BoxTypes.FAIRPLAY_IV_BOX)).getData();
        }

        public override Scheme GetScheme()
        {
            return Scheme.ITUNES_FAIR_PLAY;
        }

        public string GetUserID()
        {
            return userID;
        }

        public string GetUserName()
        {
            return userName;
        }

        public string GetUserKey()
        {
            return userKey;
        }

        public byte[] GetPrivateKey()
        {
            return privateKey;
        }

        public byte[] GetInitializationVector()
        {
            return initializationVector;
        }
    }
}
