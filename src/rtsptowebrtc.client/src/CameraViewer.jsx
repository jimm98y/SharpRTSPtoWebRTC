import React, { useEffect, useState, useRef } from 'react';

function CameraViewer({ name }) {
    const [peerConnection, setPeerConnection] = useState(null);
    const videoElement = useRef(null);

    useEffect(() => {
        startPlaying();
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);       

    return (
        <video ref={videoElement} autoPlay playsInline muted controls style={{ width: '100%' }}></video>
    );

    async function startPlaying() {
        console.log("startPlaying");
        closePeer();

        let id = new Uint32Array(1);
        id = window.crypto.getRandomValues(id);

        let baseUrl = "api/webrtc/";
        let getOfferUrl = `${baseUrl}getoffer?id=${id}&name=${encodeURIComponent(name)}`;
        let setAnswerUrl = `${baseUrl}setanswer?id=${id}`;
        let setIceCandidateUrl = `${baseUrl}addicecandidate?id=${id}`

        let pc = new RTCPeerConnection();
        setPeerConnection({
            peerConnection: pc
        });

        pc.ontrack = ({ track, streams: [stream] }) => {
            track.onunmute = () => {
                console.log("Adding track to video control.");
                videoElement.current.srcObject = stream;
            };
        };

        pc.onicecandidate = async function (event) {
            if (event.candidate) {
                console.log('new-ice-candidate:');
                console.log(event.candidate.candidate);
                console.log(event.candidate);
                console.log("JSON: " + JSON.stringify(event.candidate.toJSON()))

                await fetch(setIceCandidateUrl, {
                    method: 'POST',
                    body: JSON.stringify(event.candidate),
                    headers: { 'Content-Type': 'application/json' }
                });
            }
        };

        pc.onicegatheringstatechange = function () {
            console.log("onicegatheringstatechange: " + pc.iceGatheringState);
        }

        pc.oniceconnectionstatechange = function () {
            console.log("oniceconnectionstatechange: " + pc.iceConnectionState);
        }

        let offerResponse = await fetch(getOfferUrl);
        let offer = await offerResponse.json();
        console.log("got offer: " + offer.type + " " + offer.sdp + ".");
        await pc.setRemoteDescription(offer);

        pc.createAnswer().then(function (answer) {
            return pc.setLocalDescription(answer);
        }).then(async function () {
            console.log("Sending answer SDP.");
            console.log("SDP: " + pc.localDescription.sdp);
            await fetch(setAnswerUrl, {
                method: 'POST',
                body: JSON.stringify(pc.localDescription),
                headers: { 'Content-Type': 'application/json' }
            });
        });
    };

    function closePeer() {
        let pc = peerConnection;
        if (pc != null && pc.peerConnection != null) {
            console.log("Closing peer");
            pc.peerConnection.close();
        }
    };
}

export default CameraViewer;
