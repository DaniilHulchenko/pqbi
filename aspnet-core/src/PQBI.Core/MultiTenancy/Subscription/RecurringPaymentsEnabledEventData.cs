﻿using Abp.Events.Bus;

namespace PQBI.MultiTenancy.Subscription
{
    public class RecurringPaymentsEnabledEventData : EventData
    {
        public int TenantId { get; set; }
    }
}