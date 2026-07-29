
using Microsoft.AspNetCore.Mvc;

using FacultyApi.model;
using FacultyApi.Services;



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

        // GET: api/GeneralCoordinates/GetAll
        [HttpGet("GetAll")]
        public IActionResult GetAll()
        {
            return Ok(_service.GetAll());
        }

        // GET: api/GeneralCoordinates/GetByUserID?userid=1
        [HttpGet("GetByUserID")]
        public IActionResult GetByUserID(int userid)
        {
            return Ok(_service.GetByUserID(userid));
        }

        // POST: api/GeneralCoordinates/Insert
        [HttpPost("Insert")]
        public IActionResult Insert([FromBody] GeneralCoordinates model)
        {
            return Ok(_service.Insert(model));
        }

        // PUT: api/GeneralCoordinates/Update
        [HttpPut("Update")]
        public IActionResult Update([FromBody] GeneralCoordinates model)
        {
            return Ok(_service.Update(model));
        }

        // DELETE: api/GeneralCoordinates/Delete/1
        [HttpDelete("Delete/{code}")]
        public IActionResult Delete(int code)
        {
            return Ok(_service.Delete(code));
        }
    }
}