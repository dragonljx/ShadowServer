/*
 项目添加入了git本地库
 修改实验
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

//委托
public delegate void ReceiveCallBack(ushort[] depthData);

public class Server : MonoBehaviour
{
    private static Server _instance;
    public List<Socket> socketClient = new List<Socket>();

    public Socket serverSocket;
    private ReceiveCallBack callback;

    private static byte[] result = new byte[1024];
    public const string terminateString = "\r\n\t"; //消息的结尾标记

    public static Server Instance { get { return _instance; } }

    private void Start()
    {
        _instance = this;
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    /// <summary>
    /// 初始化服务器
    /// </summary>
    /// <param name="cb"></param>
    /// <param name="serverIP"></param>
    /// <param name="serverPort"></param>
    public void ServerInit(ReceiveCallBack cb, string serverIP, int serverPort)
    {
        callback = cb;
        IPAddress IP = IPAddress.Parse(serverIP);
        IPEndPoint Point = new IPEndPoint(IP, serverPort);
        serverSocket.Bind(Point);
        serverSocket.Listen(10);

        Debug.Log("启动 " + serverSocket.LocalEndPoint.ToString() + " 成功");

        Thread thred = new Thread(ClientConnectListen);
        thred.IsBackground = true;
        thred.Start();
    }

    /// <summary>
    /// 客户的链接请求监听
    /// </summary>
    private void ClientConnectListen()
    {
        while (true)
        {
            //为新的客户端连接创建一个Socket对象
            Socket clientSocket = serverSocket.Accept();
            socketClient.Add(clientSocket);
            Debug.Log("客户端成功连接" + clientSocket.RemoteEndPoint.ToString());
            //向连接的客户端发送连接成功的数据
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteString("Connected Server");
            clientSocket.Send(WriteMessage(buffer.ToBytes()));
            //每个客户端连接创建一个线程来接受该客户端发送的消息
            Thread thread = new Thread(RecieveMessage);
            //后台线程随着主线程结束而退出
            thread.IsBackground = true;
            thread.Start(clientSocket);
        }
    }

    /// <summary>
    /// 数据转换
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private static byte[] WriteMessage(byte[] message)
    {
        MemoryStream ms = null;
        using (ms = new MemoryStream())
        {
            ms.Position = 0;
            BinaryWriter writer = new BinaryWriter(ms);
            ushort msglen = (ushort)message.Length;
            writer.Write(msglen);
            writer.Write(message);
            writer.Flush();
            return ms.ToArray();
        }
    }

    private byte[] CacheData;                   //缓存当前接收的信息
    private int byteRead = 0;                   //数据长度
    private ushort[] depthData;                 //深度图数据
    private int len = 0;

    private void RecieveMessage(object clientSocket)
    {
        Socket mClientSocket = (Socket)clientSocket;
        //ByteBuffer buffData = null;

        while (true)
        {
            try
            {
                if (byteRead + 1024 < len || byteRead == 0)
                {
                    mClientSocket.Receive(result);

                    if (len == 0)
                    {
                        ProcessHead(result, out len, mClientSocket);
                        Debug.Log("无线运行" + len);
                        continue;
                    }
                    //Debug.Log("这里添加一行");
                    result.CopyTo(CacheData, byteRead);
                    byteRead += 1024;
                }
                else
                {
                    //Thread.Sleep(500);
                    //剩余字节
                    int remainByte = len - byteRead;
                    mClientSocket.Receive(result);
                    //剩余数据添加入数组
                    Array.Copy(result, 0, CacheData, byteRead, remainByte);

                    depthData = new ushort[len / 2];
                    for (int i = 0; i < len / 2; i++)
                    {
                        depthData[i] = BitConverter.ToUInt16(CacheData, i * 2);
                    }
                    //数据传到深度图显示
                    InfraredSourceView.Instance._Data = depthData;
                    //调用点云转换和建模
                    callback(depthData);
                    ProcessHead(result, out len, mClientSocket);

                    #region 使用多线程调用主线程方法

                    //Loom.RunAsync(() =>
                    //{
                    //    depthData = new ushort[len / 2];
                    //    for (int i = 0; i < len / 2; i++)
                    //    {
                    //        depthData[i] = BitConverter.ToUInt16(CacheData, i * 2);
                    //    }
                    //    Loom.QueueOnMainThread(() =>
                    //    {
                    //        InfraredSourceView.Instance.View(depthData);
                    //    });
                    //});

                    ////InfraredSourceView.Instance._Data = depthData;
                    //ProcessHead(result, out len);

                    #endregion 使用多线程调用主线程方法
                }

                #region 流方式传输

                /*
                if (len == 0)
                {
                    //数据长度
                    byte[] byt = ProcessHead(result, out len);
                    buffData = new ByteBuffer(len);
                    buffData.WriteBytes(byt);
                    continue;
                }

                if (buffData.stream.Length + 1024 <len)
                {
                    buffData.WriteBytes(result);
                }
                else
                {
                    //剩余数据存入
                    buffData.WriteBytes(result, 0, len - (int)buffData.stream.Length);
                    InfraredSourceView.Instance._Data = buffData.ReadShort(len);
                    //InfraredSourceView.Instance.View(buffData.ReadShort(len));
                    //判断下一个信息
                    ProcessHead(result, out len);

                    Debug.Log(buffData.stream.Length);
                }
                */

                #endregion 流方式传输
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                mClientSocket.Shutdown(SocketShutdown.Both);
                mClientSocket.Close();
                break;
            }
        }
    }

    /// <summary>
    /// 定位符查找
    /// </summary>
    /// <param name="data"></param>
    /// <param name="number"></param>
    public void ProcessHead(byte[] data, out int len, Socket socket)
    {
        try
        {
            string rawMsg = Encoding.ASCII.GetString(data);
            byteRead = 0;
            int number = 1024;
            for (int i = 0; i < rawMsg.Length; i++)
            {
                if (i <= rawMsg.Length - 3)
                {
                    //如果是消息结束符号则进行解析数据
                    string xx = rawMsg.Substring(i, 3);
                    if (rawMsg.Substring(i, 3) == terminateString)
                    {
                        if (i >= 1004)
                        {
                            Debug.Log("运行");
                            byte[] byt = new byte[2048];
                            Array.Copy(data, 0, byt, 0, data.Length);
                            socket.Receive(result);
                            Array.Copy(result, 0, byt, data.Length, result.Length);
                            data = byt;
                            byteRead += 1024;
                            number += 1024;
                        }

                        len = BitConverter.ToInt32(data, i + 3);
                        if (len < 0 || len > 500000)
                        {
                            Debug.Log("重合");
                            continue;
                        }
                        //Debug.Log("长度:" + len + "     数组长度：" + data.Length + "    位置 :" + i + "\n识别的是:" + xx + "\\");
                        CacheData = new byte[len];
                        //如果信息不满1024
                        if (len < 1018)
                        {
                            byteRead += len;
                            Array.Copy(data, i + 7, CacheData, 0, len);
                        }
                        else
                        {
                            byteRead += 1024 - (7 + i);
                            Array.Copy(data, i + 7, CacheData, 0, number - (7 + i));
                        }
                        return;
                    }
                }
            }
            len = 0;
            return;
        }
        catch (Exception e)
        {
            Debug.Log(e);
            throw;
        }
    }

    private void OnDestroy()
    {
        foreach (var item in socketClient)
        {
            item.Shutdown(SocketShutdown.Receive);
            item.Close();
        }

        serverSocket.Close();
        Debug.Log("清理完成");
    }
}