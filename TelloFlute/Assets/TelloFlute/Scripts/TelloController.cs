using UnityEngine;
using System.Text;

#if UNITY_EDITOR
using System.Net.Sockets;
#elif WINDOWS_UWP
using System;
using System.IO;
using Windows.Networking;
using Windows.Networking.Sockets;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace TelloFlute
{
    /// <summary>
    /// TELLOを操作するためのクラス
    /// </summary>
    public class TelloController : MonoBehaviour
    {
        /// <summary>
        /// TELLOのIPアドレス
        /// </summary>
        private string ip = "192.168.10.1";

        /// <summary>
        /// ポート番号
        /// </summary>
        private int port = 8889;

#if UNITY_EDITOR
        /// <summary>
        /// UDPクライアント
        /// </summary>
        private UdpClient client;

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Start()
        {
            client = new UdpClient();
            client.Connect(ip, port);
        }

        /// <summary>
        /// キーボードによるTELLOの操作（動作確認用）
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("SPACE");
                byte[] dgram = Encoding.UTF8.GetBytes("command");
                client.Send(dgram, dgram.Length);
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Debug.Log("UP");
                byte[] dgram = Encoding.UTF8.GetBytes("takeoff");
                client.Send(dgram, dgram.Length);

            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                Debug.Log("DOWN");
                byte[] dgram = Encoding.UTF8.GetBytes("land");
                client.Send(dgram, dgram.Length);

            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Debug.Log("LEFT");
                byte[] dgram = Encoding.UTF8.GetBytes("flip l");
                client.Send(dgram, dgram.Length);

            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Debug.Log("RIGHT");
                byte[] dgram = Encoding.UTF8.GetBytes("flip r");
                client.Send(dgram, dgram.Length);
            }
        }

        /// <summary>
        /// 再生モードを停止したときに呼び出されます
        /// </summary>
        private void OnApplicationQuit()
        {
            client.Close();
        }

        /// <summary>
        /// TELLOに命令を送信します
        /// </summary>
        /// <param name="command">命令</param>
        public void SendCommand(string command)
        {
            Debug.Log("Send : " + command);
            byte[] dgram = Encoding.UTF8.GetBytes(command);
            client.Send(dgram, dgram.Length);
        }

#elif WINDOWS_UWP
        private DatagramSocket socket;
        private object lockObject = new object();
        private const int MAX_BUFFER_SIZE = 1024;
        private EndpointPair endpoint = new EndpointPair(null, "", new HostName("192.168.10.1"), "8889");

        /// <summary>
        /// 初期化処理
        /// </summary>
        async void Start ()
        {
            try
            {
                socket = new DatagramSocket();
                socket.MessageReceived += OnMessage;
                await socket.BindServiceNameAsync(port.ToString());
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        /// <summary>
        /// TELLOに命令を送信します
        /// </summary>
        /// <param name="command">命令</param>
        public async Task SendCommand(string command)
        {
            using (var stream = await socket.GetOutputStreamAsync(endpoint))
            {
                var data = Encoding.UTF8.GetBytes(command);
                var operation = await stream.WriteAsync(data.AsBuffer());
            }
        }

        /// <summary>
        /// データ受信時の処理を行います
        /// </summary>
        async void OnMessage(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            using (var stream = args.GetDataStream().AsStreamForRead())
            {
                byte[] buffer = new byte[MAX_BUFFER_SIZE];
                await stream.ReadAsync(buffer, 0, MAX_BUFFER_SIZE);
                lock (lockObject)
                {
                    // データ受信処理
                }
            }
        }
#endif

    } // class TelloController
} // namespace TelloFlute
