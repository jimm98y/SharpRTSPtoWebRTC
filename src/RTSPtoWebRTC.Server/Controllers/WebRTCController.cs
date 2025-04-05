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

            return Ok(await _webRTCServer.GetOfferAsync(id, camera.Url, camera.UserName, camera.Password));
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

            _webRTCServer.SetAnswer(id, answer);
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

            _webRTCServer.AddIceCandidate(id, iceCandidate);

            return Ok();
        }
    }
}
