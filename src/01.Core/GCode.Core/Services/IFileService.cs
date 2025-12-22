using System.Threading.Tasks;

namespace GCode.Core.Services
{
    public interface IFileService
    {
        Task<string> ReadAllTextAsync(string path);
        Task WriteAllTextAsync(string path, string content);
    }
}
