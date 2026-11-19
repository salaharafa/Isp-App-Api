using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IspAppApi.Application.Features.Subscriptions.Commands
{
    public record CancelSubscriptionCommand(int Id) : IRequest<Result>;

    public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, Result>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public CancelSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<Result> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var subscription = await _subscriptionRepository.GetSubscriptionAsync(request.Id);
            if (subscription == null)
            {
                return Result.Fail("Subscription not found.");
            }

            if (subscription.Status == SubscriptionStatus.Cancelled)
            {
                return Result.Fail("Subscription is already cancelled.");
            }

            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.EndDate = DateTime.UtcNow;

            await _subscriptionRepository.UpdateSubscriptionAsync(subscription);
            return Result.Ok();
        }
    }
}
