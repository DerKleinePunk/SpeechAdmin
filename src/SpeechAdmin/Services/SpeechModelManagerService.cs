using Microsoft.Extensions.Logging;
using SpeechAdmin.Models;
using SpeechAdmin.SpeechModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeechAdmin.Services
{
    /// <summary>
    /// Service for managing speech models
    /// </summary>
    public class SpeechModelManagerService
    {
        private readonly Dictionary<string, ISpeechModel> _models = new();
        private ISpeechModel? _currentModel;
        private readonly ILogger<SpeechModelManagerService> _logger;

        public SpeechModelManagerService(ILogger<SpeechModelManagerService>? logger = null, ILoggerFactory? loggerFactory = null)
        {
            _logger = logger ?? new NullLogger<SpeechModelManagerService>();
            InitializeModels(loggerFactory);
        }

        /// <summary>
        /// Initializes available models
        /// </summary>
        private void InitializeModels(ILoggerFactory? loggerFactory = null)
        {
            // Register Whisper models in various sizes
            var whisperBaseLogger = loggerFactory?.CreateLogger<WhisperModel>();
            RegisterModel("whisper-base", new WhisperModel("base", whisperBaseLogger));
            RegisterModel("whisper-small", new WhisperModel("small", loggerFactory?.CreateLogger<WhisperModel>()));
            RegisterModel("whisper-medium", new WhisperModel("medium", loggerFactory?.CreateLogger<WhisperModel>()));
            RegisterModel("whisper-large", new WhisperModel("large", loggerFactory?.CreateLogger<WhisperModel>()));

            // Set default model
            _currentModel = _models.Values.First();
            _logger.LogInformation("Initialized {ModelCount} speech models", _models.Count);
        }

        /// <summary>
        /// Registers a new model (for extensibility)
        /// </summary>
        public void RegisterModel(string id, ISpeechModel model)
        {
            _models[id] = model;
            _logger.LogDebug("Registered model: {ModelId}", id);
        }

        /// <summary>
        /// Returns all available models
        /// </summary>
        public IEnumerable<ISpeechModel> GetAvailableModels()
        {
            return _models.Values;
        }

        /// <summary>
        /// Sets the current model
        /// </summary>
        public void SetCurrentModel(string modelId)
        {
            if (_models.TryGetValue(modelId, out var model))
            {
                _currentModel = model;
                _logger.LogInformation("Current model set to: {ModelId}", modelId);
            }
            else
            {
                _logger.LogError("Model '{ModelId}' not found", modelId);
                throw new ArgumentException($"Model '{modelId}' not found");
            }
        }

        /// <summary>
        /// Sets the current model directly
        /// </summary>
        public void SetCurrentModel(ISpeechModel model)
        {
            _currentModel = model;
            _logger.LogDebug("Current model set directly to: {ModelName}", model.Name);
        }

        /// <summary>
        /// Returns the current model
        /// </summary>
        public ISpeechModel GetCurrentModel()
        {
            return _currentModel ?? throw new InvalidOperationException("No model selected");
        }

        /// <summary>
        /// Installs a model
        /// </summary>
        public async Task<bool> InstallModelAsync(string modelId)
        {
            _logger.LogInformation("Starting installation of model: {ModelId}", modelId);
            if (_models.TryGetValue(modelId, out var model))
            {
                return await model.InstallAsync();
            }

            _logger.LogWarning("Model '{ModelId}' not found for installation", modelId);
            return false;
        }

        /// <summary>
        /// Transcribes audio with the current model
        /// </summary>
        public async Task<string> TranscribeAsync(string audioFilePath)
        {
            if (_currentModel == null)
                throw new InvalidOperationException("No model selected");

            if (!_currentModel.IsInstalled)
                throw new InvalidOperationException("Model is not installed");

            _logger.LogDebug("Starting transcription with model: {ModelName}", _currentModel.Name);
            return await _currentModel.TranscribeAsync(audioFilePath);
        }
    }
}