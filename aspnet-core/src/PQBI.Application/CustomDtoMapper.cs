using PQBI.Groups.Dtos;
using PQBI.Groups;
using PQBI.TrendWidgetConfigurations.Dtos;
using PQBI.TrendWidgetConfigurations;
using PQBI.DefaultValues.Dtos;
using PQBI.DefaultValues;
using PQBI.BarChartWidgetConfigurations.Dtos;
using PQBI.BarChartWidgetConfigurations;
using PQBI.TableWidgetConfigurations.Dtos;
using PQBI.TableWidgetConfigurations;
using PQBI.DashboardCustomization.Dtos;
using PQBI.DashboardCustomization;
using PQBI.CustomParameters.Dtos;
using PQBI.CustomParameters;
using Abp.Application.Editions;
using Abp.Application.Features;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.DynamicEntityProperties;
using Abp.EntityHistory;
using Abp.Extensions;
using Abp.Localization;
using Abp.Notifications;
using Abp.Organizations;
using Abp.UI.Inputs;
using Abp.Webhooks;
using AutoMapper;
using PQBI.Auditing.Dto;
using PQBI.Authorization.Accounts.Dto;
using PQBI.Authorization.Delegation;
using PQBI.Authorization.Permissions.Dto;
using PQBI.Authorization.Roles;
using PQBI.Authorization.Roles.Dto;
using PQBI.Authorization.Users;
using PQBI.Authorization.Users.Delegation.Dto;
using PQBI.Authorization.Users.Dto;
using PQBI.Authorization.Users.Importing.Dto;
using PQBI.Authorization.Users.Profile.Dto;
using PQBI.Chat;
using PQBI.Chat.Dto;
using PQBI.Common.Dto;
using PQBI.DynamicEntityProperties.Dto;
using PQBI.Editions;
using PQBI.Editions.Dto;
using PQBI.Friendships;
using PQBI.Friendships.Cache;
using PQBI.Friendships.Dto;
using PQBI.Localization.Dto;
using PQBI.MultiTenancy;
using PQBI.MultiTenancy.Dto;
using PQBI.MultiTenancy.HostDashboard.Dto;
using PQBI.MultiTenancy.Payments;
using PQBI.MultiTenancy.Payments.Dto;
using PQBI.Notifications.Dto;
using PQBI.Organizations.Dto;
using PQBI.Sessions.Dto;
using PQBI.WebHooks.Dto;

