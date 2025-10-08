using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace UserAuthManage.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet("db-check")]
        [AllowAnonymous]  // 👈 important
        public async Task<IActionResult> DbCheck([FromServices] IConfiguration cfg)
        {
            var cs = cfg.GetConnectionString("ConnStr");
            try
            {
                using var cn = new SqlConnection(cs);
                await cn.OpenAsync();
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return Problem(title: "DB connect failed", detail: ex.Message);
            }
        }
    }
}
