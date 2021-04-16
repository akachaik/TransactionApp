namespace TransactionApp.Services
{
    public interface IFileReaderResolver
    {
        IFileReader Resolve(string fileExtension);
    }
}
