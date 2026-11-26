using Microsoft.AspNetCore.Mvc;
using ITSupportSystem.Core.Interfaces;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.API.Data;
using ITSupportSystem.API.Repositories;

namespace ITSupportSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController(ICommentRepository repository) : ControllerBase
{

    private readonly ICommentRepository _repository = repository;
}