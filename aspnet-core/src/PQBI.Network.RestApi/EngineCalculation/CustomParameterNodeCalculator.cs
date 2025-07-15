using Abp.Collections.Extensions;
using Abp.Dependency;
using Abp.UI;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using PayPalCheckoutSdk.Orders;
using PQBI.CalculationEngine.Functions;
using PQBI.CalculationEngine.Matrix;
using PQBI.Infrastructure.Extensions;
using PQBI.PQS.CalcEngine;
using PQS.Data.Common;
using PQS.Data.Common.Units;
using PQS.Data.Measurements.StandardParameter;
using PQS.Data.Measurements.Utils;
using SkiaSharp;
using System.Text.RegularExpressions;

namespace PQBI.Network.RestApi.EngineCalculation;


public class CustomParameterNodeCalculator
{
    public List<FinalCalculationMatrix> FinalAggregationMatrixes { get; private set; } = new List<FinalCalculationMatrix>();

    public void AddFinalMaxtrixCalculationChildless()
    {
        if (ParameterMatrixes.IsCollectionEmpty())
        {
            throw new UserFriendlyException($"{nameof(ParameterMatrixes)} must be.");
        }

        foreach (var parameterMatrix in ParameterMatrixes)
        {
            var additionalItem = parameterMatrix.AggregationCalculation ?? Array.Empty<BasicValue>();


            var finalAggregationMatrix = new FinalCalculationMatrix();
            var columnAmount = parameterMatrix.AggregationCalculation.Length;


            var matrix = new BasicValue[1, parameterMatrix.AggregationCalculation.Length];
            finalAggregationMatrix.Matrix = matrix;
            finalAggregationMatrix.DataUnitType = parameterMatrix.DataUnitType;

            for (int column = 0; column < columnAmount; column++)
            {
                matrix[0, column] = additionalItem[column];
            }

            FinalAggregationMatrixes.Add(finalAggregationMatrix);
        }
    }

    public void AddFinalMatrixCalculation()
    {
        if (ParameterMatrixes.IsCollectionExists())
        {
            foreach (var parameterMatrix in ParameterMatrixes)
            {
                var finalAggregationMatrix = new FinalCalculationMatrix();
                finalAggregationMatrix.AggregationCalculation = parameterMatrix.AggregationCalculation;
                finalAggregationMatrix.DataUnitType = parameterMatrix.DataUnitType;
                FinalAggregationMatrixes.Add(finalAggregationMatrix);
            }
        }
    }

    public void AddFinalMaxtrixCalculationWithChildren()
    {
        //if (ParameterMatrixes.IsCollectionEmpty())
        //{
        //    throw new UserFriendlyException($"{nameof(ParameterMatrixes)} must be.");
        //}

        var rowAmount = 0;
        var columnAmount = 0;

        if (ParameterMatrixes.IsCollectionExists())
        {
            rowAmount = 1;
            columnAmount = ParameterMatrixes.First().AggregationCalculation.Length;
        }

        foreach (var child in Children)
        {
            rowAmount += child.ParameterMatrixes.Count;
        }

        if (ParameterMatrixes.IsCollectionExists())
        {
            foreach (var parameterMatrix in ParameterMatrixes)
            {
                var additionalItem = parameterMatrix.AggregationCalculation ?? [];
                var finalAggregationMatrix = new FinalCalculationMatrix();

                var matrix = new BasicValue[rowAmount, columnAmount];
                finalAggregationMatrix.Matrix = matrix;

                for (int column = 0; column < columnAmount; column++)
                {
                    matrix[0, column] = additionalItem[column];
                }

                foreach (var child in Children)
                {
                    var tmpRowAmount = rowAmount;
                    foreach (var childParameterMatrix in child.ParameterMatrixes)
                    {
                        var childAggregationCalculation = childParameterMatrix.AggregationCalculation;
                        for (int column = 0; column < columnAmount; column++)
                        {
                            matrix[tmpRowAmount - 1, column] = childAggregationCalculation[column];
                        }
                        tmpRowAmount--;
                    }
                }

                finalAggregationMatrix.AggregationCalculation = finalAggregationMatrix.CalculateAggregationFromMatrix(matrix, OuterAggregationFunction);
                finalAggregationMatrix.DataUnitType = parameterMatrix.DataUnitType;
                FinalAggregationMatrixes.Add(finalAggregationMatrix);
            }
        }
        else
        {
            var finalAggregationMatrix = new FinalCalculationMatrix();
            var firstChildParameterMatrix = Children.FirstOrDefault().ParameterMatrixes.FirstOrDefault();

            var dataUnitType = firstChildParameterMatrix.DataUnitType;
            columnAmount = firstChildParameterMatrix.AggregationCalculation.Length;


            var matrix = new BasicValue[rowAmount, columnAmount];
            finalAggregationMatrix.Matrix = matrix;

            foreach (var child in Children)
            {
                var tmpRowAmount = 0;
                foreach (var childParameterMatrix in child.ParameterMatrixes)
                {
                    var childAggregationCalculation = childParameterMatrix.AggregationCalculation;
                    for (int column = 0; column < columnAmount; column++)
                    {
                        matrix[tmpRowAmount, column] = childAggregationCalculation[column];
                    }
                    tmpRowAmount++;
                }
            }

            finalAggregationMatrix.AggregationCalculation = finalAggregationMatrix.CalculateAggregationFromMatrix(matrix, OuterAggregationFunctionCleaned.OuterAggregationFunction);
            finalAggregationMatrix.DataUnitType = dataUnitType;
            FinalAggregationMatrixes.Add(finalAggregationMatrix);
        }
    }

