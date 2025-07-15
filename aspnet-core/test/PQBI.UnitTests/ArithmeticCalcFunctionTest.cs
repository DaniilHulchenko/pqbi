using FluentAssertions;
using PQBI.CalculationEngine.Functions.Aggregation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQBI.UnitTests
{
    public class ArithmeticCalcFunctionTest
    {

        [Fact]
        public async Task ArithmeticCalcFunctionn_Division_By_Zero()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(1, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var arithmetic = "({bp1}/{bp2})";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            Func<Task> act = async () => await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Division_By_Zero_DefaultValue()
        {
            const double errorDefaultValue = -100;
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(1, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            expectedResult[0] = errorDefaultValue;
            for (int i = 1; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] / bp2[i];
            }

            var arithmetic = "({bp1}/{bp2})";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: false, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }


        [Fact]
        public async Task ArithmeticCalcFunctionn_Division()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] / 10;
            }

            var arithmetic = "({bp1}/10)";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_XXXXX()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var arithmetic = "{bp1}+{bp2}";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            //await act.Should().ThrowAsync<Exception>();
        }



        [Fact]
        public async Task ArithmeticCalcFunctionn_Const()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var arithmetic = "{bp1}+10";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(i + 10);
            }
        }


        [Fact]
        public async Task ArithmeticCalcFunctionn_ReturnException()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var arithmetic = "{bp1}+{bpbb2}";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            Func<Task> act = async () => await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            await act.Should().ThrowAsync<Exception>();
        }


        [Fact]
        public async Task ArithmeticCalcFunctionn_Adding()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var arithmetic = "{bp1}+{bp2}";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(i * 2);
            }
        }


        [Fact]
        public async Task ArithmeticCalcFunctionn_DoubleAdding()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var arithmetic = "{bp1}+{bp2}+{bp1}";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(i * 3);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Adding_With_Parantesses()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var arithmetic = "({bp1}+{bp2})";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(i * 2);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Adding_With_ParantessesWithAdding()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] * 2;
                expectedResult[i] *= bp1[i];
            }

            var arithmetic = "{bp1}*({bp1}+{bp2})";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity_1()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] + 10;
                expectedResult[i] *= bp1[i];
            }

            var arithmetic = "{bp1}*({bp1}+10)";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }


        public async Task ArithmeticCalcFunctionn_Sanity_2()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] + 10.25;
                expectedResult[i] *= bp1[i];
            }

            var arithmetic = "{bp1}*({bp1}+10.25)";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Multipal()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] * bp1[i];
                expectedResult[i] += bp1[i];
            }

            var arithmetic = "{bp1}+{bp1}*{bp2}";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity3()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] + bp1[i] + bp2[i] + bp1[i] + bp2[i];
            }

            var arithmetic = "{bp1}+({bp1}+{bp2})+({bp1}+{bp2})";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }



        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity4()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = (bp1[i] * bp1[i]) * 2;
            }

            var arithmetic = "({bp1}*{bp2})+({bp1}*{bp2})";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity5()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] + ((bp1[i] * bp1[i]) * 4);
            }

            var arithmetic = "{bp1}+(({bp1}*{bp2})*(2*2))";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }


        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity6()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = 7;
            }

            var arithmetic = "(2*2)+3";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "bp2", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }


        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity7()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = 12;
            }

            var arithmetic = "(2+2)*3";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }


        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity8()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = 8;
            }

            var arithmetic = "2+2*3";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity9()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = 17;
            }

            var arithmetic = "2+(1+2+3+4)+5";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity10()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = 25;
            }

            var arithmetic = "5+(2*2+4*4)";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity11()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = 4;
            }

            var arithmetic = "((((2*2))))";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity12()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = 6;
            }

            var arithmetic = "(2+((((2*2)))))";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }


        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity13()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = (bp1[i] * bp2[i]) * (bp1[i] * bp2[i]);
            }

            var arithmetic = "({bp1}*{bp2})*({bp1}*{bp2})";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity14()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] * 5;
            }

            var arithmetic = "({bp1}*5)";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity15()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] + 5;
            }

            var arithmetic = "({bp1}+5)";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity16()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] + 5;
            }

            var arithmetic = "(5+{bp1})";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity17()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] + 10;
            }

            var arithmetic = "(5+{bp1})+5";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        [Fact]
        public async Task ArithmeticCalcFunctionn_Sanity18()
        {
            var engine = new ArithmeticCalcFunction();
            var bp1 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
            var bp2 = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();

            var expectedResult = new double[bp1.Length];

            for (int i = 0; i < bp1.Length; i++)
            {
                expectedResult[i] = bp1[i] + 10;
            }

            var arithmetic = "5+({bp1}+5)";
            var varibles = new Dictionary<string, double[]> { { "{bp1}", bp1 }, { "{bp2}", bp2 } };
            var result = await engine.Calculation(arithmetic, varibles, isThrowException: true, -100);

            for (int i = 0; i < 10; i++)
            {
                result[i].Should().Be(expectedResult[i]);
            }
        }

        //-----------------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------





        //-----------------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------

        [Fact]
        public async Task ArithmeticCalcFunctionn_GetArguments()
        {
            var tmp = "arithmetics(({newbp}+3))";
            var tol = ArithmeticCalcFunction.getArguments(tmp);
            tol.Should().Be("({newbp}+3)");

            tmp = "arithmetics(({newbp}+3 +{bp2}*3))";
            tol = ArithmeticCalcFunction.getArguments(tmp);
            tol.Should().Be("({newbp}+3 +{bp2}*3)");


        }
    }
}
