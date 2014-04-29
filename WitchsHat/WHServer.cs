using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitchsHat
{
    class WHServer
    {
        volatile bool running;
        int serverPort;
        System.Net.HttpListener listener;
        System.Net.HttpListener closer;
        System.Threading.Thread serverThread;

        public void Start()
        {
            serverThread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadFunction));
            serverThread.Start();
        }

        public void Start(int port)
        {
            serverPort = port;
            serverThread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadFunction));
            serverThread.Start();
        }

        public void Stop()
        {
            running = false;
            if (listener != null)
            {
                listener.Close();
            }
        }

        public bool IsRunning()
        {
            return running;
        }

        public string RootDir { get; set; }

        public int Port
        {
            get
            {
                return serverPort;
            }
        }

        void ThreadFunction()
        {
            running = true;
            string prefix = "http://localhost:" + serverPort + "/"; // 受け付けるURL

            listener = new System.Net.HttpListener();
            listener.Prefixes.Add(prefix); // プレフィックスの登録
            listener.Start();
            try
            {
                while (running)
                {

                    System.Net.HttpListenerContext context = listener.GetContext();
                    System.Net.HttpListenerRequest req = context.Request;
                    System.Net.HttpListenerResponse res = context.Response;

                    Console.WriteLine(req.RawUrl);

                    // リクエストされたURLからファイルのパスを求める
                    string path = RootDir + req.RawUrl.Replace("/", "\\");
                    Console.WriteLine(path);

                    // ファイルが存在すればレスポンス・ストリームに書き出す
                    if (File.Exists(path))
                    {
                        byte[] content = File.ReadAllBytes(path);
                        res.OutputStream.Write(content, 0, content.Length);
                    }
                    res.Close();

                }
            }
            catch
            {

            }
            Console.WriteLine("server close");
        }
    }
}
