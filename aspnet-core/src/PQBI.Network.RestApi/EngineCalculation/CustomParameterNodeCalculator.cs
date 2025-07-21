using Abp.Collections.Extensions;
using Abp.UI;
using Castle.MicroKernel.Registration;
using PayPalCheckoutSdk.Orders;
using PQBI.CalculationEngine.Functions;
using PQBI.CalculationEngine.Matrix;
using PQBI.Infrastructure.Extensions;
using PQBI.PQS.CalcEngine;
using SkiaSharp;

namespace PQBI.Network.RestApi.EngineCalculation;


public class CustomParameterNodeCalculator
{
    public FinalCalculationMatrix FinalAggregationMatrix { get; private set; } = null;

    public void AddFinalMaxtrixCalculation(IEnumerable<IEnumerable<BasicValue>> data)
    {
        var additionalItem = ParameterMatrix222.AggregationCalculation ?? Array.Empty<BasicValue>();
        //var additionalItem = ParameterMatrix.AggregationCalculation ?? Array.Empty<BasicValue>();

        var dataList = data.Select(row => row.ToList()).ToList();
        int rowAmount = dataList.Count;

        FinalAggregationMatrix = new FinalCalculationMatrix();
        if (rowAmount == 0 && additionalItem.IsNullOrEmpty())
        {
            FinalAggregationMatrix.Matrix = null;
            FinalAggregationMatrix.AggregatedCalculated = Enumerable.Empty<BasicValue>();

            return;
        }

        var columnAmount = additionalItem.Length;
        if (columnAmount == 0)
        {
            columnAmount = dataList[0].Count;
        }

        if (additionalItem.Length > 0 && additionalItem.Length != columnAmount)
        {
            throw new UserFriendlyException("AggregationCalculation must have the same number of columns as data rows.");
        }

        //FinalAggregationMatrix = new FinalCalculationMatrix();

        var matrix = new BasicValue[rowAmount + (additionalItem.Length > 0 ? 1 : 0), columnAmount];
        FinalAggregationMatrix.Matrix = matrix;

        for (int row = 0; row < rowAmount; row++)
        {
            var rowData = dataList[row];
            for (int column = 0; column < columnAmount; column++)
            {
                matrix[row, column] = rowData[column];
            }
        }

        if (additionalItem.Length > 0)
        {
            for (int column = 0; column < columnAmount; column++)
            {
                matrix[rowAmount, column] = additionalItem[column];
            }
        }
    }


    public IEnumerable<BasicValue> CalculateAggregationMatrix(Func<BasicValue[], BasicValue> callback)
    {

        var result = FinalAggregationMatrix.CalculateAggregationMatrix(callback);
        return result;
    }

    public class FinalCalculationMatrix
    {
        public BasicValue[,] Matrix { get; set; } = null;
        public IEnumerable<BasicValue> AggregatedCalculated { get; set; } = null;


        public IEnumerable<BasicValue> CalculateAggregationMatrix(Func<BasicValue[], BasicValue> callback)
        {
            if (Matrix is null)
            {
                return [];
            }

            var result = new List<BasicValue>();
            //if (_validParameters.Count > 0)
            {
                var rowLength = Matrix.GetLength(0);
                var columnLength = Matrix.GetLength(1);

                var buffer = new List<BasicValue>();

                for (var column = 0; column < columnLength; column++)
                {
                    for (var row = 0; row < rowLength; row++)
                    {
                        buffer.Add(Matrix[row, column]);
                    }

                    var tmp = callback(buffer.ToArray());
                    result.Add(tmp);
                    buffer.Clear();
                }
            }

            AggregatedCalculated = result;
            return result;
        }
    }

    private List<BaseParameterComponent> _baseParameterComponents = new List<BaseParameterComponent>();


