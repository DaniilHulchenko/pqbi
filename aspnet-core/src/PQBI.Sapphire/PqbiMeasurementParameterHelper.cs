using PQS.CommonUI.Enums;
using PQS.Data.Measurements.CustomParameter;
using PQS.Data.Measurements.Enums;
using PQS.Data.Measurements.StandardParameter;
using PQS.Data.Measurements;
using PQS.Translator;

namespace PQBI.Sapphire
{
    //TODO nuget package
    //var dataSource = new MeasurementParameterDataSource(serverURL: null, compID: Guid.NewGuid(), parameter: param, quantity: QuantityType.Max, parameterDescriptor: null, color: null, isVisible: true);
    //var name = dataSource.GetLegendName();

    public class PqbiMeasurementParameterHelper
    {
        public static string GetLegendName(MeasurementParameterBase Parameter, QuantityType Quantity, string componentName = null, bool isDisplayFeeder = false, bool displayPhase = true, bool displayCalcBase = true, bool displayQuntity = true)
        {
            string legendName = string.Empty;

            if (Parameter is NetworkFeederMeasurementParameter)
            {
                NetworkFeederMeasurementParameter networkFeederParam = Parameter as NetworkFeederMeasurementParameter;

                legendName = networkFeederParam.Group.Description();

                if (displayPhase && networkFeederParam.Phase != PhaseMeasurementEnum.UFREQUENCY)
                {
                    legendName += " " + networkFeederParam.Phase.Description();
                }

                if (networkFeederParam.IsHarmonicParameter)
                {
                    string harmonic = TranslationManager.Instance.TranslateOrSetDefault("Common_HarmonicCaption", "Harmonic");
                    if (networkFeederParam.HarmonicNum.HasValue)
                    {
                        legendName += " " + harmonic + " " + networkFeederParam.HarmonicNum.Value;
                    }
                    else if (networkFeederParam.MinimumHarmonicNum.HasValue && networkFeederParam.MaximumHarmonicNum.HasValue)
                    {
                        legendName += " " + harmonic + " " + networkFeederParam.MinimumHarmonicNum.Value + "-" + networkFeederParam.MaximumHarmonicNum.Value;
                    }
                }

                if (displayCalcBase) legendName += " (" + networkFeederParam.CalculationBaseClass.CalculationBaseEnum.Description() + ")";
                if (displayQuntity) legendName += " " + Quantity.Description();

                if (!string.IsNullOrEmpty(componentName))
                {
                    legendName += ", " + componentName;
                }

                if (isDisplayFeeder)
                {
                    if (string.IsNullOrEmpty(componentName))
                    {
                        legendName += ", ";
                    }
                    else
                    {
                        legendName += " - ";
                    }

                    legendName += GetFeederNameOrSetDefault( Parameter);
                }
            }
            else if (Parameter is ChannelMeasurementParameter)
            {
                ChannelMeasurementParameter channelParameter = Parameter as ChannelMeasurementParameter;
                legendName = channelParameter.Group.Description() + " " + GetChannelNameOrSetDefault();

                if (channelParameter.IsHarmonicParameter)
                {
                    string harmonic = TranslationManager.Instance.TranslateOrSetDefault("Common_HarmonicCaption", "Harmonic");
                    if (channelParameter.HarmonicNum.HasValue)
                    {
                        legendName += " " + harmonic + " " + channelParameter.HarmonicNum.Value;
                    }
                    else if (channelParameter.MinimumHarmonicNum.HasValue && channelParameter.MaximumHarmonicNum.HasValue)
                    {
                        legendName += " " + harmonic + " " + channelParameter.MinimumHarmonicNum.Value + "-" + channelParameter.MaximumHarmonicNum.Value;
                    }
                }

                if (displayCalcBase) legendName += " (" + channelParameter.CalculationBaseClass.CalculationBaseEnum.Description() + ")";
                if (displayQuntity) legendName += " " + Quantity.Description();

                if (!string.IsNullOrEmpty(componentName))
                {
                    legendName += ", " + componentName;
                }
            }
            else if (Parameter is CustomMeasurementParameter)
            {
                string translatedParameter = TranslationManager.Instance.Translate(Parameter.GetGroupName()).ToString();

                // If the Custom Parameter's group failed to translate use its Name instead.
                if (translatedParameter == Parameter.GetGroupName())
                {
                    var customParam = Parameter as CustomMeasurementParameter;
                    if (!string.IsNullOrEmpty(customParam.Details.Name))
                    {
                        translatedParameter = customParam.Details.Name;
                    }
                }
                legendName = translatedParameter +
                    " (" + Parameter.CalculationBaseClass.CalculationBaseEnum.Description() + ")";
                if (displayQuntity) legendName += " " + Quantity.Description();

                if (!string.IsNullOrEmpty(componentName))
                {
                    legendName += ", " + componentName;
                }
            }
            //if (TimePercentLimit.HasValue)
            //{
            //    legendName += " - " + TimePercentLimit.Value + "%";
            //}
            return legendName;
        }

        private static string GetFeederNameOrSetDefault(MeasurementParameterBase Parameter)
        {
            //if (FeederName != null) return FeederName;

            //return TranslationManager.Instance.TranslateOrSetDefault("Common_Feeder", "Feeder") + " " + Feeder.ToString();
            return $"Feeder";
        }

        private static string GetChannelNameOrSetDefault()
        {
            //if (ChannelName != null) return ChannelName;

            //return TranslationManager.Instance.TranslateOrSetDefault("Common_Channel", "Channel") + " " + Channel.ToString();

            return "Channel";
        }
    }
}
