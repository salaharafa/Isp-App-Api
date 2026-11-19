using AutoMapper;
using IspAppApi.Application.Common;
using IspAppApi.Application.Features.Tickets.Commands;
using IspAppApi.Controllers;
using IspAppApi.Domain.Entities;
using IspAppApi.DTOs.Tickets;
using IspAppApi.Infrastructure.Data;
using IspAppApi.Infrastructure.Repositories;
using IspAppApi.Profiles;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using TaskStatus = IspAppApi.Domain.Entities.TaskStatus;

namespace IspAppApi.Tests
{
    public class TicketTests
    {
        private IspAppApiDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<IspAppApiDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new IspAppApiDbContext(options);
        }

        private IMapper CreateAutoMapper()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<TicketProfile>();
            });
            return configuration.CreateMapper();
        }

        // ==========================================
        // 1. CreateTicket Scenarios
        // ==========================================

        // 1. نجاح إنشاء التذكرة وتعيين الحالة الافتراضية New وحفظها في قاعدة البيانات
        [Fact]
        public async Task Test1_CreateTicket_WhenValid_SetsStatusToNewAndPersistsToDatabase()
        {
            using var context = CreateInMemoryDbContext(nameof(Test1_CreateTicket_WhenValid_SetsStatusToNewAndPersistsToDatabase));

            var ticketRepo = new TicketRepository(context);
            var handler = new CreateTicketCommandHandler(ticketRepo);

            var ticket = new Ticket
            {
                Name = "Router issue",
                CustomerId = 1,
                CreatedDate = DateTime.UtcNow
            };

            var createdTicket = await handler.Handle(new CreateTicketCommand(ticket), CancellationToken.None);

            Assert.NotNull(createdTicket);
            Assert.Equal(TaskStatus.New, createdTicket.Status);
            Assert.Equal("Router issue", createdTicket.Name);

            // التأكد من الحفظ الفعلي في قاعدة البيانات
            var savedTicket = await context.Tickets.FirstOrDefaultAsync(t => t.Name == "Router issue");
            Assert.NotNull(savedTicket);
            Assert.Equal(TaskStatus.New, savedTicket.Status);
        }

        // 2. اختبار Controller: نجاح إضافة تذكرة وإرجاع HTTP 201
        [Fact]
        public async Task Test2_TicketsController_AddTicket_ReturnsStatusCode201_WhenSuccess()
        {
            var fakeMediator = new FakeMediator(request =>
            {
                if (request is CreateTicketCommand cmd)
                {
                    Assert.Equal(TaskStatus.New, cmd.Ticket.Status);
                    return cmd.Ticket;
                }
                throw new NotImplementedException();
            });

            var mapper = CreateAutoMapper();
            var logger = NullLogger<TicketsController>.Instance;
            var controller = new TicketsController(fakeMediator, logger, mapper);

            var dto = new TicketCreateDTO
            {
                Name = "Fiber cut issue",
                CustomerId = 1
            };

            var actionResult = await controller.AddTicket(dto);

            var statusResult = Assert.IsType<StatusCodeResult>(actionResult);
            Assert.Equal(201, statusResult.StatusCode);
        }

        // ==========================================
        // 2. UpdateTicket Scenarios
        // ==========================================

        // 3. نجاح تعديل اسم التذكرة دون التأثير على الحالة أو تغييرها
        [Fact]
        public async Task Test3_UpdateTicket_WhenTicketExists_UpdatesNameAndPreservesStatus()
        {
            using var context = CreateInMemoryDbContext(nameof(Test3_UpdateTicket_WhenTicketExists_UpdatesNameAndPreservesStatus));

            var originalTicket = new Ticket
            {
                Id = 10,
                Name = "Original Ticket Name",
                Status = TaskStatus.InProgress,
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                CustomerId = 1
            };
            context.Tickets.Add(originalTicket);
            await context.SaveChangesAsync();

            var ticketRepo = new TicketRepository(context);
            var handler = new UpdateTicketCommandHandler(ticketRepo);

            var result = await handler.Handle(new UpdateTicketCommand(10, "Updated Ticket Name"), CancellationToken.None);

            Assert.True(result.Success);

            // التحقق من أن الاسم تم تعديله وأن الحالة InProgress بقيت كما هي
            var updated = await context.Tickets.FindAsync(10);
            Assert.NotNull(updated);
            Assert.Equal("Updated Ticket Name", updated.Name);
            Assert.Equal(TaskStatus.InProgress, updated.Status);
            Assert.Equal(1, updated.CustomerId);
        }

        // 4. فشل تعديل التذكرة عند عدم وجودها وإرجاع رسالة خطأ مناسبة
        [Fact]
        public async Task Test4_UpdateTicket_WhenTicketDoesNotExist_ReturnsFailure()
        {
            using var context = CreateInMemoryDbContext(nameof(Test4_UpdateTicket_WhenTicketDoesNotExist_ReturnsFailure));

            var ticketRepo = new TicketRepository(context);
            var handler = new UpdateTicketCommandHandler(ticketRepo);

            var result = await handler.Handle(new UpdateTicketCommand(999, "Non-existent Ticket"), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Ticket not found.", result.ErrorMessage);
        }

        // 5. اختبار Controller: إرجاع HTTP 404 عند فشل تعديل تذكرة غير موجودة
        [Fact]
        public async Task Test5_TicketsController_UpdateTicket_ReturnsNotFound_WhenTicketDoesNotExist()
        {
            var fakeMediator = new FakeMediator(request =>
            {
                if (request is UpdateTicketCommand)
                {
                    return Result.Fail("Ticket not found.");
                }
                throw new NotImplementedException();
            });

            var mapper = CreateAutoMapper();
            var logger = NullLogger<TicketsController>.Instance;
            var controller = new TicketsController(fakeMediator, logger, mapper);

            var dto = new TicketUpdateDTO { Name = "New Name" };
            var actionResult = await controller.UpdateTicket(999, dto);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        // ==========================================
        // 3. StartTicket Scenarios (New -> InProgress)
        // ==========================================

        // 6. نجاح بدء التذكرة: الانتقال من New إلى InProgress
        [Fact]
        public async Task Test6_StartTicket_WhenStatusIsNew_TransitionsToInProgress()
        {
            using var context = CreateInMemoryDbContext(nameof(Test6_StartTicket_WhenStatusIsNew_TransitionsToInProgress));

            var ticket = new Ticket
            {
                Id = 20,
                Name = "New Ticket",
                Status = TaskStatus.New,
                CreatedDate = DateTime.UtcNow
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var ticketRepo = new TicketRepository(context);
            var handler = new StartTicketCommandHandler(ticketRepo);

            var result = await handler.Handle(new StartTicketCommand(20), CancellationToken.None);

            Assert.True(result.Success);

            var updated = await context.Tickets.FindAsync(20);
            Assert.NotNull(updated);
            Assert.Equal(TaskStatus.InProgress, updated.Status);
        }

        // 7. رفض بدء التذكرة إذا كانت قيد التنفيذ InProgress أو مغلقة Closed
        [Fact]
        public async Task Test7_StartTicket_WhenStatusIsInProgressOrClosed_ReturnsFailure()
        {
            using var context = CreateInMemoryDbContext(nameof(Test7_StartTicket_WhenStatusIsInProgressOrClosed_ReturnsFailure));

            var inProgressTicket = new Ticket { Id = 21, Name = "In Progress", Status = TaskStatus.InProgress };
            var closedTicket = new Ticket { Id = 22, Name = "Closed", Status = TaskStatus.Closed };
            context.Tickets.AddRange(inProgressTicket, closedTicket);
            await context.SaveChangesAsync();

            var ticketRepo = new TicketRepository(context);
            var handler = new StartTicketCommandHandler(ticketRepo);

            // محاولة بدء تذكرة قيد التنفيذ مسبقاً
            var res1 = await handler.Handle(new StartTicketCommand(21), CancellationToken.None);
            Assert.False(res1.Success);
            Assert.Contains("already in progress", res1.ErrorMessage, StringComparison.OrdinalIgnoreCase);

            // محاولة بدء تذكرة مغلقة
            var res2 = await handler.Handle(new StartTicketCommand(22), CancellationToken.None);
            Assert.False(res2.Success);
            Assert.Contains("closed", res2.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        // 8. فشل بدء التذكرة عند عدم وجودها
        [Fact]
        public async Task Test8_StartTicket_WhenTicketDoesNotExist_ReturnsFailure()
        {
            using var context = CreateInMemoryDbContext(nameof(Test8_StartTicket_WhenTicketDoesNotExist_ReturnsFailure));

            var ticketRepo = new TicketRepository(context);
            var handler = new StartTicketCommandHandler(ticketRepo);

            var result = await handler.Handle(new StartTicketCommand(999), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Ticket not found.", result.ErrorMessage);
        }

        // ==========================================
        // 4. CloseTicket Scenarios (InProgress -> Closed)
        // ==========================================

        // 9. نجاح إغلاق التذكرة: الانتقال من InProgress إلى Closed
        [Fact]
        public async Task Test9_CloseTicket_WhenStatusIsInProgress_TransitionsToClosed()
        {
            using var context = CreateInMemoryDbContext(nameof(Test9_CloseTicket_WhenStatusIsInProgress_TransitionsToClosed));

            var ticket = new Ticket
            {
                Id = 30,
                Name = "Ticket In Progress",
                Status = TaskStatus.InProgress,
                CreatedDate = DateTime.UtcNow
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var ticketRepo = new TicketRepository(context);
            var handler = new CloseTicketCommandHandler(ticketRepo);

            var result = await handler.Handle(new CloseTicketCommand(30), CancellationToken.None);

            Assert.True(result.Success);

            var updated = await context.Tickets.FindAsync(30);
            Assert.NotNull(updated);
            Assert.Equal(TaskStatus.Closed, updated.Status);
        }

        // 10. رفض إغلاق التذكرة إذا كانت بحالة New أو مغلقة Closed مسبقاً
        [Fact]
        public async Task Test10_CloseTicket_WhenStatusIsNewOrClosed_ReturnsFailure()
        {
            using var context = CreateInMemoryDbContext(nameof(Test10_CloseTicket_WhenStatusIsNewOrClosed_ReturnsFailure));

            var newTicket = new Ticket { Id = 31, Name = "New Ticket", Status = TaskStatus.New };
            var closedTicket = new Ticket { Id = 32, Name = "Already Closed Ticket", Status = TaskStatus.Closed };
            context.Tickets.AddRange(newTicket, closedTicket);
            await context.SaveChangesAsync();

            var ticketRepo = new TicketRepository(context);
            var handler = new CloseTicketCommandHandler(ticketRepo);

            // محاولة إغلاق تذكرة جديدة مباشرة
            var res1 = await handler.Handle(new CloseTicketCommand(31), CancellationToken.None);
            Assert.False(res1.Success);
            Assert.Contains("must be in progress first", res1.ErrorMessage, StringComparison.OrdinalIgnoreCase);

            // محاولة إغلاق تذكرة مغلقة مسبقاً
            var res2 = await handler.Handle(new CloseTicketCommand(32), CancellationToken.None);
            Assert.False(res2.Success);
            Assert.Contains("already closed", res2.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        // 11. فشل إغلاق التذكرة عند عدم وجودها
        [Fact]
        public async Task Test11_CloseTicket_WhenTicketDoesNotExist_ReturnsFailure()
        {
            using var context = CreateInMemoryDbContext(nameof(Test11_CloseTicket_WhenTicketDoesNotExist_ReturnsFailure));

            var ticketRepo = new TicketRepository(context);
            var handler = new CloseTicketCommandHandler(ticketRepo);

            var result = await handler.Handle(new CloseTicketCommand(999), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Ticket not found.", result.ErrorMessage);
        }

        // ==========================================
        // 5. ReopenTicket Scenarios (Closed -> InProgress)
        // ==========================================

        // 12. نجاح إعادة فتح التذكرة: الانتقال من Closed إلى InProgress
        [Fact]
        public async Task Test12_ReopenTicket_WhenStatusIsClosed_TransitionsToInProgress()
        {
            using var context = CreateInMemoryDbContext(nameof(Test12_ReopenTicket_WhenStatusIsClosed_TransitionsToInProgress));

            var ticket = new Ticket
            {
                Id = 40,
                Name = "Closed Ticket",
                Status = TaskStatus.Closed,
                CreatedDate = DateTime.UtcNow
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var ticketRepo = new TicketRepository(context);
            var handler = new ReopenTicketCommandHandler(ticketRepo);

            var result = await handler.Handle(new ReopenTicketCommand(40), CancellationToken.None);

            Assert.True(result.Success);

            var updated = await context.Tickets.FindAsync(40);
            Assert.NotNull(updated);
            Assert.Equal(TaskStatus.InProgress, updated.Status);
        }

        // 13. رفض إعادة فتح التذكرة إذا كانت بحالة New أو قيد التنفيذ InProgress
        [Fact]
        public async Task Test13_ReopenTicket_WhenStatusIsNewOrInProgress_ReturnsFailure()
        {
            using var context = CreateInMemoryDbContext(nameof(Test13_ReopenTicket_WhenStatusIsNewOrInProgress_ReturnsFailure));

            var newTicket = new Ticket { Id = 41, Name = "New Ticket", Status = TaskStatus.New };
            var inProgressTicket = new Ticket { Id = 42, Name = "In Progress Ticket", Status = TaskStatus.InProgress };
            context.Tickets.AddRange(newTicket, inProgressTicket);
            await context.SaveChangesAsync();

            var ticketRepo = new TicketRepository(context);
            var handler = new ReopenTicketCommandHandler(ticketRepo);

            // محاولة إعادة فتح تذكرة جديدة لم تغلق بعد
            var res1 = await handler.Handle(new ReopenTicketCommand(41), CancellationToken.None);
            Assert.False(res1.Success);
            Assert.Contains("not been closed yet", res1.ErrorMessage, StringComparison.OrdinalIgnoreCase);

            // محاولة إعادة فتح تذكرة قيد التنفيذ بالفعل
            var res2 = await handler.Handle(new ReopenTicketCommand(42), CancellationToken.None);
            Assert.False(res2.Success);
            Assert.Contains("already in progress", res2.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        // 14. فشل إعادة فتح التذكرة عند عدم وجودها
        [Fact]
        public async Task Test14_ReopenTicket_WhenTicketDoesNotExist_ReturnsFailure()
        {
            using var context = CreateInMemoryDbContext(nameof(Test14_ReopenTicket_WhenTicketDoesNotExist_ReturnsFailure));

            var ticketRepo = new TicketRepository(context);
            var handler = new ReopenTicketCommandHandler(ticketRepo);

            var result = await handler.Handle(new ReopenTicketCommand(999), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Ticket not found.", result.ErrorMessage);
        }

        // ==========================================
        // 6. DeleteTicket Scenarios
        // ==========================================

        // 15. نجاح حذف التذكرة عندما تكون بحالة New وحذفها الفعلي من قاعدة البيانات
        [Fact]
        public async Task Test15_DeleteTicket_WhenStatusIsNew_DeletesTicketFromDatabase()
        {
            using var context = CreateInMemoryDbContext(nameof(Test15_DeleteTicket_WhenStatusIsNew_DeletesTicketFromDatabase));

            var ticket = new Ticket
            {
                Id = 50,
                Name = "New Ticket To Delete",
                Status = TaskStatus.New,
                CreatedDate = DateTime.UtcNow
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var ticketRepo = new TicketRepository(context);
            var handler = new DeleteTicketCommandHandler(ticketRepo);

            var result = await handler.Handle(new DeleteTicketCommand(50), CancellationToken.None);

            Assert.True(result.Success);

            // التأكد من الحذف من قاعدة البيانات
            var deleted = await context.Tickets.FindAsync(50);
            Assert.Null(deleted);
        }

        // 16. رفض حذف التذكرة عندما تكون بحالة InProgress أو Closed والتأكد من بقائها في قاعدة البيانات
        [Fact]
        public async Task Test16_DeleteTicket_WhenStatusIsInProgressOrClosed_ReturnsFailureAndDoesNotDelete()
        {
            using var context = CreateInMemoryDbContext(nameof(Test16_DeleteTicket_WhenStatusIsInProgressOrClosed_ReturnsFailureAndDoesNotDelete));

            var inProgressTicket = new Ticket { Id = 51, Name = "In Progress Ticket", Status = TaskStatus.InProgress };
            var closedTicket = new Ticket { Id = 52, Name = "Closed Ticket", Status = TaskStatus.Closed };
            context.Tickets.AddRange(inProgressTicket, closedTicket);
            await context.SaveChangesAsync();

            var ticketRepo = new TicketRepository(context);
            var handler = new DeleteTicketCommandHandler(ticketRepo);

            // محاولة حذف تذكرة InProgress
            var res1 = await handler.Handle(new DeleteTicketCommand(51), CancellationToken.None);
            Assert.False(res1.Success);
            Assert.Contains("InProgress", res1.ErrorMessage);
            Assert.Contains("Only tickets in 'New' status can be deleted", res1.ErrorMessage);

            // محاولة حذف تذكرة Closed
            var res2 = await handler.Handle(new DeleteTicketCommand(52), CancellationToken.None);
            Assert.False(res2.Success);
            Assert.Contains("Closed", res2.ErrorMessage);
            Assert.Contains("Only tickets in 'New' status can be deleted", res2.ErrorMessage);

            // التأكد من عدم حذف التذكرتين من قاعدة البيانات
            Assert.NotNull(await context.Tickets.FindAsync(51));
            Assert.NotNull(await context.Tickets.FindAsync(52));
        }

        // 17. فشل حذف التذكرة عند عدم وجودها
        [Fact]
        public async Task Test17_DeleteTicket_WhenTicketDoesNotExist_ReturnsFailure()
        {
            using var context = CreateInMemoryDbContext(nameof(Test17_DeleteTicket_WhenTicketDoesNotExist_ReturnsFailure));

            var ticketRepo = new TicketRepository(context);
            var handler = new DeleteTicketCommandHandler(ticketRepo);

            var result = await handler.Handle(new DeleteTicketCommand(999), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Ticket not found.", result.ErrorMessage);
        }

        // 18. اختبار Controller: إرجاع BadRequest عند رفض الحذف بسبب الحالة
        [Fact]
        public async Task Test18_TicketsController_DeleteTicket_ReturnsBadRequest_WhenStatusNotNew()
        {
            var fakeMediator = new FakeMediator(request =>
            {
                if (request is DeleteTicketCommand)
                {
                    return Result.Fail("Cannot delete ticket with status 'InProgress'. Only tickets in 'New' status can be deleted.");
                }
                throw new NotImplementedException();
            });

            var mapper = CreateAutoMapper();
            var logger = NullLogger<TicketsController>.Instance;
            var controller = new TicketsController(fakeMediator, logger, mapper);

            var actionResult = await controller.DeleteTicket(51);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        // 19. اختبار Controller: إرجاع NotFound عند محاولة حذف تذكرة غير موجودة
        [Fact]
        public async Task Test19_TicketsController_DeleteTicket_ReturnsNotFound_WhenTicketNotFound()
        {
            var fakeMediator = new FakeMediator(request =>
            {
                if (request is DeleteTicketCommand)
                {
                    return Result.Fail("Ticket not found.");
                }
                throw new NotImplementedException();
            });

            var mapper = CreateAutoMapper();
            var logger = NullLogger<TicketsController>.Instance;
            var controller = new TicketsController(fakeMediator, logger, mapper);

            var actionResult = await controller.DeleteTicket(999);

            var notFoundResult = Assert.IsType<NotFoundResult>(actionResult);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        // 20. اختبار Controller: إرجاع NoContent عند نجاح حذف التذكرة
        [Fact]
        public async Task Test20_TicketsController_DeleteTicket_ReturnsNoContent_WhenSuccess()
        {
            var fakeMediator = new FakeMediator(request =>
            {
                if (request is DeleteTicketCommand)
                {
                    return Result.Ok();
                }
                throw new NotImplementedException();
            });

            var mapper = CreateAutoMapper();
            var logger = NullLogger<TicketsController>.Instance;
            var controller = new TicketsController(fakeMediator, logger, mapper);

            var actionResult = await controller.DeleteTicket(50);

            var noContentResult = Assert.IsType<NoContentResult>(actionResult);
            Assert.Equal(204, noContentResult.StatusCode);
        }

        // ==========================================
        // Helper Classes
        // ==========================================

        private class FakeMediator : IMediator
        {
            private readonly Func<object, object> _handler;

            public FakeMediator(Func<object, object> handler)
            {
                _handler = handler;
            }

            public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            {
                var result = _handler(request);
                return Task.FromResult((TResponse)result);
            }

            public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            {
                _handler(request);
                return Task.CompletedTask;
            }

            public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<object?>(_handler(request));
            }

            public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }
    }
}

