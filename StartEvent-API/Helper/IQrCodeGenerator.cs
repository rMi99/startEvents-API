namespace StartEvent_API.Helper
{
    public interface IQrCodeGenerator
    {
        Task<byte[]> GenerateQrCodeAsync(string data, int size = 200);
        Task<string> GenerateQrCodeBase64Async(string data, int size = 200);
    }
}
