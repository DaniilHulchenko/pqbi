using FluentAssertions;
using PQBI.Sapphire.Options;
using PQS.Data.Common;
using PQS.Data.Events.Enums;
using PQS.Data.Measurements.Enums;
using PQS.Translator;
using MeasurementGroup = PQS.Data.Measurements.Enums.Group;

namespace PQBI.IntegrationTests.Scenarios.PopulatingParameters;

public class ParameterGroupItem
{
    public MeasurementGroup Group { get; set; }
    public string Description { get; set; }
}

public abstract class PopulateParamertAndBaseOnScenario : ScenarioBase
{
    protected List<PhaseMeasurementEnum> PhasesGroup_VI;
    protected List<PhaseMeasurementEnum> PhasesGroup_Volt;
    protected List<PhaseMeasurementEnum> PhasesGroup_I;
    protected List<PhaseMeasurementEnum> PhasesGroup_123;
    protected List<PhaseMeasurementEnum> PhasesGroup_VI123;



    protected Dictionary<CalcBaseWindowInterval, string> _calcBaseWinIntNames;
    protected List<CalcBaseWindowInterval> BasedOnGroup_Common;
    protected List<CalcBaseWindowInterval> BasedOnGroup_Power;
    protected List<CalcBaseWindowInterval> BasedOnGroup_Unbalance;
    protected List<CalcBaseWindowInterval> BasedOnGroup_Current;
    protected List<CalcBaseWindowInterval> BasedOnGroup_Harmonics;



