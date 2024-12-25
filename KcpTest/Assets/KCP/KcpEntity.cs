using KcpProject;
using System;

/// <summary>
/// 一个 Kcp 实例
/// </summary>
public class KcpEntity
{

    private KCP mKcp = null;
    private uint mNextUpdateTime = 0;

    private Action<byte[]> socketSendEvent;
    private Action<byte[]> socketReceiveEvent;

    public KcpEntity(uint conv, Action<byte[]> socketSendEvent, Action<byte[]> socketReceiveEvent)
    {
        // 实例化 kcp
        // conv 客户端与服务器必须一致
        // SocketSend 当 Kcp 认为该向服务器发送 数据时，回调 SocketSend，在 SocketSend 方发中通过 自己的网络协议比如 UDP Socket 向服务器发消息
        mKcp = new KCP(conv, ReadySend);
        this.socketSendEvent = socketSendEvent;
        this.socketReceiveEvent = socketReceiveEvent;

        // normal:  0, 40, 2, 1
        // fast:    0, 30, 2, 1
        // fast2:   1, 20, 2, 1
        // fast3:   1, 10, 2, 1
        int nodelay = 0;     // 是否启用 nodelay模式，0不启用；1启用
        int interval = 30;   // 协议内部工作的 interval，单位毫秒，比如 10ms或者 20ms
        int resend = 2;      // 快速重传模式，默认0关闭，可以设置2（2次ACK跨越将会直接重传）
        int nc = 1;          // 是否关闭流控，默认是0代表不关闭，1代表关闭
        mKcp.NoDelay(nodelay, interval, resend, nc);
        mKcp.SetStreamMode(true);

        // 最大传输单元：纯算法协议并不负责探测 MTU，默认 mtu是1400字节，可以使用ikcp_setmtu来设置该值。
        // 该值将会影响数据包归并及分片时候的最大传输单元
        mKcp.SetMtu(512);

        // 最小RTO：不管是 TCP还是 KCP计算 RTO时都有最小 RTO的限制，即便计算出来RTO为40ms，
        // 由于默认的 RTO是100ms，协议只有在100ms后才能检测到丢包，快速模式下为30ms，可以手动更改该值：
    }

    /// <summary>
    /// 向服务器发送数据，调用 mKcp.Send
    /// Kcp 并不会向服务器发送数据
    /// 而是在 kcp 认为该向服务器发送消息时，调用 实例化时(new KCP(conv, SocketSend);) 传入的 SocketSend 方法
    /// 在 SocketSend(byte[] bytes, int length) 中将 bytes 通过 UDP Socket 发送给服务器
    /// bytes 是 kcp 处理过的 byteDatas 数据，给 byteDatas 再加上 kcp 的消息头
    /// bytes 的 字节数是大于 byteDatas 的
    /// </summary>
    /// <param name="byteDatas"></param>
    public void Send(byte[] byteDatas)
    {
        mKcp.Send(byteDatas);
    }

    /// <summary>
    /// Kcp 认为该向服务器发送数据了
    /// </summary>
    /// <param name="byteDatas"> Kcp 处理后的字节，加上了 Kcp 消息头等信息 </param>
    /// <param name="length"> 需要发送的字节长度 </param>
    private void ReadySend(byte[] byteDatas, int length)
    {
        byte[] bytes = new byte[length];
        Array.Copy(byteDatas, bytes, length);
        this.socketSendEvent?.Invoke(bytes);
    }

    /// <summary>
    /// 以一定频率调用 ikcp_update来更新 kcp状态，并且传入当前时钟（毫秒单位）
    /// 如 10ms调用一次，或用 ikcp_check确定下次调用 update的时间不必每次调用
    /// </summary>
    public void Update()
    {
        if (0 == mNextUpdateTime || mKcp.CurrentMS >= mNextUpdateTime)
        {
            mKcp.Update();
            // 获取下一次更新时间
            mNextUpdateTime = mKcp.Check();
        }

        CheckReceive();
    }

