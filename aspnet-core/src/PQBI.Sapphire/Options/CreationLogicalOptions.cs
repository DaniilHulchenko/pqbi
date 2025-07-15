using PQBI.PQS;
using PQBI.Sapphire.Options;
using PQS.Data.Common;
using PQS.Data.Events.Enums;
using PQS.Data.Measurements.Enums;
using PQS.Translator;
using MeasurementGroup = PQS.Data.Measurements.Enums.Group;

namespace PQBI.IntegrationTests.Scenarios.PopulatingParameters;

public class CreationLogicalOptions : PopulateBasicParameters
{
    private List<PhaseMeasurementEnum> phasesGroup_VI;
    private List<PhaseMeasurementEnum> phasesGroup_Volt;
    private List<PhaseMeasurementEnum> phasesGroup_I;
    private List<PhaseMeasurementEnum> phasesGroup_123;
    private List<PhaseMeasurementEnum> phasesGroup_VI123;


    public CreationLogicalOptions()
    {
        Initialize();
    }

    private void Initialize()
    {
        Populate_PhasesGroup_VI();
        Populate_PhasesGroup_Volt();
        Populate_PhasesGroup_I();
        Populate_PhasesGroup_123();
        Populate_PhasesGroup_VI123();


        CreateBasedOnGroups();
    }
    

    public StaticTreeNode CreateDataAsync()
    {
        var parameters = UnderLogical();
        var tree = new StaticTreeNode { Value = StaticTreeNode.LogicalLabel , Description =  StaticTreeNode.LogicalLabel  };

        foreach (var parameter in parameters)
        {
            var node = new StaticTreeNode { Value = parameter.Group.ToString(), Description = parameter.Description };//  , IsHarmonic = parameter.IsHarmonic};
                                                                                                                      //var (minHarmonic, maxHarmonic) = GetHarmonicMinMaxForCurrentSelection(parameter.Group);
            if (parameter.IsHarmonic)
            {
                var harmonic = GetHarmonicMinMaxForCurrentSelection(parameter.Group);
                node.Range = $"{harmonic.MinHarmonic}:{harmonic.MaxHarmonic}";
            }

            var phases = FillPhasesColumnSortedByFeederNetwork(parameter.Group);
            var baseOns = FillBasedOnColumnSortedByFeederNetwork(parameter.Group);

            foreach (var phase in phases)
            {
                if(phase.Description.Contains("All"))
                {
                    continue;
                }
                var phaseRoot = new StaticTreeNode { Value = phase.Value.ToString(), Description = phase.Description };
                node.Children.Add(phaseRoot);

                foreach (var baseOn in baseOns)
                {
                    if (baseOn.Description.Contains("All"))
                    {
                        continue;
                    }
                    var baseOnRoot = new StaticTreeNode { Value = baseOn.Value.ToString(), Description = baseOn.Description };
                    phaseRoot.Children.Add(baseOnRoot);
                }
            }

            tree.Children.Add(node);
        }

        return tree;
    }

    #region Phase

