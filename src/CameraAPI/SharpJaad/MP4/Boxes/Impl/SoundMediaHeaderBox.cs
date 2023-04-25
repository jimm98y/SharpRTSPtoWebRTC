namespace SharpJaad.MP4.Boxes.Impl
{
    /**
      * The sound media header contains general presentation information, independent
      * of the coding, for audio media. This header is used for all tracks containing
      * audio.
      *
      * @author in-somnia
      */
    public class SoundMediaHeaderBox : FullBox
    {
        private double _balance;

        public SoundMediaHeaderBox() : base("Sound Media Header Box")
        {  }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _balance = input.readFixedPoint(8, 8);
            input.skipBytes(2); //reserved
        }

        /**
         * The balance is a floating-point number that places mono audio tracks in a
         * stereo space: 0 is centre (the normal value), full left is -1.0 and full
         * right is 1.0.
         *
         * @return the stereo balance for a mono track
         */
        public double GetBalance()
        {
            return _balance;
        }
    }
}
