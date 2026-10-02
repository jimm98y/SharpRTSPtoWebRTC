using System.Runtime.CompilerServices;

// The audio pipeline pieces - the sample buffer, the clock scaler, the reconnect policy - are
//  internal because nothing outside this assembly should build one, but they carry the logic most
//  worth testing directly rather than through a live stream.
[assembly: InternalsVisibleTo("SharpRTSPtoWebRTC.Tests")]
