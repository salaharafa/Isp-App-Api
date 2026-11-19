using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Subscriptions.Commands
{
    public record CreateSubscriptionCommand(Subscription Subscription) : IRequest<Result<Subscription>>;

    public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, Result<Subscription>>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly ICustomerRepository _customerRepository;

        public CreateSubscriptionCommandHandler(
            ISubscriptionRepository subscriptionRepository,
            ICustomerRepository customerRepository)
        {
            _subscriptionRepository = subscriptionRepository;
            _customerRepository = customerRepository;
        }

        public async Task<Result<Subscription>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.GetCustomerAsync(request.Subscription.CustomerId);
            if (customer == null)
            {
                return Result.Fail<Subscription>($"Customer with ID {request.Subscription.CustomerId} not found.");
            }

            // تثبيت قواعد دورة الحياة — يبدأ كل اشتراك دائماً بـ Active وبدون EndDate
            request.Subscription.Status = SubscriptionStatus.Active;
            request.Subscription.EndDate = null;

            await _subscriptionRepository.AddSubscriptionAsync(request.Subscription);
            return Result.Ok(request.Subscription);
        }
    }
}
