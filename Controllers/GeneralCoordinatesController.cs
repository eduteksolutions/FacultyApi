using FacultyApi.@interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FacultyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeneralCoordinatesController : ControllerBase
    {
        private readonly IGeneralCoordinatesService _service;

        public GeneralCoordinatesController(
            IGeneralCoordinatesService service)
        {
            _service = service;
        }

        [HttpGet("GetByUserID")]
        public IActionResult GetByUserID(int userid)
        {
            return Ok(_service.GetByUserID(userid));
        }

        // API methods here
    }
}
