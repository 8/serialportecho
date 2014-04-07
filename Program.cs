using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace SerialPortEcho
{
  class Program
  {
    static void Main(string[] args)
    {
      new Program().Run(args);
    }

    private readonly OptionSet OptionSet;
    enum AppAction { ShowHelp, ListPorts, Echo };

    public string PortName { get; set; }
    private AppAction Action;
    private bool NoEcho = false;
    private int BaudRate = 9600;

    public Program()
    {
      this.OptionSet = new Mono.Options.OptionSet()
      {
        { "h|help", "shows this help", s => this.Action = AppAction.ShowHelp },
        { "p=|port=", "sets the name of the serialport (COM1, COM2, etc)", s => { this.Action = AppAction.Echo; this.PortName = s; }},
        { "l|listports", "lists the name of all available COM ports", s => this.Action = AppAction.ListPorts },
        { "n|no-echo", "does not echo the received byte back", s => this.NoEcho = true },
        { "b=|baudrate=", "sets the baudrate of the serialport", s => this.BaudRate = TryParseBaudRate(s) }
      };
    }

    private int TryParseBaudRate(string text)
    {
      int baudRate;
      if (!Int32.TryParse(text, out baudRate))
        baudRate = 9600;

      return baudRate;
    }

    private void ListPortNames()
    {
      try
      {
        var portNames = SerialPort.GetPortNames();
        Console.WriteLine(string.Format("Available Ports ({0}):", portNames.Length));
        foreach (var name in portNames)
          Console.WriteLine(name);
      }
      catch (Exception ex)
      {
        Console.Write(string.Format("The following error occurred while trying to list available SerialPorts:{0}{1}", Environment.NewLine, ex));
      }
    }

    private void EchoOnPort(string portName, int baudRate)
    {
      try
      {
        Console.WriteLine(string.Format("Opening port: '{0}' with baudrate {1}...", portName, baudRate));
        SerialPort serialPort = new SerialPort();
        serialPort.PortName = portName;
        serialPort.BaudRate = baudRate;
        serialPort.Open();
        Console.WriteLine("Opened port successfully!");

        int readByte; char[] writeBuffer = new char[1];
        while ((readByte = serialPort.ReadByte()) != -1)
        {
          /* write the received byte to the console */
          Console.Write((char)readByte);
          writeBuffer[0] = (char)readByte;

          /* echo the byte back to sender */
          if (!this.NoEcho)
            serialPort.Write(writeBuffer, 0, 1);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(string.Format("The following error occurred while trying to open SerialPort '{0}':{1}{2}", portName, Environment.NewLine, ex));
      }
    }

    public void Run(string[] args)
    {
      this.OptionSet.Parse(args);

      switch (this.Action)
      {
        case AppAction.Echo: EchoOnPort(this.PortName, this.BaudRate); break;
        case AppAction.ListPorts: ListPortNames(); break;
        case AppAction.ShowHelp: this.OptionSet.WriteOptionDescriptions(Console.Out); break;
      }
    }


  }
}
