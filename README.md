# SharpRTSP to WebRTC
This is a bridge in between RTSP and WebRTC implemented in C#. It can take any H264/H265/AV1/VP9 RTSP stream and feed it through WebRTC to the web browser. It does not perform
any video transcoding which makes it lightweight and portable. It does support audio transcoding from AAC to Opus, all implemented in netstandard and NET10 without any native dependencies.

[![NuGet version](https://img.shields.io/nuget/v/SharpRTSPtoWebRTC.svg?style=flat-square)](https://www.nuget.org/packages/SharpRTSPtoWebRTC)

## What can it do?
- Re-stream H264/H265/AV1/VP9 RTSP from any source to the web browser
- Stream aggregation - there is only a single session in between the gateway and the RTSP source, no matter how many users are watching the stream
- Transcode AAC audio to Opus with a small latency in audio

![Preview](demo.gif)

## Compatibility
Because no video transcoding is being performed, the web browsers must support decoding of the source video codecs in WebRTC.

### H264
This should be supported by the majority of web browsers as it is among the codecs required by WebRTC. There might be an exception for Firefox on Android according to this: https://developer.mozilla.org/en-US/docs/Web/Media/Formats/WebRTC_codecs.

### H265
Although most of the web browsers today support H265 video decoding, it does not mean H265 will also work in WebRTC. As of April 2025, H265 in WebRTC is supported in the latest releases of Safari. It is also supported in Chrome Canary 136+.

### AV1
Most modern web browsers support AV1 in WebRTC.

### VP9
VP9 is supported in WebRTC by all major web browsers. The stream is offered with the profile the RTSP source signals (`profile-id` in its SDP, profile 0 when it gives none). Profile 0 plays everywhere; 10-bit profile 2 only where the browser can decode it, which is mostly Chrome.

## Samples
### RTSPtoWebRTCGateway
There is a sample ASP.NET Core app that demonstrates the functionality on multiple live streams. To change the default configuration, just modify the `appsettings.json`:
```json
"Cameras": [
    {
      "Name": "name1",
      "Url": "rtsp://url1",
      "UserName": "MyUserName",
      "Password": "MyPassword"
    },
    {
      "Name": "name2",
      "Url": "rtsp://url2",
      "UserName": null,
      "Password": null
    }
  ]
```

### Minimal example
Start with the standard "React and ASP.NET Core" project template. In `Program.cs`, add the following piece of code to register the `RTSPtoWebRTCProxyService`:
```cs
builder.Services.AddSingleton<RTSPtoWebRTCProxyService>();
```

Then (optionally) add the configuration of streams from `appsettings.json`:
```cs
builder.Services.Configure<List<CameraConfiguration>>(builder.Configuration.GetSection("Cameras"));
```

Implement a minimal WebRTC signalling controller, for instance:
```cs
[ApiController]
[Route("api/[controller]")]
public class WebRTCController : ControllerBase
{
    private readonly IList<CameraConfiguration> _cameras;
    private readonly RTSPtoWebRTCProxyService _webRTCServer;

    public WebRTCController(IOptions<List<CameraConfiguration>> cameras, RTSPtoWebRTCProxyService webRTCServer)
    {
        _cameras = cameras.Value;
        _webRTCServer = webRTCServer;
    }

    [HttpGet]
    [Route("getcameras")]
    public IActionResult GetCameras()
    {
        return Ok(_cameras.Select(x => x.Name).ToList());
    }

    [HttpGet]
    [Route("getoffer")]
    public async Task<IActionResult> GetOffer(string id, string name)
    {
        return Ok(await _webRTCServer.GetOfferAsync(id, camera.Url, camera.UserName, camera.Password, camera.StartPort, camera.EndPort, user: User?.Identity?.Name));
    }

    [HttpPost]
    [Route("setanswer")]
    public IActionResult SetAnswer(string id, [FromBody] SessionDescriptionInit answer)
    {
        _webRTCServer.SetAnswer(id, Request.Headers[SessionToken.Header], User?.Identity?.Name, answer);
        return Ok();
    }

    [HttpPost]
    [Route("addicecandidate")]
    public IActionResult AddIceCandidate(string id, [FromBody] IceCandidateInit iceCandidate)
    {
        _webRTCServer.AddIceCandidate(id, Request.Headers[SessionToken.Header], User?.Identity?.Name, iceCandidate);
        return Ok();
    }
}
```

Finally, for the WebRTC viewer you can refer to `src/rtsptowebrtc.client/src/CameraViewer.jsx`.

Each offer carries a `token`, a secret the server makes for the session: the answer and the ICE candidates of the session are taken only with it in the `X-Session-Token` header, and - of a server of authentication - only of the user who asked for the offer. Anything else is answered 404, as an unknown session is, so neither a guessed id nor another user's session can be taken over.

### Authentication
The page and the API take HTTP Basic authentication of the users of `BasicAuthentication` in `appsettings.json`. Each user's password is best kept as a hash, which the server makes:
```
dotnet RTSPtoWebRTC.Server.dll hash-password "the password"
```
```json
"BasicAuthentication": {
  "Realm": "RTSPtoWebRTC",
  "Users": {
    "viewer": "pbkdf2-sha256$210000$...$..."
  }
}
```
The browser asks for the user and password once, as the page loads. A .NET client gives them in the URL of the API, `https://viewer:password@host/api/webrtc`, which `WebRtcSource` of SharpMediaPlayer sends as the Authorization header. Of no users, the API is open, and the server says so as it starts. Basic authentication sends the password readable by anyone on the path over plain HTTP: use HTTPS beyond the loopback.

### Hosting on a public IP address
It is recommended to use a TURN server such as https://github.com/coturn/coturn when hosting this project on the Internet. 
To host this project on a public IP address without using a TURN server, you will have to configure port ranges (`StartPort`, `EndPort`) for each camera in `appsettings.json`. Then you have to configure port forwarding of all the ports in the configured port ranges to this server + port forwarding for the web application. Each viewer takes one UDP port of the range for each local address of the server, so make the range large enough for the viewers you expect. 
When the server is behind a NAT that maps each of its ports to the same port of a public address - a cloud VM, for instance - configure that address with `PublicIPv4` and `PublicIPv6` in `appsettings.json`: it is offered as an ICE candidate of each of the server's ports.
The signalling API has no authentication of its own, so put it behind your own (a reverse proxy, for instance) when it is reachable from the Internet. `MaxViewers` in `appsettings.json` (100 by default) caps the viewers at once: each holds ports, a DTLS key and the camera's bitrate of upload, and more are answered 503.

## Credits
- SharpWebRTC - WebRTC peer connection, of SharpICE (ICE, STUN, TURN) and SharpSRTP (DTLS-SRTP) https://github.com/jimm98y/SharpWebRTC
- sipsorcery - WebRTC implementation in netstandard which this project was first built on https://github.com/sipsorcery-org/sipsorcery
- SharpRTSP - RTSP client https://github.com/ngraziano/SharpRTSP
- concentus - Opus codec implementation https://github.com/lostromb/concentus
