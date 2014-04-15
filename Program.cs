using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace SerialPortTest
{
  enum AppAction { ShowHelp, ListPorts, Listen, SendAscii, SendFile, SendText };

  class Settings
  {
    public string PortName { get; set; }
    public AppAction Action { get; set; }
    public bool NoEcho { get; set; }
    public int BaudRate { get; set; }
    public char Ascii { get; set; }
    public int Count { get; set; }
    public string FilePath { get; set; }
    public string Text { get; set; }

    public Settings()
    {
      this.Action   = AppAction.ShowHelp;
      this.NoEcho   = false;
      this.BaudRate = 9600;
      this.Ascii    = (char)0x00;
      this.Text     = string.Empty;
      this.Count    = 1;
      this.FilePath = null;
    }
  }

  class Program
  {
    static void Main(string[] args)
    {
      new Program().Run(args);
    }

    public Program() { }

    #region Methods

    private int TryParseBaudRate(string text)
    {
      int baudRate;
      if (!Int32.TryParse(text, out baudRate))
        baudRate = 9600;
      return baudRate;
    }

    private char TryParseAscii(string text)
    {
      int number;
      if (!Int32.TryParse(text, out number))
        number = 0;
      return (char)number;
    }

    private int TryParseCount(string text)
    {
      int count;
      if (!Int32.TryParse(text, out count))
        count = 1;
      return count;
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

    private SerialPort GetOpenedPort(string portName, int baudRate)
    {
      Console.WriteLine(string.Format("Opening port: '{0}' with baudrate {1}...", portName, baudRate));
      SerialPort serialPort = new SerialPort();
      serialPort.PortName = portName;
      serialPort.BaudRate = baudRate;
      serialPort.Open();
      Console.WriteLine("Opened port successfully!");
      return serialPort;
    }

    private void EchoLoop(SerialPort serialPort, bool noEcho)
    {
      try
      {
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
        Console.WriteLine(string.Format("The following error occurred while trying to use SerialPort '{0}':{1}{2}", serialPort.PortName, Environment.NewLine, ex));
      }
    }

    private void SendAscii(SerialPort serialPort, char sendPattern, int count)
    {
      SendText(serialPort, new string(new char[] { sendPattern }), count);
    }

    private void SendText(SerialPort serialPort, string text, int count)
    {
      for (int i = 0; i < count || count == 0; i++)
        serialPort.Write(text);
    }

    private OptionSet ParseArguments(string[] args, Settings settings)
    {
      var optionSet = new Mono.Options.OptionSet()
      {
        { "h|help", "shows this help", s => settings.Action = AppAction.ShowHelp },
        { "p=|port=", "sets the name of the serialport (COM1g, COM2, etc)", s => { settings.Action = AppAction.Listen; settings.PortName = s; }},
        { "l|listports", "lists the name of all available COM ports", s => settings.Action = AppAction.ListPorts },
        { "n|no-echo", "does not echo the received byte back", s => settings.NoEcho = true },
        { "b=|baudrate=", "sets the baudrate of the serialport", s => settings.BaudRate = TryParseBaudRate(s) },
        //{ "f=|send-file=", "sends the specified file over the serialport", s => { settings.FilePath = s; settings.Action = AppAction.SendFile; } },
        { "a=|send-ascii=", "sends the specified ascii value over the serialport", s => { settings.Ascii = TryParseAscii(s); settings.Action = AppAction.SendAscii; } },
        { "c=|count=", "specifies the number of files or ascii characters that are sent over the serialport", s => { settings.Count = TryParseCount(s); } },
        { "t=|text=", "specifies the text that is to be sent over the serialport", s => { settings.Text = s; settings.Action = AppAction.SendText; } }
      };

      optionSet.Parse(args);

      return optionSet;
    }

    public void Run(string[] args)
    {
      /* create default settings */
      var settings = new Settings();

      /* update the settings with the commandline arguments */
      var optionSet = ParseArguments(args, settings);

      /* do what we are told */
      switch (settings.Action)
      {
        case AppAction.ListPorts: ListPortNames(); break;
        case AppAction.ShowHelp: optionSet.WriteOptionDescriptions(Console.Out); break;

        case AppAction.Listen:
        case AppAction.SendAscii:
        case AppAction.SendFile:
        case AppAction.SendText:

          SerialPort serialPort = null;
          try { serialPort = GetOpenedPort(settings.PortName, settings.BaudRate); }
          catch (Exception ex) { Console.WriteLine(string.Format("The following error occurred while trying to open SerialPort '{0}':{1}{2}", settings.PortName, Environment.NewLine, ex)); }

          if (serialPort != null)
          {
            switch (settings.Action)
            {
              case AppAction.Listen: EchoLoop(serialPort, settings.NoEcho); break;
              case AppAction.SendAscii: SendAscii(serialPort, settings.Ascii, settings.Count); break;
              case AppAction.SendText: SendText(serialPort, settings.Text, settings.Count); break;
            }
          }
          break;
      }
    }

    #endregion

  }
}
