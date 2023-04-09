import React, { Component } from 'react';

export class CameraViewer extends Component {
    static displayName = CameraViewer.name;

    videoElement = null;

    constructor(props) {
        super(props);
        this.state = { peerConnection: null };
        this.startPlaying = this.startPlaying.bind(this);
        this.closePeer = this.closePeer.bind(this);
        this.videoElement = React.createRef();
    }

    componentDidMount() {
        setTimeout(() => {
            this.startPlaying();
        }, 500);
    }

    async startPlaying() {
        this.closePeer();

        let id = new Uint32Array(1);
        id = window.crypto.getRandomValues(id);

        let baseUrl = "api/webrtc/";
        let getOfferUrl = `${baseUrl}getoffer?id=${id}&name=${encodeURIComponent(this.props.name)}`;
        let setAnswerUrl = `${baseUrl}setanswer?id=${id}`;
        let setIceCandidateUrl = `${baseUrl}addicecandidate?id=${id}`

        let pc = new RTCPeerConnection();
        this.setState({
            peerConnection: pc
        });

        pc.ontrack = ({ track, streams: [stream] }) => {
            track.onunmute = () => {
                console.log("Adding track to video control.");
                this.videoElement.current.srcObject = stream;
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

    closePeer() {
        let pc = this.state.peerConnection;
        if (pc != null) {
            console.log("Closing peer");
            pc.close();
        }
    };

    render() {
        return (
            <div>
                <video ref={this.videoElement} autoPlay playsInline muted controls style={{ width: '100%' }}></video>
                {/*    <button className="btn btn-primary" onClick={this.startPlaying}>Play</button>*/}
            </div>
        );
    }
}