    private List<ValueAndDescription<PhaseMeasurementWithDuplicationsEnum>> FillPhasesColumnSortedByFeederNetwork(MeasurementGroup _selectedParameter)
    {
        var AvailablePhases = new List<ValueAndDescription<PhaseMeasurementWithDuplicationsEnum>>();

        #region Add Phases with duplications (all voltages and all currents)
        ValueAndDescription<PhaseMeasurementWithDuplicationsEnum> phaseAndDescriptionAll = new ValueAndDescription<PhaseMeasurementWithDuplicationsEnum>();
        phaseAndDescriptionAll.Value = PhaseMeasurementWithDuplicationsEnum.AllDuplications;
        phaseAndDescriptionAll.Description = PhaseMeasurementWithDuplicationsEnum.AllDuplications.Description();
        phaseAndDescriptionAll.IsEnabled = true;
        ValueAndDescription<PhaseMeasurementWithDuplicationsEnum> phaseAndDescriptionVolt = new ValueAndDescription<PhaseMeasurementWithDuplicationsEnum>();
        phaseAndDescriptionVolt.Value = PhaseMeasurementWithDuplicationsEnum.VoltagesDuplications;
        phaseAndDescriptionVolt.Description = PhaseMeasurementWithDuplicationsEnum.VoltagesDuplications.Description();
        phaseAndDescriptionVolt.IsEnabled = true;
        ValueAndDescription<PhaseMeasurementWithDuplicationsEnum> phaseAndDescriptionCurr = new ValueAndDescription<PhaseMeasurementWithDuplicationsEnum>();
        phaseAndDescriptionCurr.Value = PhaseMeasurementWithDuplicationsEnum.CurrentsDuplications;
        phaseAndDescriptionCurr.Description = PhaseMeasurementWithDuplicationsEnum.CurrentsDuplications.Description();
        phaseAndDescriptionCurr.IsEnabled = true;

        switch (_selectedParameter)
        {
            case MeasurementGroup.RMS:
            case MeasurementGroup.RMSFUND:
            case MeasurementGroup.RMSNONFUND:
            case MeasurementGroup.RMSRW:
            case MeasurementGroup.THD:
            case MeasurementGroup.THDODD:
            case MeasurementGroup.THDEVEN:
            case MeasurementGroup.THDI:
            case MeasurementGroup.CRESTF:
            case MeasurementGroup.HRMS:
            case MeasurementGroup.HRMSINCYC:
            case MeasurementGroup.IHRMS:
            case MeasurementGroup.IHRMSB:
            case MeasurementGroup.HRMSPER:
            case MeasurementGroup.IHRMSPER:
            case MeasurementGroup.HRMSPERINCYC:
            case MeasurementGroup.WAVE:
            case MeasurementGroup.WAVEF:
            case MeasurementGroup.WAVEH:
            case MeasurementGroup.PHASORANG:
            case MeasurementGroup.TIF:
                AvailablePhases.Add(phaseAndDescriptionAll);
                AvailablePhases.Add(phaseAndDescriptionVolt);
                AvailablePhases.Add(phaseAndDescriptionCurr);
                break;
            case MeasurementGroup.KF:
            case MeasurementGroup.TEMPC:
            case MeasurementGroup.IL:
            case MeasurementGroup.TDD:
            case MeasurementGroup.HDD:
                AvailablePhases.Add(phaseAndDescriptionCurr);
                break;
            case MeasurementGroup.PST:
            case MeasurementGroup.PLT:
                AvailablePhases.Add(phaseAndDescriptionVolt);
                break;
            #region Empty cases
            case MeasurementGroup.HRMSG:
            case MeasurementGroup.IHRMSG:
            case MeasurementGroup.IHRMSA:
            case MeasurementGroup.IHRMSASIN:
            case MeasurementGroup.IHRMSACOS:
            case MeasurementGroup.IHRMSPA:
            case MeasurementGroup.IHRMSPASIN:
            case MeasurementGroup.IHRMSPACOS:
            case MeasurementGroup.HRMSANGREV1:
            case MeasurementGroup.HRMSANGREV1SIN:
            case MeasurementGroup.HRMSANGREV1COS:
            case MeasurementGroup.HRMSANG:
            case MeasurementGroup.HRMSANGSIN:
            case MeasurementGroup.HRMSANGCOS:
            case MeasurementGroup.HRMSPERG:
            case MeasurementGroup.FREQ:
            case MeasurementGroup.FREQBYPHASE:
            case MeasurementGroup.PEAK:
            case MeasurementGroup.PEAKF:
            case MeasurementGroup.PEAKH:
            case MeasurementGroup.UNDERDEV:
            case MeasurementGroup.OVERDEV:
            case MeasurementGroup.PINSTMAX:
            case MeasurementGroup.PHASORANGSIN:
            case MeasurementGroup.PHASORANGCOS:
            case MeasurementGroup.PHASORAMPANG:
            case MeasurementGroup.AI:
            case MeasurementGroup.AO:
            case MeasurementGroup.DI:
            case MeasurementGroup.DO:
            case MeasurementGroup.RELAY:
            case MeasurementGroup.DIPULSECOUNT:
            case MeasurementGroup.CUSTOM:
            case MeasurementGroup.TOLERANCE:
            case MeasurementGroup.ACTPWR:
            case MeasurementGroup.REAPWR:
            case MeasurementGroup.APPPWR:
            case MeasurementGroup.PF:
            case MeasurementGroup.PFSOURCE:
            case MeasurementGroup.PFTRUE:
            case MeasurementGroup.PFTRUESOURCE:
            case MeasurementGroup.ACTPWRF:
            case MeasurementGroup.REAPWRF:
            case MeasurementGroup.APPPWRF:
            case MeasurementGroup.PFF:
            case MeasurementGroup.PFFSOURCE:
            case MeasurementGroup.ACTPWRH:
            case MeasurementGroup.REAPWRH:
            case MeasurementGroup.APPPWRH:
            case MeasurementGroup.PFH:
            case MeasurementGroup.PFHSOURCE:
            case MeasurementGroup.R:
            case MeasurementGroup.RH:
            case MeasurementGroup.RF:
            case MeasurementGroup.ENERGYINACT:
            case MeasurementGroup.ENERGYINREA:
            case MeasurementGroup.ENERGYAPP:
            case MeasurementGroup.ENERGYOUTACT:
            case MeasurementGroup.ENERGYOUTREA:
            case MeasurementGroup.UNBAL:
            case MeasurementGroup.ZUNBAL:
            case MeasurementGroup.ZSEQ:
            case MeasurementGroup.NSEQ:
            case MeasurementGroup.PSEQ:
            case MeasurementGroup.TOTALRMS:
            case MeasurementGroup.PWRHRMSANG:
            case MeasurementGroup.PWRHRMSPF:
            case MeasurementGroup.PWRHRMSPFSOURCE:
            case MeasurementGroup.PWRHRMSACT:
            case MeasurementGroup.PWRHRMSREACT:
            case MeasurementGroup.PWRHRMSAPP:
            case MeasurementGroup.WAVECYCSTAT:
            case MeasurementGroup.GETTIMES:
            case MeasurementGroup.IMPAMP:
            case MeasurementGroup.IMPANG:
            case MeasurementGroup.IMPAMPANG:
            case MeasurementGroup.IMPANGSIN:
            case MeasurementGroup.IMPANGCOS:
            case MeasurementGroup.ADMAMPANG:
            #endregion
            default:
                break;
        }
        #endregion Add Phases with duplications (all voltages and all currents)

        foreach (PhaseMeasurementWithDuplicationsEnum item in GetSupportedNetworkFeederParametersPhases(_selectedParameter))
        {
            ValueAndDescription<PhaseMeasurementWithDuplicationsEnum> phaseAndDescription = new ValueAndDescription<PhaseMeasurementWithDuplicationsEnum>();
            phaseAndDescription.Value = item;
            phaseAndDescription.Description = item.Description();
            phaseAndDescription.IsEnabled = true;

            AvailablePhases.Add(phaseAndDescription);
        }

        return AvailablePhases;
    }

