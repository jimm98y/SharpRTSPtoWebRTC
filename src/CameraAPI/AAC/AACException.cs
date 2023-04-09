using System.IO;

namespace CameraAPI.AAC
{
    /// <summary>
    /// Standard exception, thrown when decoding of an AAC frame fails.
    /// The message gives more detailed information about the error.
    /// @author in-somnia
    /// </summary>
    public class AACException : IOException
    {
        private readonly bool _eos;

        public bool IsEndOfStream {  get { return _eos;  } }

	    public AACException(string message) : this(message, false)
        { }

        public AACException(string message, bool eos) : base(message)
        {
            this._eos = eos;
        }
    }
}
