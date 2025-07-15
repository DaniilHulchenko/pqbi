using System;
using System.Text;
using PQS.Data.Measurements.StandardParameter;
using PQS.Data.Measurements.CustomParameter;
using PQS.Data.Common.Units;
using PQS.Data.Measurements.Utils;
using PQS.Translator;
using PQS.Data.Measurements;
using PQS.Data.Measurements.Enums;

namespace PQS.CommonReport
{
    public static class MsrPrmTranslator
    {
        public static string GetMsrPrmGroupName(MeasurementParameterBase msrPrm)
        {
            string displayName;
            string groupStr = msrPrm.GetGroupName();
            Group group;
            if (Enum.TryParse<Group>(groupStr, out group))
            {
                displayName = group.Description();
            }
            else
            {
                displayName = groupStr;
            }
            return displayName;
        }

        public static string GetMsrPrmNameForGraphLegend(MeasurementParameterBase msrPrm, bool isShowHarmonicNum = false)
        {
            string msrPrmNameForGraphLegend;
            if (msrPrm is NetworkFeederMeasurementParameter)
            {
                NetworkFeederMeasurementParameter networkFeederMeasurementParameter = msrPrm as NetworkFeederMeasurementParameter;
                msrPrmNameForGraphLegend = GetMsrPrmGroupAndPhaseName(networkFeederMeasurementParameter);
            }
            else if (msrPrm is ChannelMeasurementParameter)
            {
                ChannelMeasurementParameter channelMeasurementParameter = msrPrm as ChannelMeasurementParameter;
                msrPrmNameForGraphLegend = GetMsrPrmGroupAndNumberName(channelMeasurementParameter);
            }
            else
            {
                msrPrmNameForGraphLegend = GetMsrPrmGroupName(msrPrm);
            }
            string msrPrmBase = GetPrmBase(msrPrm);

            string msrPrmQuantity = string.Empty;
            switch (msrPrm.Quantity)
            {
                case QuantityEnum.QMAX:
                    msrPrmQuantity = QuantityEnum.QMAX.Description();
                    break;
                case QuantityEnum.QMIN:
                    msrPrmQuantity = QuantityEnum.QMIN.Description();
                    break;
                case QuantityEnum.QAVG:
                    msrPrmQuantity = QuantityEnum.QAVG.Description();
                    break;
                default:
                    break;
            }

            StringBuilder builder = new StringBuilder();
            if (!isShowHarmonicNum || msrPrm.HarmonicNum == null)
                msrPrmNameForGraphLegend = builder.Append(msrPrmNameForGraphLegend).Append(" (").Append(msrPrmBase).Append(") ").Append(msrPrmQuantity).ToString();
            else
                msrPrmNameForGraphLegend = builder.Append(msrPrmNameForGraphLegend).Append(" (").Append(msrPrmBase).Append(") ").Append(msrPrmQuantity).Append(" ").Append(msrPrm.HarmonicNum).ToString();

            return msrPrmNameForGraphLegend;
        }

        public static string GetMsrPrmNameForScatterGraphLegend(MeasurementParameterBase msrPrm)
        {
            string msrPrmNameForGraphLegend;
            if (msrPrm is NetworkFeederMeasurementParameter)
            {
                NetworkFeederMeasurementParameter networkFeederMeasurementParameter = msrPrm as NetworkFeederMeasurementParameter;
                msrPrmNameForGraphLegend = GetMsrPrmGroupAndPhaseName(networkFeederMeasurementParameter);
            }
            else if (msrPrm is ChannelMeasurementParameter)
            {
                ChannelMeasurementParameter channelMeasurementParameter = msrPrm as ChannelMeasurementParameter;
                msrPrmNameForGraphLegend = GetMsrPrmGroupAndNumberName(channelMeasurementParameter);
            }
            else
            {
                msrPrmNameForGraphLegend = GetMsrPrmGroupName(msrPrm);
            }
            string msrPrmBase = GetPrmBase(msrPrm);
            StringBuilder builder = new StringBuilder();
            msrPrmNameForGraphLegend = builder.Append(msrPrmNameForGraphLegend).Append(" (").Append(msrPrmBase).Append(") ").Append(msrPrm.Quantity.Description()).ToString();
            return msrPrmNameForGraphLegend;
        }

