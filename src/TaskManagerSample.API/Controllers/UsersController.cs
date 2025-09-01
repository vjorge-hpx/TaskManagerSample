using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagerSample.API.ViewModels;
using TaskManagerSample.Core.Intefaces;

namespace TaskManagerSample.API.Controllers
{
    [Authorize]
    [Route("api/users")]
    public class UsersController : MainController
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public UsersController(IUserRepository userRepository,
                               IUserService userService,
                               IMapper mapper,
                               INotifier notifier,
                               IUser user) : base(notifier, user)
        {
            _userRepository = userRepository;
            _userService = userService;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IEnumerable<UserViewModel>> GetList()
        {
            return _mapper.Map<IEnumerable<UserViewModel>>(await _userRepository.GetList());
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TaskViewModel>> GetById(Guid id)
        {
            return new TaskViewModel();
            /*
            var task = await _taskRepository.GetById(id);

            if (task == null) return NotFound();

            return _mapper.Map<TaskViewModel>(task);
            */
        }

        ////[ClaimsAuthorize("Fornecedor", "Adicionar")]
        //[AllowAnonymous]
        //[HttpPost]
        //public async Task<ActionResult<TaskViewModel>> Add(TaskViewModel taskViewModel)
        //{
        //    if (!ModelState.IsValid) return CustomResponse(ModelState);

        //    await _taskService.Add(_mapper.Map<Core.Models.Task>(taskViewModel));

        //    return CustomResponse(taskViewModel);
        //}

        ////[ClaimsAuthorize("Fornecedor", "Atualizar")]
        //[HttpPut("{id:guid}")]
        //public async Task<ActionResult<TaskViewModel>> Update(Guid id, TaskViewModel taskViewModel)
        //{
        //    if (id != taskViewModel.Id)
        //    {
        //        NotifyError("O id informado não é o mesmo que foi passado na query");
        //        return CustomResponse(taskViewModel);
        //    }

        //    if (!ModelState.IsValid) return CustomResponse(ModelState);

        //    await _taskService.Update(_mapper.Map<Core.Models.Task>(taskViewModel));

        //    return CustomResponse(taskViewModel);
        //}

        ////[ClaimsAuthorize("Fornecedor", "Excluir")]
        //[HttpDelete("{id:guid}")]
        //public async Task<ActionResult<TaskViewModel>> Delete(Guid id)
        //{
        //    var taskViewModel = await _taskRepository.GetById(id);

        //    if (taskViewModel == null) return NotFound();

        //    await _taskService.Delete(id);

        //    return CustomResponse(taskViewModel);
        //}
    }
}