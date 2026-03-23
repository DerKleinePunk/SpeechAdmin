using Microsoft.Extensions.Logging;
using SpeechAdmin.Models;
using SpeechAdmin.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SpeechAdmin.ViewModels
{
    /// <summary>
    /// ViewModel for the main application
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly SpeechModelManagerService? _modelManager;
        private readonly AudioRecorderService? _audioRecorder;
        private readonly KeyboardSimulatorService? _keyboardSimulator;
        private readonly ILogger<MainViewModel> _logger;
        private string _currentAudioPath = string.Empty;

        private bool _isRecording;
        private string _transcribedText = string.Empty;
        private int _selectedModelIndex;
        private bool _isProcessing;
        private string _statusMessage = string.Empty;
        private bool _useStreamingMode;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsRecording
        {
            get => _isRecording;
            set => SetProperty(ref _isRecording, value);
        }

        public string TranscribedText
        {
            get => _transcribedText;
            set => SetProperty(ref _transcribedText, value);
        }

        public int SelectedModelIndex
        {
            get => _selectedModelIndex;
            set
            {
                if (value == _selectedModelIndex || value < 0) return;

                _selectedModelIndex = value;
                OnPropertyChanged();
                try
                {
                    var models = _modelManager?.GetAvailableModels().ToList();
                    if (models == null) return;
                    if (value >= models.Count) return;

                    _modelManager?.SetCurrentModel(models[value]);
                    UpdateModelStatus();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error changing model: {ex.Message}";
                    _logger.LogError(ex, "Error changing model");
                }
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool UseStreamingMode
        {
            get => _useStreamingMode;
            set => SetProperty(ref _useStreamingMode, value);
        }

        public ObservableCollection<ISpeechModel> AvailableModels { get; }

        public RelayCommand? StartRecordingCommand { get; }
        public RelayCommand? StopRecordingCommand { get; }
        public RelayCommand? SendToApplicationCommand { get; }
        public RelayAsyncCommand? InstallModelCommand { get; }

        public MainViewModel()
        {
            AvailableModels = new ObservableCollection<ISpeechModel>();
            _logger = new NullLogger<MainViewModel>();
        }

        public MainViewModel(
            SpeechModelManagerService modelManager,
            AudioRecorderService audioRecorder,
            KeyboardSimulatorService keyboardSimulator,
            ILogger<MainViewModel>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(modelManager);
            _modelManager = modelManager;
            ArgumentNullException.ThrowIfNull(audioRecorder);
            _audioRecorder = audioRecorder;
            ArgumentNullException.ThrowIfNull(audioRecorder);
            _keyboardSimulator = keyboardSimulator;
            _logger = logger ?? new NullLogger<MainViewModel>();

            AvailableModels = new ObservableCollection<ISpeechModel>();
            InitializeModels();

            StartRecordingCommand = new RelayCommand(StartRecording, () => !IsRecording);
            StopRecordingCommand = new RelayCommand(StopRecording, () => IsRecording);
            SendToApplicationCommand = new RelayCommand(SendToApplication, () => !string.IsNullOrEmpty(TranscribedText));
            InstallModelCommand = new RelayAsyncCommand(InstallSelectedModel);

            // Subscribe to streaming events
            _audioRecorder.SpeechSegmentDetected += OnSpeechSegmentDetected;

            UpdateModelStatus();
            _logger.LogInformation("MainViewModel initialized");
        }

        protected void SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(field, newValue))
            {
                field = newValue;
                OnPropertyChanged(propertyName);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Initializes the model list
        /// </summary>
        private void InitializeModels()
        {
            if (_modelManager == null) return;

            try
            {
                var models = _modelManager.GetAvailableModels().ToList();
                foreach (var model in models)
                {
                    AvailableModels.Add(model);
                }

                if (AvailableModels.Count > 0)
                {
                    SelectedModelIndex = 0;
                    StatusMessage = "Ready";
                    _logger.LogInformation("Initialized {ModelCount} models", AvailableModels.Count);
                }
                else
                {
                    StatusMessage = "No models available";
                    _logger.LogWarning("No models available");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading models: {ex.Message}";
                _logger.LogError(ex, "Error loading models");
            }
        }

        /// <summary>
        /// Updates status of the current model
        /// </summary>
        private void UpdateModelStatus()
        {
            if (_modelManager == null) return;

            try
            {
                var currentModel = _modelManager.GetCurrentModel();
                StatusMessage = currentModel.IsInstalled ? $"✓ {currentModel.Name} installed" : $"⚠ {currentModel.Name} not installed - Please install";
                _logger.LogDebug("Model status updated: {ModelName} - Installed: {IsInstalled}",
                    currentModel.Name, currentModel.IsInstalled);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                _logger.LogError(ex, "Error updating model status");
            }
        }

        /// <summary>
        /// Starts audio recording
        /// </summary>
        private void StartRecording()
        {
            if (_modelManager == null)
            {
                throw new InvalidOperationException("Model manager service is not available.");
            }

            try
            {
                var currentModel = _modelManager.GetCurrentModel();
                if (!currentModel.IsInstalled)
                {
                    StatusMessage = "Model not installed. Please install first.";
                    _logger.LogWarning("Recording attempted with uninstalled model");
                    return;
                }

                if (_audioRecorder == null)
                {
                    StatusMessage = "Audio recorder service not available.";
                    _logger.LogWarning("Audio recorder service not available.");
                    return;
                }

                if (UseStreamingMode)
                {
                    // Streaming mode with VAD
                    _audioRecorder.StartRecording(RecordingMode.StreamWithVAD);
                    IsRecording = true;
                    StatusMessage = "🎤 Streaming... (Speak and pause for transcription)";
                    _logger.LogInformation("Recording started in STREAMING mode");
                }
                else
                {
                    // File mode (original behavior)
                    _currentAudioPath = Path.Combine(Path.GetTempPath(), $"speech_{Guid.NewGuid()}.wav");
                    _audioRecorder.StartRecording(RecordingMode.File, _currentAudioPath);
                    IsRecording = true;
                    StatusMessage = "🎤 Recording... (Speak now)";
                    _logger.LogInformation("Recording started in FILE mode: {AudioPath}", _currentAudioPath);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error starting recording: {ex.Message}";
                IsRecording = false;
                _logger.LogError(ex, "Error starting recording");
            }
        }

        /// <summary>
        /// Stops audio recording and transcribes
        /// </summary>
        private async void StopRecording()
        {
            try
            {
                IsRecording = false;
                _audioRecorder?.StopRecording();

                if (UseStreamingMode)
                {
                    // In streaming mode, transcription happens automatically via events
                    StatusMessage = "✓ Streaming stopped";
                    _logger.LogInformation("Streaming recording stopped");
                }
                else
                {
                    // File mode - transcribe the recorded file
                    StatusMessage = "⏳ Transcribing...";
                    IsProcessing = true;
                    _logger.LogDebug("Recording stopped, starting transcription");

                    // Short delay to finalize the file
                    await Task.Delay(500);

                    // Transcribe
                    if (!string.IsNullOrEmpty(_currentAudioPath) && File.Exists(_currentAudioPath) && _modelManager != null)
                    {
                        try
                        {
                            var text = await _modelManager.TranscribeAsync(_currentAudioPath);
                            TranscribedText = text;
                            StatusMessage = "✓ Transcription completed";
                            _logger.LogInformation("Transcription completed successfully");
                        }
                        finally
                        {
                            // Cleanup
                            try
                            {
                                File.Delete(_currentAudioPath);
                                _logger.LogDebug("Temporary audio file deleted");
                            }
                            catch
                            {
                                _logger.LogWarning("Failed to delete temporary audio file: {AudioPath}", _currentAudioPath);
                            }
                        }
                    }
                    else
                    {
                        StatusMessage = "Error: Audio file not found";
                        _logger.LogError("Audio file not found: {AudioPath}", _currentAudioPath);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error during recording stop and transcription");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Sends the transcribed text to the active application
        /// </summary>
        private void SendToApplication()
        {
            if (_keyboardSimulator == null)
            {
                throw new InvalidOperationException("Keyboard simulator service is not available.");
            }

            try
            {
                StatusMessage = "📝 Writing text...";
                _keyboardSimulator.TypeText(TranscribedText);
                StatusMessage = "✓ Text inserted";
                _logger.LogInformation("Text sent to application ({CharacterCount} characters)", TranscribedText.Length);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error inserting text: {ex.Message}";
                _logger.LogError(ex, "Error sending text to application");
            }
        }

        /// <summary>
        /// Handles speech segment detection from streaming mode
        /// </summary>
        private async void OnSpeechSegmentDetected(object? sender, AudioSegmentEventArgs e)
        {
            try
            {
                if (_modelManager == null)
                    return;

                StatusMessage = "⏳ Transcribing speech segment...";
                IsProcessing = true;
                _logger.LogDebug("Speech segment detected, starting transcription");

                // Save audio segment to temporary file
                var tempFile = Path.Combine(Path.GetTempPath(), $"speech_segment_{Guid.NewGuid()}.wav");
                await File.WriteAllBytesAsync(tempFile, e.AudioData);

                try
                {
                    // Transcribe the segment
                    var text = await _modelManager.TranscribeAsync(tempFile);

                    // Append to existing text (with space if not empty)
                    if (!string.IsNullOrEmpty(TranscribedText) && !TranscribedText.EndsWith(" "))
                    {
                        TranscribedText += " ";
                    }
                    TranscribedText += text;

                    StatusMessage = $"✓ Segment transcribed: \"{text}\"";
                    _logger.LogInformation("Speech segment transcribed: {Text}", text);
                }
                finally
                {
                    // Cleanup
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch
                    {
                        _logger.LogWarning("Failed to delete temporary segment file: {TempFile}", tempFile);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error transcribing segment: {ex.Message}";
                _logger.LogError(ex, "Error transcribing speech segment");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Installs the selected model
        /// </summary>
        private async Task InstallSelectedModel()
        {
            if (_modelManager == null)
            {
                throw new InvalidOperationException("Model manager service is not available.");
            }

            try
            {
                var model = _modelManager.GetCurrentModel();
                StatusMessage = $"⬇️  Installing {model.Name}...";
                IsProcessing = true;
                _logger.LogInformation("Starting installation of {ModelName}", model.Name);

                if (await model.InstallAsync())
                {
                    StatusMessage = $"✓ {model.Name} installed successfully";
                    UpdateModelStatus();
                    _logger.LogInformation("Model installation completed: {ModelName}", model.Name);
                }
                else
                {
                    StatusMessage = $"❌ Installation failed for {model.Name}";
                    _logger.LogError("Model installation failed: {ModelName}", model.Name);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during installation: {ex.Message}";
                _logger.LogError(ex, "Error during model installation");
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}