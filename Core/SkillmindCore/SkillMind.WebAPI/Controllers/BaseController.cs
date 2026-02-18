using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace SkillMind.WebAPI.Controllers;
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class BaseController : ControllerBase {}