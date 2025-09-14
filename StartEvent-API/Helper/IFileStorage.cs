namespace StartEvent_API.Helper
{
    public interface IFileStorage
    {
        Task<string> SaveFileAsync(byte[] fileData, string fileName, string folder = "qrcodes");
        Task<byte[]?> GetFileAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
        Task<string> GetFileUrlAsync(string filePath);
    }
}
