﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Speech.V1;
using STT.Code;

namespace STT
{
    public class CGoogle
    {
        // example copy for https://cloud.google.com/speech-to-text/docs/streaming-recognize
        static async Task<object> StreamingMicRecognizeAsync(int seconds)
        {
            var speech = SpeechClient.Create();
            var streamingCall = speech.StreamingRecognize();
            // Write the initial request with the config.
            await streamingCall.WriteAsync(new StreamingRecognizeRequest()
            {
                StreamingConfig = new StreamingRecognitionConfig()
                {
                    Config = new RecognitionConfig()
                    {
                        Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                        SampleRateHertz = 16000,
                        LanguageCode = Language_Google.English_UnitedStates,
                    },
                    InterimResults = true,
                }
            });

            // Print responses as they arrive.
            Task printResponses = Task.Run(async () =>
            {
                var responseStream = streamingCall.GetResponseStream();
                while (await responseStream.MoveNextAsync())
                {
                    StreamingRecognizeResponse response = responseStream.Current;
                    foreach (StreamingRecognitionResult result in response.Results)
                    {
                        foreach (SpeechRecognitionAlternative alternative in result.Alternatives)
                        {
                            Console.WriteLine(alternative.Transcript);
                        }
                    }
                }
            });

            // Read from the microphone and stream to API.
            object writeLock = new object();
            bool writeMore = true;
            var waveIn = new NAudio.Wave.WaveInEvent();
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
            waveIn.DataAvailable += (object sender, NAudio.Wave.WaveInEventArgs args) =>
            {
                lock (writeLock)
                {
                    if (!writeMore)
                    {
                        return;
                    }

                    streamingCall.WriteAsync(new StreamingRecognizeRequest()
                    {
                            AudioContent = Google.Protobuf.ByteString.CopyFrom(args.Buffer, 0, args.BytesRecorded)
                    }).Wait();
                }
            };
            waveIn.StartRecording();
            Console.WriteLine("Speak now.");
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            // Stop recording and shut down.
            waveIn.StopRecording();
            lock (writeLock)
            {
                writeMore = false;
            }

            await streamingCall.WriteCompleteAsync();
            await printResponses;
            return 0;
        }
    }
}