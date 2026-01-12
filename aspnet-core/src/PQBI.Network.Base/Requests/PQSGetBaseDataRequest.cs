using Microsoft.AspNetCore.Hosting;
using PayPalCheckoutSdk.Orders;
using PQBI.CalculationEngine.Matrix;
using PQBI.Configuration;
using PQBI.Infrastructure.Extensions;
using PQBI.Tenants.Dashboard.Dto;
using PQS.Data.Common;
using PQS.Data.Common.Values;
using PQS.Data.Configurations;
using PQS.Data.Configurations.Enums;
using PQS.Data.Events;
using PQS.Data.Measurements;
using PQS.Data.Measurements.StandardParameter;
using PQS.Data.Measurements.Utils;
using PQS.Data.RecordsContainer;
using PQS.Data.RecordsContainer.Records;
using PQZTimeFormat;
using TimeZoneConverter;

namespace PQBI.Requests;

public record GetBaseDataInfoInput(Guid ComponentId, long StartTime, long EndTime, IEnumerable<MeasurementParameterBase> Parameters, CalculationTypeEnum CalculationType = CalculationTypeEnum.FORCE_DB_DATA, int whishedPoints = 0, FiltersGroup filtersGroup = null);

public class PQSGetBaseDataRequest : PQSCommonRequest
{    
    public PQSGetBaseDataRequest(string session, uint? timeZoneID, params GetBaseDataInfoInput[] inputs) : base(session)
    {
        TimeZoneID = timeZoneID;
        Inputs = inputs;
        AddConfigurations();
    }

    public GetBaseDataInfoInput[] Inputs { get; protected set; }

    protected override void AddConfigurations()
    {
        var configurationRecords = new List<GetBaseConfigurationRecord>();
        foreach (var input in Inputs.SafeList())
        {
            var opRec = new GetBaseDataRecord(
                input.ComponentId,
                input.StartTime,
                input.EndTime,
                input.Parameters.ToList(),
                input.CalculationType,
                input.whishedPoints,
                timeZoneID: TimeZoneID,
                classFilter: input.filtersGroup,
                isMondayStartOfWeek: WeekConfiguration.IsMondayStartOfWeek);
            AddRecord(opRec);

            var configurations = new List<ConfigurationParameterBase>();
            foreach (var parameter in input.Parameters)
            {
                if (parameter is ChannelMeasurementParameter chParam)
                {
                    ChannelConfiguration ch = new ChannelConfiguration(StandardConfigurationEnum.STD_TYPE, PQSType.INT1, chParam.ChannelNumber);
                    configurations.Add(ch);
                }
            }

            if (configurations.Count > 0)
            {
                var configurationRecord = new GetBaseConfigurationRecord(input.ComponentId, new PQZDateTime(input.StartTime), new PQZDateTime(input.EndTime), configurations);
                configurationRecords.Add(configurationRecord);
            }
        }

        foreach (var item in configurationRecords)
        {
            AddRecord(item);
        }

        //var configurationRecord = new GetBaseConfigurationRecord(null, new PQZDateTime(firstInput.StartTime), new PQZDateTime(firstInput.EndTime), configurations);
    }
}

public class PQSGetBaseDataResponse : PQSOperationResponseBase<PQSGetBaseDataRequest>
{
    public PQSGetBaseDataResponse(PQSGetBaseDataRequest request, PQSResponse response, string timezone)
        : base(request, response, timezone)
    {
    }

