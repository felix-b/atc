using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Zero.Doubt.Logging.Viewer
{
    public class LogController : Controller
    {
        private readonly BinaryLogStreamReader _logReader;
        private readonly string _fileName;

        public LogController(BinaryLogStreamReader logReader)
        {
            _logReader = logReader;
            _fileName = (logReader.Binary.BaseStream as FileStream)?.Name ?? "N/A";
        }

        [Route("")]
        public IActionResult Index()
        {
            return Redirect("static/index.html");
        }

        [Route("file")]
        public IActionResult File()
        {
            return Json(new {
                FileName = _fileName,
                Nodes = _logReader.RootNode.Nodes.Select(GetNodeForJson)
            });
        }        

        [Route("node/{id}")]
        public IActionResult Node(int id)
        {
            var node = _logReader.TryGetNodeById(id);
            if (node == null)
            {
                return StatusCode(404);
            }
        
            return Json(new {
                Nodes = node.Nodes.Select(GetNodeForJson)
            });
        }

        private object GetNodeForJson(BinaryLogStreamReader.Node node)
        {
            return new {
                Id = node.NodeId,
                Time = node.Time,
                Duration = node.Duration.HasValue ? node.Duration.Value.TotalMilliseconds : -1,
                Message = node.MessageId,
                Depth = node.Depth,
                HasNodes = node.Nodes.Count > 0
            };
        }
    }
}
