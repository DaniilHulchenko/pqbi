using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace RunDockerFromImages
{
	public class RemoteMsiCalls
	{
		enum RemoteFunctions
		{
			MsiGetProperty,
			MsiSetProperty,
			MsiCreateRecord,
			MsiRecordGetString,
			MsiRecordSetString,
			MsiRecordGetInteger,
			MsiRecordSetInteger,
			MsiProcessMessage,
			MsiFormatRecord,
			ResolveFormatted,
			MsiRecordGetFieldCount,
			MsiCloseHandle,
			MsiGetComponentState,
			MsiSetComponentState,
			MsiGetFeatureState,
			MsiSetFeatureState,
			MsiGetFeatureValidStates,
			MsiEnumComponentCosts,
			MsiGetFeatureCost,
			MsiGetActiveDatabase,
			ReadTableDatabase,
			CountTableRows,
			MsiEvaluateCondition,
			MsiSetTargetPath,
			MsiSetInstallLevel,
			MsiGetLastErrorRecord
		};

		private NamedPipeClientStream mClientPipe;
		private bool Is64 { get { return IntPtr.Size == 8; } }

		public RemoteMsiCalls(string aPipeName)
		{
			mClientPipe = new NamedPipeClientStream(aPipeName);
			mClientPipe.Connect();
		}

		public int MsiCreateRecord(uint aParams)
		{
			WriteInt((int)RemoteFunctions.MsiCreateRecord);
			WriteInt((int)aParams);
			return ReadInt();
		}

		public uint MsiRecordSetString(int aRecordHandle, uint aField, string aValue)
		{
			WriteInt((int)RemoteFunctions.MsiRecordSetString);
			WriteInt(aRecordHandle);
			WriteInt((int)aField);
			WriteString(aValue);
			return (uint)ReadInt();
		}

		public uint MsiRecordSetInteger(int aRecordHandle, uint aField, int aValue)
		{
			WriteInt((int)RemoteFunctions.MsiRecordSetInteger);
			WriteInt(aRecordHandle);
			WriteInt((int)aField);
			WriteInt(aValue);
			return (uint)ReadInt();
		}

		public uint MsiProcessMessage(uint aMsiHandle, uint aMessageType, int aRecordHandle)
		{
			WriteInt((int)RemoteFunctions.MsiProcessMessage);
			WriteInt(aMsiHandle);
			WriteInt((int)aMessageType);
			WriteInt(aRecordHandle);
			return (uint)ReadInt();
		}

		public uint MsiCloseHandle(int aHandle)
		{
			WriteInt((int)RemoteFunctions.MsiCloseHandle);
			WriteInt(aHandle);
			return (uint)ReadInt();
		}

		public string MsiGetProperty(uint aMsiHandle, string aPropName)
		{
			WriteInt((int)RemoteFunctions.MsiGetProperty);
			WriteInt(aMsiHandle);
			WriteString(aPropName);

			return ReadString();
		}

		public uint MsiSetProperty(uint aMsiHandle, string aPropName, string aValue)
		{
			WriteInt((int)RemoteFunctions.MsiSetProperty);
			WriteInt(aMsiHandle);
			WriteString(aPropName);
			WriteString(aValue);
			return (uint)ReadInt();
		}

		public string ResolveFormatted(uint aMsiHandle, string aFormatted)
		{
			WriteInt((int)RemoteFunctions.ResolveFormatted);
			WriteInt(aMsiHandle);
			WriteString(aFormatted);

			return ReadString();
		}

		public uint MsiGetComponentState(uint aMsiHandle, string aComponent, ref int aState, ref int aAction)
		{
			WriteInt((int)RemoteFunctions.MsiGetComponentState);
			WriteInt(aMsiHandle);
			WriteString(aComponent);

			uint retCode = (uint)ReadInt();
			aState       = ReadInt();
			aAction      = ReadInt();
			return retCode;
		}

		public uint MsiSetComponentState(uint aMsiHandle, string aComponent, int aState)
		{
			WriteInt((int)RemoteFunctions.MsiSetComponentState);
			WriteInt(aMsiHandle);
			WriteString(aComponent);
			WriteInt(aState);
			return (uint)ReadInt();
		}

		public uint MsiGetFeatureState(uint aMsiHandle, string aFeature, ref int aState, ref int aAction)
		{
			WriteInt((int)RemoteFunctions.MsiGetFeatureState);
			WriteInt(aMsiHandle);
			WriteString(aFeature);

			uint retCode = (uint)ReadInt();
			aState       = ReadInt();
			aAction      = ReadInt();
			return retCode;
		}

		public uint MsiEnumComponentCosts(uint aMsiHandle, string aComponent, int aIndex, int aInstallState, ref string szDriveBuf, ref int piCost, ref int piTempCost)
		{
			WriteInt((int)RemoteFunctions.MsiEnumComponentCosts);
			WriteInt(aMsiHandle);
			WriteString(aComponent);
			WriteInt(aIndex);
			WriteInt(aInstallState);

			uint retCode = (uint)ReadInt();
			szDriveBuf   = ReadString();
			piCost       = ReadInt();
			piTempCost   = ReadInt();
			
			return retCode;
		}

		public string MsiFormatRecord(uint aMsiHandle, int aRecordHandle)
		{
			WriteInt((int)RemoteFunctions.MsiFormatRecord);
			WriteInt(aMsiHandle);
			WriteInt(aRecordHandle);
			return ReadString();
		}

		public List<List<string>> ReadTableDatabase(int aDatabaseHandle, string aTable, IEnumerable<string> aColumns, string aWhereClause)
		{
			WriteInt((int)RemoteFunctions.ReadTableDatabase);
			WriteInt(aDatabaseHandle);
			WriteString(aTable);
			WriteInt(aColumns.Count());
			foreach (var col in aColumns)
			WriteString(col);
			WriteString("");
			WriteString(aWhereClause);

			List<List<string>> result = new List<List<string>>();
			int rowsCount = ReadSize();
			for (int rowIndex = 0; rowIndex < rowsCount; rowIndex++)
			{
				List<string> row = new List<string>();
				int colCount = ReadSize();
				for (int colIndex = 0; colIndex < colCount; colIndex++)
					row.Add(ReadString());
				result.Add(row);
			}
		
			return result;
		}

		public string MsiRecordGetString(int aRecordHandle, int aPos)
		{
			WriteInt((int)RemoteFunctions.MsiRecordGetString);
			WriteInt(aRecordHandle);
			WriteInt(aPos);

			int retCode = ReadInt();
			string val = ReadString();
		
			return retCode == 0 ? val : string.Empty;
		}

		public int MsiRecordGetInteger(int aRecordHandle, int aPos)
		{
			WriteInt((int)RemoteFunctions.MsiRecordGetInteger);
			WriteInt(aRecordHandle);
			WriteInt(aPos);
		
			return ReadInt();
		}

		public uint MsiGetFeatureValidStates(uint aMsiHandle, string aFeature, ref int aStates)
		{
			WriteInt((int)RemoteFunctions.MsiGetFeatureValidStates);
			WriteInt(aMsiHandle);
			WriteString(aFeature);

			uint retCode = (uint)ReadInt();
			aStates      = ReadInt();

			return retCode;
		}

		public int CountTableRows(int aDatabaseHandle, string aTableName)
		{
			WriteInt((int)RemoteFunctions.CountTableRows);
			WriteInt(aDatabaseHandle);
			WriteString(aTableName);

			return ReadInt();
		}

		public int MsiGetFeatureCost(uint aMsiHandle, string aFeature, bool aChildren)
		{
			WriteInt((int)RemoteFunctions.MsiGetFeatureCost);
			WriteInt(aMsiHandle);
			WriteString(aFeature);
			WriteBool(aChildren);

			return ReadInt();
		}

		public int MsiEvaluateCondition(uint aMsiHandle, string aCondition)
		{
			WriteInt((int)RemoteFunctions.MsiEvaluateCondition);
			WriteInt(aMsiHandle);
			WriteString(aCondition);

			return ReadInt();
		}

		public uint MsiSetTargetPath(uint aMsiHandle, string aFolderIdentifier, string aFolderPath)
		{
			WriteInt((int)RemoteFunctions.MsiSetTargetPath);
			WriteInt(aMsiHandle);
			WriteString(aFolderIdentifier);
			WriteString(aFolderPath);

			return (uint)ReadInt();
		}
		
		public uint MsiSetFeatureState(uint aMsiHandle, string aFeature, int aState)
		{
			WriteInt((int)RemoteFunctions.MsiSetFeatureState);
			WriteInt(aMsiHandle);
			WriteString(aFeature);
			WriteInt(aState);

			return (uint)ReadInt();
		}
		
		public int MsiGetActiveDatabase(uint aMsiHandle)
		{
			WriteInt((int)RemoteFunctions.MsiGetActiveDatabase);
			WriteInt(aMsiHandle);
			return ReadInt();
		}

		public uint MsiRecordGetFieldCount(int aRecordHandle)
		{
			WriteInt((int)RemoteFunctions.MsiRecordGetFieldCount);
			WriteInt(aRecordHandle);
			return (uint)ReadInt();
		}

		public uint MsiSetInstallLevel(uint aMsiHandle, int aInstallLevel)
		{
			WriteInt((int)RemoteFunctions.MsiSetInstallLevel);
			WriteInt(aMsiHandle);
			WriteInt(aInstallLevel);
			return (uint)ReadInt();
		}
		
		public int MsiGetLastErrorRecord()
		{
			WriteInt((int)RemoteFunctions.MsiGetLastErrorRecord);
			return ReadInt();
		}

		// ------------------------------------

		private void WriteString(string aValue)
		{
			WriteSize(aValue.Length);

			byte[] bytes = Encoding.Unicode.GetBytes(aValue);
			mClientPipe.Write(bytes, 0, bytes.Length);
		}

		private void WriteInt(int aValue)
		{
			byte[] bytes = BitConverter.GetBytes(aValue);
			mClientPipe.Write(bytes, 0, bytes.Length);
		}

		private void WriteInt(uint aValue)
		{
			byte[] bytes = BitConverter.GetBytes(aValue);
			mClientPipe.Write(bytes, 0, bytes.Length);
		}

		private void WriteLong(long aValue)
		{
			byte[] bytes = BitConverter.GetBytes(aValue);
			mClientPipe.Write(bytes, 0, bytes.Length);
		}

		private void WriteBool(bool aValue)
		{
			byte[] bytes = BitConverter.GetBytes(aValue);
			mClientPipe.Write(bytes, 0, bytes.Length);
		}

		private string ReadString()
		{
			int length = ReadSize() * 2;
			byte[] bytes = new byte[length];
			mClientPipe.Read(bytes, 0, length);
			return Encoding.Unicode.GetString(bytes);
		}

		private int ReadInt()
		{
			byte[] bytes = new byte[4];
			mClientPipe.Read(bytes, 0, 4);
			return BitConverter.ToInt32(bytes, 0);
		}

		private long ReadLong()
		{
			byte[] bytes = new byte[8];
			mClientPipe.Read(bytes, 0, 8);
			return BitConverter.ToInt64(bytes, 0);
		}

		private int ReadSize()
		{
			return Is64 ? (int)ReadLong() : ReadInt();
		}

		private void WriteSize(int aSize)
		{
			byte[] bytes = Is64 ? BitConverter.GetBytes((long)aSize) : BitConverter.GetBytes(aSize);
			mClientPipe.Write(bytes, 0, bytes.Length);
		}
	}
}
