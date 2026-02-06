using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

public interface ISettingsRepository
{
    AppSettings Load();
    void Save(AppSettings settings);
}
