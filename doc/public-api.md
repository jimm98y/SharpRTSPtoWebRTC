# Public API

What SharpRTSPtoWebRTC exposes is written down, and a removal from it does not build until it is
written down too, an addition warns until it is. Two checks do this, both on every `dotnet build` of
the library:

- **The API files.** `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` next to the project list
  its public types and members. They are the same on all four target frameworks. Shipped is the API
  as released; Unshipped is what changed since. A public symbol missing from them is a warning,
  RS0016, until it is listed - an addition breaks no one - and a listed one that is gone fails the
  build with RS0017.
- **Package validation.** The package being built is compared with the last release
  (`PackageValidationBaselineVersion` in the project, 0.1.6 now), and every binary or source breaking
  change fails the build with a CP error: a member added to an interface or an abstract class among
  them, which breaks whoever implements it, and a value of an enum changed. It also checks that the
  target frameworks agree: what netstandard2.0 offers, net481, net8.0 and net10.0 have to offer too.

## Adding to the API

List what the RS0016 warnings name: accept their fix in the IDE ("Add to public API"), or from the
command line:

```
dotnet format analyzers src/SharpRTSPtoWebRTC/SharpRTSPtoWebRTC.csproj --diagnostics RS0016 --severity warn
```

The new lines land in `PublicAPI.Unshipped.txt`. A value added to an enum goes last, of a number
of its own: inserted before another, it changes that one's value. `ProxyVideoCodecs` and
`ProxyAudioCodecs` give theirs explicitly, `Unknown` of 0.

## Removing or changing the API

RS0017 names what disappeared. Moving its line from Shipped to Unshipped with a `*REMOVED*` prefix
records the removal; a member whose signature changed is a removal of the old line and an addition
of the new one.

If package validation then reports a CP error, the change breaks code built against another
framework or the last release. When that is intended, record it:

```
dotnet build src/SharpRTSPtoWebRTC/SharpRTSPtoWebRTC.csproj -c Release -p:ApiCompatGenerateSuppressionFile=true
```

That writes `CompatibilitySuppressions.xml` next to the project. Say in the pull request what broke.

Validation runs as part of the build, because the build packs. `dotnet pack` on its own does not
rebuild and validates whatever binaries are already in `bin`.

## Releasing

1. Move every line of `PublicAPI.Unshipped.txt` into `PublicAPI.Shipped.txt`, dropping the
   `*REMOVED*` lines together with the shipped lines they remove.
2. Once the release is on nuget.org, set `PackageValidationBaselineVersion` in the project to it,
   and delete `CompatibilitySuppressions.xml`: what it excused is in the new baseline.

## What is recorded now

Against 0.1.6, `CompatibilitySuppressions.xml` records the move from SIPSorcery to SharpWebRTC: the
members of SIPSorcery's types are gone - `RTSPtoWebRTCProxyService.GetOfferAsync` of
`RTCSessionDescriptionInit`, `SetAnswer`, `AddIceCandidate`, `RTSPtoWebRTCProxy.AddPeerConnection` of
`RTCPeerConnection`, `AudioFormat` and `VideoFormat` - and `RTSPtoWebRTCProxy.RemovePeerConnection`
changed. It also records the values of `ProxyVideoCodecs` and `ProxyAudioCodecs`, renumbered so that
`Unknown` is 0, the default of a codec not yet known: every other value of 0.1.6 is one more now.
