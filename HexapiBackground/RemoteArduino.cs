﻿using System.Diagnostics;
using Windows.Devices.I2c;
using Microsoft.Maker.RemoteWiring;
using Microsoft.Maker.Serial;

namespace HexapiBackground
{
    //This works, not doing anything yet with it
    //TODO : Add ping sensor events 
    sealed internal class RemoteArduino
    {
        IStream _connection;
        RemoteDevice _arduino;
        private bool _isInitialized;

        internal void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            _connection = new UsbSerial("VID_2341", "PID_0042"); //Arduino MEGA is VID_2341 and PID_0042
            _connection.ConnectionEstablished += _connection_ConnectionEstablished;
            _connection.ConnectionFailed += _connection_ConnectionFailed;
            _connection.begin(57600, SerialConfig.SERIAL_8N1);
        }

        private void _connection_ConnectionFailed(string message)
        {
            Debug.WriteLine("Serial connection to the Arduino failed. Probably a USB problem");
        }

        private void _arduino_DeviceConnectionFailed(string message)
        {
            Debug.WriteLine("Arduino connection failed - " + message);
        }

        private void _connection_ConnectionEstablished()
        {
            Debug.WriteLine("Serial connection to the Arduino established");
            _arduino = new RemoteDevice(_connection);
            _arduino.DeviceConnectionFailed += _arduino_DeviceConnectionFailed;
            _arduino.DeviceReady += _arduino_DeviceReady;
        }

        private void _arduino_DeviceReady()
        {
            Debug.WriteLine("Arduino connection established");

            _arduino.DigitalPinUpdated += _arduino_DigitalPinUpdated;
            _arduino.StringMessageReceived += _arduino_StringMessageReceived;
        }

        private void _arduino_StringMessageReceived(string message)
        {
            Debug.WriteLine($"Message from the Arduino : {message}");
        }

        private void _arduino_DigitalPinUpdated(byte pin, PinState state)
        {
            Debug.WriteLine($"Digital pin state changed - pin: {pin}, state: {state}");
        }
    }
}