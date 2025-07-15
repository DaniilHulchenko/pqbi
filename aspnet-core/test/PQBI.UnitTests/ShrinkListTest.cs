//using FluentAssertions;
//using NUglify.JavaScript;
//using PQBI.Infrastructure;
//using PQBI.Network.RestApi.EngineCalculation;
//using PQS.CommonUI.Data;
//using PQS.CommonUI.Enums;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace PQBI.UnitTests
//{
//    public class ShrinkListTest
//    {

//        [Fact]
//        public void List_Zero()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var ptr = shrinkList.Shrink(new List<MagicBox>());
//            ptr.Count().Should().Be(0);
//        }


//        [Fact]
//        public void TwoItems_Return_TwoItems()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second
//            };

//            var ptr = shrinkList.Shrink(input);

//            ptr.Count().Should().Be(1);
//            ptr.First().Value.Count().Should().Be(2);
//        }

//        [Fact]
//        public void TwoItemsOverlapping_Return_1_Item()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 1000 // 1 second
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second
//            };

//            var ptr = shrinkList.Shrink(input);

//            ptr.Count().Should().Be(1);
//            ptr.First().Value.Count().Should().Be(1);
//        }

//        [Fact]
//        public void ThreeItems_Return_3_Items()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var third = new MagicBox
//            {
//                Id = "3",
//                StartTime = second.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second,third
//            };

//            var ptr = shrinkList.Shrink(input);
//            ptr.Count().Should().Be(1);
//            ptr.First().Value.Count().Should().Be(3);
//        }

//        [Fact]
//        public void ThreeItemsLastOverlaping_Return_2_Items()
//        {
//            const string firstName = "1";
//            const string secondName = "2";

//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 1000
//            };

//            var third = new MagicBox
//            {
//                Id = "3",
//                StartTime = second.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second,third
//            };

//            var ptr = shrinkList.Shrink(input);

//            ptr.Count().Should().Be(1);
//            ptr.First().Value.Count().Should().Be(2);
//            ptr.First().Value.First().Id.Should().Be(firstName);
//            ptr.First().Value.Last().Id.Should().Be(secondName);

//            //ptr.Count().Should().Be(2);
//            //ptr.First().Id.Should().Be(firstName);
//            //ptr.Last().Id.Should().Be(secondName);

//        }

//        [Fact]
//        public void FifeItems_Return_3_Items()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var third = new MagicBox
//            {
//                Id = "3",
//                StartTime = second.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var forth = new MagicBox
//            {
//                Id = "4",
//                StartTime = third.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var fife = new MagicBox
//            {
//                Id = "5",
//                StartTime = forth.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second,third,forth, fife
//            };

//            var ptr = shrinkList.Shrink(input);

//            ptr.Count().Should().Be(1);
//            ptr.First().Value.Count().Should().Be(5);
//            //ptr.Count().Should().Be(5);
//        }


//        [Fact]
//        public void FifeItemsOverlapping_Return_4_Items()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var third = new MagicBox
//            {
//                Id = "3",
//                StartTime = second.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 1000
//            };

//            var forth = new MagicBox
//            {
//                Id = "4",
//                StartTime = third.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var fife = new MagicBox
//            {
//                Id = "5",
//                StartTime = forth.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second,third,forth, fife
//            };

//            var ptr = shrinkList.Shrink(input).ToList();

//            ptr.Count().Should().Be(1);

//            var mtr = ptr.First().Value;
//            mtr.Count().Should().Be(4);


//            mtr[0].Id.Should().Be("1");
//            mtr[1].Id.Should().Be("2");
//            mtr[2].Id.Should().Be("3");
//            mtr[3].Id.Should().Be("5");

//            //ptr.Count().Should().Be(4);
//        }

//        //----------------------------------------------------------With Delta milliSecomnds------------------------------------------
//        //----------------------------------------------------------With Delta milliSecomnds------------------------------------------
//        //----------------------------------------------------------With Delta milliSecomnds------------------------------------------

//        [Fact]
//        public void TwoItems_Delta_Return_TwoItems()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second
//            };

//            var ptr = shrinkList.Shrink(input, 10);

//            ptr.Count().Should().Be(1);

//            var mtr = ptr.First().Value;
//            mtr.Count().Should().Be(2);

//            //ptr.Count().Should().Be(2);
//        }

//        [Fact]
//        public void TwoItemsOverlapping_Delta_Return_1_Item()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 500 // 1 second
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second
//            };

//            var ptr = shrinkList.Shrink(input, 500);

//            ptr.Count().Should().Be(1);

//            var mtr = ptr.First().Value;
//            mtr.Count().Should().Be(1);

//            //ptr.Count().Should().Be(1);
//        }

//        [Fact]
//        public void ThreeItems_Delta_Return_3_Items()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var third = new MagicBox
//            {
//                Id = "3",
//                StartTime = second.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second,third
//            };

//            var ptr = shrinkList.Shrink(input, 500);

