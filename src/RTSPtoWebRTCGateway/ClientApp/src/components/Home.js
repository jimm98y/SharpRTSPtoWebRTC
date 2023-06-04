import React, { Component } from 'react';
import { CameraViewer } from './CameraViewer';

export class Home extends Component {
    static displayName = Home.name;

    constructor(props) {
        super(props);
        this.state = { cameras: [], loading: true };
    }

    componentDidMount() {
        this.populateCameras();
    }

    async populateCameras() {
        const response = await fetch('api/webrtc/getcameras');
        const data = await response.json();
        this.setState({ cameras: data, loading: false });
    }

    static renderCameras(cameras) {
        return (
            <div>
                {cameras.map(camera =>
                    <CameraViewer key={camera} name={camera} />
                )}
            </div>
        );
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : Home.renderCameras(this.state.cameras);

        return (
            <div>
                <h2 id="tabelLabel" >RTSP streams</h2>
                {contents}
            </div>
        );
    }
}
