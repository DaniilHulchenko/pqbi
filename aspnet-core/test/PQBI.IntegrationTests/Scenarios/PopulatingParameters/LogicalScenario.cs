using FluentAssertions;
using PQBI.Sapphire.Options;
using PQS.Data.Common;
using PQS.Data.Events.Enums;
using PQS.Data.Measurements.Enums;

namespace PQBI.IntegrationTests.Scenarios.PopulatingParameters;
public class LogicalScenario : PopulateParamertAndBaseOnScenario
{
    public override string ScenarioName => "Creating Logical Branch of Options";

    public override string Description => "Creating and Testing Logical Branch of Options";

    protected override async Task RunScenario()
    {
        var root = new OptionTree();
        var parameters = UnderLogical();


        parameters.Count().Should().Be(81);

        foreach (var parameter in parameters)
        {
            var parameterNode = new ParameterNode(parameter.Group, parameter.Description);
            var phases = FillPhasesColumnSortedByFeederNetwork(parameter.Group);

            foreach (var phase in phases)
            {
                var phaseNode = new PhaseNode(phase.Value, phase.Description);
                parameterNode.Phases.Add(phaseNode);
            }

            if (parameterNode.Description.Equals("Frequency By Phase", StringComparison.OrdinalIgnoreCase))
            {
                parameterNode.Phases.Count.Should().Be(20);
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
        string input = @"
DEMANDINACT--Active Demand Consumption (In)
DEMANDOUTACT--Active Demand Production (Out)
ENERGYACTCOUNTER--Active Energy Counter
ENERGYINACTCOUNTER--Active Energy Counter (In)
ENERGYOUTACTCOUNTER--Active Energy Counter (Out)
ACTPWR--Active Power
ACTPWRF--Active Power - Fundamental
ACTPWRH--Active Power - Harmonics Aggregation
PWRHRMSACT--Active Power - per Harmonic
ADMIMG--Admittance Imaginary
ADMREAL--Admittance Real
DEMANDAPP--Apparent Demand
ENERGYAPP--Apparent Energy
ENERGYAPPCOUNTER--Apparent Energy Counter
APPPWR--Apparent Power
APPPWRF--Apparent Power - Fundamental
APPPWRH--Apparent Power - Harmonics Aggregation
PWRHRMSAPP--Apparent Power - per Harmonic
CRESTF--Crest Factor
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
IMPAMP--Impedance Amplitude
IMPAMPANG--Impedance Amplitude and Angle
IMPANG--Impedance Angle
IMPIMG--Impedance Imaginary
IMPREAL--Impedance Real
KF--K Factor
NSEQ--Negative Sequence (U2)
UNBAL--Negative Sequence Unbalance (U2/U1)
UNBALIL--Negative Sequence Unbalance Based On IL (U2/IL)
OVERDEV--Over Deviation
PHASORANG--Phasor Angle
PLT--PLT
PSEQ--Positive Sequence (U1)
PF--Power Factor
PFF--Power Factor - Fundamental
PFH--Power Factor - Harmonics Aggregation
PWRHRMSPF--Power Factor - per harmonic
PST--PST
DEMANDINREA--Reactive Demand Consumption (In)
DEMANDOUTREA--Reactive Demand Production (Out)
ENERGYREACOUNTER--Reactive Energy Consumption (In)
ENERGYINREACOUNTER--Reactive Energy Counter (In)
ENERGYOUTREACOUNTER--Reactive Energy Counter (Out)
REAPWR--Reactive Power
REAPWRF--Reactive Power - Fundamental
REAPWRH--Reactive Power - Harmonics Aggregation
PWRHRMSREACT--Reactive Power - per Harmonic
R--Resistance
RF--Resistance - Fundamental
RH--Resistance - Harmonics Aggregation
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
PFTRUE--True Power Factor (λ)
PFTRUESOURCE--True Power Factor (λ)  Source
UNDERDEV--Under Deviation
HRMSINCYC--Voltage and Current - Harmonics Amplitude
HRMSPERINCYC--Voltage and Current - Harmonics Amplitude (%)
WAVE--Waveform
WAVEF--Waveform - Fundamental
ZSEQ--Zero Sequence (U0)
ZUNBAL--Zero Sequence Unbalance (U0/U1)
ZUNBALIL--Zero Sequence Unbalance Based On IL (U0/IL)
";

        return CreateGroupItems(input);
    }


}
