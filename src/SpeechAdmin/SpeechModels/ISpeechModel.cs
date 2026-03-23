using System;
using System.Threading.Tasks;

namespace SpeechAdmin.Models
{
    /// <summary>
    /// Schnittstelle für ein Sprachmodell-Anbieter
    /// </summary>
    public interface ISpeechModel : IDisposable, IAsyncDisposable
    {
        string Name { get; }
        string Description { get; }
        bool IsInstalled { get; }
        Task<bool> InstallAsync();
        Task<string> TranscribeAsync(string audioFilePath);
    }

    /// <summary>
    /// Konfiguration für Sprachmodelle
    /// </summary>
    public class SpeechModelConfig
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ModelPath { get; set; }
        public string? DownloadUrl { get; set; }
    }
}