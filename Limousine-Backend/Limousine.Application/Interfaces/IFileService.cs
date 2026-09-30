using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Interfaces
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string folderName, CancellationToken cancellationToken = default);
        bool DeleteFile(string relativeFilePath);
    }
}
