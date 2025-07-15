using PQBI.Sapphire.Options;
using PQS.Data.Common;
using PQS.Translator;
using MeasurementGroup = PQS.Data.Measurements.Enums.Group;

namespace PQBI.IntegrationTests.Scenarios.PopulatingParameters;

public abstract class PopulateBasicParameters
{
    protected Dictionary<CalcBaseWindowInterval, string> _calcBaseWinIntNames;
    protected List<CalcBaseWindowInterval> BasedOnGroup_Common;
    protected List<CalcBaseWindowInterval> BasedOnGroup_Power;
    protected List<CalcBaseWindowInterval> BasedOnGroup_Unbalance;
    protected List<CalcBaseWindowInterval> BasedOnGroup_Current;
    protected List<CalcBaseWindowInterval> BasedOnGroup_Harmonics;

    protected PopulateBasicParameters()
    {
        _calcBaseWinIntNames = [];
    }

    //public abstract IEnumerable<ParameterNode> CreateDataAsync();
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


    protected List<ValueAndDescription<CalcBaseWindowInterval>> FillBasedOnColumnSortedByFeederNetwork(MeasurementGroup selectedParameter)
    {
        var AvailableBases = new List<ValueAndDescription<CalcBaseWindowInterval>>();

        List<CalcBaseWindowInterval> bases = [.. GetSupportedNetworkFeederParametersBases(selectedParameter), .. _calcBaseWinIntNames.Keys];
        bases.Sort((x, y) => x.CalculationBase.CompareTo(y.CalculationBase));
        try
        {

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
        }
        catch (Exception ex)
        {
            int x = 0;
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

    protected void CreateBasedOnGroups()
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

    protected (int MinHarmonic, int MaxHarmonic) GetHarmonicMinMaxForCurrentSelection(MeasurementGroup SelectedParameter)
    {

        var harmonic = (1, 511);

        if (SelectedParameter == MeasurementGroup.HRMS)
        {
            //_currentHarmonicMin = 1;
            //_currentHarmonicMax = 50;
            harmonic = (1, 50);
        }
        else if (SelectedParameter == MeasurementGroup.HRMSINCYC)
        {
            //_currentHarmonicMin = 1;
            //_currentHarmonicMax = 511;
            harmonic = (1, 511);
        }
        else if (SelectedParameter == MeasurementGroup.IHRMS)
        {
            //_currentHarmonicMin = 1;
            //_currentHarmonicMax = 50;
            harmonic = (1, 50);
        }
        else if (SelectedParameter == MeasurementGroup.IHRMSB)
        {
            //_currentHarmonicMin = 0;
            //_currentHarmonicMax = 511;
            harmonic = (0, 511);
        }
        else if (SelectedParameter == MeasurementGroup.HRMSPER || SelectedParameter == MeasurementGroup.IHRMSPER)
        {
            //_currentHarmonicMin = 2;
            //_currentHarmonicMax = 50;
            harmonic = (2, 50);
        }
        else if (SelectedParameter == MeasurementGroup.HRMSPERINCYC)
        {
            //_currentHarmonicMin = 2;
            //_currentHarmonicMax = 511;
            harmonic = (2, 511);
        }
        else if (SelectedParameter == MeasurementGroup.PWRHRMSPF ||
                 SelectedParameter == MeasurementGroup.PWRHRMSACT ||
                 SelectedParameter == MeasurementGroup.PWRHRMSREACT ||
                 SelectedParameter == MeasurementGroup.PWRHRMSAPP)
        {
            //_currentHarmonicMin = 1;
            //_currentHarmonicMax = 511;
            harmonic = (1, 511);
        }

        return harmonic;
    }


}
