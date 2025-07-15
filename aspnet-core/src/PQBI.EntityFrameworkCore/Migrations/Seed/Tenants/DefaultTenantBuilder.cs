using System.Linq;
using Abp.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using PQBI.Editions;
using PQBI.EntityFrameworkCore;
using PQBI.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PQBI.CustomParameters;
using PQBI.MultiTenancy;
using PQBI.CalculationEngine.Functions.Aggregation;
using PQBI.CalculationEngine.Functions;

namespace PQBI.Migrations.Seed.Tenants
{
    public class DefaultTenantBuilder
    {

        public const int TreeTableCustomerParameterId = 100;
        public const int MPSC_CustomerParameterId = 101;
        public const int Exception_CustomerParameterId = 102;
        public const int SPMC_CustomerParameterId = 103;
        public const int BPCP_CustomerParameterId = 104;
        public const int MultiParameterMC_Arithmetic_CustomerParameterId = 105;

        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------

        public const int Multi_ParameterCustomeId = 201;
        public const int Single_ParameterCustomerId = 203;
        public const int Single_MaxPoints_ParameterCustomerId = 204;
        public const int Single_Auto_And_Percentile_ParameterCustomerId = 205;

        public const int Multi_ParameterCustomeId222 = 206;


        public const int Multi_Parameter_With_Single_CustomParameter = 207;
        public const int Single_Parameter_With_Multi_CustomParameter = 208;
        public const int Multi_Parameter_With_Multi_CustomParameter = 209;
        public const int Single_Parameter_With_Multi_And_Multi_CustomParameter = 210;
        public const int Multi_Parameter__Without_BaseParameters_CustomeId = 211;
        public const int Single_Parameter_With_Multi_0_BaseParameters = 212;
        public const int Single_Parameter_With_1Hour__Resolution = 213;
        public const int Multi_WitnSingle_With_1Hour__Resolution = 214;
        public const int Multi_Parameter_With_Single_CustomParameter_id = 215;
        public const int Multi_Parameter_Zero_BaseParameter_With_Single_CustomParameter_id = 216;
        public const int Sanity_Without_Interpolation_id = 217;
        public const int Sanity_With_Interpolation_id = 218;
        public const int Sanity_Multi_WithZero_Parameters_Id = 219;


        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------

        private readonly PQBIDbContext _context;
        private readonly TenantSeedConfig _tenantConfig;
        private readonly ILogger<PQBIEntityFrameworkCoreModule> _logger;

        public DefaultTenantBuilder(PQBIDbContext context, TenantSeedConfig tenantConfig, ILogger<PQBIEntityFrameworkCoreModule> logger)
        {
            _context = context;
            _tenantConfig = tenantConfig;
            _logger = logger;
        }

        public void Create()
        {
            CreateDefaultTenant();
        }

        private void TreeTableCreateParameter(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == TreeTableCustomerParameterId);
            if (customerParameter is null)
            {
                var json = """
{
    "Name": "TreeTable_CustomParameter",
    "AggregationFunction": "TBD()",
                    "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"MULT(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"zD3e5\"}]",
    "Type": "MPSC",
    "TimeRange": null,
    "ApplyTo": null,
    "Id": 100,
    "ResolutionInSeconds":60
}

""";

                StoreCustomParameter(tenant, TreeTableCustomerParameterId, json);
            }
        }


