using System.IO;
using QRCoder;

namespace StartEvent_API.Helper
{
    public class QrCodeGenerator : IQrCodeGenerator
    {
        public async Task<byte[]> GenerateQrCodeAsync(string data, int size = 200)
        {
            return await Task.Run(() =>
            {
                using (var qrGenerator = new QRCodeGenerator())
                using (var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q))
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    return qrCode.GetGraphic(20);
                }
            });
        }

        public async Task<string> GenerateQrCodeBase64Async(string data, int size = 200)
        {
            var qrCodeBytes = await GenerateQrCodeAsync(data, size);
            return Convert.ToBase64String(qrCodeBytes);
        }
    }
}