        public static string GetMsrPrmNameForGraphAxis(MeasurementParameterBase msrPrm)
        {
            string msrPrmNameForGraphLegend;
            if (msrPrm is NetworkFeederMeasurementParameter)
            {
                NetworkFeederMeasurementParameter networkFeederMeasurementParameter = msrPrm as NetworkFeederMeasurementParameter;
                msrPrmNameForGraphLegend = GetMsrPrmGroupAndPhaseName(networkFeederMeasurementParameter);
            }
            else if (msrPrm is ChannelMeasurementParameter)
            {
                ChannelMeasurementParameter channelMeasurementParameter = msrPrm as ChannelMeasurementParameter;
                msrPrmNameForGraphLegend = GetMsrPrmGroupAndNumberName(channelMeasurementParameter);
            }
            else
            {
                msrPrmNameForGraphLegend = GetMsrPrmGroupName(msrPrm);
            }
            StringBuilder builder = new StringBuilder();
            msrPrmNameForGraphLegend = builder.Append(msrPrmNameForGraphLegend).ToString();
            return msrPrmNameForGraphLegend;
        }

        public static string GetMsrPhaseName(NetworkFeederMeasurementParameter networkFeederMeasurementParameter)
        {
            switch (networkFeederMeasurementParameter.Phase)
            {
                case PhaseMeasurementEnum.UV1N:
                    return "L1";
                case PhaseMeasurementEnum.UV2N:
                    return "L2";
                case PhaseMeasurementEnum.UV3N:
                    return "L3";
                case PhaseMeasurementEnum.UVN:
                    return "LN";
                case PhaseMeasurementEnum.UV1G:
                    return "L1";
                case PhaseMeasurementEnum.UV2G:
                    return "L2";
                case PhaseMeasurementEnum.UV3G:
                    return "L3";
                case PhaseMeasurementEnum.UV12:
                    return "L12";
                case PhaseMeasurementEnum.UV23:
                    return "L23";
                case PhaseMeasurementEnum.UV31:
                    return "L31";
                case PhaseMeasurementEnum.UI1:
                    return "L1";
                case PhaseMeasurementEnum.UI2:
                    return "L2";
                case PhaseMeasurementEnum.UI3:
                    return "L3";
                case PhaseMeasurementEnum.UIN:
                    return "LN";
                case PhaseMeasurementEnum.UIG:
                    return "LN";
                case PhaseMeasurementEnum.UI12:
                    return "L12";
                case PhaseMeasurementEnum.UI23:
                    return "L23";
                case PhaseMeasurementEnum.UI31:
                    return "L31";
                case PhaseMeasurementEnum.UFREQUENCY:
                    return "Frequency";
                case PhaseMeasurementEnum.UVDC:
                    return "LN";
                case PhaseMeasurementEnum.UIDC:
                    return "LN";
                case PhaseMeasurementEnum.UIAUX:
                    return "LN";
                case PhaseMeasurementEnum.UP1:
                    return "L1";
                case PhaseMeasurementEnum.UP2:
                    return "L2";
                case PhaseMeasurementEnum.UP3:
                    return "L3";
                case PhaseMeasurementEnum.UPN:
                    return "LN";
                case PhaseMeasurementEnum.UP12:
                    return "L12";
                case PhaseMeasurementEnum.UP23:
                    return "L23";
                case PhaseMeasurementEnum.UP31:
                    return "L31";
                case PhaseMeasurementEnum.UPDC:
                    return "LDC";
                case PhaseMeasurementEnum.UL1:
                    return "L1";
                case PhaseMeasurementEnum.UL2:
                    return "L2";
                case PhaseMeasurementEnum.UL3:
                    return "L3";
                case PhaseMeasurementEnum.ULN:
                    return "LN";
                case PhaseMeasurementEnum.UL12:
                    return "L12";
                case PhaseMeasurementEnum.UL23:
                    return "L23";
                case PhaseMeasurementEnum.UL31:
                    return "L31";
                case PhaseMeasurementEnum.UV123:
                case PhaseMeasurementEnum.UV123LL:
                    return "Total";
                case PhaseMeasurementEnum.UI123:
                    return "Total";
                case PhaseMeasurementEnum.UP123:
                    return "Total";
                case PhaseMeasurementEnum.UL123:
                    return "Total";
                case PhaseMeasurementEnum.UVLEAK:
                    return "Total";
                case PhaseMeasurementEnum.UILEAK:
                    return "Total";
                case PhaseMeasurementEnum.None:
                    return "Total";
                default:
                    break;
            }
            return "";
        }

