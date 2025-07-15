using PQS.Data.Common.Mapping;
using PQS.Data.RecordsContainer;
using PQS.PQZBinary;

namespace PQBI.PQS
{
    public interface IPQZBinaryWriterWrapper
    {
        byte[] WriteMessage(PQSRecordsContainer container, ProtocolMapping protocolMapping = null);
    }

    public class PQZBinaryWriterWrapper : IPQZBinaryWriterWrapper
    {
        private readonly PQZBinaryWriter _writer;

        public PQZBinaryWriterWrapper()
        {
            _writer = new PQZBinaryWriter();

        }


        public byte[] WriteMessage(PQSRecordsContainer container, ProtocolMapping protocolMapping = null)
        {
            var ptr = PQZBinaryWriter.WriteMessage(container, protocolMapping);
            return ptr;
        }
    }
}
