using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Subscriptions.Commands
{
    public record UpdateSubscriptionCommand(int Id, string Name) : IRequest<Result>
    {
        public UpdateSubscriptionCommand(Subscription subscription) : this(subscription.Id, subscription.Name) { }
    }

    public class UpdateSubscriptionCommandHandler : IRequestHandler<UpdateSubscriptionCommand, Result>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public UpdateSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<Result> Handle(UpdateSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var subscription = await _subscriptionRepository.GetSubscriptionAsync(request.Id);
            if (subscription == null)
            {
                return Result.Fail("Subscription not found.");
            }

            subscription.Name = request.Name;
            await _subscriptionRepository.UpdateSubscriptionAsync(subscription);
            return Result.Ok();
        }
    }
}