        public static string GetMsrPrmGroupAndPhaseName(NetworkFeederMeasurementParameter networkFeederMeasurementParameter)
        {
            string groupName = GetMsrPrmGroupName(networkFeederMeasurementParameter);
            if (networkFeederMeasurementParameter.Phase != Data.Measurements.Enums.PhaseMeasurementEnum.UFREQUENCY)
            {
                string phase = networkFeederMeasurementParameter.Phase.Description();               
                return groupName + " " + phase;
            }
            else
            {
                return groupName;
            }
        }

        public static string GetMsrPrmGroupAndNumberName(ChannelMeasurementParameter channelMeasurementParameter)
        {
            string groupName = GetMsrPrmGroupName(channelMeasurementParameter);
            uint channelNum = channelMeasurementParameter.ChannelNumber;
        
            //if (channelMeasurementParameter.HarmonicNum != null)
            //    return groupName + $" H{channelMeasurementParameter.HarmonicNum} " + channelNum;
            
            return groupName + " " + channelNum;
        }

        public static string GetMsrPrmUnits(MeasurementParameterBase msrPrm)
        {
            string unitCode = GetUnitsCode(msrPrm);
            UnitsEnum unit;
            if (Enum.TryParse<UnitsEnum>(unitCode, out unit))
            {
                return unit.Description();
            }
            else
            {
                return unitCode;
            }
        }

        private static string GetUnitsCode(MeasurementParameterBase msrPrm)
        {
            string unitsCode = string.Empty;

            if (msrPrm is ChannelMeasurementParameter)
            {
                ChannelMeasurementParameter channelParam = msrPrm as ChannelMeasurementParameter;
                if (channelParam != null)
                {
                    ///If we want also channels we need channel type, in order to get it we need to ask server for mapping ******
                    //UnitsEnum unit = GetUnitsUtility.GetUnitsFromGroupAndPhase(channelParam.Group, null, ChannelType.Value);
                    //unitsCode = unit.ToString();
                }

            }
            else if (msrPrm is NetworkFeederMeasurementParameter)
            {
                NetworkFeederMeasurementParameter networkFeederParam = msrPrm as NetworkFeederMeasurementParameter;
                if (networkFeederParam != null)
                {
                    UnitsEnum unit = UnitsUtility.GetUnitsFromGroupAndPhase(networkFeederParam.Group, networkFeederParam.Phase);
                    unitsCode = unit.ToString();
                }
            }
            else // custom parameter
            {
                CustomMeasurementParameter cmp = msrPrm as CustomMeasurementParameter;
                if (cmp.Details == null || cmp.Details.Units == null)
                {
                    return string.Empty;
                }
                else
                {
                    return cmp.Details.Units.Name;
                }
            }
            return unitsCode;
        }

        public static string GetPrmBase(MeasurementParameterBase msrPrm)
        {
            return msrPrm.CalculationBaseClass.CalculationBaseEnum.Description();
        }

    }
}
