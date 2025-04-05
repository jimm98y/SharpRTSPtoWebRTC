import { useEffect, useState } from 'react';
import { CameraViewer } from './CameraViewer'
import './App.css';

function App() {
    const [cameras, setCameras] = useState();

    useEffect(() => {
        populateCameras();
    }, []);

    const contents = cameras === undefined
        ? <p><em>Loading... Please refresh once the ASP.NET backend has started.</em></p>
        : <div>
            {cameras.cameras.map(camera =>
                <CameraViewer key={camera} name={camera} />
            )}
        </div>;

    return (
        <div>
            <h1 id="tableLabel">Cameras</h1>
            <p>This component demonstrates RTSP to WebRTC gateway.</p>
            {contents}
        </div>
    );
    
    async function populateCameras() {
        const response = await fetch('api/webrtc/getcameras');
        const data = await response.json();
        setCameras({ cameras: data, loading: false });
    }
}

export default App;