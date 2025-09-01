using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManagerSample.API.Controllers;
using TaskManagerSample.API.ViewModels;
using TaskManagerSample.Core.Intefaces;
using Xunit;

namespace TaskManagerSample.Tests.API.Controllers
{
    public class TasksControllerTests
    {
        private readonly Mock<ITaskRepository> _repoMock = new();
        private readonly Mock<ITaskService> _serviceMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<INotifier> _notifierMock = new();
        private readonly Mock<IUser> _userMock = new();

        private TasksController CreateSut()
            => new(_repoMock.Object, _serviceMock.Object, _mapperMock.Object, _notifierMock.Object, _userMock.Object);

        private static TaskViewModel MakeVm(Guid? id = null) => new TaskViewModel
        {
            Id = id ?? Guid.NewGuid(),
            Title = "Title 1",
            Description = "Desc"
        };

        private static TaskManagerSample.Core.Models.Task MakeEntity(Guid? id = null) =>
            new TaskManagerSample.Core.Models.Task
            {
                Id = id ?? Guid.NewGuid(),
                Title = "Title 1",
                Description = "Desc"
            };

        [Fact]
        public async Task GetList_ShouldReturnMappedList()
        {
            // Arrange
            var entities = new List<TaskManagerSample.Core.Models.Task>
            {
                MakeEntity(), MakeEntity()
            };

            var vms = entities.Select(e => new TaskViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description
            }).ToList();

            _repoMock.Setup(r => r.GetList()).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<TaskViewModel>>(entities)).Returns(vms);

            var sut = CreateSut();

            // Act
            var result = await sut.GetList();

            // Assert
            result.Should().BeEquivalentTo(vms);
            _repoMock.Verify(r => r.GetList(), Times.Once);
            _mapperMock.Verify(m => m.Map<IEnumerable<TaskViewModel>>(entities), Times.Once);
        }

        [Fact]
        public async Task GetById_WhenFound_ShouldReturnViewModel()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity = MakeEntity(id);
            var vm = MakeVm(id);

            _repoMock.Setup(r => r.GetById(id)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<TaskViewModel>(entity)).Returns(vm);

            var sut = CreateSut();

            // Act
            var action = await sut.GetById(id);

            // Assert
            action.Result.Should().BeNull(); // ActionResult<T> with value
            action.Value.Should().BeEquivalentTo(vm);
            _repoMock.Verify(r => r.GetById(id), Times.Once);
            _mapperMock.Verify(m => m.Map<TaskViewModel>(entity), Times.Once);
        }

        [Fact]
        public async Task GetById_WhenNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetById(id)).ReturnsAsync((TaskManagerSample.Core.Models.Task?)null);
            var sut = CreateSut();

            // Act
            var action = await sut.GetById(id);

            // Assert
            action.Result.Should().BeOfType<NotFoundResult>();
            _repoMock.Verify(r => r.GetById(id), Times.Once);
            _mapperMock.Verify(m => m.Map<TaskViewModel>(It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task Add_WithInvalidModelState_ShouldNotCallService()
        {
            var sut = CreateSut();
            sut.ModelState.AddModelError("Title", "Required");

            var vm = MakeVm();

            var action = await sut.Add(vm);

            // Como CustomResponse é do MainController, aqui validamos comportamento (não chamar service).
            _serviceMock.Verify(s => s.Add(It.IsAny<TaskManagerSample.Core.Models.Task>()), Times.Never);
            action.Should().NotBeNull();
        }

        [Fact]
        public async Task Add_WithValidModel_ShouldCallServiceAndReturnCustomResponse()
        {
            var sut = CreateSut();
            var vm = MakeVm();
            var entity = MakeEntity(vm.Id);

            _mapperMock.Setup(m => m.Map<TaskManagerSample.Core.Models.Task>(vm)).Returns(entity);
            _serviceMock.Setup(s => s.Add(entity)).Returns(Task.CompletedTask);

            var action = await sut.Add(vm);

            _mapperMock.Verify(m => m.Map<TaskManagerSample.Core.Models.Task>(vm), Times.Once);
            _serviceMock.Verify(s => s.Add(entity), Times.Once);
            action.Should().NotBeNull();
        }

        [Fact]
        public async Task Update_WhenIdMismatch_ShouldNotifyError_AndNotCallService()
        {
            var sut = CreateSut();
            var vm = MakeVm(Guid.NewGuid());
            var differentId = Guid.NewGuid();

            var action = await sut.Update(differentId, vm);

            _serviceMock.Verify(s => s.Update(It.IsAny<TaskManagerSample.Core.Models.Task>()), Times.Never);
            _notifierMock.Verify(n => n.Handle(It.IsAny<Core.Notifications.Notification>()), Times.AtLeastOnce);
            action.Should().NotBeNull();
        }

        [Fact]
        public async Task Update_WithInvalidModelState_ShouldNotCallService()
        {
            var sut = CreateSut();
            var vm = MakeVm();
            sut.ModelState.AddModelError("Title", "Required");

            var action = await sut.Update(vm.Id, vm);

            _serviceMock.Verify(s => s.Update(It.IsAny<TaskManagerSample.Core.Models.Task>()), Times.Never);
            action.Should().NotBeNull();
        }

        [Fact]
        public async Task Update_WithValidModel_ShouldCallService()
        {
            var sut = CreateSut();
            var vm = MakeVm();
            var entity = MakeEntity(vm.Id);

            _mapperMock.Setup(m => m.Map<TaskManagerSample.Core.Models.Task>(vm)).Returns(entity);
            _serviceMock.Setup(s => s.Update(entity)).Returns(Task.CompletedTask);

            var action = await sut.Update(vm.Id, vm);

            _mapperMock.Verify(m => m.Map<TaskManagerSample.Core.Models.Task>(vm), Times.Once);
            _serviceMock.Verify(s => s.Update(entity), Times.Once);
            action.Should().NotBeNull();
        }

        [Fact]
        public async Task Delete_WhenNotFound_ShouldReturnNotFound_AndNotCallService()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetById(id)).ReturnsAsync((TaskManagerSample.Core.Models.Task?)null);

            var sut = CreateSut();

            var action = await sut.Delete(id);

            action.Result.Should().BeOfType<NotFoundResult>();
            _serviceMock.Verify(s => s.Delete(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenFound_ShouldCallServiceAndReturnCustomResponse()
        {
            var id = Guid.NewGuid();
            var entity = MakeEntity(id);

            _repoMock.Setup(r => r.GetById(id)).ReturnsAsync(entity);
            _serviceMock.Setup(s => s.Delete(id)).Returns(Task.CompletedTask);

            var sut = CreateSut();

            var action = await sut.Delete(id);

            _serviceMock.Verify(s => s.Delete(id), Times.Once);
            action.Should().NotBeNull();
        }
    }
}