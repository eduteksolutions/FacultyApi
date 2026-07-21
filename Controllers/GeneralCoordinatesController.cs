
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using FacultyApi.Services;
using FacultyApi.Internal;


namespace FacultyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeneralCoordinatesController : ControllerBase
    {
        private readonly IGeneralCoordinatesService _service;

        public GeneralCoordinatesController(IGeneralCoordinatesService service)
        {
            _service = service;
        }

        // GET: api/GeneralCoordinates/GetByUserID?userid=1
        [HttpGet("GetByUserID")]
        public IActionResult GetByUserID(int userid)
        {
            var result = _service.GetByUserID(userid);

            return Ok(result);
        }
    }
}