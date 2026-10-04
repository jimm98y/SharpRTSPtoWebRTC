// SharpRTSPtoWebRTC
// Copyright (C) 2026 Lukas Volf
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpRTSPtoWebRTC.WebRTCProxy;
using SIPSorcery.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTSPtoWebRTC.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebRTCController : ControllerBase
    {
        private readonly ILogger<WebRTCController> _logger;
        private readonly IList<CameraConfiguration> _cameras;
        private readonly RTSPtoWebRTCProxyService _webRTCServer;

        public WebRTCController(ILogger<WebRTCController> logger, IOptions<List<CameraConfiguration>> cameras, RTSPtoWebRTCProxyService webRTCServer)
        {
            _logger = logger;
            _cameras = cameras.Value;
            _webRTCServer = webRTCServer;
        }

        /// <summary>
        /// List all available cameras.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("getcameras")]
        public IActionResult GetCameras()
        {
            return Ok(_cameras.Select(x => x.Name).ToList());
        }

        [HttpGet]
        [Route("getoffer")]
        public async Task<IActionResult> GetOffer(string id, string name)
        {
            _logger.LogDebug($"WebRTCController GetOffer {id}.");

            var camera = _cameras.FirstOrDefault(x => x.Name == name);
            if (camera == null)
            {
                _logger.LogError($"Camera {name} does not exist.");
                return NotFound();
            }

            try
            {
                return Ok(await _webRTCServer.GetOfferAsync(
                    id,
                    camera.Url,
                    camera.UserName,
                    camera.Password,
                    camera.StartPort,
                    camera.EndPort,
                    camera.Transport,
                    camera.RtspStartPort,
                    camera.RtspEndPort));
            }
            catch (DuplicateSessionException ex)
            {
                _logger.LogWarning(ex.Message);
                return Conflict(ex.Message);
            }
        }

        [HttpPost]
        [Route("setanswer")]
        public IActionResult SetAnswer(string id, [FromBody] RTCSessionDescriptionInit answer)
        {
            _logger.LogDebug($"SetAnswer {id} {answer?.type} {answer?.sdp}.");

            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("The id cannot be empty in SetAnswer.");
            }
            else if (string.IsNullOrWhiteSpace(answer?.sdp))
            {
                return BadRequest("The SDP answer cannot be empty in SetAnswer.");
            }

            if (!_webRTCServer.SetAnswer(id, answer))
            {
                return NotFound($"No peer connection is available for id {id}.");
            }

            return Ok();
        }

        [HttpPost]
        [Route("addicecandidate")]
        public IActionResult AddIceCandidate(string id, [FromBody] RTCIceCandidateInit iceCandidate)
        {
            _logger.LogDebug($"SetIceCandidate {id} {iceCandidate?.candidate}.");

            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("The id cannot be empty in AddIceCandidate.");
            }
            else if (string.IsNullOrWhiteSpace(iceCandidate?.candidate))
            {
                return BadRequest("The candidate field cannot be empty in AddIceCandidate.");
            }

            if (!_webRTCServer.AddIceCandidate(id, iceCandidate))
            {
                return NotFound($"No peer connection is available for id {id}.");
            }

            return Ok();
        }
    }
}
