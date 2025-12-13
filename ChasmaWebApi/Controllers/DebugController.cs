using ChasmaWebApi.Data.Requests;
using ChasmaWebApi.Data.Responses;
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
        [HttpGet]
        [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<string>> GetProducts()
        {
            TodoResponse response = new()
            {
                IsDone = true,
                Todo = "This is a todo",
            };

            return Ok(response);
        }

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

        [HttpPost]
        [Route("/addTodo")]
        public ActionResult AddTodo(TodoRequest todoRequest) {
            string details = $"Request received {todoRequest.Id} and scream is set to {todoRequest.Scream}";
            return Ok(details);
        }
    }
}