    public PopulateParamertAndBaseOnScenario()
    {
        _calcBaseWinIntNames = [];

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

    protected override async Task RunScenario()
    {
        var root = new OptionTree();
        var parameters = UnderLogical();

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


    protected List<ValueAndDescription<CalcBaseWindowInterval>> FillBasedOnColumnSortedByFeederNetwork(MeasurementGroup selectedParameter)
    {
        var AvailableBases = new List<ValueAndDescription<CalcBaseWindowInterval>>();

        List<CalcBaseWindowInterval> bases = [.. GetSupportedNetworkFeederParametersBases(selectedParameter), .. _calcBaseWinIntNames.Keys];
        bases.Sort((x, y) => x.CalculationBase.CompareTo(y.CalculationBase));
        foreach (CalcBaseWindowInterval calcBaseWinInt in bases)
        {
            ValueAndDescription<CalcBaseWindowInterval> baseAndDescription = new();
            baseAndDescription.Value = calcBaseWinInt;
            string description;
            //if (calcBaseWinInt.IsCustom && _calcBaseWinIntNames.TryGetValue(calcBaseWinInt, out string name))
            //{
            //    description = name;
            //}
            //else
            //{
            description = calcBaseWinInt.CalculationBase.CalculationBaseEnum.Description();
            //}
            baseAndDescription.Description = description;
            baseAndDescription.IsEnabled = true;

            AvailableBases.Add(baseAndDescription);
        }

        return AvailableBases;
    }

    private List<CalcBaseWindowInterval> GetSupportedNetworkFeederParametersBases(MeasurementGroup param)
    {
        switch (param)
        {
            // Fixed issue #11694 - Analog input parameter is missing from Custom event parameter list
            case MeasurementGroup.AI:
            case MeasurementGroup.AO:
            case MeasurementGroup.DI:
            case MeasurementGroup.DO:
                List<CalcBaseWindowInterval> cycleList = [new(CalcBase.BCYC)];
                return cycleList;

            case MeasurementGroup.RMS:
            case MeasurementGroup.RMSFUND:
            case MeasurementGroup.RMSNONFUND:
            case MeasurementGroup.RMSRW:
            case MeasurementGroup.THD:
            case MeasurementGroup.THDODD:
            case MeasurementGroup.THDI:
            case MeasurementGroup.THDEVEN:
            case MeasurementGroup.CRESTF:
            case MeasurementGroup.WAVE:
            case MeasurementGroup.WAVEF:
            case MeasurementGroup.WAVEH:
                return BasedOnGroup_Common;

            case MeasurementGroup.IHRMSB:
                List<CalcBaseWindowInterval> b1012List = [new(CalcBase.B200MS)];
                return b1012List;

            case MeasurementGroup.HRMSINCYC:
            case MeasurementGroup.HRMSPERINCYC:
            case MeasurementGroup.IL:
                List<CalcBaseWindowInterval> HarmAmpList = [new(CalcBase.BHCYC)];
                return HarmAmpList;

            case MeasurementGroup.HRMS:
            case MeasurementGroup.IHRMS:
            case MeasurementGroup.HRMSPER:
            case MeasurementGroup.IHRMSPER:
            case MeasurementGroup.UNDERDEV:
            case MeasurementGroup.OVERDEV:
                return BasedOnGroup_Harmonics;

            case MeasurementGroup.FREQBYPHASE:
            case MeasurementGroup.PHASORANG:
                List<CalcBaseWindowInterval> phasorList = [new(CalcBase.BCYC)];
                return phasorList;

            case MeasurementGroup.KF:
            case MeasurementGroup.TDD:
            case MeasurementGroup.HDD:
                return BasedOnGroup_Current;

            case MeasurementGroup.PLT:
                List<CalcBaseWindowInterval> twoHoursList = [new(CalcBase.B2HOUR)];
                return twoHoursList;

            case MeasurementGroup.PST:
            case MeasurementGroup.TIF:
                List<CalcBaseWindowInterval> temMinList = [new(CalcBase.B10MIN)];
                return temMinList;

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
                return [];

            case MeasurementGroup.FREQ:
                List<CalcBaseWindowInterval> freqList = [new(CalcBase.BCYC), new(CalcBase.B200MS), new(CalcBase.B10SEC)];
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
                return BasedOnGroup_Power;

            case MeasurementGroup.UNBAL:
            case MeasurementGroup.ZUNBAL:
            case MeasurementGroup.ZSEQ:
            case MeasurementGroup.NSEQ:
            case MeasurementGroup.PSEQ:
                return BasedOnGroup_Unbalance;

            default:
                return [];
        }
    }




    #region Phase

    protected List<ValueAndDescription<PhaseMeasurementWithDuplicationsEnum>> FillPhasesColumnSortedByFeederNetwork(MeasurementGroup _selectedParameter)
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
                return PhasesGroup_VI;

            case MeasurementGroup.PST:
            case MeasurementGroup.PLT:
                return PhasesGroup_Volt;

            case MeasurementGroup.KF:
            case MeasurementGroup.IL:
            case MeasurementGroup.TIF:
            case MeasurementGroup.TDD:
            case MeasurementGroup.HDD:
                return PhasesGroup_I;

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
                return PhasesGroup_123;

            case MeasurementGroup.UNBAL:
            case MeasurementGroup.ZUNBAL:
            case MeasurementGroup.ZSEQ:
            case MeasurementGroup.NSEQ:
            case MeasurementGroup.PSEQ:
                return PhasesGroup_VI123;

            default:
                return new List<PhaseMeasurementEnum>();
        }
    }

    private void Populate_PhasesGroup_VI()
    {
        PhasesGroup_VI = new List<PhaseMeasurementEnum>();
        foreach (PhaseMeasurementEnum item in Enum.GetValues(typeof(PhaseMeasurementEnum)))
            if ((byte)item >= 1 && (byte)item <= 18)
                PhasesGroup_VI.Add(item);
        PhasesGroup_VI.Add(PhaseMeasurementEnum.UVDC);
        PhasesGroup_VI.Add(PhaseMeasurementEnum.UIDC);
    }