    /// <summary>
    /// UDP Socket 接收到的数据，不要直接处理，而是要通过 mKcp.Input 方法将数据传递给 Kcp
    /// 因为 byteDatas 是 kcp 封装后的消息，需要 kcp 处理后才能使用
    /// byteDatas 中包含了 Kcp 封装的消息头，Kcp 就是通过这里的消息头，来确认当先接收的是第几个消息包
    /// 有没有丢包超时等
    /// </summary>
    /// <param name="byteDatas"> 接收到的数据 </param>
    /// <param name="length"> 接收到的有效字节个数 </param>
    public void SocketReceive(byte[] byteDatas, int length)
    {
        bool ackNoDelay = true;
        mKcp.Input(byteDatas, 0, length, true, ackNoDelay);
    }

    /// <summary>
    /// 检测 Kcp 是否有接收到的数据
    /// </summary>
    private void CheckReceive()
    {
        for (; ; )
        {
            // 查看是否有需要接收的字节，返回需要接收的字节数量
            int size = mKcp.PeekSize();
            if (size < 0)
                break;

            byte[] byteDatas = new byte[size];
            // 从 Kcp 中获取接收的数据
            int n = mKcp.Recv(byteDatas, 0, size);
            if (n > 0)
            {
                // 从 Kcp 中读取到 服务端发送的数据，不包含 Kcp 头的消息体
                socketReceiveEvent?.Invoke(byteDatas);
            }
        }
    }

}


/*

在 KCP 协议中，conv（conversation ID）是用来标识一个通信会话的唯一标识符。这个 ID 用来区分不同的通信通道和会话，确保在同一物理连接（如UDP连接）上，可以区分不同的客户端或会话。

1. conv 的作用：
conv 用于标识客户端和服务器之间的会话。在 KCP 协议中，每个会话（即一个客户端和服务器之间的连接）都会有一个唯一的 conv 值。
当客户端与服务器建立连接时，客户端和服务器都会使用相同的 conv 来标识这条通信线路。这个 conv 在数据包的头部进行传递，以确保数据能够正确地传送到目标会话。

2. conv 的使用场景：
客户端与服务器之间的会话：每个客户端与服务器之间的连接会有一个唯一的 conv。例如，假设有两个客户端 A 和 B 连接到同一个服务器，那么客户端 A 和服务器之间的 conv 可以是 conv1，客户端 B 和服务器之间的 conv 可以是 conv2，这两个 conv 值是不同的。
同一个客户端与多个服务器的连接：如果同一个客户端与多个服务器建立连接，则每个连接（客户端与不同服务器之间的连接）会有一个独立的 conv。

3. 关于 conv1 和 conv2 的问题：
不同客户端的 conv：当你提到客户端 A 和 B 时，通常来说，A 和 B 的 conv 必须是不同的。也就是说：
客户端 A 和服务器的连接使用 conv1
客户端 B 和服务器的连接使用 conv2
为什么 conv 必须唯一：
conv 是用来区分不同通信会话的标识符。对于同一个服务器来说，A 客户端和 B 客户端的会话需要通过不同的 conv 来区分，否则服务器会无法正确地将数据包发送到对应的客户端。
如果 conv1 和 conv2 相同，服务器就无法区分这两个客户端的通信，可能会导致数据混淆或丢失。

4. 是否可以使用相同的 conv？
对于同一客户端与服务器之间的通信，conv 必须保持一致，即客户端 A 和服务器之间的通信必须使用同一个 conv，客户端 B 与服务器之间的通信也需要使用一个不同的 conv。
对于不同客户端（如 A 和 B）连接同一服务器时，它们的 conv 不能相同，因为这样会导致服务器无法区分不同客户端的数据包。

5. 总结：
每个客户端和服务器的会话都有唯一的 conv，即每个客户端与服务器之间的 conv 是不同的。
同一客户端与不同服务器之间的会话，每个连接需要有唯一的 conv，例如客户端 A 和服务器 1 使用 conv1，客户端 A 和服务器 2 使用 conv2。
不同客户端之间的 conv 必须不同，即 A 客户端和 B 客户端的 conv 需要各自独立

*/




















