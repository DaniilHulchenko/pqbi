using PQS.Data.Measurements.Enums;
using PQZTimeFormat;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml;

namespace PQBI.Sapphire.Options
{

    public class CalcBase : ICloneable, IComparable<CalcBase>, IXmlSerializable
    {
        #region Members

        protected double _calcInSec;
        protected int _numOfSamples;

        #endregion

        #region Ctor
        protected CalcBase() { }

        public CalcBase(CalculationBase calculationBase)
        {
            CalculationBaseEnum = calculationBase;
        }

        public CalcBase(CalculationBase calculationBase, double timeInSec)
        {
            CalculationBaseEnum = calculationBase;
            _calcInSec = timeInSec;
        }

        public CalcBase(CalculationBase calculationBase, double timeInSec, OldCalculationBaseEnum oldBase)
        {
            OldCalculationBaseEnum = oldBase;
            CalculationBaseEnum = calculationBase;
            _calcInSec = timeInSec;
        }

        public CalcBase(CalculationBase calculationBase, int numOfSamples, OldCalculationBaseEnum oldBase)
        {
            OldCalculationBaseEnum = oldBase;
            CalculationBaseEnum = calculationBase;
            _numOfSamples = numOfSamples;
        }

        #endregion

        #region Private

        //private void initTimeIntervalInSec()
        //{
        //    switch (SyncIntervalEnum)
        //    {
        //        case IntervalSynchronized.IS1YEAR:
        //            TimeIntervalInSec = PQZTimeSpan.FromDays(365).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS1MONTH:
        //            TimeIntervalInSec = PQZTimeSpan.FromDays(30).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS1WEEK:
        //            TimeIntervalInSec = PQZTimeSpan.FromDays(7).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS1DAY:
        //            TimeIntervalInSec = PQZTimeSpan.FromDays(1).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS2HOUR:
        //            TimeIntervalInSec = PQZTimeSpan.FromHours(2).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS1HOUR:
        //            TimeIntervalInSec = PQZTimeSpan.FromHours(1).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS30MIN:
        //            TimeIntervalInSec = PQZTimeSpan.FromMinutes(30).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS15MIN:
        //            TimeIntervalInSec = PQZTimeSpan.FromMinutes(15).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS10MIN:
        //            TimeIntervalInSec = PQZTimeSpan.FromMinutes(10).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS5MIN:
        //            TimeIntervalInSec = PQZTimeSpan.FromMinutes(5).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS3MIN:
        //            TimeIntervalInSec = PQZTimeSpan.FromMinutes(3).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS1MIN:
        //            TimeIntervalInSec = 60;
        //            break;
        //        case IntervalSynchronized.IS30SEC:
        //            TimeIntervalInSec = 30;
        //            break;
        //        case IntervalSynchronized.IS10SEC:
        //            TimeIntervalInSec = 10;
        //            break;
        //        case IntervalSynchronized.IS5SEC:
        //            TimeIntervalInSec = 5;
        //            break;
        //        case IntervalSynchronized.IS3SEC:
        //            TimeIntervalInSec = 3;
        //            break;
        //        case IntervalSynchronized.IS1SEC:
        //            TimeIntervalInSec = 1;
        //            break;
        //        case IntervalSynchronized.IS200MS:
        //            TimeIntervalInSec = PQZTimeSpan.FromMilliseconds(200).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.ISCYC:
        //            TimeIntervalInSec = PQZTimeSpan.FromMilliseconds(20).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.ISHC:
        //            TimeIntervalInSec = PQZTimeSpan.FromMilliseconds(10).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS64PC:
        //            TimeIntervalInSec = PQZTimeSpan.FromMicroseconds(312.5).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS128PC:
        //            TimeIntervalInSec = PQZTimeSpan.FromMicroseconds(156.25).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS256PC:
        //            TimeIntervalInSec = PQZTimeSpan.FromMicroseconds(78.125).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS512PC:
        //            TimeIntervalInSec = PQZTimeSpan.FromMicroseconds(39.0625).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.IS1024PC:
        //            TimeIntervalInSec = PQZTimeSpan.FromMicroseconds(19.53125).TotalSeconds;
        //            break;
        //        case IntervalSynchronized.ISX:
        //            break;
        //        case IntervalSynchronized.ISN:
        //            TimeIntervalInSec = double.NaN;
        //            break;
        //        default:
        //            break;
        //    }
        //}

        #endregion

        #region Properties

        /// <summary>
        /// The parameter time interval. 
        /// If the interval is not defined in the enums than the value is : ISX. 
        /// If the value is not sync (random time stamps) than the value is : IN
        /// </summary>
        public CalculationBase CalculationBaseEnum { get; set; }
        public OldCalculationBaseEnum OldCalculationBaseEnum { get; set; }

