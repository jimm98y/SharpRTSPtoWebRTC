namespace SharpJaad.MP4.Boxes.Impl.OMA
{
    /**
 * The OMA DRM Transaction Tracking Box enables transaction tracking as defined 
 * in 'OMA DRM v2.1' section 15.3. The box includes a single transaction-ID and 
 * may appear in both DCF and PDCF.
 * 
 * @author in-somnia
 */
    public class OMATransactionTrackingBox : FullBox
    {
        private string _transactionID;

        public OMATransactionTrackingBox() : base("OMA DRM Transaction Tracking Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);
            _transactionID = input.readString(16);
        }

        /**
         * Returns the transaction-ID of the DCF or PDCF respectively.
         * 
         * @return the transaction-ID
         */
        public string GetTransactionID()
        {
            return _transactionID;
        }
    }
}