    private List<PhaseMeasurementEnum> GetSupportedNetworkFeederParametersPhases(MeasurementGroup param)
    {
        switch (param)
        {
            case MeasurementGroup.RMS:
            case MeasurementGroup.RMSFUND:
            case MeasurementGroup.RMSNONFUND:
            case MeasurementGroup.RMSRW:
            case MeasurementGroup.THD:
            case MeasurementGroup.THDODD:
            case MeasurementGroup.THDEVEN:
            case MeasurementGroup.THDI:
            case MeasurementGroup.CRESTF:
            case MeasurementGroup.HRMS:
            case MeasurementGroup.HRMSINCYC:
            case MeasurementGroup.IHRMS:
            case MeasurementGroup.IHRMSB:
            case MeasurementGroup.HRMSPER:
            case MeasurementGroup.IHRMSPER:
            case MeasurementGroup.HRMSPERINCYC:
            case MeasurementGroup.FREQBYPHASE:
            case MeasurementGroup.WAVE:
            case MeasurementGroup.WAVEF:
            case MeasurementGroup.WAVEH:
            case MeasurementGroup.PHASORANG:
            case MeasurementGroup.UNDERDEV:
            case MeasurementGroup.OVERDEV:
                return phasesGroup_VI;

            case MeasurementGroup.PST:
            case MeasurementGroup.PLT:
                return phasesGroup_Volt;

            case MeasurementGroup.KF:
            case MeasurementGroup.IL:
            case MeasurementGroup.TIF:
            case MeasurementGroup.TDD:
            case MeasurementGroup.HDD:
                return phasesGroup_I;

            case MeasurementGroup.HRMSG:
            case MeasurementGroup.IHRMSG:
            case MeasurementGroup.IHRMSA:
            case MeasurementGroup.IHRMSASIN:
            case MeasurementGroup.IHRMSACOS:
            case MeasurementGroup.IHRMSPA:
            case MeasurementGroup.IHRMSPASIN:
            case MeasurementGroup.IHRMSPACOS:
            case MeasurementGroup.HRMSANG:
            case MeasurementGroup.HRMSANGSIN:
            case MeasurementGroup.HRMSANGCOS:
            case MeasurementGroup.HRMSPERG:
            case MeasurementGroup.PEAK:
            case MeasurementGroup.PEAKF:
            case MeasurementGroup.PEAKH:
            case MeasurementGroup.PINSTMAX:
            case MeasurementGroup.PHASORANGSIN:
            case MeasurementGroup.PHASORANGCOS:
            case MeasurementGroup.PHASORAMPANG:
            case MeasurementGroup.AI:
            case MeasurementGroup.AO:
            case MeasurementGroup.DI:
            case MeasurementGroup.DO:
            case MeasurementGroup.RELAY:
            case MeasurementGroup.DIPULSECOUNT:
            case MeasurementGroup.CUSTOM:
            case MeasurementGroup.TOLERANCE:
            case MeasurementGroup.TOTALRMS:
            case MeasurementGroup.PWRHRMSANG:
            case MeasurementGroup.TEMPC:
            case MeasurementGroup.WAVECYCSTAT:
            case MeasurementGroup.GETTIMES:
            case MeasurementGroup.IMPANGCOS:
            case MeasurementGroup.IMPANGSIN:
            case MeasurementGroup.ADMANGCOS:
            case MeasurementGroup.ADMANGSIN:
                return null;

            case MeasurementGroup.FREQ:
                List<PhaseMeasurementEnum> freqList = new List<PhaseMeasurementEnum>();
                freqList.Add(PhaseMeasurementEnum.UFREQUENCY);
                return freqList;

            case MeasurementGroup.ACTPWR:
            case MeasurementGroup.REAPWR:
            case MeasurementGroup.APPPWR:
            case MeasurementGroup.PF:
            case MeasurementGroup.PFSOURCE:
            case MeasurementGroup.PFTRUE:
            case MeasurementGroup.PFTRUESOURCE:
            case MeasurementGroup.ACTPWRF:
            case MeasurementGroup.REAPWRF:
            case MeasurementGroup.APPPWRF:
            case MeasurementGroup.PFF:
            case MeasurementGroup.PFFSOURCE:
            case MeasurementGroup.ACTPWRH:
            case MeasurementGroup.REAPWRH:
            case MeasurementGroup.APPPWRH:
            case MeasurementGroup.PFH:
            case MeasurementGroup.PFHSOURCE:
            case MeasurementGroup.R:
            case MeasurementGroup.RH:
            case MeasurementGroup.RF:
            case MeasurementGroup.ENERGYINACT:
            case MeasurementGroup.ENERGYINREA:
            case MeasurementGroup.ENERGYAPP:
            case MeasurementGroup.ENERGYOUTACT:
            case MeasurementGroup.ENERGYOUTREA:
            case MeasurementGroup.PWRHRMSPF:
            case MeasurementGroup.PWRHRMSPFSOURCE:
            case MeasurementGroup.PWRHRMSACT:
            case MeasurementGroup.PWRHRMSREACT:
            case MeasurementGroup.PWRHRMSAPP:
            case MeasurementGroup.IMPAMP:
            case MeasurementGroup.IMPAMPANG:
            case MeasurementGroup.IMPANG:
            case MeasurementGroup.DEMANDAPP:
            case MeasurementGroup.DEMANDINACT:
            case MeasurementGroup.DEMANDINREA:
            case MeasurementGroup.DEMANDOUTACT:
            case MeasurementGroup.DEMANDOUTREA:
            case MeasurementGroup.ADMAMP:
            case MeasurementGroup.ADMAMPANG:
            case MeasurementGroup.ADMANG:
                return phasesGroup_123;

            case MeasurementGroup.UNBAL:
            case MeasurementGroup.ZUNBAL:
            case MeasurementGroup.ZSEQ:
            case MeasurementGroup.NSEQ:
            case MeasurementGroup.PSEQ:
                return phasesGroup_VI123;

            default:
                return new List<PhaseMeasurementEnum>();
        }
    }