        private void MPSCCreateParameter(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == MPSC_CustomerParameterId);
            if (customerParameter is null)
            {
                var json = """
{
    "Name": "MPSC_CustomParameter",
                    "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"MULT(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"zD3e5\"}]",
    "Type": "MPSC",
    "TimeRange": null,
    "ApplyTo": null,
    "Id": 101,
   "ResolutionInSeconds":60
}

""";
                //"aggregationFunction":"arithmetic(bp1+bp2)",

                StoreCustomParameter(tenant, MPSC_CustomerParameterId, json);
            }
        }


        private void ExceptionCreateParameter(Tenant tenant)
        {

            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == Exception_CustomerParameterId);
            if (customerParameter is null)
            {
                var json = """
{
  "Name": "Exception_CustomParameter",
"CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"feeder\":\"FEEDER_1\",\"group\":\"RMS\",\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"quantity\":\"QAVG\",\"resolution\":60,\"operator\":\"MULT(2)\",\"aggregationFunction\":\"AVG()\",\"fromComponents\":{\"componentId\":\"08c3912f-0275-4278-bf86-917168d88eef\",\"id\":1}},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"feeder\":null,\"group\":\"RMS\",\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"quantity\":\"QAVG\",\"resolution\":60,\"operator\":\"MULT(2)\",\"aggregationFunction\":\"AVG()\",\"fromComponents\":{\"componentId\":\"a059db13-2390-432a-a062-6ac41f213612\",\"id\":1},\"id\":\"RkA7g\"}]",
  

  "Type": "EXCEPTION",

  "TimeRange": null,
  "ApplyTo": null,
  "Id": 102,
   "ResolutionInSeconds":60
}

""";



                StoreCustomParameter(tenant, Exception_CustomerParameterId, json);
            }
        }

        private void SPMCCreateParameter(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == SPMC_CustomerParameterId);
            if (customerParameter is null)
            {
                var json = """
{
  "Name": "SPMC_CustomParameter",
  "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"MULT(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"}]",
  "Type": "SPMC",
  "TimeRange": null,
  "ApplyTo": null,
  "Id": 103,
   "ResolutionInSeconds":60
}



""";
                //"CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"MULT(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"}]",

                StoreCustomParameter(tenant, SPMC_CustomerParameterId, json);
            }
        }

        private void BPCPCreateParameter(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == BPCP_CustomerParameterId);
            if (customerParameter is null)
            {
                var json = """
{
    "Name": "BPCP",
     "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"aggregationFunction\":\"Avg\",\"Operator\":\"MULT(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"}]",
         "Type": "BPCP",
         "Id":104,
   "ResolutionInSeconds":60
}
""";

                StoreCustomParameter(tenant, BPCP_CustomerParameterId, json);
            }
        }


        private void SPMCArithmeticParameter(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == MultiParameterMC_Arithmetic_CustomerParameterId);
            if (customerParameter is null)
            {
                var json = """
{
  "Name": "SPMC_CustomParameter",
  "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"LOGICAL\",\"name\":\"bp2\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj5a\"}]",
    "Type": "MPSC",
  "TimeRange": null,
  "ApplyTo": null,
  "Id": 105,
"AggregationFunction":"arithmetic({bp1}+{bp2})",
   "ResolutionInSeconds":60

}



""";



                StoreCustomParameter(tenant, MultiParameterMC_Arithmetic_CustomerParameterId, json);
            }
        }



        private void MPSCCreateParameter2222(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == Multi_ParameterCustomeId222);
            if (customerParameter is null)
            {
                var json = """
{
    "Name": "MPSC_CustomParameter_MIN_MAX",
                    "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"min\",\"Operator\":\"Mult(2)\",\"quantity\":\"max\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"aggregationFunction\":\"min\",\"quantity\":\"min\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"zD3e5\"}]",
    "Type": "MPSC",
    "TimeRange": null,
    "ApplyTo": null,
    "Id": 101,
   "ResolutionInSeconds":60
}

""";
                //"aggregationFunction":"arithmetic(bp1+bp2)",

                StoreCustomParameter(tenant, Multi_ParameterCustomeId222, json);
            }
        }


        //---------------------------------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------------------------------

        private void Singale_ParameterCustomerId_Refatoring(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == Single_ParameterCustomerId);
            if (customerParameter is null)
            {
                var json = """
{
  "Name": "SingleParameter_Refactored",
  "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"Mult(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"}]",
  "Type": "SPMC",
  "TimeRange": null,
  "ApplyTo": null,
  "Id": 203,
   "ResolutionInSeconds":60
}



""";

                StoreCustomParameter(tenant, Single_ParameterCustomerId, json);
            }
        }


        private void Singale_Parameter_MaximumPoints_Refatoring(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == Single_MaxPoints_ParameterCustomerId);
            if (customerParameter is null)
            {
                var json = """
{
  "Name": "SingleParameter_MaximumPointsRefactored",
  "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"Mult(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"}]",
  "Type": "SPMC",
  "TimeRange": null,
  "ApplyTo": null,
  "Id": 203,
   "ResolutionInSeconds":60
}



""";

                StoreCustomParameter(tenant, Single_MaxPoints_ParameterCustomerId, json);
            }
        }


        private void Multi_ParameterCustomeId_Refactoring(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == Multi_ParameterCustomeId);
            if (customerParameter is null)
            {
                var json = """
{
    "Name": "MultiParameter_Refactored",
                    "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"Mult(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"zD3e5\"}]",
    "Type": "MPSC",
    "TimeRange": null,
    "ApplyTo": null,
    "Id": 201,
   "ResolutionInSeconds":60
}

""";
                //"aggregationFunction":"arithmetic(bp1+bp2)",

                StoreCustomParameter(tenant, Multi_ParameterCustomeId, json);
            }
        }

        //XXXXXXXXXXXXXXHEREEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEXXXXXXXXXXXXX
        private void Multi_Parameter_Without_BaseParameters__CustomeId_Refactoring(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == Multi_Parameter__Without_BaseParameters_CustomeId);
            if (customerParameter is null)
            {
                var json = """
{
    "Name": "MultiParameter_0_BaseParameters__Refactored",
    "Type": "MPSC",
    "TimeRange": null,
    "ApplyTo": null,
    "Id": 211,
   "ResolutionInSeconds":60
}

""";
                //"aggregationFunction":"arithmetic(bp1+bp2)",

                StoreCustomParameter(tenant, Multi_Parameter__Without_BaseParameters_CustomeId, json);
            }
        }

        private void SPMC_Auto_Percentile_Parameter(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == Single_Auto_And_Percentile_ParameterCustomerId);
            if (customerParameter is null)
            {
                var json = """
{
  "name": "cust_test",
  "aggregationFunction": "PERCENTILE(0.2)",
  "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"fromComponents\":null,\"group\":\"RMS\",\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"quantity\":\"AVG\",\"resolution\":60,\"operator\":null,\"aggregationFunction\":\"Avg()\",\"name\":\"bp1\",\"id\":\"GpNsX\"}]",
  "type": "SPMC",
     "ResolutionInSeconds":60,
  "quantity": "AVG"
}
""";




                StoreCustomParameter(tenant, Single_Auto_And_Percentile_ParameterCustomerId, json);
            }

        }


        //xxxxxxxxxxxxxxxx
        private void Multi_Parameter_1Hour_Resolution_Heighter_WithSingleParameter_Refactoring(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == Multi_Parameter_With_Single_CustomParameter);
            if (customerParameter is null)
            {
                var json = """
        {
            "Name": "MultiParameter_With_SingleParameter_Refactored",
            "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"MULT(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"zD3e5\"}]",
            "Type": "MPSC",
            "TimeRange": null,
            "ApplyTo": null,
            "Id": 201,
            "InnerCustomParameters": "[{\"CustomParameterId\":203,\"Quantity\":\"AVG\",\"Resolution\":7200,\"Operator\":\"absolute\",\"InnerAggregationFunction\":\"AVG\"}]",
               "ResolutionInSeconds":3600
        }
        """;
                //HERE
                StoreCustomParameter(tenant, Multi_Parameter_With_Single_CustomParameter, json);
            }
        }

        private void Multi_Parameter_1Hour_Resolution_Heighter_WithSingleParameter_Refactoring_Override_10Min(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == Multi_Parameter_With_Single_CustomParameter_id);
            if (customerParameter is null)
            {
                var json = """
        {
            "Name": "MultiParameter_With_SingleParameter_Refactored",
            "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"mult(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"zD3e5\"}]",
            "Type": "MPSC",
            "TimeRange": null,
            "ApplyTo": null,
            "Id": 201,
            "InnerCustomParameters": "[{\"CustomParameterId\":203,\"Quantity\":\"AVG\",\"Resolution\":600,\"Operator\":\"absolute\",\"InnerAggregationFunction\":\"AVG\"}]",
              "ResolutionInSeconds":3600
        }
        """;
                //HERE
                StoreCustomParameter(tenant, Multi_Parameter_With_Single_CustomParameter_id, json);
            }
        }



        private void Multi_Parameter_Zero_BaseParameter_With_Single_CustomParameter(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == Multi_Parameter_Zero_BaseParameter_With_Single_CustomParameter_id);
            if (customerParameter is null)
            {
                var json = """
        {
            "Name": "MultiParameter_With_SingleParameter_Refactored",
            "Type": "MPSC",
            "TimeRange": null,
            "ApplyTo": null,
            "Id": 203,
            "InnerCustomParameters": "[{\"CustomParameterId\":203,\"Quantity\":\"AVG\",\"Resolution\":600,\"Operator\":\"absolute\",\"InnerAggregationFunction\":\"AVG\"}]",
            "ResolutionInSeconds":3600
        }
        """;
                //HERE
                StoreCustomParameter(tenant, Multi_Parameter_Zero_BaseParameter_With_Single_CustomParameter_id, json);
            }
        }

        private void SingleParameter_ParameterCustomeId_With_Multi_Parameter_Refactoring(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters
         .FirstOrDefault(x => x.Id == Single_Parameter_With_Multi_CustomParameter);

            if (customerParameter is null)
            {
                var json = """
        {
            "Name": "Single_With_MultiParameter_Refactored",
            "Type": "SPMC",
            "TimeRange": null,
            "ApplyTo": null,
            "Id": 201,
            "InnerCustomParameters": "[{\"CustomParameterId\":201,\"Quantity\":\"AVG\",\"Resolution\":7200,\"Operator\":\"absolute\",\"InnerAggregationFunction\":\"AVG\"}]",
        "ResolutionInSeconds":60
        }
        """;

                StoreCustomParameter(tenant, Single_Parameter_With_Multi_CustomParameter, json);
            }
        }


        private void Singale_Parameter__1Hour__Refatoring(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == Single_Parameter_With_1Hour__Resolution);
            if (customerParameter is null)
            {
                var json = """
{
  "Name": "SingleParameter_Refactored",
  "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"MULT(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"}]",
  "Type": "SPMC",
  "TimeRange": null,
  "ApplyTo": null,
  "Id": 213,
 "ResolutionInSeconds":3600

}



""";

                StoreCustomParameter(tenant, Single_Parameter_With_1Hour__Resolution, json);
            }
        }

        private void Multi_With_Single_Parameter_1Hour_Refactoring(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters
                .FirstOrDefault(x => x.Id == Multi_WitnSingle_With_1Hour__Resolution);

            if (customerParameter is null)
            {
                var json = """
        {
            "Name": "MultiParameter_Refactored",
            "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"MULT(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"zD3e5\"}]",
            "Type": "MPSC",
            "TimeRange": null,
            "ApplyTo": null,
            "Id": 214,
            "InnerCustomParameters": "[{\"CustomParameterId\":213,\"Quantity\":\"AVG\",\"Resolution\":7200,\"Operator\":\"absolute\",\"InnerAggregationFunction\":\"AVG\"}]",
        "ResolutionInSeconds":60
        }
        """;

                StoreCustomParameter(tenant, Multi_WitnSingle_With_1Hour__Resolution, json);
            }
        }


        private void MultiComponent_With_Multi_Custom_Parameter_Component(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters
                .FirstOrDefault(x => x.Id == Multi_Parameter_With_Multi_CustomParameter);

            if (customerParameter is null)
            {
                var json = """
        {
            "Name": "Multi_Parameter_With_Multi_CustomParameter",
            "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"MULT(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"zD3e5\"}]",
            "Type": "MPSC",
            "TimeRange": null,
            "ApplyTo": null,
            "Id": 209,
            "InnerCustomParameters": "[{\"CustomParameterId\":201,\"Quantity\":\"AVG\",\"Resolution\":7200,\"Operator\":\"absolute\",\"InnerAggregationFunction\":\"AVG\"}]",
        "ResolutionInSeconds":60
        }
        """;

                StoreCustomParameter(tenant, Multi_Parameter_With_Multi_CustomParameter, json);
            }
        }


        private void SingleComponent_With_2222_Multi_Custom_Parameter_Component(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters
                .FirstOrDefault(x => x.Id == Single_Parameter_With_Multi_And_Multi_CustomParameter);

            if (customerParameter is null)
            {
                var json = """
        {
            "Name": "MultiParameter_With_SingleParameter_Refactored",
            "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"MULT(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"zD3e5\"}]",
            "Type": "SPMC",
            "TimeRange": null,
            "ApplyTo": null,
            "Id": 210,
            "InnerCustomParameters": "[{\"CustomParameterId\":201,\"Quantity\":\"AVG\",\"Resolution\":7200,\"Operator\":\"absolute\",\"InnerAggregationFunction\":\"AVG\"},{\"CustomParameterId\":201,\"Quantity\":\"AVG\",\"Resolution\":7200,\"Operator\":\"absolute\",\"InnerAggregationFunction\":\"AVG\"}]",
        "ResolutionInSeconds":60
        }
        """;

                StoreCustomParameter(tenant, Single_Parameter_With_Multi_And_Multi_CustomParameter, json);
            }
        }



        private void SingleParameter_With_Multi_Parameter_0_BaseParameters_Refactoring(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters
                .FirstOrDefault(x => x.Id == Single_Parameter_With_Multi_0_BaseParameters);

            if (customerParameter is null)
            {
                var json = """
        {
            "Name": "Single_With_MultiParameter_Refactored",
            "Type": "SPMC",
            "TimeRange": null,
            "ApplyTo": null,
            "Id": 212,
            "InnerCustomParameters": "[{\"CustomParameterId\":211,\"Quantity\":\"AVG\",\"Resolution\":7200,\"Operator\":\"absolute\",\"InnerAggregationFunction\":\"AVG\"}]",
        "ResolutionInSeconds":60
        }
        """;

                StoreCustomParameter(tenant, Single_Parameter_With_Multi_0_BaseParameters, json);
            }
        }

        private void MultiComponentSanity_Without_Interpolation_Component(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters
                .FirstOrDefault(x => x.Id == Sanity_Without_Interpolation_id);

            if (customerParameter is null)
            {
                var json = """
        {
            "Name": "MultiParameter_Without_Interpolation_SingleParameter_Refactored",
            "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"Mult(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"zD3e5\"}]",
            "Type": "MPSC",
            "TimeRange": null,
            "ApplyTo": null,
            "Id": 209,
            "ResolutionInSeconds":3600,
            "InnerCustomParameters": "[{\"CustomParameterId\":203,\"Quantity\":\"AVG\",\"Resolution\":3600,\"InnerAggregationFunction\":\"AVG\"}]"
        }
        """;

                StoreCustomParameter(tenant, Sanity_Without_Interpolation_id, json);
            }
        }

        private void MultiComponentSanity_With_Interpolation_Component(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters
                .FirstOrDefault(x => x.Id == Sanity_With_Interpolation_id);

            if (customerParameter is null)
            {
                var json = """
        {
            "Name": "MultiParameter_With_Interpolation_SingleParameter_Refactored",
            "CustomBaseDataList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"Mult(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"UV1N\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"6nCqj\"},{\"type\":\"CHANNEL\",\"name\":\"bp2\",\"aggregationFunction\":\"Avg\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":60,\"phase\":\"CH_1\",\"harmonics\":{\"range\":null,\"rangeOn\":null},\"baseResolution\":\"BHCYC\",\"id\":\"zD3e5\"}]",
            "Type": "MPSC",
            "TimeRange": null,
            "ApplyTo": null,
            "Id": 209,
            "ResolutionInSeconds":3600,
        
            "InnerCustomParameters": "[{\"CustomParameterId\":203,\"Quantity\":\"AVG\",\"Resolution\":7200,\"InnerAggregationFunction\":\"AVG\"}]"
        }
        """;

                StoreCustomParameter(tenant, Sanity_With_Interpolation_id, json);
            }
        }



        private void Sanity_Multi_WithZero_Parameters_Id_Component(Tenant tenant)
        {
            var customerParameter = _context.CustomParameters.FirstOrDefault(x => x.Id == Sanity_Multi_WithZero_Parameters_Id);

            if (customerParameter is null)
            {
                var json = """
        {
            "Name": "MultiParameter_With_ZeroBaseParameters_Refactored",
            "Type": "MPSC",
            "TimeRange": null,
            "ApplyTo": null,
            "Id": 209,
            "ResolutionInSeconds":3600,
            "InnerCustomParameters": "[{\"CustomParameterId\":203,\"Quantity\":\"AVG\",\"Resolution\":3600,\"InnerAggregationFunction\":\"AVG\"}]"
        }
        """;

                StoreCustomParameter(tenant, Sanity_Multi_WithZero_Parameters_Id, json);
            }
        }


        private void StoreCustomParameter(Tenant tenant, int id, string json)
        {
            var dto = JsonConvert.DeserializeObject<CustomParameter>(json);
            dto.TenantId = tenant.Id;
            dto.Id = id;
            //dto.Resolution = dto.Resolution ?? "IS1MIN";
            //dto.ResolutionInSeconds = GroupByCalcFunction.ParseAndConvertToSecond(dto.Resolution);
            dto.ResolutionInSeconds = dto.ResolutionInSeconds;
            dto.AggregationFunction = dto.AggregationFunction ?? "avg";
            //dto.Quantity = AvgCalcFunction.Avg_Function;
            //dto.QuantityResolution = dto.QuantityResolution ?? "IS1MIN";

            _context.CustomParameters.Add(dto);
            _context.SaveChanges();
        }

        private void CreateDefaultTenant()
        {
            //Default tenant

            var defaultTenant = _context.Tenants.IgnoreQueryFilters().FirstOrDefault(t => t.TenancyName == MultiTenancy.Tenant.DefaultTenantName);
            if (defaultTenant == null)
            {
                defaultTenant = new MultiTenancy.Tenant(AbpTenantBase.DefaultTenantName, AbpTenantBase.DefaultTenantName);

                var defaultEdition = _context.Editions.IgnoreQueryFilters().FirstOrDefault(e => e.Name == EditionManager.DefaultEditionName);
                if (defaultEdition != null)
                {
                    defaultTenant.EditionId = defaultEdition.Id;
                }

                defaultTenant.IsInTrialPeriod = false;

                _logger.LogInformation($"DefaultTenantBuilder PQSServiceRestUrl = {_tenantConfig.PQSComunication.PQSServiceRestUrl}");

                defaultTenant.PQSServiceUrl = _tenantConfig.PQSComunication.PQSServiceRestUrl;
                defaultTenant.PQSCommunitcationType = _tenantConfig.PQSComunication.DefaultCommunicationType;

                _context.Tenants.Add(defaultTenant);
                _context.SaveChanges();

                Singale_ParameterCustomerId_Refatoring(defaultTenant);
                Multi_ParameterCustomeId_Refactoring(defaultTenant);
                MultiComponentSanity_Without_Interpolation_Component(defaultTenant);
                MultiComponentSanity_With_Interpolation_Component(defaultTenant);
                Sanity_Multi_WithZero_Parameters_Id_Component(defaultTenant);

                ExceptionCreateParameter(defaultTenant);

                //SPMCArithmeticParameter(defaultTenant);
                //Singale_Parameter_MaximumPoints_Refatoring(defaultTenant);
                //SPMC_Auto_Percentile_Parameter(defaultTenant);

                //MPSCCreateParameter2222(defaultTenant);

                //Multi_Parameter_1Hour_Resolution_Heighter_WithSingleParameter_Refactoring(defaultTenant);
                Multi_Parameter_1Hour_Resolution_Heighter_WithSingleParameter_Refactoring_Override_10Min(defaultTenant);
                //SingleParameter_ParameterCustomeId_With_Multi_Parameter_Refactoring(defaultTenant);
                MultiComponent_With_Multi_Custom_Parameter_Component(defaultTenant);
                //SingleComponent_With_2222_Multi_Custom_Parameter_Component(defaultTenant);


                //Multi_Parameter_Without_BaseParameters__CustomeId_Refactoring(defaultTenant);
                //SingleParameter_With_Multi_Parameter_0_BaseParameters_Refactoring(defaultTenant);
                //Singale_Parameter__1Hour__Refatoring(defaultTenant);
                //Multi_With_Single_Parameter_1Hour_Refactoring(defaultTenant);

                //Multi_Parameter_Zero_BaseParameter_With_Single_CustomParameter(defaultTenant);
            }
            else
            {
                _logger.LogInformation($"DefaultTenantBuilder defaultTenant present.");

            }
        }
    }
}