    private void Populate_PhasesGroup_Volt()
    {
        PhasesGroup_Volt = new List<PhaseMeasurementEnum>();
        PhasesGroup_Volt.Add(PhaseMeasurementEnum.UV1N);
        PhasesGroup_Volt.Add(PhaseMeasurementEnum.UV2N);
        PhasesGroup_Volt.Add(PhaseMeasurementEnum.UV3N);
        PhasesGroup_Volt.Add(PhaseMeasurementEnum.UV12);
        PhasesGroup_Volt.Add(PhaseMeasurementEnum.UV23);
        PhasesGroup_Volt.Add(PhaseMeasurementEnum.UV31);
    }

    private void Populate_PhasesGroup_I()
    {
        PhasesGroup_I = new List<PhaseMeasurementEnum>();
        foreach (PhaseMeasurementEnum item in Enum.GetValues(typeof(PhaseMeasurementEnum)))
            if ((byte)item >= 11 && (byte)item <= 18)
                PhasesGroup_I.Add(item);
    }

    private void Populate_PhasesGroup_123()
    {
        PhasesGroup_123 = new List<PhaseMeasurementEnum>();
        foreach (PhaseMeasurementEnum item in Enum.GetValues(typeof(PhaseMeasurementEnum)))
            if ((byte)item >= 50 && (byte)item <= 52 || (byte)item == 66)
                PhasesGroup_123.Add(item);
    }

    private void Populate_PhasesGroup_VI123()
    {
        PhasesGroup_VI123 = new List<PhaseMeasurementEnum>();
        PhasesGroup_VI123.Add(PhaseMeasurementEnum.UV123);
        PhasesGroup_VI123.Add(PhaseMeasurementEnum.UV123LL);
        PhasesGroup_VI123.Add(PhaseMeasurementEnum.UI123);
    }

    #endregion

    private void CreateBasedOnGroups()
    {
        BasedOnGroup_Common =
        [
            new(CalcBase.BHCYC),
                new(CalcBase.B200MS),
                new(CalcBase.B3SEC),
                new(CalcBase.B10MIN),
                new(CalcBase.B2HOUR)
        ];

        BasedOnGroup_Current =
        [
            new(CalcBase.BHCYC),
                new(CalcBase.B200MS),
                new(CalcBase.B3SEC),
                new(CalcBase.B10MIN),
                new(CalcBase.B2HOUR)
        ];

        BasedOnGroup_Power = [new(CalcBase.BCYC)];

        BasedOnGroup_Unbalance =
        [
            new(CalcBase.BCYC),
                new(CalcBase.B200MS),
                new(CalcBase.B3SEC),
                new(CalcBase.B10MIN),
                new(CalcBase.B2HOUR)
        ];


        BasedOnGroup_Harmonics =
        [
            new(CalcBase.B200MS),
                new(CalcBase.B3SEC),
                new(CalcBase.B10MIN),
                new(CalcBase.B2HOUR),
            ];
    }


    protected abstract List<ParameterGroupItem> UnderLogical();

    protected List<ParameterGroupItem> CreateGroupItems(string input)
    {
        var list = new List<ParameterGroupItem>();

        // Split the input by lines
        string[] lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            // Split each line by "--"
            string[] parts = line.Split(new[] { "--" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                // First part should correspond to enum value
                string enumPart = parts[0].Trim().ToUpper(); // Assuming the enum part is properly formatted

                if (Enum.TryParse(enumPart, out MeasurementGroup group))
                {
                    // Create a new ToliGroupItem and add it to the list
                    ParameterGroupItem item = new ParameterGroupItem
                    {
                        Group = group,
                        Description = parts[1].Trim()
                    };

                    list.Add(item);
                }
                else
                {
                    Console.WriteLine($"Failed to parse enum value: {enumPart}");
                }
            }
            else
            {
                Console.WriteLine($"Invalid input format for line: {line}");
            }
        }

        //foreach (var item in list)
        //{
        //    Console.WriteLine($"Group: {item.Group}, Description: {item.Description}");
        //}

        return list;
    }
}
