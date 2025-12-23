import { useEffect, useRef, useState } from 'react';
import CameraViewer from './CameraViewer'
import './App.css';

function App() {
    const [cameras, setCameras] = useState({ c: null, loading: true });
    const hasRun = useRef(false);

    useEffect(() => {
        if (hasRun.current) return;
        hasRun.current = true;

        fetch('api/webrtc/getcameras').then(response => {
            response.json().then(data => {
                setCameras({ c: data, loading: false });
            });
        });
    }, []);

    const contents =
        cameras.loading
            ? <p><em>Loading... Please refresh once the ASP.NET backend has started.</em></p>
            : <div>{cameras.c.map(camera => <CameraViewer key={camera} name={camera} />)}</div>;

    return (
        <div>
            <h1 id="tableLabel">RTSP to WebRTC</h1>
            {contents}
        </div>
    );
}

export default App;