    public void CalculateAggregationFinalMatrixChildless()
    {
        foreach (var finalAggregationMatrix in FinalAggregationMatrixes)
        {
            finalAggregationMatrix.CalculateAggregationMatrix(OuterAggregationFunctionCleaned.OuterAggregationFunction);
        }
    }

    public class FinalCalculationMatrix : MatrixBase
    {
        public BasicValue[,] Matrix { get; set; } = null;
        private readonly IEngineCalculationService _engineControllerService;

        public IEnumerable<BasicValue> CalculateAggregationMatrix(string aggregationFunction)
        {
            if (Matrix is null)
            {
                return [];
            }

            var result = new List<BasicValue>();
            var rowLength = Matrix.GetLength(0);
            var columnLength = Matrix.GetLength(1);
            var buffer = new List<BasicValue>();

            if (AggregationMatrix.TryGetArithmetics(aggregationFunction, out string expression))
            {
                for (var column = 0; column < columnLength; column++)
                {
                    result.Add(Matrix[0, column]);
                }
            }
            else
            {
                for (var column = 0; column < columnLength; column++)
                {
                    for (var row = 0; row < rowLength; row++)
                    {
                        buffer.Add(Matrix[row, column]);
                    }

                    var tmp = AggregationMatrix.Run(buffer, 0, aggregationFunction);

                    result.Add(tmp);
                    buffer.Clear();
                }
            }

            AggregationCalculation = result.ToArray();
            return result;
        }
    }

    private List<BaseParameterComponent> _baseParameterComponents = new List<BaseParameterComponent>();


    public CustomParameterNodeCalculator(CustomParameterType customParameterType, int customParameterResolutionInSeconds, bool isAutoResolution, string outerAggregationFunction, DateTime startDate, DateTime endDate,
         int widgetResolutionInSecond, string widgetAggregation, string customParameterName = null,
        InnerCustomParameter innerCustomParameter = null, AdvancedSettings? advancedSettingsForTable = null
        )
    {
        SetWidgetResolution(customParameterResolutionInSeconds, widgetResolutionInSecond, isAutoResolution);


        CustomParameterType = customParameterType;
        CustomParameterResolutionRecalculatedInSeconds = customParameterResolutionInSeconds;
        //CustomParameterResolutionOriginal = customParameterResolution;
        StartDate = startDate;
        EndDate = endDate;
        OuterAggregationFunction = outerAggregationFunction;
        AdvancedSettingsForTable = advancedSettingsForTable;
        WidgetAggragationFunction = widgetAggregation;
        CustomParameterName = customParameterName;
        InnerCustomParameter = innerCustomParameter;

        //ParameterMatrix = new ParameterMatrix();
    }

    public List<ParameterMatrix> ParameterMatrixes { get; } = new List<ParameterMatrix>();
    public TimeSpan Duration => EndDate - StartDate;

    public List<CustomParameterNodeCalculator> Children = new List<CustomParameterNodeCalculator>();

    public IEnumerable<BaseParameterComponent> BaseParameterComponents => _baseParameterComponents;


