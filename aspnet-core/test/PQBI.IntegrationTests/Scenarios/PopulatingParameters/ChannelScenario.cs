using FluentAssertions;
using PQBI.Sapphire.Options;
using PQS.Data.Events.Enums;

namespace PQBI.IntegrationTests.Scenarios.PopulatingParameters;

public class ChannelScenario : PopulateParamertAndBaseOnScenario
{
    public override string ScenarioName => "Creating Channel Branch of Options";
    public override string Description => "Creating and Testing Channel Branch of Options";

    protected override async Task RunScenario()
    {
        var root = new OptionTree();
        var parameters = UnderLogical();

        parameters.Count().Should().Be(51);

        foreach (var parameter in parameters)
        {
            var parameterNode = new ParameterNode(parameter.Group, parameter.Description);

            for (var i = 1; i < 10001; i++)
            {
                var channel = new ChannelNodeDto(i, i.ToString());
                parameterNode.Channels.Add(channel);
            }

            var baseOns = FillBasedOnColumnSortedByFeederNetwork(parameter.Group);
            foreach (var baseOn in baseOns)
            {
                var baseOnNode = new BaseOnNode(baseOn.Description);
                parameterNode.BaseOns.Add(baseOnNode);
            }

            root.LogicalParameters.Add(parameterNode);
        }
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
