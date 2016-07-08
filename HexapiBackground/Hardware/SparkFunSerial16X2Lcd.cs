﻿using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace HexapiBackground.Hardware{

    /// <summary>
    /// SparkFun Serial 16x2 LCD
    /// </summary>
    internal class SparkFunSerial16X2Lcd
    {
        private readonly byte[] _startOfFirstLine = {0xfe, 0x80};
        private readonly byte[] _startOfSecondLine = {0xfe, 0xc0};
        private DataWriter _outputStream;

        private SerialDevice _lcdSerialDevice;

        internal SparkFunSerial16X2Lcd()
        {
        }

        internal async Task<bool> Initialize()
        {
            _lcdSerialDevice = await StartupTask.SerialDeviceHelper.GetSerialDevice("DN01E099", 9600, new TimeSpan(0, 0, 0, 1), new TimeSpan(0, 0, 0, 1));
         
            if (_lcdSerialDevice == null)
                return false;

            _outputStream = new DataWriter(_lcdSerialDevice.OutputStream);

            return true;
        }

        private async Task Write(string text, byte[] line, bool clear)
        {
            if (text == null)
                return;

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(text);

            var count = 0;

            if (!clear)
                count = 16 - text.Length;
            else
                count = 32 - text.Length;

            if (count > 0)
            {
                for (var i = 0; i < count; i++)
                {
                    stringBuilder.Append(' ');
                }
            }

            if (_outputStream == null || _lcdSerialDevice == null)
            {
                Debug.WriteLine(text);
                return;
            }

            try
            {
                _outputStream.WriteBytes(line);
                _outputStream.WriteString(stringBuilder.ToString());
                await _outputStream.StoreAsync().AsTask();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private async Task WriteToFirstLine(string text)
        {
            if (text == null)
                return;

            await Write(text, _startOfFirstLine, false);
        }

        private async Task WriteToSecondLine(string text)
        {
            if (text == null)
                return;

            await Write(text, _startOfSecondLine, false);
        }

        internal async Task Write(string text)
        {
            if (text == null)
                return;

            await Write(text, _startOfFirstLine, true);
        }

        internal async Task Write(string text, int line)
        {
            if (line == 1)
                await WriteToFirstLine(text);

            if (line == 2)
                await WriteToSecondLine(text);
        }
        
    }
}