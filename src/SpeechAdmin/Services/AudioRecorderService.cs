using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using WebRtcVadSharp;

namespace SpeechAdmin.Services
{
    /// <summary>
    /// Recording mode for audio service
    /// </summary>
    public enum RecordingMode
    {
        /// <summary>
        /// Record to file (traditional mode)
        /// </summary>
        File,

        /// <summary>
        /// Stream with VAD (real-time mode)
        /// </summary>
        StreamWithVAD
    }

    /// <summary>
    /// Service for audio recording with support for file and streaming modes
    /// </summary>
    public class AudioRecorderService : IDisposable
    {
        private string _currentFilePath = string.Empty;
        private readonly ILogger<AudioRecorderService> _logger;
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _waveWriter;
        private WebRtcVad? _vadDetector;
        private readonly List<byte> _audioBuffer = new();
        private bool _isSpeechActive;

        public bool IsRecording { get; private set; }
        public RecordingMode CurrentMode { get; private set; }

        public event EventHandler<EventArgs>? DataAvailable;
        public event EventHandler<AudioSegmentEventArgs>? SpeechSegmentDetected;

        public AudioRecorderService(ILogger<AudioRecorderService>? logger = null)
        {
            _logger = logger ?? new NullLogger<AudioRecorderService>();
        }

        /// <summary>
        /// Starts audio recording in file mode (existing functionality)
        /// </summary>
        public void StartRecording(string outputPath, int deviceNumber = 0)
        {
            StartRecording(RecordingMode.File, outputPath, deviceNumber);
        }

        /// <summary>
        /// Starts audio recording in specified mode
        /// </summary>
        public void StartRecording(RecordingMode mode, string? outputPath = null, int deviceNumber = 0)
        {
            try
            {
                CurrentMode = mode;

                if (mode == RecordingMode.File)
                {
                    if (string.IsNullOrEmpty(outputPath))
                        throw new ArgumentNullException(nameof(outputPath), "Output path required for file mode");

                    _currentFilePath = outputPath;
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "");
                    _logger.LogInformation("Audio recording started in FILE mode: {OutputPath}", outputPath);
                }
                else
                {
                    _logger.LogInformation("Audio recording started in STREAMING mode with VAD");
                    InitializeVAD();
                }

                IsRecording = true;

                // Initialize NAudio components
                _waveIn = new WaveInEvent
                {
                    DeviceNumber = deviceNumber,
                    WaveFormat = new WaveFormat(16000, 16, 1) // 16kHz, 16-bit, Mono (required for VAD)
                };

                if (mode == RecordingMode.File && !string.IsNullOrEmpty(_currentFilePath))
                {
                    _waveWriter = new WaveFileWriter(_currentFilePath, _waveIn.WaveFormat);
                }

                _waveIn.DataAvailable += OnDataAvailable;
                _waveIn.RecordingStopped += WaveInOnRecordingStopped;
                _waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting recording: {Message}", ex.Message);
                IsRecording = false;
                throw;
            }
        }

        private void InitializeVAD()
        {
            try
            {
                _vadDetector = new WebRtcVad();
                _vadDetector.OperatingMode = OperatingMode.HighQuality; // HighQuality, LowBitrate, Aggressive, VeryAggressive
                _audioBuffer.Clear();
                _isSpeechActive = false;
                _logger.LogDebug("WebRTC VAD initialized successfully (Mode: HighQuality)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing WebRTC VAD: {Message}", ex.Message);
                throw;
            }
        }