    private void Populate_PhasesGroup_VI()
    {
        phasesGroup_VI = new List<PhaseMeasurementEnum>();
        foreach (PhaseMeasurementEnum item in Enum.GetValues(typeof(PhaseMeasurementEnum)))
            if ((byte)item >= 1 && (byte)item <= 18)
                phasesGroup_VI.Add(item);
        phasesGroup_VI.Add(PhaseMeasurementEnum.UVDC);
        phasesGroup_VI.Add(PhaseMeasurementEnum.UIDC);
    }

    private void Populate_PhasesGroup_Volt()
    {
        phasesGroup_Volt = new List<PhaseMeasurementEnum>();
        phasesGroup_Volt.Add(PhaseMeasurementEnum.UV1N);
        phasesGroup_Volt.Add(PhaseMeasurementEnum.UV2N);
        phasesGroup_Volt.Add(PhaseMeasurementEnum.UV3N);
        phasesGroup_Volt.Add(PhaseMeasurementEnum.UV12);
        phasesGroup_Volt.Add(PhaseMeasurementEnum.UV23);
        phasesGroup_Volt.Add(PhaseMeasurementEnum.UV31);
    }

    private void Populate_PhasesGroup_I()
    {
        phasesGroup_I = new List<PhaseMeasurementEnum>();
        foreach (PhaseMeasurementEnum item in Enum.GetValues(typeof(PhaseMeasurementEnum)))
            if ((byte)item >= 11 && (byte)item <= 18)
                phasesGroup_I.Add(item);
    }

    private void Populate_PhasesGroup_123()
    {
        phasesGroup_123 = new List<PhaseMeasurementEnum>();
        foreach (PhaseMeasurementEnum item in Enum.GetValues(typeof(PhaseMeasurementEnum)))
            if ((byte)item >= 50 && (byte)item <= 52 || (byte)item == 66)
                phasesGroup_123.Add(item);
    }

    private void Populate_PhasesGroup_VI123()
    {
        phasesGroup_VI123 = new List<PhaseMeasurementEnum>();
        phasesGroup_VI123.Add(PhaseMeasurementEnum.UV123);
        phasesGroup_VI123.Add(PhaseMeasurementEnum.UV123LL);
        phasesGroup_VI123.Add(PhaseMeasurementEnum.UI123);
    }

    #endregion



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
