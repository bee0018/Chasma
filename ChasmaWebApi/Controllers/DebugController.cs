using Microsoft.AspNetCore.Mvc;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class representing the debug controller for testing api route operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {

        [HttpGet("{id}")]
        public ActionResult<string> GetProductsById(int id)
        {
            return $"Product {id}";
        }

        [HttpPost]
        public ActionResult<string> AddProduct([FromBody] string value)
        {
            return $"Posted: {value}";
        }
    }
}
