import React, { useEffect, useState, useRef } from 'react';

function CameraViewer({ name }) {
    const [peerConnection, setPeerConnection] = useState(null);
    const videoElement = useRef(null);
    useEffect(() => {
        if (peerConnection == null) {
            (async () => {
                const id = window.crypto.getRandomValues(new Uint32Array(1));
                const rtcConnection = new RTCPeerConnection();
                rtcConnection.ontrack = ({ track, streams: [stream] }) => {
                    track.onunmute = () => { videoElement.current.srcObject = stream; };
                };
                rtcConnection.onicecandidate = async (event) => {
                    if (event.candidate) {
                        await fetch(`api/webrtc/addicecandidate?id=${id}`, {
                            method: 'POST',
                            body: JSON.stringify(event.candidate),
                            headers: { 'Content-Type': 'application/json' }
                        });
                    }
                };
                const offer = await fetch(`api/webrtc/getoffer?id=${id}&name=${encodeURIComponent(name)}`)
                    .then((offerResult) => offerResult.json());
                await rtcConnection.setRemoteDescription(offer);
                rtcConnection.createAnswer()
                    .then((answer) => rtcConnection.setLocalDescription(answer))
                    .then(async () => {
                        await fetch(`api/webrtc/setanswer?id=${id}`, {
                            method: 'POST',
                            body: JSON.stringify(rtcConnection.localDescription),
                            headers: { 'Content-Type': 'application/json' }
                        });
                    });
                setPeerConnection(rtcConnection);
            })();
        }
    });       
    return <video ref={videoElement} autoPlay playsInline muted controls style={{ width: '100%' }}></video>;
}

export default CameraViewer;