    private void SetWidgetResolution(int customParameterResolution, int widgetResolutionAutoOrInSeconds, bool isAutoResolution)
    {
        IsWidgetResolutionAuto = isAutoResolution;

        if (IsWidgetResolutionAuto)
        {
            AutoWishListNumber = widgetResolutionAutoOrInSeconds;
            return;
        }


        if (widgetResolutionAutoOrInSeconds == GroupByCalcFunction.Single_Resolution)
        {
            WidgetResolutionAutoOrInSeconds = -1;
            return;
        }

        if (widgetResolutionAutoOrInSeconds < customParameterResolution)
        {
            throw new UserFriendlyException("Resolution issue", $"{widgetResolutionAutoOrInSeconds} cannot be bigger then {customParameterResolution}");
        }

        WidgetResolutionAutoOrInSeconds = widgetResolutionAutoOrInSeconds;
    }

    public CustomParameterType CustomParameterType { get; }

    public int CustomParameterResolutionRecalculatedInSeconds { get; }

    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public string OuterAggregationFunction { get; }
    public (string OuterAggregationFunction, double? Parameter) OuterAggregationFunctionCleaned => MatrixBase.CleanFunctionId(OuterAggregationFunction);

    public string WidgetAggragationFunction { get; }
    public string CustomParameterName { get; }
    public AdvancedSettings? AdvancedSettingsForTable { get; set; }
    public InnerCustomParameter InnerCustomParameter { get; }

    public int WidgetResolutionAutoOrInSeconds { get; private set; }

    public bool IsWidgetResolutionAuto { get; private set; }
    public int AutoWishListNumber { get; private set; } = 0;

    public IEnumerable<FeederComponentInfo> Feeders { get; set; } = [];

    public void PopulateWithBaseParameterComponents(IEnumerable<BaseParameterComponent> parameterComponents)
    {
        if (parameterComponents.IsCollectionExists())
        {
            _baseParameterComponents.AddRange(parameterComponents);
        }
    }


    public void CalculatedInnerAlignment(IEnumerable<BaseParameterComponent> baseParameterComponents, AdvancedSettings? advancedSettings = null)
    {
        var matrix = new ParameterMatrix();
        if (baseParameterComponents.IsCollectionExists())
        {
            using (var mainLogger = PqbiStopwatch.Anchor($"{nameof(CalculatedInnerAlignment)}"))
            {
                double nominalVal = -1;
                bool isExcludeFlagged = false;
                bool isIgnoreAligningFunction = false;
                NormalizeEnum normalizeType = NormalizeEnum.NO;
                if (advancedSettings != null)
                {
                    isIgnoreAligningFunction = advancedSettings.IsIgnoreAligningFunction;

                    if (advancedSettings.IsExcludeFlaggedData)
                        isExcludeFlagged = true;

                    normalizeType = advancedSettings.NormalizeType;
                }

                if (normalizeType != NormalizeEnum.NO)
                {
                    //var unitState = UnitsUtility.GetUnitsFromGroupAndPhase(channelMeasurementParameter.Group, channelType: channelTypeEnum);
                  
                    var unitState = UnitsEnum.STD_PERCENT;
                    var token = UnitsEnumHelper.GetLocalizedDescriptionKey(unitState);
                    matrix.DataUnitType = new DataUnitType((int)unitState, token);
                    if (normalizeType == NormalizeEnum.VALUE)
                        nominalVal = (double)advancedSettings!.NominalVal;
                }
                else
                {
                    matrix.DataUnitType = baseParameterComponents.First().DataUnitType;
                }

                foreach (var item in baseParameterComponents)
                {
                    var points = item.Axis.DataTimeStamps.Select(x => new BasicValue(x.Point, x.DataValueStatus.ToPqbiDataValueStatus())).ToArray();
                    var points2 = matrix.CalculateOperator(points, item.Operator);

                    if (normalizeType == NormalizeEnum.NOMINAL)
                    {
                        if (item.Axis.Nominal.HasValue)
                            nominalVal = item.Axis.Nominal.Value;
                        else
                            nominalVal = 100;
                    }

                    var calculated = matrix.CalculateAggregation(points2, item.AggregationFunction, CustomParameterResolutionRecalculatedInSeconds, item.BaseParameterResolutionInSeconds, isExcludeFlagged, nominalVal, isIgnoreAligningFunction);
                    matrix.AddSeries(item.ScadaParameterName, calculated, item.PQZStatus);
                }
            }
        }

        foreach (var childCalculator in Children)
        {
            InnerCustomParameter innerCustomParameter = childCalculator.InnerCustomParameter;

            foreach (var item in childCalculator.FinalAggregationMatrixes)
            {
                matrix.DataUnitType = item.DataUnitType;
                //var points = item.Axis.DataTimeStamps.Select(x => new BasicValue(x.Point, x.DataValueStatus.ToPqbiDataValueStatus())).ToArray();
                var points2 = matrix.CalculateOperator(item.AggregationCalculation, innerCustomParameter.Operator);

                var calculated = matrix.CalculateAggregation(points2, innerCustomParameter.InnerAggregationFunction, CustomParameterResolutionRecalculatedInSeconds, childCalculator.CustomParameterResolutionRecalculatedInSeconds, false);

                matrix.AddSeries(childCalculator.CustomParameterName, calculated, PQZStatus.OK);
            }
        }
        ParameterMatrixes.Add(matrix);
    }

