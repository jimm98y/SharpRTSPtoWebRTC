import React, { useEffect, useRef } from 'react';

function CameraViewer({ name }) {
    const videoElement = useRef(null);
    const hasRun = useRef(false);
    useEffect(() => {
        if (hasRun.current) return;
        hasRun.current = true;

        const id = window.crypto.getRandomValues(new Uint32Array(1))[0];
        const rtcConnection = new RTCPeerConnection();
        // Set once the component goes away, so a fetch that is still in flight does not carry on
        //  talking to a connection that has been closed.
        let closed = false;

        rtcConnection.ontrack = ({ track, streams: [stream] }) => {
            track.onunmute = () => {
                if (videoElement.current) videoElement.current.srcObject = stream;
            };
        };
        rtcConnection.onicecandidate = async (event) => {
            if (!event.candidate || closed) return;
            try {
                await fetch(`api/webrtc/addicecandidate?id=${id}`, {
                    method: 'POST',
                    body: JSON.stringify(event.candidate),
                    headers: { 'Content-Type': 'application/json' }
                });
            } catch (err) {
                console.error(`Camera ${name}: failed to send an ICE candidate`, err);
            }
        };

        (async () => {
            try {
                const offerResult = await fetch(`api/webrtc/getoffer?id=${id}&name=${encodeURIComponent(name)}`);
                if (!offerResult.ok) {
                    throw new Error(`getoffer returned ${offerResult.status} ${offerResult.statusText}`);
                }

                const offer = await offerResult.json();
                if (closed) return;

                await rtcConnection.setRemoteDescription(offer);
                const answer = await rtcConnection.createAnswer();
                await rtcConnection.setLocalDescription(answer);
                if (closed) return;

                const answerResult = await fetch(`api/webrtc/setanswer?id=${id}`, {
                    method: 'POST',
                    body: JSON.stringify(rtcConnection.localDescription),
                    headers: { 'Content-Type': 'application/json' }
                });
                if (!answerResult.ok) {
                    throw new Error(`setanswer returned ${answerResult.status} ${answerResult.statusText}`);
                }
            } catch (err) {
                // Without this the whole handshake failed silently and the element just stayed black.
                console.error(`Camera ${name}: could not start the stream`, err);
            }
        })();

        return () => {
            // Closing it tells the server to drop the peer connection and, once the last viewer has
            //  gone, to stop pulling from the camera. Leaving it open leaked both.
            closed = true;
            rtcConnection.close();
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    return <video ref={videoElement} autoPlay playsInline muted controls style={{ width: '100%' }}></video>;
}

export default CameraViewer;
