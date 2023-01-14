﻿using System;
using System.Net;
using System.Net.Sockets;
// ReSharper disable UseNullPropagation

namespace EdiabasLib
{
    public class EdElmWifiInterface
    {
#if Android
        public class ConnectParameterType
        {
            public ConnectParameterType(TcpClientWithTimeout.NetworkData networkData)
            {
                NetworkData = networkData;
            }

            public TcpClientWithTimeout.NetworkData NetworkData { get; }
        }
#endif

        protected delegate void ExecuteNetworkDelegate();

        public const string PortId = "ELM327WIFI";
        public static string ElmIp = "192.168.0.10";
        public static int ElmPort = 35000;
        protected static TcpClient TcpElmClient;
        protected static NetworkStream TcpElmStream;
        protected static int ConnectTimeout = 5000;
        protected static string ConnectPort;
        protected static object ConnectParameter;
        protected static object NetworkData;
        private static EdElmInterface _edElmInterface;

        static EdElmWifiInterface()
        {
        }

        public static NetworkStream NetworkStream => TcpElmStream;

        public static EdiabasNet Ediabas { get; set; }

        public static bool InterfaceConnect(string port, object parameter)
        {
            if (TcpElmClient != null)
            {
                return true;
            }
            try
            {
                ConnectPort = port;
                ConnectParameter = parameter;
                NetworkData = null;

                if (!port.StartsWith(PortId, StringComparison.OrdinalIgnoreCase))
                {
                    InterfaceDisconnect();
                    return false;
                }

                string adapterIp = ElmIp;
                int adapterPort = ElmPort;
                string portData = port.Remove(0, PortId.Length);
                if ((portData.Length > 0) && (portData[0] == ':'))
                {
                    // special ip
                    string addr = portData.Remove(0, 1);
                    string[] stringList = addr.Split(':');
                    if (stringList.Length == 0)
                    {
                        InterfaceDisconnect();
                        return false;
                    }

                    adapterIp = stringList[0];
                    if (stringList.Length > 1)
                    {
                        if (int.TryParse(stringList[1], out int portNum))
                        {
                            adapterPort = portNum;
                        }
                    }
                }
#if Android
                if (ConnectParameter is ConnectParameterType connectParameter)
                {
                    NetworkData = connectParameter.NetworkData;
                }
#endif
                IPAddress hostIpAddress = IPAddress.Parse(adapterIp);
                TcpClientWithTimeout.ExecuteNetworkCommand(() =>
                {
                    TcpElmClient = new TcpClientWithTimeout(hostIpAddress, adapterPort, ConnectTimeout, true).Connect();
                }, hostIpAddress, NetworkData);
                TcpElmStream = TcpElmClient.GetStream();
                _edElmInterface = new EdElmInterface(Ediabas, TcpElmStream, TcpElmStream);
                if (!_edElmInterface.Elm327Init())
                {
                    InterfaceDisconnect();
                    return false;
                }
            }
            catch (Exception)
            {
                InterfaceDisconnect();
                return false;
            }
            return true;
        }

        public static bool InterfaceDisconnect()
        {
            bool result = true;
            if (_edElmInterface != null)
            {
                _edElmInterface.Dispose();
                _edElmInterface = null;
            }

            try
            {
                if (TcpElmStream != null)
                {
                    TcpElmStream.Close();
                    TcpElmStream = null;
                }
            }
            catch (Exception)
            {
                result = false;
            }

            try
            {
                if (TcpElmClient != null)
                {
                    TcpElmClient.Close();
                    TcpElmClient = null;
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        public static EdInterfaceObd.InterfaceErrorResult InterfaceSetConfig(EdInterfaceObd.Protocol protocol, int baudRate, int dataBits, EdInterfaceObd.SerialParity parity, bool allowBitBang)
        {
            if (TcpElmStream == null)
            {
                return EdInterfaceObd.InterfaceErrorResult.ConfigError;
            }
            if ((protocol != EdInterfaceObd.Protocol.Uart) ||
                (baudRate != 115200) || (dataBits != 8) || (parity != EdInterfaceObd.SerialParity.None))
            {
                return EdInterfaceObd.InterfaceErrorResult.ConfigError;
            }
            return EdInterfaceObd.InterfaceErrorResult.NoError;
        }

        public static bool InterfaceSetDtr(bool dtr)
        {
            if (TcpElmStream == null)
            {
                return false;
            }
            return true;
        }

        public static bool InterfaceSetRts(bool rts)
        {
            if (TcpElmStream == null)
            {
                return false;
            }
            return true;
        }

        public static bool InterfaceGetDsr(out bool dsr)
        {
            dsr = true;
            if (TcpElmStream == null)
            {
                return false;
            }
            return true;
        }

        public static bool InterfaceSetBreak(bool enable)
        {
            return false;
        }

        public static bool InterfaceSetInterByteTime(int time)
        {
            return true;
        }

        public static bool InterfaceSetCanIds(int canTxId, int canRxId, EdInterfaceObd.CanFlags canFlags)
        {
            return true;
        }

        public static bool InterfacePurgeInBuffer()
        {
            if (TcpElmStream == null)
            {
                return false;
            }
            if (_edElmInterface == null)
            {
                return false;
            }
            return _edElmInterface.InterfacePurgeInBuffer();
        }

        public static bool InterfaceAdapterEcho()
        {
            return false;
        }

        public static bool InterfaceHasPreciseTimeout()
        {
            return false;
        }

        public static bool InterfaceHasAutoBaudRate()
        {
            return false;
        }

        public static bool InterfaceHasAutoKwp1281()
        {
            return false;
        }

        public static int? InterfaceAdapterVersion()
        {
            return null;
        }

        public static byte[] InterfaceAdapterSerial()
        {
            return null;
        }

        public static double? InterfaceAdapterVoltage()
        {
            return null;
        }

        public static bool InterfaceHasIgnitionStatus()
        {
            return false;
        }

        public static bool InterfaceSendData(byte[] sendData, int length, bool setDtr, double dtrTimeCorr)
        {
            if (TcpElmStream == null)
            {
                return false;
            }
            if (_edElmInterface == null)
            {
                return false;
            }
            if (_edElmInterface.StreamFailure)
            {
                if (Ediabas != null)
                {
                    Ediabas.LogString(EdiabasNet.EdLogLevel.Ifh, "Reconnecting");
                }
                InterfaceDisconnect();
                if (!InterfaceConnect(ConnectPort, ConnectParameter))
                {
                    _edElmInterface.StreamFailure = true;
                    return false;
                }
            }
            if (!_edElmInterface.InterfaceSendData(sendData, length, setDtr, dtrTimeCorr))
            {
                return false;
            }
            return true;
        }

        public static bool InterfaceReceiveData(byte[] receiveData, int offset, int length, int timeout, int timeoutTelEnd, EdiabasNet ediabasLog)
        {
            if (TcpElmStream == null)
            {
                return false;
            }
            if (_edElmInterface == null)
            {
                return false;
            }
            return _edElmInterface.InterfaceReceiveData(receiveData, offset, length, timeout, timeoutTelEnd, ediabasLog);
        }

        public static bool InterfaceSendPulse(UInt64 dataBits, int length, int pulseWidth, bool setDtr, bool bothLines, int autoKeyByteDelay)
        {
            return false;
        }
    }
}
