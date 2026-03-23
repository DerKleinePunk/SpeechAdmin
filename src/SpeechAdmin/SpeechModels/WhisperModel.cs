using Microsoft.Extensions.Logging;
using SpeechAdmin.Models;
using SpeechAdmin.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.LibraryLoader;
using Whisper.net.Logger;

namespace SpeechAdmin.SpeechModels
{
    /// <summary>
    /// Implementation of the Whisper speech model
    /// </summary>
    public partial class WhisperModel : ISpeechModel
    {
        private readonly string _modelPath;
        private readonly string _modelSize;
        private readonly ILogger<WhisperModel> _logger;
        private const string MODELS_DIR = "Models";
        private WhisperProcessor? _whisperProcessor;
        private WhisperFactory? _whisperFactory;

        public string Name => $"Whisper ({_modelSize})";
        public string Description => $"OpenAI Whisper - {_modelSize} model for local Speech-to-Text";
        public bool IsInstalled => File.Exists(_modelPath);

        public WhisperModel(string modelSize = "base", ILogger<WhisperModel>? logger = null)
        {
            _modelSize = modelSize;
            _logger = logger ?? new NullLogger<WhisperModel>();
            Directory.CreateDirectory(MODELS_DIR);
            _modelPath = Path.Combine(MODELS_DIR, $"ggml-{modelSize}.bin");
            LogProvider.AddLogger(WriteLog);

            // Optional set the order of the runtimes:
            RuntimeOptions.RuntimeLibraryOrder = [RuntimeLibrary.Cuda, RuntimeLibrary.Vulkan, RuntimeLibrary.Cpu];
        }

        private void WriteLog(WhisperLogLevel logLevel, string? message)
        {
            // Map Whisper.net log levels to Microsoft.Extensions.Logging levels
            //None,
            // Error,
            // Warning,
            // Info,
            // Cont,
            // Debug,
            var mappedLevel = logLevel switch
            {
                WhisperLogLevel.Debug => LogLevel.Debug,
                WhisperLogLevel.Info => LogLevel.Information,
                WhisperLogLevel.Warning => LogLevel.Warning,
                WhisperLogLevel.Error => LogLevel.Error,
                _ => LogLevel.None
            };

            message ??= "Empty log message";

            _logger.Log(mappedLevel, "Whisper Log {message}", message.Replace("\n", string.Empty).Replace("\r", string.Empty));
        }

        public async Task<bool> InstallAsync()
        {
            try
            {
                if (IsInstalled)
                {
                    _logger.LogInformation("✓ Model '{ModelName}' is already installed", Name);
                    return true;
                }

                _logger.LogInformation("⬇️  Downloading Whisper model ('{ModelSize}')...", _modelSize);
                _logger.LogInformation("   Target location: {ModelPath}", _modelPath);

                return await DownloadModelAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error installing model: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<string> TranscribeAsync(string audioFilePath)
        {
            try
            {
                if (!File.Exists(audioFilePath))
                {
                    throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
                }

                if (!IsInstalled)
                    throw new InvalidOperationException($"Model '{Name}' is not installed. Please install first.");

                _logger.LogDebug("Transcribing audio file with '{ModelName}'...", Name);

                var result = new StringBuilder();

                if (_whisperProcessor == null)
                {
                    _whisperFactory = WhisperFactory.FromPath(_modelPath);
                    _whisperProcessor = _whisperFactory.CreateBuilder()
                        .WithLanguage("auto")
                        .Build();
                }

                await using var audioStream = File.OpenRead(audioFilePath);
                await foreach (var segment in _whisperProcessor.ProcessAsync(audioStream))
                {
                    result.Append(segment.Text);
                    LogStartEndText(segment.Start, segment.End, segment.Text);
                }

                return result.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during transcription: {Message}", ex.Message);
                throw new InvalidOperationException($"Error during transcription: {ex.Message}", ex);
            }
        }

        private async Task<bool> DownloadModelAsync()
        {
            try
            {
                // Whisper models from Hugging Face
                var modelUrls = new Dictionary<string, string>
                {
                    { "tiny", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin" },
                    { "base", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin" },
                    { "small", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin" },
                    { "medium", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin" },
                    { "large", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large.bin" }
                };

                if (!modelUrls.TryGetValue(_modelSize, out var modelUrl))
                    return false;

                using var client = new HttpClient();
                // Timeout to 30 minutes for large models
                client.Timeout = TimeSpan.FromMinutes(30);

                try
                {
                    _logger.LogInformation("   URL: {ModelUrl}", modelUrl);
                    var response = await client.GetAsync(modelUrl, HttpCompletionOption.ResponseHeadersRead);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("   HTTP Error: {StatusCode}", response.StatusCode);
                        return false;
                    }

                    var totalBytes = response.Content.Headers.ContentLength ?? 0L;
                    var canReportProgress = totalBytes > 0;

                    using (var content = response.Content)
                    await using (var stream = await content.ReadAsStreamAsync())
                    await using (var fileStream = new FileStream(_modelPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new Memory<byte>(new byte[8192]);
                        var totalRead = 0L;
                        int bytesRead;

                        while ((bytesRead = await stream.ReadAsync(buffer)) != 0)
                        {
                            await fileStream.WriteAsync(buffer);
                            totalRead += bytesRead;

                            if (canReportProgress)
                            {
                                var percentage = (totalRead * 100) / totalBytes;
                                _logger.LogDebug("   Download: {Percentage}% ({MegaBytes}MB)",
                                    percentage, totalRead / (1024 * 1024));
                            }
                        }
                    }

                    _logger.LogInformation("✓ Model successfully downloaded!");
                    return true;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "   Download error: {Message}", ex.Message);
                    if (File.Exists(_modelPath))
                        File.Delete(_modelPath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Download error: {Message}", ex.Message);
                if (File.Exists(_modelPath))
                    File.Delete(_modelPath);
                return false;
            }
        }

        public void Dispose()
        {
            _whisperProcessor?.Dispose();
            _whisperFactory?.Dispose();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (_whisperProcessor != null)
                await _whisperProcessor.DisposeAsync();

            _whisperFactory?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}