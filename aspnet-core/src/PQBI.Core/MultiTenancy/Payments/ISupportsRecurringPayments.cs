using Abp.Events.Bus.Handlers;
using PQBI.MultiTenancy.Subscription;

namespace PQBI.MultiTenancy.Payments
{
    public interface ISupportsRecurringPayments : 
        IEventHandler<RecurringPaymentsDisabledEventData>, 
        IEventHandler<RecurringPaymentsEnabledEventData>,
        IEventHandler<SubscriptionUpdatedEventData>,
        IEventHandler<SubscriptionCancelledEventData>
    {

    }
}