    public CustomParameterNodeCalculator(CustomParameterType customParameterType, int customParameterResolutionInSeconds, bool isAutoResolution, string innerAggregationFunction, DateTime startDate, DateTime endDate,
         int widgetResolutionInSecond, string widgetAggregation, string customParameterName = null,
        InnerCustomParameter innerCustomParameter = null
        )
    {

        SetWidgetResolution(customParameterResolutionInSeconds, widgetResolutionInSecond, isAutoResolution);


        CustomParameterType = customParameterType;
        CustomParameterResolutionRecalculatedInSeconds = customParameterResolutionInSeconds;
        //CustomParameterResolutionOriginal = customParameterResolution;
        StartDate = startDate;
        EndDate = endDate;
        InnerAggregationFunction = innerAggregationFunction;

        WidgetAggragationFunction = widgetAggregation;
        CustomParameterName = customParameterName;
        InnerCustomParameter = innerCustomParameter;

        //ParameterMatrix = new ParameterMatrix();
        ParameterMatrix222 = new ParameterMatrix();
    }

    //public ParameterMatrix ParameterMatrix { get; }
    public ParameterMatrix ParameterMatrix222 { get; }
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

        //var resolutionSeconds = GroupByCalcFunction.ParseAndConvertToSecond(customParameterResolution);

        if (widgetResolutionAutoOrInSeconds < customParameterResolution)
        {
            throw new UserFriendlyException("Resolution issue", $"{widgetResolutionAutoOrInSeconds} cannot be bigger then {customParameterResolution}");
        }

        //WidgetResolution = widgetResolution;
        WidgetResolutionAutoOrInSeconds = widgetResolutionAutoOrInSeconds;
    }

    public CustomParameterType CustomParameterType { get; }
    //public string CustomParameterResolutionOriginal { get; }

    public int CustomParameterResolutionRecalculatedInSeconds { get; }
    //public int CustomParameterResolutionRecalculatedInSeconds => GroupByCalcFunction.ParseAndConvertToSecond(CustomParameterResolutionOriginal);

    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public string InnerAggregationFunction { get; }

    public string WidgetAggragationFunction { get; }
    public string CustomParameterName { get; }
    public InnerCustomParameter InnerCustomParameter { get; }

    public int WidgetResolutionAutoOrInSeconds { get; private set; }

    public bool IsWidgetResolutionAuto { get; private set; }
    public bool IsWidgetResolutionAuto222 { get; private set; }
    public int AutoWishListNumber { get; private set; } = 0;

    public IEnumerable<FeederComponentInfo> Feeders { get; set; } = [];

    public void PopulateWithBaseParameterComponents(IEnumerable<BaseParameterComponent> parameterComponents)
    {
        if (parameterComponents.IsCollectionExists())
        {
            foreach (var parameterComponent in parameterComponents)
            {
                Add(parameterComponent);
            }
        }
    }

    void Add(BaseParameterComponent item) => _baseParameterComponents.Add(item);

    internal DataUnitType GetDataType()
    {
        DataUnitType dataType = null;
        if (BaseParameterComponents.IsCollectionEmpty() == false)
        {
            dataType = BaseParameterComponents.FirstOrDefault().DataUnitType;
        }
        else
        {
            var child = Children.FirstOrDefault(x => x.BaseParameterComponents.IsCollectionEmpty() == false);
            if (child is not null)
            {
                dataType = child.BaseParameterComponents.FirstOrDefault().DataUnitType;
            }
        }

        return dataType;
    }

    public void CalculatedInnerAlignment()
    {
        using (var mainLogger = PqbiStopwatch.Anchor($"{nameof(CalculatedInnerAlignment)}"))
        {
            foreach (var item in _baseParameterComponents)
            {
                var points = item.Axis.DataTimeStamps.Select(x => new BasicValue(x.Point, x.DataValueStatus.ToPqbiDataValueStatus())).ToArray();
                var points2 = ParameterMatrix222.CalculateOperator(points, item.Operator);

                var calculated = ParameterMatrix222.CalculateAggregation(points2, item.AggregationFunction, CustomParameterResolutionRecalculatedInSeconds, item.BaseParameterResolutionInSeconds);
                ParameterMatrix222.AddSeries(item, calculated, item.PQZStatus);

            }

            ParameterMatrix222.CalculateAndSetOutterAggregation2222(InnerAggregationFunction);
        }
    }
}