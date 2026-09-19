import { useEffect, useRef, useState } from 'react';
import CameraViewer from './CameraViewer'
import './App.css';

function App() {
    const [cameras, setCameras] = useState({ c: null, loading: true, error: null });
    const hasRun = useRef(false);

    useEffect(() => {
        if (hasRun.current) return;
        hasRun.current = true;

        fetch('api/webrtc/getcameras')
            .then(response => {
                if (!response.ok) {
                    throw new Error(`getcameras returned ${response.status} ${response.statusText}`);
                }
                return response.json();
            })
            .then(data => setCameras({ c: data, loading: false, error: null }))
            // Without this a failed load sat on "Loading..." for ever with nothing in the console.
            .catch(err => {
                console.error('Could not load the camera list', err);
                setCameras({ c: [], loading: false, error: err.message });
            });
    }, []);

    const contents =
        cameras.loading
            ? <p><em>Loading... Please refresh once the ASP.NET backend has started.</em></p>
            : cameras.error
                ? <p><em>Could not load the camera list: {cameras.error}</em></p>
                : <div>{cameras.c.map(camera => <CameraViewer key={camera} name={camera} />)}</div>;

    return (
        <div>
            <h1 id="tableLabel">RTSP to WebRTC</h1>
            {contents}
        </div>
    );
}

export default App;