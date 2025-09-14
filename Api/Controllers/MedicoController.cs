using Microsoft.AspNetCore.Mvc;
using SqlServerHelper;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicoController : ControllerBase
    {
        // Ejemplo de uso de SqlServerHelper
        [HttpGet]
        public IActionResult GetMedicos()
        {
            // Implementación de ejemplo
            return Ok();
        }
    }
}