//            ptr.Count().Should().Be(1);

//            var mtr = ptr.First().Value;
//            mtr.Count().Should().Be(3);
//            //ptr.Count().Should().Be(3);
//        }

//        [Fact]
//        public void ThreeItemsLastOverlaping_Delta_Return_2_Items()
//        {
//            const string firstName = "1";
//            const string secondName = "2";

//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 500
//            };

//            var third = new MagicBox
//            {
//                Id = "3",
//                StartTime = second.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second,third
//            };

//            var ptr = shrinkList.Shrink(input, 500);

//            ptr.Count().Should().Be(1);

//            var mtr = ptr.First().Value;
//            mtr.Count().Should().Be(2);
//            mtr.First().Id.Should().Be(firstName);
//            mtr.Last().Id.Should().Be(secondName);

//            //ptr.Count().Should().Be(2);
//            //ptr.First().Id.Should().Be(firstName);
//            //ptr.Last().Id.Should().Be(secondName);

//        }

//        [Fact]
//        public void FifeItems_Delta_Return_5_Items()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var third = new MagicBox
//            {
//                Id = "3",
//                StartTime = second.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var forth = new MagicBox
//            {
//                Id = "4",
//                StartTime = third.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var fife = new MagicBox
//            {
//                Id = "5",
//                StartTime = forth.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second,third,forth, fife
//            };

//            var ptr = shrinkList.Shrink(input, 500);

//            ptr.Count().Should().Be(1);

//            var mtr = ptr.First().Value;
//            mtr.Count().Should().Be(5);

//            //ptr.Count().Should().Be(5);
//        }


//        [Fact]
//        public void FifeItemsOverlapping_Delta_Return_4_Items()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var third = new MagicBox
//            {
//                Id = "3",
//                StartTime = second.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 500
//            };

//            var forth = new MagicBox
//            {
//                Id = "4",
//                StartTime = third.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var fife = new MagicBox
//            {
//                Id = "5",
//                StartTime = forth.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second,third,forth, fife
//            };

//            var ptr = shrinkList.Shrink(input, 500).ToList();

//            ptr.Count().Should().Be(1);

//            var mtr = ptr.First().Value;
//            mtr.Count().Should().Be(4);
//            mtr[0].Id.Should().Be("1");
//            mtr[1].Id.Should().Be("2");
//            mtr[2].Id.Should().Be("3");
//            mtr[3].Id.Should().Be("5");

//            //ptr.Count().Should().Be(4);

//            //ptr[0].Id.Should().Be("1");
//            //ptr[1].Id.Should().Be("2");
//            //ptr[2].Id.Should().Be("3");
//            //ptr[3].Id.Should().Be("5");
//        }

//        //----------------------------------------------------------With Names------------------------------------------
//        //----------------------------------------------------------With Names------------------------------------------
//        //----------------------------------------------------------With Names------------------------------------------



//        [Fact]
//        public void TwoDifferentItems_Names_Return_TwoItems()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first_1 = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100,
//                EventId = "1"
//            };

//            var second_1 = new MagicBox
//            {
//                Id = "2",
//                StartTime = first_1.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "2"
//            };

//            var input = new List<MagicBox>
//            {
//                first_1, second_1
//            };

//            var ptr = shrinkList.Shrink(input);

//            ptr.Count().Should().Be(2);
//            ptr.First().Value.Count().Should().Be(1);
//            ptr.Last().Value.Count().Should().Be(1);



//            var first_2 = new MagicBox
//            {
//                Id = "3",
//                StartTime = DateTime.Now.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "1"
//            };

//            var second_2 = new MagicBox
//            {
//                Id = "4",
//                StartTime = first_1.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "2"
//            };

//            input = new List<MagicBox>
//            {
//                first_1, second_1,first_2, second_2
//            };

//            ptr = shrinkList.Shrink(input);

//            ptr.Count().Should().Be(2);
//            var first = ptr.First().Value;
//            var second = ptr.Last().Value;
//        }



//        [Fact]
//        public void TwoDifferentItemsOverlapping_Return_1_Item()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 1000 // 1 second
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100
//            };

//            var input = new List<MagicBox>
//            {
//                first, second
//            };

//            var ptr = shrinkList.Shrink(input);

//            ptr.Count().Should().Be(1);
//            ptr.First().Value.Count().Should().Be(1);
//        }

//        [Fact]
//        public void ThreeDifferentItems_Return_3_Items()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first_1 = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100,
//                EventId = "1"
//            };

//            var second_1 = new MagicBox
//            {
//                Id = "2",
//                StartTime = first_1.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "2"
//            };

//            var third_1 = new MagicBox
//            {
//                Id = "3",
//                StartTime = second_1.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "3"
//            };

//            var input = new List<MagicBox>
//            {
//                first_1, second_1,third_1
//            };

