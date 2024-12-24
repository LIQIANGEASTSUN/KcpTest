using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using KcpProject;
using System;

public class KcpTest : MonoBehaviour
{

    private KCP mKcp = null;

    // Start is called before the first frame update
    void Start()
    {
        uint conv = (uint)UnityEngine.Random.Range(1, uint.MaxValue);
        mKcp = new KCP(conv, SocketSend);

        // normal:  0, 40, 2, 1
        // fast:    0, 30, 2, 1
        // fast2:   1, 20, 2, 1
        // fast3:   1, 10, 2, 1
        mKcp.NoDelay(0, 30, 2, 1);
        mKcp.SetStreamMode(true);
    }

    private uint mNextUpdateTime = 0;

    private int number = 0;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            string msg = $"·¢ËÍÏûÏ¢:{number}";
            ++number;
            byte[] byteDatas = Encoding.UTF8.GetBytes(msg);
            Debug.LogError("Send Origin byteLength:" + byteDatas.Length);
            Send(byteDatas);
        }

        if (0 == mNextUpdateTime || mKcp.CurrentMS >= mNextUpdateTime)
        {
            mKcp.Update();
            mKcp.Check();
        }

        CheckReceive();
    }

    private void CheckReceive()
    {
        for (; ; )
        {
            int size = mKcp.PeekSize();
            if (size < 0)
                break;

            byte[] byteDatas = new byte[size];
            int n = mKcp.Recv(byteDatas, 0, size);
            if (n > 0)
            {
                Receive(byteDatas);
            }
        }
    }

    private void SocketSend(byte[] byteDatas, int length)
    {
        byte[] bytes = new byte[length];
        Array.Copy(byteDatas, bytes, length);
        string msg = Encoding.UTF8.GetString(bytes);
        Debug.LogError("SocketSend byteLength:" + bytes.Length);
        Debug.LogError("SocketSend:" + msg);

        bool ackNoDelay = true;
        mKcp.Input(bytes, 0, bytes.Length, true, ackNoDelay);
    }

    public void Send(byte[] byteDatas)
    {
        mKcp.Send(byteDatas);
    }

    private void Receive(byte[] byteDatas)
    {
        string msg = Encoding.UTF8.GetString(byteDatas);
        Debug.LogError("Receive:" + msg + "     byteLength:" + byteDatas.Length);
    }

}
