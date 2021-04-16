using Microsoft.Extensions.DependencyInjection;
using System;

namespace TransactionApp.Services
{
    public class FileReaderResolver : IFileReaderResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public FileReaderResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IFileReader Resolve(string fileExtension)
        {
            switch (fileExtension.ToLower())
            {
                case ".csv":
                    return _serviceProvider.GetRequiredService<CsvFileReader>();
                case ".xml":
                    return _serviceProvider.GetRequiredService<XmlFileReader>();
                default:
                    throw new NotSupportedException($"{fileExtension} is not supported");
            }
        }
    }
}