//            var ptr = shrinkList.Shrink(input).ToList();
//            ptr.Count().Should().Be(3);
//            var first = ptr[0].Value;
//            var second = ptr[1].Value;
//            var third = ptr[2].Value;

//            first.Count.Should().Be(1);
//            second.Count.Should().Be(1);
//            third.Count.Should().Be(1);


//            var first_2 = new MagicBox
//            {
//                Id = "3",
//                StartTime = first_1.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "1"
//            };

//            var second_2 = new MagicBox
//            {
//                Id = "4",
//                StartTime = second_1.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "2"
//            };

//            var third_2 = new MagicBox
//            {
//                Id = "5",
//                StartTime = third_1.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "3"
//            };

//            input = new List<MagicBox>
//            {
//                first_1, second_1,third_1,first_2,second_2,third_2
//            };

//            ptr = shrinkList.Shrink(input).ToList();
//            ptr.Count().Should().Be(3);
//            var tag1 = ptr[0].Value;
//            var tag2 = ptr[1].Value;
//            var tag3 = ptr[2].Value;

//            tag1.Count.Should().Be(2);
//            tag2.Count.Should().Be(2);
//            tag3.Count.Should().Be(2);
//        }

//        [Fact]
//        public void ThreeeDifferentItemsLastOverlaping_Return_2_Items()
//        {
//            const string firstName = "1";
//            const string secondName = "2";

//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100,
//                EventId = "2"
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 1000,
//                EventId = "1"
//            };

//            var third = new MagicBox
//            {
//                Id = "3",
//                StartTime = second.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "1"
//            };

//            var input = new List<MagicBox>
//            {
//                first, second,third
//            };

//            var result = shrinkList.Shrink(input);

//            result.Count().Should().Be(2);

//            var isExists = result.TryGetValue("1", out var list);
//            isExists.Should().BeTrue();

//            list.Count().Should().Be(1);

//            isExists = result.TryGetValue("2", out list);
//            isExists.Should().BeTrue();

//            list.Count().Should().Be(1);


//        }

//        [Fact]
//        public void FifeDifferentItems_Return_5_Items()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100,
//                EventId = "1"
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "2"
//            };

//            var third = new MagicBox
//            {
//                Id = "3",
//                StartTime = second.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "3"
//            };

//            var forth = new MagicBox
//            {
//                Id = "4",
//                StartTime = third.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "4"
//            };

//            var fife = new MagicBox
//            {
//                Id = "5",
//                StartTime = forth.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "5"
//            };

//            var input = new List<MagicBox>
//            {
//                first, second,third,forth, fife
//            };

//            var ptr = shrinkList.Shrink(input);

//            ptr.Count().Should().Be(5);

//            foreach (var keyAndValue in ptr)
//            {
//                keyAndValue.Value.Count().Should().Be(1);
//            }
//            //ptr.Count().Should().Be(5);
//        }


//        [Fact]
//        public void FifeDifferentItemsOverlapping_Return_4_Items()
//        {
//            var shrinkList = new ShrinkList<MagicBox>();

//            var first = new MagicBox
//            {
//                Id = "1",
//                StartTime = DateTime.Now,
//                DurationMilliSeconds = 100,
//                EventId = "1"
//            };

//            var second = new MagicBox
//            {
//                Id = "2",
//                StartTime = first.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "1"
//            };

//            var third = new MagicBox
//            {
//                Id = "3",
//                StartTime = second.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 1000,
//                EventId = "1"
//            };

//            var forth = new MagicBox
//            {
//                Id = "4",
//                StartTime = third.StartTime.AddSeconds(1),
//                DurationMilliSeconds = 100,
//                EventId = "1"
//            };

//            var fife = new MagicBox
//            {
//                Id = "5",
//                StartTime = forth.StartTime.AddSeconds(0.5),
//                DurationMilliSeconds = 100,
//                EventId = "1"
//            };

//            var input = new List<MagicBox>
//            {
//                first, second,third,forth, fife
//            };

//            var ptr = shrinkList.Shrink(input).ToList();

//            ptr.Count().Should().Be(1);
//            ptr.First().Value.Count().Should().Be(4);   

//            //foreach (var keyAndValue in ptr)
//            //{
//            //    keyAndValue.Value.Count().Should().Be(1);
//            //}


//            //ptr.Count().Should().Be(1);

//            //var mtr = ptr.First().Value;
//            //mtr.Count().Should().Be(4);


//            //mtr[0].Id.Should().Be("1");
//            //mtr[1].Id.Should().Be("2");
//            //mtr[2].Id.Should().Be("3");
//            //mtr[3].Id.Should().Be("5");

//        }

//    }


//    public class MagicBox : IShrinkBusiness
//    {
//        public string Id { get; set; }

//        public DateTime StartTime { get; init; }

//        public int DurationMilliSeconds { get; init; }
//        public string EventId { get; init; }
//        public string Phase { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

//        public string ComponentId => throw new NotImplementedException();

//        public string Feeder => throw new NotImplementedException();
//    }
//}