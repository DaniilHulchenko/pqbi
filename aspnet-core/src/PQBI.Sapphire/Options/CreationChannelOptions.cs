using Castle.MicroKernel.Registration;
using PQBI.PQS;
using PQBI.Sapphire.Options;
using PQS.Data.Measurements.CustomParameter;
using PQS.Data.Measurements.Enums;
using System.Xml.Linq;

namespace PQBI.IntegrationTests.Scenarios.PopulatingParameters;

public class CreationChannelOptions : PopulateBasicParameters
{

    public CreationChannelOptions()
    {
        Initialize();
    }

    private void Initialize()
    {

        CreateBasedOnGroups();
    }

    public StaticTreeNode CreateDataAsync(List<CustomCalculationBaseInfo> customCalcBaseInfoList)
    {
        var channelParameters = new List<ParameterNode>();
        var tree = new StaticTreeNode { Value = StaticTreeNode.ChannelLabel, Description = StaticTreeNode.ChannelLabel };

        var parameters = UnderLogical();

        foreach (var parameter in parameters)
        {
            var root = new StaticTreeNode { Value = parameter.Group.ToString(), Description = parameter.Description };//, IsHarmonic = parameter.IsHarmonic };
            //var (minHarmonic, maxHarmonic) = GetHarmonicMinMaxForCurrentSelection(parameter.Group);
            //root.Range = $"{minHarmonic}:{maxHarmonic}";

            if (parameter.IsHarmonic)
            {
                var harmonic = GetHarmonicMinMaxForCurrentSelection(parameter.Group);
                root.Range = $"{harmonic.MinHarmonic}:{harmonic.MaxHarmonic}";
            }

            var baseOns = FillBasedOnColumnSortedByFeederNetwork(parameter.Group);

            // Precompute which OldCalculationBaseEnum are actually present in baseOns
            var allowedOldBases = new HashSet<OldCalculationBaseEnum>(
                baseOns.Select(b => MapToOldEnum(b.Value?.CalculationBase?.CalculationBaseEnum))
                       .Where(e => e.HasValue)
                       .Select(e => e!.Value));

            // Build base-on nodes once
            var baseOnChildren = baseOns
                .Select(b => NewNode(b.Value?.ToString() ?? string.Empty, b.Description ?? string.Empty))
                .ToList();

            // Build custom base nodes (dedup by Value string)
            var customBaseNodes = new List<StaticTreeNode>();
            //var seenCustomValues = new HashSet<string>(StringComparer.Ordinal);

            foreach (var ci in customCalcBaseInfoList)
            {
                var oldEnum = ci.CalcBase?.OldCalculationBaseEnum;
                if (oldEnum == null || !allowedOldBases.Contains(oldEnum.Value))
                    continue;

                var valString = ci.WindowInterval == null
                    ? ci.CalcBase.ToString()
                    : $"{ci.CalcBase}_{ci.WindowInterval}";

                if (string.IsNullOrEmpty(valString))
                    //if (string.IsNullOrEmpty(valString) || !seenCustomValues.Add(valString))
                    continue;

                customBaseNodes.Add(NewNode(valString, ci.PresentedName));
            }


            if (baseOns.Count > 0)
            {
                for (var i = 1; i < 369; i++)
                {
                    var channelNode = new StaticTreeNode { Value = $"CH_{i}", Description = $"Channel {i}" };
                    root.Children.Add(channelNode);

                    foreach (var baseOn in baseOnChildren)
                    {                        
                        channelNode.Children.Add(baseOn);
                    }

                    foreach (var baseOn in baseOnChildren)
                    {
                        channelNode.Children.Add(baseOn);
                    }
                }

                tree.Children.Add(root);
            }
        }
     
        return tree;

        static StaticTreeNode NewNode(string value, string description) =>
           new StaticTreeNode { Value = value, Description = description, Children = new List<StaticTreeNode>() };
       

        static OldCalculationBaseEnum? MapToOldEnum(CalculationBase? calcBase) =>
            calcBase switch
            {
                CalculationBase.BHCYC => OldCalculationBaseEnum.BHCYC,
                CalculationBase.BCYC => OldCalculationBaseEnum.BCYC,
                CalculationBase.B200MS => OldCalculationBaseEnum.B200MS,
                _ => (OldCalculationBaseEnum?)null
            };
    }

    protected override List<ParameterGroupItem> UnderLogical()
    {
        string data = @"ENERGYACTCOUNTER--Active Energy Counter
ENERGYINACTCOUNTER--Active Energy Counter (In)
ENERGYOUTACTCOUNTER--Active Energy Counter (Out)
ADMIMG--Admittance Imaginary
ADMREAL--Admittance Real
AI--Analog Input
AO--Analog Output
ENERGYAPPCOUNTER--Apparent Energy Counter
CRESTF--Crest Factor
DI--Digital Input
DO--Digital Output
DF--Distortion Factor
FREQ--Frequency
FREQBYPHASE--Frequency By Phase
HRAWDATA--Harmonics Raw Data
HRAWDATAIMAGE--Harmonics Raw Data Image
HRAWDATAREAL--Harmonics Raw Data Real
WAVEH--Harmonics Waveform
HDD--HDD
HRMS--IEC 61000-4-30 Voltage and Current - Harmonics Amplitude
HRMSPER--IEC 61000-4-30 Voltage and Current - Harmonics Amplitude (%)
IHRMS--IEC 61000-4-30 Voltage and current - Inter-Harmonics Amplitude
IHRMSPER--IEC 61000-4-30 Voltage and current - Inter-Harmonics Amplitude (%)
IMPIMG--Impedance Imaginary
IMPREAL--Impedance Real
KF--K Factor
UNBALIL--Negative Sequence Unbalance Based On IL (U2/IL)
OVERDEV--Over Deviation
PHASORANG--Phasor Angle
PLT--PLT
PST--PST
ENERGYREACOUNTER--Reactive Energy Consumption (In)
ENERGYINREACOUNTER--Reactive Energy Counter (In)
ENERGYOUTREACOUNTER--Reactive Energy Counter (Out)
RMS--RMS
RMSFUND--RMS - Fundamental
RMSNONFUND--RMS - non-Fundamental
RMSRW--RMS - Rolling window
TDD--TDD
THD--THD
THDEVEN--THD - Even Harmonics
THDI--THD - Inter Harmonics
THDODD--THD - Odd Harmonics
TIF--TIF
PFTRUESOURCE--True Power Factor (λ) Source
UNDERDEV--Under Deviation
HRMSINCYC--Voltage and Current - Harmonics Amplitude
HRMSPERINCYC--Voltage and Current - Harmonics Amplitude (%)
WAVE--Waveform
WAVEF--Waveform - Fundamental
ZUNBALIL--Zero Sequence Unbalance Based On IL (U0/IL)";

        return CreateGroupItems(data);
    }
}