    // NEW: convertToUserTime controls if timestamps are returned as user-local or UTC
    public virtual PQZStatus ExtractGetParametersOrError(
        out IEnumerable<PQBIAxisData> parameters,
        bool convertToUserTime)
    {
        parameters = null;

        ExtractBaseDataAllRecords(out BaseDataRecord[] baseDataRecords, out var error);
        ExtractGetBaseConfigurationRecord(out BaseConfigurationRecord[] getBaseConfigurationRecords, out var getBaseConfigurationRecordError);

        if (error != null)
            return error.Status;

        var getBaseConfigurationRecorsDictionary = new Dictionary<Guid, BaseConfigurationRecord>();
        foreach (var item in getBaseConfigurationRecords)
        {
            if (item.ObjectID is not null)
                getBaseConfigurationRecorsDictionary[item.ObjectID.Value] = item;
        }

        // Resolve tz ONCE
        TimeZoneInfo? tz = null;
        if (convertToUserTime)
            tz = TimeZoneInfo.FindSystemTimeZoneById(TZConvert.IanaToWindows(Timezone));

        var paramList = new List<PQBIAxisData>();

        foreach (var record in baseDataRecords)
        {
            var compId = record.ObjectID.Value;
            var timeStamps = record.DataTimeStamps;

            var allMeasurementsParameter = record.MeasurementContainer.GetAllMeasurementsParameter();

            foreach (MeasurementParameterBase paramAndVal in allMeasurementsParameter)
            {
                int? feederId = null;
                DataUnitType dataUnitType = new EmptyDataUnitType();

                // (your existing feeder/unit detection stays as-is)
                if (paramAndVal is NetworkFeederMeasurementParameter networkFeederParam)
                {
                    feederId = (int)networkFeederParam.FeederNumber;
                    var unitState = UnitsUtility.GetUnitsFromGroupAndPhase(networkFeederParam.Group, networkFeederParam.Phase);
                    var token = UnitsEnumHelper.GetLocalizedDescriptionKey(unitState);
                    dataUnitType = new DataUnitType((int)unitState, token);
                }
                else if (paramAndVal is ChannelMeasurementParameter channelMeasurementParameter)
                {
                    if (getBaseConfigurationRecorsDictionary.TryGetValue(compId, out var baseConfigurationRecord))
                    {
                        ChannelConfiguration chConf = new ChannelConfiguration(
                            StandardConfigurationEnum.STD_TYPE,
                            PQSType.INT1,
                            channelMeasurementParameter.ChannelNumber);

                        var containerParam = baseConfigurationRecord.TimeToConfigurationContainerDictionary.First().Value;
                        if (containerParam.TryGetConfigurationValue<byte>(chConf, out var type))
                        {
                            ChannelTypeEnum channelTypeEnum = (ChannelTypeEnum)type;
                            var unitState = UnitsUtility.GetUnitsFromGroupAndPhase(
                                channelMeasurementParameter.Group,
                                channelType: channelTypeEnum);

                            var token = UnitsEnumHelper.GetLocalizedDescriptionKey(unitState);
                            dataUnitType = new DataUnitType((int)unitState, token);
                        }
                    }
                }

                var dataTimeStemps = new List<PQBIDataTimeStampDto>();
                var paramName = paramAndVal.ToString();
                var nominal = paramAndVal.Nominal;

                var container = record.MeasurementContainer[paramAndVal];

                if (container.Status == PQZStatus.OK)
                {
                    List<BaseDataValue<float>> points = container.GetBaseDataValue<float>();
                    var index = 0;

                    foreach (var point in points)
                    {
                        var dt = timeStamps[index++];

                        double? val = null;
                        if (!float.IsNaN(point.Value))
                            val = (double)point.Value;

                        // IMPORTANT: always treat dt.DateTimeUTC as UTC
                        var tsUtc = DateTime.SpecifyKind(dt.DateTimeUTC, DateTimeKind.Utc);

                        var tsOut = convertToUserTime
                            ? TimeZoneInfo.ConvertTimeFromUtc(tsUtc, tz!)
                            : tsUtc;

                        dataTimeStemps.Add(new PQBIDataTimeStampDto(tsOut, val, point.Status));
                    }

                    paramList.Add(new PQBIAxisData(
                        compId, feederId, paramName, nominal,
                        dataTimeStemps.ToArray(),
                        container.Status,
                        dataUnitType));
                }
                else
                {
                    paramList.Add(new PQBIAxisDataEmpty(compId, feederId, paramName, container.Status, dataUnitType));
                }
            }
        }

        parameters = paramList;
        return PQZStatus.OK;
    }

    // keep old signature for existing callers (defaults to user-local behavior)
    public virtual PQZStatus ExtractGetParametersOrError(out IEnumerable<PQBIAxisData> parameters)
        => ExtractGetParametersOrError(out parameters, convertToUserTime: true);
}


public class EmptyPQSGetBaseDataResponse : PQSGetBaseDataResponse
{
    public EmptyPQSGetBaseDataResponse(PQSGetBaseDataRequest request, PQSResponse response) : base(request, response, null)
    {
    }

    public override PQZStatus ExtractGetParametersOrError(out IEnumerable<PQBIAxisData> parameters)
    {
        parameters = new List<PQBIAxisData>();
        return PQZStatus.OK;
    }
}