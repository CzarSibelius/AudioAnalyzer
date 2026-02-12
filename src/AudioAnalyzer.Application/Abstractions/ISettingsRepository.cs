using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

public interface ISettingsRepository
{
    AppSettings LoadAppSettings();
    void SaveAppSettings(AppSettings settings);
}
