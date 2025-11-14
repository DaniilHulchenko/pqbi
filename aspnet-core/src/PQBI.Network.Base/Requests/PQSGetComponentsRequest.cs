using PQS.Data.Configurations.Enums;
using PQS.Data.Configurations;
using PQS.Data.RecordsContainer.Records;
using PQS.Data.RecordsContainer;
using PQS.Data.Common.Units;
using PQS.Data.Configurations.Utilities;
using PQBI.PQS;
using PQS.Data.Measurements.ParameterOfUnit;
using PQS.PQZxml;
using PQS.Data.Measurements;
using PQS.Data.Measurements.CustomParameter;
using PQS.Data.Measurements.Enums;

namespace PQBI.Requests
{
    public class PQSGetComponentsRequest : PQSCommonRequest
    {
        public PQSGetComponentsRequest(string session) : base(session)
        {
            AddConfigurations();
        }

        public PQSGetComponentsRequest(bool isTreeMap, bool isStaticTreeMap, string session) : base(session)
        {
            if (isTreeMap)
                AddTreeMapConfigurations();
            else if (isStaticTreeMap)
                AddStaticTreeConfigurations();
            else
                AddConfigurations();
        }

        protected void AddStaticTreeConfigurations()
        {
            var configurations = new List<ConfigurationParameterBase>();
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_GUID));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_UNIT_TYPE));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_ALL_PARAMETERS));

            var compRecord = new ObjectsRequestRecord(null, ObjectType.PhysicalAndVirtualComponents, ObjectFilterType.NoFilter, null, configurations);

            AddRecord(compRecord);
        }

        protected override void AddConfigurations()
        {
            var configurations = new List<ConfigurationParameterBase>();

            //configurations.Add(new StandardConfiguration(StandardConfigurationEnum.STD_VIRTUAL_NAME, PQSType.STRING));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_VIRTUAL_NAME));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_GEOGRAPHIC_COORDINATE));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_UNIT_TYPE));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_DEVICE_IP));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_FEEDERS_NAMES_AND_NETWORKS));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_SYSTEM_ELECTRICAL_MAP));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_RUNNING_EVENTS));
            //configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_TOPOLOGY_TYPE));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_GUID));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_ALL_PARAMETERS));
            //configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_ALL_PARAMETERS));

            var compRecord = new ObjectsRequestRecord(null, ObjectType.PhysicalAndVirtualComponents, ObjectFilterType.NoFilter, null, configurations);

            AddRecord(compRecord);
        }

        protected void AddTreeMapConfigurations()
        {
            var configurations = new List<ConfigurationParameterBase>();
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_VIRTUAL_NAME));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_UNIT_TYPE));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_SYSTEM_ELECTRICAL_MAP));
            configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_GUID));
            //configurations.Add(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_ALL_PARAMETERS));
            var compRecord = new ObjectsRequestRecord(null, ObjectType.PhysicalAndVirtualComponents, ObjectFilterType.NoFilter, null, configurations);

            AddRecord(compRecord);
        }

    }

    public class PQSGetComponentsResponse : PQSCommonResponse<PQSRequestBase>
    {
        public PQSGetComponentsResponse(PQSGetComponentsRequest request, PQSResponse response) : base(request, response)
        {

        }

        public bool TryGetMap(out IEnumerable<ComponentSlimDto> components)
        {
            components = null;

            if (!TryExtractObjectsResponseRecord(out var operationResponseRecord) ||
                operationResponseRecord?.ObjectsAndConfigurations == null ||
                operationResponseRecord.ObjectsAndConfigurations.Count == 0)
            {
                return false;
            }

            var componentList = new List<ComponentSlimDto>(operationResponseRecord.ObjectsAndConfigurations.Count);

            foreach (var val in operationResponseRecord.ObjectsAndConfigurations.Values)
            {
                // Get GUID
                val.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_GUID), out var guid);

                // Get component name
                var componentName = val.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_VIRTUAL_NAME), out var compName)
                    ? compName?.Trim() ?? "Component has no name"
                    : "Component has no name";

                // Feeders and channels
                var feedersDto = new List<FeederDescriptionDto>();
                var channelsDto = new List<ChannelDescriptionDto>();

                if (val.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_SYSTEM_ELECTRICAL_MAP), out var map))
                {
                    XMLSystemElectricalMappingUtils.ReadNetworksWithFeedersMap(
                        map,
                        out var feederToTopology,
                        out var networksToFeeders,
                        out var networksWithFeeders,
                        out var typeToChannel,
                        out var channelToTypeMap,
                        out var channelToNames,
                        out var networksNames,
                        out var feederNames,
                        out var channelsToUnits,
                        out var allExistingChannels);

                    // Channels
                    foreach (var key in channelToTypeMap.Keys)
                    {
                        if (channelToNames.TryGetValue(key, out var channelName))
                            channelsDto.Add(new ChannelDescriptionDto(key, channelName));
                    }

                    // Feeders
                    foreach (var (key, feederName) in feederNames)
                    {
                        feedersDto.Add(new FeederDescriptionDto(key, feederName));
                    }
                }

                // Additional parameters
                //var additionalParameters = new List<AdditionalData>();
                //if (val.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_ALL_PARAMETERS), out var prms))
                //{
                //    foreach (var parsedParam in ParameterOfUnitUtils.ReadParameterOfUnits(prms))
                //    {
                //        if (!parsedParam.ParameterName.StartsWith("STD", StringComparison.OrdinalIgnoreCase) &&
                //            !parsedParam.ParameterName.StartsWith("MULTI", StringComparison.OrdinalIgnoreCase))
                //        {
                //            var details = PQZxmlReader.ReadMeasurementParameterDetails(parsedParam.ParamDetails);
                //            var props = parsedParam.ParameterName.Split('_');
                //            additionalParameters.Add(new AdditionalData
                //            {
                //                MeasurmentsParameterDetails = details,
                //                PropertyName = props.FirstOrDefault(),
                //                Base = props.LastOrDefault()
                //            });
                //        }
                //    }
                //}

                componentList.Add(new ComponentSlimDto(
                    guid ?? string.Empty,
                    componentName,
                    feedersDto,
                    channelsDto
                ));
            }

            components = componentList;
            return true;
        }

        public (IEnumerable<ComponentSlimDto>, HashSet<CustomCalculationBaseInfo>) PopulateWithAdditionalData(HashSet<CustomCalculationBaseInfo> serverCustomCalculationBases)
        {
            var componentList = new List<ComponentSlimDto>();

            HashSet<CustomCalculationBaseInfo> customCalcBaseInfo = new HashSet<CustomCalculationBaseInfo>(new CalcBaseEqualityComparer());
            if (TryExtractObjectsResponseRecord(out var operationResponseRecord))
            {
                foreach (var val in operationResponseRecord.ObjectsAndConfigurations)
                {
                    var additionalParameters = new List<AdditionalData>();

                    val.Value.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_GUID), out var guid);                   

                    if (val.Value.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_ALL_PARAMETERS), out var prms))
                    {
                        var parsedPArams = ParameterOfUnitUtils.ReadParameterOfUnits(prms);

                        foreach (var parsedPAram in parsedPArams)
                        {
                            if (!parsedPAram.ParameterName.StartsWith("STD", StringComparison.OrdinalIgnoreCase) && !parsedPAram.ParameterName.StartsWith("MULTI", StringComparison.OrdinalIgnoreCase))
                            {
                                MeasurmentsParameterDetails ptrr = PQZxmlReader.ReadMeasurementParameterDetails(parsedPAram.ParamDetails);
                                string[] props = parsedPAram.ParameterName.Split('_');
                                additionalParameters.Add(new AdditionalData { MeasurmentsParameterDetails = ptrr, PropertyName = props.FirstOrDefault(), Base = props.LastOrDefault() });
                            }
                            else if (parsedPAram.ParameterName.Contains("_OB"))
                            {
                                string[] msrPrmStrArray = parsedPAram.ParameterName.Split('_');
                                MeasurementParameterBase msrPrmBase = MeasurementParameterFactory.GenerateNewMesurmentParameterWithoutSplit(msrPrmStrArray);

                                CustomCalculationBaseInfo compCalcBase = new() { CalcBase = msrPrmBase.CalculationBaseClass, WindowInterval = msrPrmBase.PrmWindowInterval };
                                if (serverCustomCalculationBases.TryGetValue(compCalcBase, out var serverCalcBase))
                                {
                                    compCalcBase = serverCalcBase;
                                }
                                else
                                {
                                    compCalcBase.GenerateDefaultPresentedName();
                                }
                                customCalcBaseInfo.Add(compCalcBase);

                            }
                        }
                    }

                    componentList.Add(new ComponentSlimDto(guid.ToString(), additionalParameters));
                }
            }
            return (componentList, customCalcBaseInfo);
        }

        //public bool TryGetMap(out IEnumerable<ComponentSlimDto> components)
        //{
        //    components = null;
        //    var result = false;
        //    var componentList = new List<ComponentSlimDto>();

        //    if (TryExtractObjectsResponseRecord(out var operationResponseRecord))
        //    {

        //        foreach (var val in operationResponseRecord.ObjectsAndConfigurations)
        //        {
        //            val.Value.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_GUID), out var guid);

        //            var feedersDto = new List<FeederDescriptionDto>();
        //            var channelsDto = new List<ChannelDescriptionDto>();

        //            string map;
        //            if (val.Value.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_SYSTEM_ELECTRICAL_MAP), out map))
        //            {
        //                Dictionary<uint, TopologyEnum> feederToTopology;
        //                Dictionary<uint, List<uint>> networksToFeeders;
        //                List<uint> networksWithFeeders;
        //                Dictionary<ChannelTypeEnum, List<uint>> typeToChannel;
        //                Dictionary<uint, TagConfigEnum> channelToTypeMap;
        //                Dictionary<uint, string> channelToNames;
        //                Dictionary<uint, string> networksNames;
        //                Dictionary<uint, string> feederNames;
        //                Dictionary<uint, UnitBase> channelsToUnits;
        //                HashSet<uint> allExistingChannels;
        //                XMLSystemElectricalMappingUtils.ReadNetworksWithFeedersMap(map,
        //                    out feederToTopology,
        //                    out networksToFeeders,
        //                    out networksWithFeeders,
        //                    out typeToChannel,
        //                    out channelToTypeMap,
        //                    out channelToNames,//From channelToTypeMap id
        //                    out networksNames,
        //                    out feederNames,
        //                    out channelsToUnits,
        //                    out allExistingChannels);
        //                Dictionary<uint, ChannelTypeEnum> channelToType = new Dictionary<uint, ChannelTypeEnum>();
        //                foreach (var item in typeToChannel)
        //                {
        //                    foreach (uint channelID in item.Value)
        //                    {
        //                        channelToType[channelID] = item.Key;
        //                    }
        //                }

        //                var channelKeys = channelToTypeMap.Keys.ToArray();

        //                foreach (var key in channelKeys)
        //                {
        //                    var channelName = channelToNames[key];
        //                    var channel = new ChannelDescriptionDto(key, channelName);
        //                    channelsDto.Add(channel);
        //                }


        //                foreach (var (key, feederName) in feederNames)
        //                {
        //                    var fedder = new FeederDescriptionDto(key, feederName);
        //                    feedersDto.Add(fedder);
        //                }
        //            }

        //            var list = new HashSet<string>();
        //            var additionalParameters = new List<AdditionalData>();

        //            if (val.Value.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_ALL_PARAMETERS), out var prms))
        //            {
        //                var parsedPArams = ParameterOfUnitUtils.ReadParameterOfUnits(prms);

        //                foreach (var parsedPAram in parsedPArams)
        //                {
        //                    if (parsedPAram.ParameterName.StartsWith("STD", StringComparison.OrdinalIgnoreCase) || parsedPAram.ParameterName.StartsWith("MULTI", StringComparison.OrdinalIgnoreCase))
        //                    {
        //                        list.Add(parsedPAram.ParameterName);
        //                    }
        //                    else
        //                    {
        //                        MeasurmentsParameterDetails ptrr = PQZxmlReader.ReadMeasurementParameterDetails(parsedPAram.ParamDetails);
        //                        string[] props = parsedPAram.ParameterName.Split('_');
        //                        additionalParameters.Add(new AdditionalData { MeasurmentsParameterDetails = ptrr, PropertyName = props.FirstOrDefault(), Base = props.LastOrDefault() });

        //                    }
        //                }
        //            }

        //            var componentName = "Component has no name";
        //            if (val.Value.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_VIRTUAL_NAME), out var compName))
        //            {
        //                componentName = compName.Trim();
        //            }

        //            componentList.Add(new ComponentSlimDto(guid.ToString(), componentName, feedersDto, channelsDto, additionalParameters));
        //        }

        //        components = componentList.ToArray();
        //        result = true;
        //    }

        //    return result;
        //}

        public IEnumerable<ComponentSlimDto> Components
        {
            get
            {
                IEnumerable<ComponentSlimDto> components = null;

                TryGetMap(out components);
                return components;
            }
        }
    }
}