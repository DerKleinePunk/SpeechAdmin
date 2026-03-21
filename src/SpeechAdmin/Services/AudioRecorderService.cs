using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System;
using System.IO;

namespace SpeechAdmin.Services
{
    /// <summary>
    /// Service for audio recording (simplified version)
    /// </summary>
    public class AudioRecorderService(ILogger<AudioRecorderService>? logger = null) : IDisposable
    {
        private string _currentFilePath = string.Empty;
        private readonly ILogger<AudioRecorderService> _logger = logger ?? new NullLogger<AudioRecorderService>();
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _waveWriter;

        public bool IsRecording { get; private set; }

        public event EventHandler<EventArgs>? DataAvailable;

        /// <summary>
        /// Starts audio recording
        /// </summary>
        public void StartRecording(string outputPath, int deviceNumber = 0)
        {
            try
            {
                _currentFilePath = outputPath;
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "");

                IsRecording = true;
                _logger.LogInformation("Audio recording started: {OutputPath}", outputPath);

                // Initialize NAudio components
                _waveIn = new WaveInEvent
                {
                    DeviceNumber = deviceNumber,
                    WaveFormat = new WaveFormat(16000, 16, 1) // 16kHz, 16-bit, Mono
                };
                _waveWriter = new WaveFileWriter(_currentFilePath, _waveIn.WaveFormat);

                _waveIn.DataAvailable += OnDataAvailable;
                _waveIn.RecordingStopped += WaveInOnRecordingStopped;
                _waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting recording: {Message}", ex.Message);
                IsRecording = false;
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
            _logger.LogInformation("Audio recording stopped");

            _waveIn?.StopRecording();
            _waveIn?.RecordingStopped -= WaveInOnRecordingStopped;
            _waveIn?.DataAvailable -= OnDataAvailable;
            _waveIn?.Dispose();

            _waveIn = null;

            // Create a dummy WAV file for demo purposes
            if (!string.IsNullOrEmpty(_currentFilePath) && _waveWriter != null)
            {
                _waveWriter.Dispose();
            }
        }


        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!IsRecording || _waveWriter == null)
            {
                return;
            }

            _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
        }

        public void Dispose()
        {
            if (IsRecording)
                StopRecording();
        }
    }
}