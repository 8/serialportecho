using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace SerialPortEcho
{
  enum AppAction { ShowHelp, ListPorts, Listen };

  class Settings
  {
    public string PortName { get; set; }
    public AppAction Action;
    public bool NoEcho = false;
    public int BaudRate = 9600;
  }

  class Program
  {
    static void Main(string[] args)
    {
      new Program().Run(args);
    }

    public Program()
    {
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

    private void StartListenOnPort(string portName, int baudRate, bool noEcho)
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
          if (!noEcho)
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
      var settings = new Settings();

      var optionSet = new Mono.Options.OptionSet()
      {
        { "h|help", "shows this help", s => settings.Action = AppAction.ShowHelp },
        { "p=|port=", "sets the name of the serialport (COM1g, COM2, etc)", s => { settings.Action = AppAction.Listen; settings.PortName = s; }},
        { "l|listports", "lists the name of all available COM ports", s => settings.Action = AppAction.ListPorts },
        { "n|no-echo", "does not echo the received byte back", s => settings.NoEcho = true },
        { "b=|baudrate=", "sets the baudrate of the serialport", s => settings.BaudRate = TryParseBaudRate(s) }
      };

      optionSet.Parse(args);

      switch (settings.Action)
      {
        case AppAction.Listen: StartListenOnPort(settings.PortName, settings.BaudRate, settings.NoEcho); break;
        case AppAction.ListPorts: ListPortNames(); break;
        case AppAction.ShowHelp: optionSet.WriteOptionDescriptions(Console.Out); break;
      }
    }


  }
}