    //public void CalculatedInnerAlignment(IEnumerable<ParameterMatrix> baseParameterComponents)
    //{
    //    baseParameterComponents.First()

    //    foreach (var childCalculator in Children)
    //    {
    //        foreach (var item in childCalculator.FinalAggregationMatrixes)
    //        {
    //            item.AggregationCalculation
    //        }



    //        foreach (var childParameterMatrix in childCalculator.ParameterMatrixes)
    //        {
    //            if (childParameterMatrix.ValidParameters.Count > 0)
    //            {
    //                matrixPrmKeyList.Add(childParameterMatrix.ValidParameters.Keys.First());
    //                var childAggregationCalculation = childParameterMatrix.AggregationCalculation;
    //                childrenAggregationCalculationList.Add(childAggregationCalculation);
    //            }
    //        }
    //    }

    //    if (baseParameterComponents.IsCollectionExists())
    //    {
    //        var matrix = new ParameterMatrix();
    //        using (var mainLogger = PqbiStopwatch.Anchor($"{nameof(CalculatedInnerAlignment)}"))
    //        {
    //            matrix.DataUnitType = baseParameterComponents.First().DataUnitType;
    //            foreach (var item in baseParameterComponents)
    //            {



    //                var points = item.Axis.DataTimeStamps.Select(x => new BasicValue(x.Point, x.DataValueStatus.ToPqbiDataValueStatus())).ToArray();
    //                var points2 = matrix.CalculateOperator(points, item.Operator);

    //                var calculated = matrix.CalculateAggregation(points2, item.AggregationFunction, CustomParameterResolutionRecalculatedInSeconds, item.BaseParameterResolutionInSeconds);
    //                matrix.AddSeries(item, calculated, item.PQZStatus);
    //            }
    //        }

    //        ParameterMatrixes.Add(matrix);
    //    }
    //}


    public void CalculatedParameterOuterMatrixAndAggregation(bool isIgnoreAligningFunction)
    {
        using (var mainLogger = PqbiStopwatch.Anchor($"{nameof(CalculatedParameterOuterMatrixAndAggregation)}"))
        {

            List<BasicValue[]> childrenAggregationCalculationList = new List<BasicValue[]>();
            //List<IMatrixBaseParameterKey> matrixPrmKeyList = new List<IMatrixBaseParameterKey>();
            //foreach (var childCalculator in Children)
            //{                
            //    foreach (var childParameterMatrix in childCalculator.ParameterMatrixes)
            //    {
            //        if (childParameterMatrix.ValidParameters.Count > 0)
            //        {
            //            matrixPrmKeyList.Add(childParameterMatrix.ValidParameters.Keys.First());
            //            var childAggregationCalculation = childParameterMatrix.AggregationCalculation;
            //            childrenAggregationCalculationList.Add(childAggregationCalculation);
            //        }
            //    }
            //}


            string aggregationFunction = OuterAggregationFunction;
            if (ParameterMatrixes.Count > 0)
            {
                //IocManager.Instance.
                //ActivatorUtilities.CreateInstance
                //calculated = _engineControllerService.AggregationFunctionsAsync(quantity, values);

                if (!isIgnoreAligningFunction)
                {
                    foreach (var parameterMatrix in ParameterMatrixes)
                    {
                        parameterMatrix.CalculateAndSetOutterAggregation(aggregationFunction);
                    }
                }
                else
                {
                    foreach (var parameterMatrix in ParameterMatrixes)
                    {
                        parameterMatrix.CalculateAndSetOutterAggregationOnSingleCollection(aggregationFunction);                      
                    }
                }
            }
            //else
            //{
            //    var matrix = new ParameterMatrix();
            //    matrix.CalculateAndSetOutterAggregation(aggregationFunction);
            //    ParameterMatrixes.Add(matrix);
            //}
        }
    }
}