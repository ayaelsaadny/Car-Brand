namespace Limousine.Application.Interfaces
{
    public interface ILocalizationService
    {
        string GetMessage(string key, params object[] args);
        string this[string key] { get; }
    }
}
