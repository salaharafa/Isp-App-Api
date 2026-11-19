using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace IspAppApi.Application.Features.Subscriptions.Commands
{
    public record SuspendSubscriptionCommand(int Id) : IRequest<Result>;

    public class SuspendSubscriptionCommandHandler : IRequestHandler<SuspendSubscriptionCommand, Result>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public SuspendSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<Result> Handle(SuspendSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var subscription = await _subscriptionRepository.GetSubscriptionAsync(request.Id);
            if (subscription == null)
            {
                return Result.Fail("Subscription not found.");
            }

            if (subscription.Status == SubscriptionStatus.Cancelled)
            {
                return Result.Fail("Cannot suspend a cancelled subscription.");
            }

            if (subscription.Status == SubscriptionStatus.Suspended)
            {
                return Result.Fail("Subscription is already suspended.");
            }

            subscription.Status = SubscriptionStatus.Suspended;

            await _subscriptionRepository.UpdateSubscriptionAsync(subscription);
            return Result.Ok();
        }
    }
}