        public double GetCalculationBaseInSec(double freq = 50)
        {
            double calcInSec = _calcInSec;
            if (calcInSec == 0)
            {
                switch (CalculationBaseEnum)
                {
                    case CalculationBase.BAUTO:
                        {
                            return 0;
                        }
                    case CalculationBase.BS:
                        {
                            double cycleDuration = 1 / freq;
                            switch (OldCalculationBaseEnum)
                            {
                                case OldCalculationBaseEnum.BHCYC:
                                    calcInSec = cycleDuration / 2 * _numOfSamples;
                                    break;
                                case OldCalculationBaseEnum.BCYC:
                                    calcInSec = cycleDuration * _numOfSamples;
                                    break;
                                case OldCalculationBaseEnum.B200MS:
                                    calcInSec = 0.2 * _numOfSamples;
                                    break;
                            }
                        }
                        break;
                    case CalculationBase.B1DAY:
                        calcInSec = PQZTimeSpan.FromDays(1).TotalSeconds;
                        break;
                    case CalculationBase.B1HOUR:
                        calcInSec = PQZTimeSpan.FromHours(1).TotalSeconds;
                        break;
                    case CalculationBase.B30MIN:
                        calcInSec = PQZTimeSpan.FromMinutes(30).TotalSeconds;
                        break;
                    case CalculationBase.B15MIN:
                        calcInSec = PQZTimeSpan.FromMinutes(15).TotalSeconds;
                        break;
                    case CalculationBase.B10MIN:
                        calcInSec = PQZTimeSpan.FromMinutes(10).TotalSeconds;
                        break;
                    case CalculationBase.B5MIN:
                        calcInSec = PQZTimeSpan.FromMinutes(5).TotalSeconds;
                        break;
                    case CalculationBase.B1MIN:
                        calcInSec = PQZTimeSpan.FromMinutes(1).TotalSeconds;
                        break;
                    case CalculationBase.B1SEC:
                        calcInSec = PQZTimeSpan.FromSeconds(1).TotalSeconds;
                        break;
                    case CalculationBase.B2HOUR:
                        calcInSec = PQZTimeSpan.FromHours(2).TotalSeconds;
                        break;
                    case CalculationBase.B1024SPC:
                        calcInSec = 1 / freq / 1024;
                        break;
                    case CalculationBase.B512SPC:
                        calcInSec = 1 / freq / 512;
                        break;
                    case CalculationBase.B256SPC:
                        calcInSec = 1 / freq / 256;
                        break;
                    case CalculationBase.B128SPC:
                        calcInSec = 1 / freq / 128;
                        break;
                    case CalculationBase.B64SPC:
                        calcInSec = 1 / freq / 64;
                        break;
                    case CalculationBase.B32SPC:
                        calcInSec = 1 / freq / 32;
                        break;
                    case CalculationBase.B16SPC:
                        calcInSec = 1 / freq / 16;
                        break;
                    case CalculationBase.B200MS:
                        calcInSec = PQZTimeSpan.FromMilliseconds(200).TotalSeconds;
                        break;
                    case CalculationBase.B3SEC:
                        calcInSec = 3;
                        break;
                    case CalculationBase.B10SEC:
                        calcInSec = 10;
                        break;
                    case CalculationBase.BCYC:
                        calcInSec = 1 / freq;
                        break;
                    case CalculationBase.BHCYC:
                        calcInSec = 1 / freq / 2;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            return calcInSec;
        }

        public int CalculationBaseInNumOfSamples
        {
            get => _numOfSamples;
            set
            {
                _numOfSamples = value;

            }
        }

        #endregion

        #region Public

        public bool IsCustomBase => CalculationBaseEnum == CalculationBase.BX || CalculationBaseEnum == CalculationBase.BS;

        public override bool Equals(object obj)
        {
            CalcBase objSync = obj as CalcBase;
            if (objSync == null)
                return false;
            if (!CalculationBaseEnum.Equals(objSync.CalculationBaseEnum))
                return false;
            if (OldCalculationBaseEnum != objSync.OldCalculationBaseEnum)
                return false;
            if (_numOfSamples != objSync._numOfSamples)
                return false;
            if (_calcInSec != objSync._calcInSec)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return CalculationBaseEnum.GetHashCode();
        }

        public string ToStringWithoutOldCalculationBase()
        {
            if (CalculationBaseEnum == CalculationBase.BX)
                return CalculationBaseEnum.ToString() + _calcInSec.ToString();
            else if (CalculationBaseEnum == CalculationBase.BS)
                return CalculationBaseEnum.ToString() + _numOfSamples.ToString();
            else
                return CalculationBaseEnum.ToString();
        }
        public override string ToString()
        {
            string str = ToStringWithoutOldCalculationBase();

            if (OldCalculationBaseEnum != OldCalculationBaseEnum.NotExist)
                str += "_O" + OldCalculationBaseEnum.ToString();

            return str;
        }

        // Override == and != operators
        public static bool operator ==(CalcBase calcBase1, CalcBase calcBase2)
        {
            if (ReferenceEquals(calcBase1, calcBase2))
            {
                return true;
            }

            if (calcBase1 is null || calcBase2 is null)
            {
                return false;
            }

            return calcBase1.Equals(calcBase2);
        }

        public double GetOldCalculationBaseInSec(double freq = 50)
        {
            double calcInSec = 0;
            double cycleDuration = 0;
            switch (OldCalculationBaseEnum)
            {
                case OldCalculationBaseEnum.BHCYC:
                    cycleDuration = 1 / freq;
                    calcInSec = cycleDuration / 2;
                    break;
                case OldCalculationBaseEnum.BCYC:
                    cycleDuration = 1 / freq;
                    calcInSec = cycleDuration * _numOfSamples;
                    break;
                case OldCalculationBaseEnum.B200MS:
                    calcInSec = 0.2 * _numOfSamples;
                    break;
            }
            return calcInSec;
        }

        public static bool operator !=(CalcBase calcBase1, CalcBase calcBase2)
        {
            return !(calcBase1 == calcBase2);
        }

        //public bool IsSynced => SyncIntervalEnum != IntervalSynchronized.ISN;

        public virtual object Clone()
        {
            CalcBase baseInterval = null;

            if (CalculationBaseEnum == CalculationBase.BX)
            {
                baseInterval = new CalcBase(CalculationBaseEnum, _calcInSec);
                baseInterval.OldCalculationBaseEnum = OldCalculationBaseEnum;
            }
            else if (CalculationBaseEnum == CalculationBase.BS)
            {
                baseInterval = new CalcBase(CalculationBaseEnum, _numOfSamples, OldCalculationBaseEnum);
            }
            else
            {
                baseInterval = new CalcBase(CalculationBaseEnum);
            }

            return baseInterval;
        }

        public int CompareTo(CalcBase other)
        {
            return GetCalculationBaseInSec().CompareTo(other.GetCalculationBaseInSec());
        }

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();
            CalculationBaseEnum = Enum.Parse<CalculationBase>(reader.ReadElementString("CalculationBaseEnum"));
            OldCalculationBaseEnum = Enum.Parse<OldCalculationBaseEnum>(reader.ReadElementString("OldCalculationBaseEnum"));
            _calcInSec = double.Parse(reader.ReadElementString("CalcInSec"));
            _numOfSamples = int.Parse(reader.ReadElementString("NumOfSamples"));
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString("CalculationBaseEnum", CalculationBaseEnum.ToString());
            writer.WriteElementString("OldCalculationBaseEnum", OldCalculationBaseEnum.ToString());
            writer.WriteElementString("CalcInSec", _calcInSec.ToString());
            writer.WriteElementString("NumOfSamples", _numOfSamples.ToString());
        }

        #endregion

        #region Constants
        public static CalcBase BU => new(CalculationBase.BU);
        public static CalcBase B1024SPC => new(CalculationBase.B1024SPC);
        public static CalcBase B512SPC => new(CalculationBase.B512SPC);
        public static CalcBase B128SPC => new(CalculationBase.B128SPC);
        public static CalcBase B64SPC => new(CalculationBase.B64SPC);
        public static CalcBase B32SPC => new(CalculationBase.B32SPC);
        public static CalcBase B16SPC => new(CalculationBase.B16SPC);
        public static CalcBase BHCYC => new(CalculationBase.BHCYC);
        public static CalcBase BCYC => new(CalculationBase.BCYC);
        public static CalcBase B200MS => new(CalculationBase.B200MS);
        public static CalcBase B3SEC => new(CalculationBase.B3SEC);
        public static CalcBase B10SEC => new(CalculationBase.B10SEC);
        public static CalcBase B10MIN => new(CalculationBase.B10MIN);
        public static CalcBase B2HOUR => new(CalculationBase.B2HOUR);
        public static CalcBase B1MIN => new(CalculationBase.B1MIN);
        public static CalcBase B5MIN => new(CalculationBase.B5MIN);
        public static CalcBase B15MIN => new(CalculationBase.B15MIN);
        public static CalcBase B30MIN => new(CalculationBase.B30MIN);
        public static CalcBase B1HOUR => new(CalculationBase.B1HOUR);
        public static CalcBase B1DAY => new(CalculationBase.B1DAY);
        public static CalcBase BAUTO => new(CalculationBase.BAUTO);
        public static CalcBase B1SEC => new(CalculationBase.B1SEC);
        public static CalcBase B256SPC => new(CalculationBase.B256SPC);
        #endregion Constants
    }

}
