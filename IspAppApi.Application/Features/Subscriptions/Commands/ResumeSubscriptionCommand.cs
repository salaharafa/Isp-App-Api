using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace IspAppApi.Application.Features.Subscriptions.Commands
{
    public record ResumeSubscriptionCommand(int Id) : IRequest<Result>;

    public class ResumeSubscriptionCommandHandler : IRequestHandler<ResumeSubscriptionCommand, Result>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public ResumeSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<Result> Handle(ResumeSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var subscription = await _subscriptionRepository.GetSubscriptionAsync(request.Id);
            if (subscription == null)
            {
                return Result.Fail("Subscription not found.");
            }

            if (subscription.Status == SubscriptionStatus.Cancelled)
            {
                return Result.Fail("Cannot resume a cancelled subscription.");
            }

            if (subscription.Status == SubscriptionStatus.Active)
            {
                return Result.Fail("Subscription is already active.");
            }

            subscription.Status = SubscriptionStatus.Active;

            await _subscriptionRepository.UpdateSubscriptionAsync(subscription);
            return Result.Ok();
        }
    }
}
