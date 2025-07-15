using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RunDockerFromImages
{
	public class MsiSession
	{
		public class NativeMethods
		{
			public const uint ERROR_SUCCESS = 0;
			public const uint ERROR_MORE_DATA = 0xEA;

			public const ulong WS_VISIBLE = 0x10000000L;
	
			public const int GWL_STYLE = -16;

			// Declare the delegate for EnumWindows callback
			public delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);

			// Import the user32.dll library
			[DllImport("user32.dll")]
			public static extern int EnumWindows(EnumWindowsCallback callback, int lParam);

			[DllImport("user32.dll")]
			public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

			[DllImport("user32.dll", SetLastError = true)]
			public static extern UInt32 GetWindowLong(IntPtr hWnd, int nIndex);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiGetProperty(
			uint hInstall,
			string szName,
			StringBuilder szValueBuf,
			ref uint pcchValueBuf);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiSetProperty(uint hInstall, string szName, string szValue);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern int MsiCreateRecord(uint cParams);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiRecordGetString(int hRecord, uint iField, StringBuilder szValueBuf, ref uint pcchValueBuf);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiRecordSetString(int hRecord, uint iField, string szValue);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern int MsiRecordGetInteger(int hRecord, uint iField);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiRecordSetInteger(int hRecord, uint iField, int iValue);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiProcessMessage(uint hInstall, uint eMessageType, int hRecord);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiFormatRecord(uint hInstall, int hRecord, StringBuilder szResultBuf, ref int pcchResultBuf);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiRecordGetFieldCount(int hAny);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiCloseHandle(int hRecord);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiGetComponentState(uint hInstall, string szComponent, ref int piInstalled, ref int piAction);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiSetComponentState(uint hInstall, string szComponent, int iState);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiGetFeatureState(uint hInstall, string szFeature, ref int piInstalled, ref int piAction);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiSetFeatureState(uint hInstall, string szComponent, int iState);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiGetFeatureValidStates(uint hInstall, string szFeature, ref int lpInstallStates);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiGetFeatureCost(uint hInstall, string szFeature, int iCostTree, int iState, ref int piCost);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern int MsiGetActiveDatabase(uint hInstall);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern int MsiEvaluateCondition(uint hInstall, string szCondition);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiSetTargetPath(uint hInstall, string szFolder, string szFolderPath);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiEnumComponentCosts(
				uint hInstall,
				string szComponent,
				int dwIndex,
				int iState,
				StringBuilder szDriveBuf,
				ref uint pcchValueBuf,
				ref int piCost,
				ref int piTempCost);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern uint MsiSetInstallLevel(uint hInstall, int iInstallLevel);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			public static extern int MsiGetLastErrorRecord();
		}

		public enum MSICOSTTREE
		{
			MSICOSTTREE_SELFONLY = 0,
			MSICOSTTREE_CHILDREN = 1,
			MSICOSTTREE_PARENTS = 2,
			MSICOSTTREE_RESERVED = 3, // Reserved for future use
		}

		public enum INSTALLSTATE
		{
			INSTALLSTATE_NOTUSED = -7,  // component disabled
			INSTALLSTATE_BADCONFIG = -6,  // configuration data corrupt
			INSTALLSTATE_INCOMPLETE = -5,  // installation suspended or in progress
			INSTALLSTATE_SOURCEABSENT = -4,  // run from source, source is unavailable
			INSTALLSTATE_MOREDATA = -3,  // return buffer overflow
			INSTALLSTATE_INVALIDARG = -2,  // invalid function argument
			INSTALLSTATE_UNKNOWN = -1,  // unrecognized product or feature
			INSTALLSTATE_BROKEN = 0,  // broken
			INSTALLSTATE_ADVERTISED = 1,  // advertised feature
			INSTALLSTATE_REMOVED = 1,  // component being removed (action state, not settable)
			INSTALLSTATE_ABSENT = 2,  // uninstalled (or action state absent but clients remain)
			INSTALLSTATE_LOCAL = 3,  // installed on local drive
			INSTALLSTATE_SOURCE = 4,  // run from source, CD or net
			INSTALLSTATE_DEFAULT = 5,  // use default, local or source
		}

		public enum InstallMessage : uint
		{
			FATALEXIT = 0x00000000, // premature termination, possibly fatal OOM
			ERROR = 0x01000000, // formatted error message
			WARNING = 0x02000000, // formatted warning message
			USER = 0x03000000, // user request message
			INFO = 0x04000000, // informative message for log
			FILESINUSE = 0x05000000, // list of files in use that need to be replaced
			RESOLVESOURCE = 0x06000000, // request to determine a valid source location
			OUTOFDISKSPACE = 0x07000000, // insufficient disk space message
			ACTIONSTART = 0x08000000, // start of action: action name & description
			ACTIONDATA = 0x09000000, // formatted data associated with individual action item
			PROGRESS = 0x0A000000, // progress gauge info: units so far, total
			COMMONDATA = 0x0B000000, // product info for dialog: language Id, dialog caption
			INITIALIZE = 0x0C000000, // sent prior to UI initialization, no string data
			TERMINATE = 0x0D000000, // sent after UI termination, no string data
			SHOWDIALOG = 0x0E000000, // sent prior to display or authored dialog or wizard
		}

		private IntPtr mMsiWindowHandle = IntPtr.Zero;
		private RemoteMsiCalls mRemoteMsiCalls;
		private bool IsRemoteActivated { get { return mRemoteMsiCalls != null; } }

		private bool EnumWindowCallback(IntPtr hwnd, int lParam)
		{
			uint wnd_proc = 0;
			NativeMethods.GetWindowThreadProcessId(hwnd, out wnd_proc);
		
			if (wnd_proc == lParam)
			{
				UInt32 style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
				if ((style & NativeMethods.WS_VISIBLE) != 0)
				{
					mMsiWindowHandle = hwnd;
					return false;
				}
			}

			return true;
		}

		public uint MsiHandle { get; private set; }

		public string CustomActionData { get; private set; }

		public MsiSession(string aSessionInfo)
		{
			string[] sessionInfoTokens = aSessionInfo.Split('_');

			string msiHandleToken = sessionInfoTokens[0];
	
			if (string.IsNullOrEmpty(msiHandleToken))
				throw new ArgumentNullException();

			uint msiHandle = 0;
			if (!uint.TryParse(msiHandleToken, out msiHandle))
				throw new ArgumentException("Invalid msi handle");

			MsiHandle = msiHandle;

			if (sessionInfoTokens.Length >= 2)
				mRemoteMsiCalls = new RemoteMsiCalls(sessionInfoTokens[1]);

			var allData = GetProperty("CustomActionData").Split(new char[] { '|' });

			CustomActionData = allData.First();

		}
		
		public string GetProperty(string aProperty)
		{
			if (IsRemoteActivated)
				return mRemoteMsiCalls.MsiGetProperty(MsiHandle, aProperty);

			// Get buffer size
			uint pSize = 0;
			StringBuilder valueBuffer = new StringBuilder();
			NativeMethods.MsiGetProperty(MsiHandle, aProperty, valueBuffer, ref pSize);

			// Get property value
			pSize++; // null terminated
			valueBuffer.Capacity = (int)pSize;
			NativeMethods.MsiGetProperty(MsiHandle, aProperty, valueBuffer, ref pSize);

			return valueBuffer.ToString();
		}

		public void SetProperty(string aProperty, string aValue)
		{
			if (IsRemoteActivated)
				mRemoteMsiCalls.MsiSetProperty(MsiHandle, aProperty, aValue);
			else
				NativeMethods.MsiSetProperty(MsiHandle, aProperty, aValue);
		}

		public int CreateRecord(uint aParams)
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiCreateRecord(aParams)
				: NativeMethods.MsiCreateRecord(aParams);
		}
		
		public string RecordGetString(int hRecord, uint iField)
		{
			if (IsRemoteActivated)
				return mRemoteMsiCalls.MsiRecordGetString(hRecord, (int)iField);

			uint length = 0;
			StringBuilder buffer = new StringBuilder(0);
			if (NativeMethods.MsiRecordGetString(hRecord, iField, buffer, ref length) != NativeMethods.ERROR_MORE_DATA)
				return "";

			buffer.Capacity = (int)length;
			if (NativeMethods.MsiRecordGetString(hRecord, iField, buffer, ref length) != NativeMethods.ERROR_SUCCESS)
				return "";

			return buffer.ToString();
		}

		public uint RecordSetString(int hRecord, uint iField, string szValue)
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiRecordSetString(hRecord, iField, szValue)
				: NativeMethods.MsiRecordSetString(hRecord, iField, szValue);
		}

		public int RecordGetInteger(int hRecord, uint iField)
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiRecordGetInteger(hRecord, (int)iField)
				: NativeMethods.MsiRecordGetInteger(hRecord, iField);
		}

		public uint RecordSetInteger(int hRecord, uint iField, int iValue)
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiRecordSetInteger(hRecord, iField, iValue)
				: NativeMethods.MsiRecordSetInteger(hRecord, iField, iValue);
		}

		public uint RecordGetFieldCount(int hRecord)
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiRecordGetFieldCount(hRecord)
				: NativeMethods.MsiRecordGetFieldCount(hRecord);
		}

		public uint MsiProcessMessage(uint eMessageType, int hRecord)
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiProcessMessage(MsiHandle, eMessageType, hRecord)
				: NativeMethods.MsiProcessMessage(MsiHandle, eMessageType, hRecord);
		}

		public void Log(string aMessage, InstallMessage aMessageType)
		{
			int hRecord = CreateRecord(1);
			RecordSetString(hRecord, 0, "[1]");
			RecordSetString(hRecord, 1, aMessage);
			MsiProcessMessage((uint)aMessageType, hRecord);
		}

		public uint MsiCloseHandle(int aHandle)
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiCloseHandle(aHandle)
				: NativeMethods.MsiCloseHandle(aHandle);
		}

		public string ResolveFormatted(string aFormatted)
		{
			if (IsRemoteActivated)
				return mRemoteMsiCalls.ResolveFormatted(MsiHandle, aFormatted);
		
			if (string.IsNullOrEmpty(aFormatted))
				return string.Empty;

			int hRecord = CreateRecord(1);
			if (hRecord == 0)
				return string.Empty;

			if (RecordSetString(hRecord, 0, aFormatted) != 0)
				return string.Empty;

			return FormatRecord(hRecord);
		}

		public string FormatRecord(int hRecord)
		{
			if (IsRemoteActivated)
				return mRemoteMsiCalls.MsiFormatRecord(MsiHandle, hRecord);

			StringBuilder buffer = new StringBuilder();
			int bufSize = 0;

			// determine buffer size
			uint ret = NativeMethods.MsiFormatRecord(MsiHandle, hRecord, buffer, ref bufSize);
			if (ret == NativeMethods.ERROR_MORE_DATA)
			{
				bufSize++;
				buffer.Capacity = bufSize;
				ret = NativeMethods.MsiFormatRecord(MsiHandle, hRecord, buffer, ref bufSize);
			}

			if (ret != NativeMethods.ERROR_SUCCESS)
				return string.Empty;
	
			return buffer.ToString();
		}

		public uint GetComponentState(string aComponent, ref INSTALLSTATE aState, ref INSTALLSTATE aAction)
		{
			int installState = -1;
			int action       = -1;
			
			if (IsRemoteActivated)
			{
				uint ret =
				mRemoteMsiCalls.MsiGetComponentState(MsiHandle, aComponent, ref installState, ref action);
				aState  = (INSTALLSTATE)installState;
				aAction = (INSTALLSTATE)action;
				return ret;
			}

			uint res = NativeMethods.MsiGetComponentState(MsiHandle, aComponent, ref installState, ref action);
			aState = (INSTALLSTATE)installState;
			aAction = (INSTALLSTATE)action;
			return res;
		}

		public uint SetComponentState(string aFeature, INSTALLSTATE aState)
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiSetComponentState(MsiHandle, aFeature, (int)aState)
				: NativeMethods.MsiSetComponentState(MsiHandle, aFeature, (int)aState);
		}

		public uint GetFeatureState(string aFeature, ref INSTALLSTATE aState, ref INSTALLSTATE aAction)
		{
			int installState = -1;
			int action       = -1;

			if (IsRemoteActivated)
			{
				var ret =
				mRemoteMsiCalls.MsiGetFeatureState(MsiHandle, aFeature, ref installState, ref action);
				aState  = (INSTALLSTATE)installState;
				aAction = (INSTALLSTATE)action;
				return ret;
			}

			uint res = NativeMethods.MsiGetFeatureState(MsiHandle, aFeature, ref installState, ref action);
			aState = (INSTALLSTATE)installState;
			aAction = (INSTALLSTATE)action;
			return res;
		}

		public uint SetFeatureState(string aFeature, INSTALLSTATE aState)
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiSetFeatureState(MsiHandle, aFeature, (int)aState)
				: NativeMethods.MsiSetFeatureState(MsiHandle, aFeature, (int)aState);
		}

		public uint EnumerateComponentCosts(string aComponent, 
											int aIndex, 
											INSTALLSTATE aInstallState, 
											ref string aDriveName,
											ref int aCost,
											ref int aTempCost)
		{
			if (IsRemoteActivated)
				return mRemoteMsiCalls.MsiEnumComponentCosts(MsiHandle, aComponent, aIndex, (int)aInstallState, ref aDriveName, ref aCost, ref aTempCost);

			StringBuilder buffer   = new StringBuilder();
			uint          length   = 0;
			int           cost     = 0;
			int           tempCost = 0;

			uint res =
			NativeMethods.MsiEnumComponentCosts(MsiHandle, aComponent, aIndex, (int)aInstallState, buffer,
												ref length, ref cost, ref tempCost);
			aDriveName = buffer.ToString();
			aCost      = cost;
			aTempCost  = tempCost;
			
			return res;
		}

		public uint GetFeatureValidStates(string aFeature, ref int aValidStates)
		{
			if (IsRemoteActivated)
				return mRemoteMsiCalls.MsiGetFeatureValidStates(MsiHandle, aFeature, ref aValidStates);

			int states = 0;
			uint res = NativeMethods.MsiGetFeatureValidStates(MsiHandle, aFeature, ref states);
			aValidStates = states;
			return res;
		}

		public int GetFeatureCost(string aFeature, bool aChildren)
		{
			if (IsRemoteActivated)
				return mRemoteMsiCalls.MsiGetFeatureCost(MsiHandle, aFeature, aChildren);

			int featureState = 0;
			int featureAction = 0;
			uint ret = NativeMethods.MsiGetFeatureState(MsiHandle, aFeature, ref featureState, ref featureAction);
			if (ret != NativeMethods.ERROR_SUCCESS)
				return 0;

			if (featureAction == (int)INSTALLSTATE.INSTALLSTATE_UNKNOWN)
				featureAction = featureState;
	
			int featureCost = 0;
			int costTree = (int)(aChildren ? MSICOSTTREE.MSICOSTTREE_CHILDREN : MSICOSTTREE.MSICOSTTREE_SELFONLY);
			NativeMethods.MsiGetFeatureCost(MsiHandle, aFeature, costTree, featureAction, ref featureCost);
			return featureCost;
		}

		public int GetActiveDatabase()
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiGetActiveDatabase(MsiHandle)
				: NativeMethods.MsiGetActiveDatabase(MsiHandle);
		}

		public List<List<string>> ReadTableDatabase(string aTableName, IEnumerable<string> aColumns, string aWhereClause)
		{
			if (IsRemoteActivated)
				return mRemoteMsiCalls.ReadTableDatabase(GetActiveDatabase(), aTableName, aColumns, aWhereClause);

			throw new NotImplementedException();
		}
	
		public int CountTableRows(string aTableName)
		{
			if (IsRemoteActivated)
				return mRemoteMsiCalls.CountTableRows(GetActiveDatabase(), aTableName);

			throw new NotImplementedException();
		}

		public int EvaluateCondition(string aCondition)
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiEvaluateCondition(MsiHandle, aCondition)
				: NativeMethods.MsiEvaluateCondition(MsiHandle, aCondition);
		}

		public uint SetTargetPath(string aTargetFolderId, string aTargetFolderPath)
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiSetTargetPath(MsiHandle, aTargetFolderId, aTargetFolderPath)
				: NativeMethods.MsiSetTargetPath(MsiHandle, aTargetFolderId, aTargetFolderPath);
		}

		public uint SetInstallLevel(int iInstallLevel)
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiSetInstallLevel(MsiHandle, iInstallLevel)
				: NativeMethods.MsiSetInstallLevel(MsiHandle, iInstallLevel);
		}

		public int GetLastErrorRecord()
		{
			return IsRemoteActivated
				? mRemoteMsiCalls.MsiGetLastErrorRecord()
				: NativeMethods.MsiGetLastErrorRecord();
		}

  public IntPtr GetMsiWindowHandle()
		{
			string msiProcId = GetProperty("CLIENTPROCESSID");
			if (string.IsNullOrEmpty(msiProcId))
				return IntPtr.Zero;

			IntPtr handle = new IntPtr(Convert.ToInt32(msiProcId));
			mMsiWindowHandle = IntPtr.Zero;
			NativeMethods.EnumWindows(EnumWindowCallback, (int)handle);

			return mMsiWindowHandle;
		}
	}
}