namespace PQBI
{
    internal static class CustomDtoMapper
    {
        public static void CreateMappings(IMapperConfigurationExpression configuration)
        {
            configuration.CreateMap<CreateOrEditGroupDto, Group>().ReverseMap();
            configuration.CreateMap<GroupDto, Group>().ReverseMap();
            configuration.CreateMap<CreateOrEditTrendWidgetConfigurationDto, TrendWidgetConfiguration>().ReverseMap();
            configuration.CreateMap<TrendWidgetConfigurationDto, TrendWidgetConfiguration>().ReverseMap();
            configuration.CreateMap<CreateOrEditDefaultValueDto, DefaultValue>().ReverseMap();
            configuration.CreateMap<DefaultValueDto, DefaultValue>().ReverseMap();
            configuration.CreateMap<CreateOrEditBarChartWidgetConfigurationDto, BarChartWidgetConfiguration>().ReverseMap();
            configuration.CreateMap<BarChartWidgetConfigurationDto, BarChartWidgetConfiguration>().ReverseMap();
            configuration.CreateMap<CreateOrEditTableWidgetConfigurationDto, TableWidgetConfiguration>().ReverseMap();
            configuration.CreateMap<TableWidgetConfigurationDto, TableWidgetConfiguration>().ReverseMap();
            configuration.CreateMap<CreateOrEditWidgetConfigurationDto, WidgetConfiguration>().ReverseMap();
            configuration.CreateMap<WidgetConfigurationDto, WidgetConfiguration>().ReverseMap();
            configuration.CreateMap<CreateOrEditCustomParameterDto, CustomParameter>().ReverseMap();
            configuration.CreateMap<CustomParameterDto, CustomParameter>().ReverseMap();
            //Inputs
            configuration.CreateMap<CheckboxInputType, FeatureInputTypeDto>();
            configuration.CreateMap<SingleLineStringInputType, FeatureInputTypeDto>();
            configuration.CreateMap<ComboboxInputType, FeatureInputTypeDto>();
            configuration.CreateMap<IInputType, FeatureInputTypeDto>()
                .Include<CheckboxInputType, FeatureInputTypeDto>()
                .Include<SingleLineStringInputType, FeatureInputTypeDto>()
                .Include<ComboboxInputType, FeatureInputTypeDto>();
            configuration.CreateMap<StaticLocalizableComboboxItemSource, LocalizableComboboxItemSourceDto>();
            configuration.CreateMap<ILocalizableComboboxItemSource, LocalizableComboboxItemSourceDto>()
                .Include<StaticLocalizableComboboxItemSource, LocalizableComboboxItemSourceDto>();
            configuration.CreateMap<LocalizableComboboxItem, LocalizableComboboxItemDto>();
            configuration.CreateMap<ILocalizableComboboxItem, LocalizableComboboxItemDto>()
                .Include<LocalizableComboboxItem, LocalizableComboboxItemDto>();

            //Chat
            configuration.CreateMap<ChatMessage, ChatMessageDto>();
            configuration.CreateMap<ChatMessage, ChatMessageExportDto>();

            //Feature
            configuration.CreateMap<FlatFeatureSelectDto, Feature>().ReverseMap();
            configuration.CreateMap<Feature, FlatFeatureDto>();

            //Role
            configuration.CreateMap<RoleEditDto, Role>().ReverseMap();
            configuration.CreateMap<Role, RoleListDto>();
            configuration.CreateMap<UserRole, UserListRoleDto>();

            //Edition
            configuration.CreateMap<EditionEditDto, SubscribableEdition>().ReverseMap();
            configuration.CreateMap<EditionCreateDto, SubscribableEdition>();
            configuration.CreateMap<EditionSelectDto, SubscribableEdition>().ReverseMap();
            configuration.CreateMap<SubscribableEdition, EditionInfoDto>();

            configuration.CreateMap<Edition, EditionInfoDto>().Include<SubscribableEdition, EditionInfoDto>();

            configuration.CreateMap<SubscribableEdition, EditionListDto>();
            configuration.CreateMap<Edition, EditionEditDto>();
            configuration.CreateMap<Edition, SubscribableEdition>();
            configuration.CreateMap<Edition, EditionSelectDto>();

            //Payment
            configuration.CreateMap<SubscriptionPaymentDto, SubscriptionPayment>()
                .ReverseMap()
                .ForMember(dto => dto.TotalAmount, options => options.MapFrom(e => e.GetTotalAmount()));
            configuration.CreateMap<SubscriptionPaymentProductDto, SubscriptionPaymentProduct>().ReverseMap();
            configuration.CreateMap<SubscriptionPaymentListDto, SubscriptionPayment>().ReverseMap();
            configuration.CreateMap<SubscriptionPayment, SubscriptionPaymentInfoDto>();

            //Permission
            configuration.CreateMap<Permission, FlatPermissionDto>();
            configuration.CreateMap<Permission, FlatPermissionWithLevelDto>();

            //Language
            configuration.CreateMap<ApplicationLanguage, ApplicationLanguageEditDto>();
            configuration.CreateMap<ApplicationLanguage, ApplicationLanguageListDto>();
            configuration.CreateMap<NotificationDefinition, NotificationSubscriptionWithDisplayNameDto>();
            configuration.CreateMap<ApplicationLanguage, ApplicationLanguageEditDto>()
                .ForMember(ldto => ldto.IsEnabled, options => options.MapFrom(l => !l.IsDisabled));

            //Tenant
            configuration.CreateMap<Tenant, RecentTenant>();
            configuration.CreateMap<Tenant, TenantLoginInfoDto>();
            configuration.CreateMap<Tenant, TenantListDto>();
            configuration.CreateMap<TenantEditDto, Tenant>().ReverseMap();
            configuration.CreateMap<CurrentTenantInfoDto, Tenant>().ReverseMap();

            //User
            configuration.CreateMap<User, UserEditDto>()
                .ForMember(dto => dto.Password, options => options.Ignore())
                .ReverseMap()
                .ForMember(user => user.Password, options => options.Ignore());
            configuration.CreateMap<User, UserLoginInfoDto>();
            configuration.CreateMap<User, UserListDto>();
            configuration.CreateMap<User, ChatUserDto>();
            configuration.CreateMap<User, OrganizationUnitUserListDto>();
            configuration.CreateMap<Role, OrganizationUnitRoleListDto>();
            configuration.CreateMap<CurrentUserProfileEditDto, User>().ReverseMap();
            configuration.CreateMap<UserLoginAttemptDto, UserLoginAttempt>().ReverseMap();
            configuration.CreateMap<ImportUserDto, User>();
            configuration.CreateMap<User, FindUsersOutputDto>();
            configuration.CreateMap<User, FindOrganizationUnitUsersOutputDto>();

            //AuditLog
            configuration.CreateMap<AuditLog, AuditLogListDto>();
            configuration.CreateMap<EntityChange, EntityChangeListDto>();
            configuration.CreateMap<EntityPropertyChange, EntityPropertyChangeDto>();

            //Friendship
            configuration.CreateMap<Friendship, FriendDto>();
            configuration.CreateMap<FriendCacheItem, FriendDto>();

            //OrganizationUnit
            configuration.CreateMap<OrganizationUnit, OrganizationUnitDto>();

            //Webhooks
            configuration.CreateMap<WebhookSubscription, GetAllSubscriptionsOutput>();
            configuration.CreateMap<WebhookSendAttempt, GetAllSendAttemptsOutput>()
                .ForMember(webhookSendAttemptListDto => webhookSendAttemptListDto.WebhookName,
                    options => options.MapFrom(l => l.WebhookEvent.WebhookName))
                .ForMember(webhookSendAttemptListDto => webhookSendAttemptListDto.Data,
                    options => options.MapFrom(l => l.WebhookEvent.Data));

            configuration.CreateMap<WebhookSendAttempt, GetAllSendAttemptsOfWebhookEventOutput>();

            configuration.CreateMap<DynamicProperty, DynamicPropertyDto>().ReverseMap();
            configuration.CreateMap<DynamicPropertyValue, DynamicPropertyValueDto>().ReverseMap();
            configuration.CreateMap<DynamicEntityProperty, DynamicEntityPropertyDto>()
                .ForMember(dto => dto.DynamicPropertyName,
                    options => options.MapFrom(entity => entity.DynamicProperty.DisplayName.IsNullOrEmpty() ? entity.DynamicProperty.PropertyName : entity.DynamicProperty.DisplayName));
            configuration.CreateMap<DynamicEntityPropertyDto, DynamicEntityProperty>();

            configuration.CreateMap<DynamicEntityPropertyValue, DynamicEntityPropertyValueDto>().ReverseMap();

            //User Delegations
            configuration.CreateMap<CreateUserDelegationDto, UserDelegation>();

            /* ADD YOUR OWN CUSTOM AUTOMAPPER MAPPINGS HERE */
        }
    }
}