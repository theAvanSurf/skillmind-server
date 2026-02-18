using Microsoft.AspNetCore.Mvc;

namespace SkillMind.WebAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    
}