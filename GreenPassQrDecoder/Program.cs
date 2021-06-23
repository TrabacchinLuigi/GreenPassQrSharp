using PeterO.Cbor;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GreenPassQrDecoder
{
    class Program
    {
        static void Main(String[] args)
        {
            var arguments = args
                .Select(x => x.Split('=', StringSplitOptions.RemoveEmptyEntries))
                .GroupBy(x => x.First()).Select(g => g.Last())
                .ToDictionary(x => x.First(), x => x.Last());
            var data = arguments["Data"];
            // Strip off the HC1 header if present
            if (data.StartsWith("HC1"))
            {
                data = data.Substring(3);
                if (data.StartsWith(":"))
                {
                    data = data.Substring(1);
                }
                else
                {
                    Console.WriteLine("Warning: unsafe HC1: header - update to v0.0.4");
                }
            }
            else
            {
                Console.WriteLine("Warning: no HC1: header - update to v0.0.4");
            }
            data = data.FromBase45();

            var dataBytes = Encoding.Latin1.GetBytes(data);
            // Zlib magic headers:
            // 78 01 - No Compression/low
            // 78 9C - Default Compression
            // 78 DA - Best Compression 
            if (dataBytes[0] == 0x78 && (dataBytes[1] == 0x01 || dataBytes[1] == 0x9C || dataBytes[1] == 0xDA))
            {
                using (var inputStream = new MemoryStream(dataBytes))
                using (var unzippedStream = new Ionic.Zlib.ZlibStream(inputStream, Ionic.Zlib.CompressionMode.Decompress))
                using (var outputStream = new MemoryStream())
                {
                    unzippedStream.CopyTo(outputStream);
                    dataBytes = outputStream.ToArray();
                }
            }
            var cbor = CBORObject.DecodeFromBytes(dataBytes);
            var cborItem = cbor[2];

            try
            {
                if (cborItem.Type == CBORType.ByteString)
                {
                    var valueBytes = cborItem.GetByteString();
                    var payload = CBORObject.DecodeFromBytes(valueBytes);
                    var json = payload.ToJSONString();
                    Console.WriteLine(json);
                }
                else { throw new InvalidOperationException("The second CBOR item is not a bytestring"); }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //Console.Write(data);
        }
    }
}
