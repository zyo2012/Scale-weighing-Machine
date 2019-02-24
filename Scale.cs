using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;

namespace Pos.Hardware
{

	/// <summary>
	/// Read scale weight, this has been test on the KPOS 1530 model
	/// </summary>
	public class Scale
	{
		private const int Baud = 9600;
		private const int Timeout = 300; // in milliseconds

		private static readonly byte[] ReadValueMessage = { 0x5, 0x12 };

		public readonly string Port;
		private SerialPort _serialPort;

		private static Scale _current;
		public static Scale Current {
			get {
				if (_current == null) {
					if (Settings.Current.ScalePort != null) {  // have you own settings value for port, will be something like COM4
						_current = new Scale(Settings.Current.ScalePort);
					}
				}
				return _current;
			}
		}
		public string LastError { get; set; }

		private void CreatePort() {
			_serialPort = new SerialPort(Port, Baud, Parity.None, 8);
			_serialPort.DataReceived += Port_DataReceived;
			_serialPort.Open();
		}

		private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e) {
			string dataRead = _serialPort.ReadExisting();
			if (dataRead.Length == 9 && dataRead.StartsWith("\u0002")) {
				string strValue = dataRead.Substring(2, 5);
				_lastValue = 0.01m * int.Parse(strValue);
			}
		}

		private decimal _lastValue;

		public decimal ReadValue() {
			LastError = null;
			if (_serialPort == null) {
				try {
					CreatePort();
				} catch (Exception ex) {
					LastError = ex.Message;
					return -2;
				}

			}
			_lastValue = 0;
			try {
				Debug.Assert(_serialPort != null, "_serialPort != null");
				_serialPort.Write(ReadValueMessage, 0, ReadValueMessage.Length);
			} catch (Exception ex) {
				LastError = ex.Message;
				_serialPort = null; // error
				return -1;
			}

			System.Threading.Thread.Sleep(Timeout); // wait scale to reply
			return _lastValue;
		}

		/// <summary>
		/// Create the Scale object and connect to the COM port
		/// </summary>
		/// <param name="port"></param>
		public Scale(string port) {
			Port = port;
			CreatePort();
		}

		/// <summary>
		/// Try to find a scale, (find the COM1 port that a scale respond properly)
		/// </summary>
		/// <returns></returns>
		public static string Detect() {
			if (_current != null) {
				_current.Close(); // prevent having 2 socket on the same port
				System.Threading.Thread.Sleep(3000); // need to wait because the serial port doesn't close immediatly
			}
			// Get a list of serial port names.
			IEnumerable<string> ports = SerialPort.GetPortNames().OrderByDescending(s => s);

			Console.WriteLine("The following serial ports were found:");

			// Display each port name to the console.
			foreach (string port in ports) {
				try {
					Scale s = new Scale(port);
					if (s.ReadValue() >= 0) {
						_current = s;
						return port;
					}
				} catch (Exception) {
				}

			}
			return null; // no scale detected
		}

		private void Close() {
			_serialPort?.Dispose();
		}
	}
}
