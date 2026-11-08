import React, { useEffect, useRef, useState } from 'react';

function CameraViewer({ name }) {
    const videoElement = useRef(null);
    const rtcConnectionRef = useRef(null);
    // Counted up to start the stream again: the effect closes the connection and makes a new one.
    const [attempt, setAttempt] = useState(0);

    useEffect(() => {
        const id = window.crypto.getRandomValues(new Uint32Array(1))[0];
        const rtcConnection = new RTCPeerConnection();
        rtcConnectionRef.current = rtcConnection;
        // Set once the component goes away, so a fetch that is still in flight does not carry on
        // talking to a connection that has been closed.
        let closed = false;
        // The session's secret, of its offer: the server takes its answer and candidates of it alone.
        let token = null;
        let retryTimer = null;
        const retry = (delay) => {
            clearTimeout(retryTimer);
            retryTimer = setTimeout(() => setAttempt(a => a + 1), delay);
        };

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
                    headers: { 'Content-Type': 'application/json', 'X-Session-Token': token }
                });
            } catch (err) {
                console.error(`Camera ${name}: failed to send an ICE candidate`, err);
            }
        };

        // Without this a dropped connection - the gateway restarted, the machine slept - left the last picture
        //  standing until the page was reloaded. Disconnected often comes back of itself, so it is given a moment.
        rtcConnection.onconnectionstatechange = () => {
            if (closed) return;
            const state = rtcConnection.connectionState;
            if (state === 'failed') retry(1000);
            else if (state === 'disconnected') retry(5000);
            else clearTimeout(retryTimer);
        };

        (async () => {
            try {
                const offerResult = await fetch(`api/webrtc/getoffer?id=${id}&name=${encodeURIComponent(name)}`);
                if (!offerResult.ok) {
                    throw new Error(`getoffer returned ${offerResult.status} ${offerResult.statusText}`);
                }

                const offer = await offerResult.json();
                if (closed) return;
                token = offer.token;

                await rtcConnection.setRemoteDescription({ type: offer.type, sdp: offer.sdp });
                const answer = await rtcConnection.createAnswer();
                await rtcConnection.setLocalDescription(answer);
                if (closed) return;

                const answerResult = await fetch(`api/webrtc/setanswer?id=${id}`, {
                    method: 'POST',
                    body: JSON.stringify(rtcConnection.localDescription),
                    headers: { 'Content-Type': 'application/json', 'X-Session-Token': token }
                });
                if (!answerResult.ok) {
                    throw new Error(`setanswer returned ${answerResult.status} ${answerResult.statusText}`);
                }
            } catch (err) {
                // Without this the whole handshake failed silently and the element just stayed black.
                console.error(`Camera ${name}: could not start the stream`, err);
                if (!closed) retry(5000);
            }
        })();

        return () => {
            // Closing it tells the server to drop the peer connection and, once the last viewer has
            //  gone, to stop pulling from the camera. Leaving it open leaked both.
            closed = true;
            clearTimeout(retryTimer);
            rtcConnectionRef.current?.close();
            rtcConnectionRef.current = null;
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [attempt]);
    return <video ref={videoElement} autoPlay playsInline muted controls style={{ width: '100%' }}></video>;
}

export default CameraViewer;