        private void WaveInOnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                _logger.LogError(e.Exception, "Recording stopped due to an error: {Message}", e.Exception.Message);
                throw e.Exception;
            }
        }

        /// <summary>
        /// Stops audio recording
        /// </summary>
        public void StopRecording()
        {
            if (!IsRecording)
                return;

            IsRecording = false;
            _logger.LogInformation("Audio recording stopped (Mode: {Mode})", CurrentMode);

            _waveIn?.StopRecording();
            _waveIn?.RecordingStopped -= WaveInOnRecordingStopped;
            _waveIn?.DataAvailable -= OnDataAvailable;
            _waveIn?.Dispose();
            _waveIn = null;

            if (CurrentMode == RecordingMode.File && _waveWriter != null)
            {
                _waveWriter.Dispose();
                _waveWriter = null;
            }

            if (CurrentMode == RecordingMode.StreamWithVAD)
            {
                // Process any remaining audio in buffer
                if (_audioBuffer.Count > 0 && _isSpeechActive)
                {
                    ProcessSpeechSegment();
                }

                _vadDetector?.Dispose();
                _vadDetector = null;
                _audioBuffer.Clear();
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!IsRecording)
                return;

            if (CurrentMode == RecordingMode.File)
            {
                // Original file mode behavior
                _waveWriter?.Write(e.Buffer, 0, e.BytesRecorded);
                DataAvailable?.Invoke(this, EventArgs.Empty);
            }
            else if (CurrentMode == RecordingMode.StreamWithVAD)
            {
                // Streaming mode with VAD
                ProcessAudioWithVAD(e.Buffer, e.BytesRecorded);
            }
        }

        private void ProcessAudioWithVAD(byte[] buffer, int bytesRecorded)
        {
            try
            {
                // WebRTC VAD works with specific frame sizes (10ms, 20ms, 30ms at 16kHz)
                // At 16kHz: 10ms = 160 samples, 20ms = 320 samples, 30ms = 480 samples
                const int frameSizeMs = 30;
                const int frameSize = 480; // 30ms at 16kHz = 480 samples
                const int frameSizeBytes = frameSize * 2; // 16-bit = 2 bytes per sample

                // Process in frames
                for (var offset = 0; offset < bytesRecorded; offset += frameSizeBytes)
                {
                    var remainingBytes = Math.Min(frameSizeBytes, bytesRecorded - offset);
                    if (remainingBytes < frameSizeBytes)
                        break; // Skip incomplete frames

                    var frame = new byte[frameSizeBytes];
                    Array.Copy(buffer, offset, frame, 0, frameSizeBytes);

                    // Detect speech using WebRTC VAD
                    if (_vadDetector == null)
                    {
                        throw new InvalidOperationException("VAD detector not initialized");
                    }

                    var isSpeech = _vadDetector.HasSpeech(frame, SampleRate.Is16kHz, FrameLength.Is30ms);

                    if (isSpeech)
                    {
                        // Speech detected
                        if (!_isSpeechActive)
                        {
                            _logger.LogDebug("Speech started (frame at {Position}ms)", (offset / 2 * 1000) / 16000);
                            _isSpeechActive = true;
                        }

                        // Add bytes directly to buffer - no conversion needed!
                        for (var i = 0; i < frameSizeBytes; i++)
                        {
                            _audioBuffer.Add(frame[i]);
                        }
                    }
                    else if (_isSpeechActive)
                    {
                        // Speech ended
                        _logger.LogDebug("Speech ended, processing segment ({Bytes} bytes)", _audioBuffer.Count);
                        _isSpeechActive = false;

                        ProcessSpeechSegment();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio with VAD: {Message}", ex.Message);
            }
        }

        private void ProcessSpeechSegment()
        {
            if (_audioBuffer.Count == 0)
                return;

            try
            {
                // Convert byte list to WAV file format
                var wavBytes = ConvertSamplesToWavBytes(_audioBuffer.ToArray());

                // Raise event with speech segment
                SpeechSegmentDetected?.Invoke(this, new AudioSegmentEventArgs(wavBytes));

                _logger.LogInformation("Speech segment detected and processed: {Bytes} bytes", wavBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing speech segment: {Message}", ex.Message);
            }
            finally
            {
                _audioBuffer.Clear();
            }
        }

        private byte[] ConvertSamplesToWavBytes(byte[] pcmData)
        {
            using var memoryStream = new MemoryStream();
            using (var waveWriter = new WaveFileWriter(memoryStream, new WaveFormat(16000, 16, 1)))
            {
                // Write PCM data directly - no conversion needed!
                waveWriter.Write(pcmData, 0, pcmData.Length);
            }

            return memoryStream.ToArray();
        }

        public void Dispose()
        {
            if (IsRecording)
                StopRecording();

            _vadDetector?.Dispose();
        }
    }

    /// <summary>
    /// Event args for audio segment detection
    /// </summary>
    public class AudioSegmentEventArgs : EventArgs
    {
        public byte[] AudioData { get; }

        public AudioSegmentEventArgs(byte[] audioData)
        {
            AudioData = audioData;
        }
    }
}