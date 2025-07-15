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

namespace PQBI.Requests
{
    public class PQSGetComponentsRequest : PQSCommonRequest
    {
        public PQSGetComponentsRequest(string session) : base(session)
        {

            AddConfigurations();
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

    }

    public class PQSGetComponentsResponse : PQSCommonResponse<PQSGetComponentsRequest>
    {
        public PQSGetComponentsResponse(PQSGetComponentsRequest request, PQSResponse response) : base(request, response)
        {

        }

        public bool TryGetMap(out IEnumerable<ComponentSlimDto> components)
        {
            components = null;
            var result = false;
            var componentList = new List<ComponentSlimDto>();

            if (TryExtractObjectsResponseRecord(out var operationResponseRecord))
            {

                foreach (var val in operationResponseRecord.ObjectsAndConfigurations)
                {
                    val.Value.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_GUID), out var guid);

                    var feedersDto = new List<FeederDescriptionDto>();
                    var channelsDto = new List<ChannelDescriptionDto>();

                    string map;
                    if (val.Value.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_SYSTEM_ELECTRICAL_MAP), out map))
                    {
                        Dictionary<uint, TopologyEnum> feederToTopology;
                        Dictionary<uint, List<uint>> networksToFeeders;
                        List<uint> networksWithFeeders;
                        Dictionary<ChannelTypeEnum, List<uint>> typeToChannel;
                        Dictionary<uint, TagConfigEnum> channelToTypeMap;
                        Dictionary<uint, string> channelToNames;
                        Dictionary<uint, string> networksNames;
                        Dictionary<uint, string> feederNames;
                        Dictionary<uint, UnitBase> channelsToUnits;
                        HashSet<uint> allExistingChannels;
                        XMLSystemElectricalMappingUtils.ReadNetworksWithFeedersMap(map,
                            out feederToTopology,
                            out networksToFeeders,
                            out networksWithFeeders,
                            out typeToChannel,
                            out channelToTypeMap,
                            out channelToNames,//From channelToTypeMap id
                            out networksNames,
                            out feederNames,
                            out channelsToUnits,
                            out allExistingChannels);
                        Dictionary<uint, ChannelTypeEnum> channelToType = new Dictionary<uint, ChannelTypeEnum>();
                        foreach (var item in typeToChannel)
                        {
                            foreach (uint channelID in item.Value)
                            {
                                channelToType[channelID] = item.Key;
                            }
                        }

                        var channelKeys = channelToTypeMap.Keys.ToArray();

                        foreach (var key in channelKeys)
                        {
                            var channelName = channelToNames[key];
                            var channel = new ChannelDescriptionDto(key, channelName);
                            channelsDto.Add(channel);
                        }


                        foreach (var (key, feederName) in feederNames)
                        {
                            var fedder = new FeederDescriptionDto(key, feederName);
                            feedersDto.Add(fedder);
                        }
                    }

                    var list = new HashSet<string>();
                    var additionalParameters = new List<AdditionalData>();

                    if (val.Value.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_ALL_PARAMETERS), out var prms))
                    {
                        var parsedPArams = ParameterOfUnitUtils.ReadParameterOfUnits(prms);

                        foreach (var parsedPAram in parsedPArams)
                        {
                            if (parsedPAram.ParameterName.StartsWith("STD", StringComparison.OrdinalIgnoreCase) || parsedPAram.ParameterName.StartsWith("MULTI", StringComparison.OrdinalIgnoreCase))
                            {
                                list.Add(parsedPAram.ParameterName);
                            }
                            else
                            {
                                MeasurmentsParameterDetails ptrr = PQZxmlReader.ReadMeasurementParameterDetails(parsedPAram.ParamDetails);
                                string[] props = parsedPAram.ParameterName.Split('_');
                                additionalParameters.Add(new AdditionalData { MeasurmentsParameterDetails = ptrr, PropertyName = props.FirstOrDefault(), Base = props.LastOrDefault() });

                            }
                        }
                    }

                    var componentName = "Component has no name";
                    if (val.Value.TryGetConfigurationValue<string>(StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_VIRTUAL_NAME), out var compName))
                    {
                        componentName = compName.Trim();
                    }

                    componentList.Add(new ComponentSlimDto(guid.ToString(), componentName, feedersDto, channelsDto, additionalParameters));
                }

                components = componentList.ToArray();
                result = true;
            }

            return result;
        }

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