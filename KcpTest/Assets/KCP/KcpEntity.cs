using KcpProject;
using System;

/// <summary>
/// һ�� Kcp ʵ��
/// </summary>
public class KcpEntity
{

    private KCP mKcp = null;
    private uint mNextUpdateTime = 0;

    private Action<byte[]> socketSendEvent;
    private Action<byte[]> socketReceiveEvent;

    public KcpEntity(uint conv, Action<byte[]> socketSendEvent, Action<byte[]> socketReceiveEvent)
    {
        // ʵ���� kcp
        // conv �ͻ��������������һ��
        // SocketSend �� Kcp ��Ϊ������������� ����ʱ���ص� SocketSend���� SocketSend ������ͨ�� �Լ�������Э����� UDP Socket �����������Ϣ
        mKcp = new KCP(conv, ReadySend);
        this.socketSendEvent = socketSendEvent;
        this.socketReceiveEvent = socketReceiveEvent;

        // normal:  0, 40, 2, 1
        // fast:    0, 30, 2, 1
        // fast2:   1, 20, 2, 1
        // fast3:   1, 10, 2, 1
        int nodelay = 0;     // �Ƿ����� nodelayģʽ��0�����ã�1����
        int interval = 30;   // Э���ڲ������� interval����λ���룬���� 10ms���� 20ms
        int resend = 2;      // �����ش�ģʽ��Ĭ��0�رգ���������2��2��ACK��Խ����ֱ���ش���
        int nc = 1;          // �Ƿ�ر����أ�Ĭ����0�����رգ�1����ر�
        mKcp.NoDelay(nodelay, interval, resend, nc);
        mKcp.SetStreamMode(true);

        // ����䵥Ԫ�����㷨Э�鲢������̽�� MTU��Ĭ�� mtu��1400�ֽڣ�����ʹ��ikcp_setmtu�����ø�ֵ��
        // ��ֵ����Ӱ�����ݰ��鲢����Ƭʱ�������䵥Ԫ
        mKcp.SetMtu(512);

        // ��СRTO�������� TCP���� KCP���� RTOʱ������С RTO�����ƣ�����������RTOΪ40ms��
        // ����Ĭ�ϵ� RTO��100ms��Э��ֻ����100ms����ܼ�⵽����������ģʽ��Ϊ30ms�������ֶ����ĸ�ֵ��
    }

    /// <summary>
    /// ��������������ݣ����� mKcp.Send
    /// Kcp ���������������������
    /// ������ kcp ��Ϊ���������������Ϣʱ������ ʵ����ʱ(new KCP(conv, SocketSend);) ����� SocketSend ����
    /// �� SocketSend(byte[] bytes, int length) �н� bytes ͨ�� UDP Socket ���͸�������
    /// bytes �� kcp ������� byteDatas ���ݣ��� byteDatas �ټ��� kcp ����Ϣͷ
    /// bytes �� �ֽ����Ǵ��� byteDatas ��
    /// </summary>
    /// <param name="byteDatas"></param>
    public void Send(byte[] byteDatas)
    {
        mKcp.Send(byteDatas);
    }

    /// <summary>
    /// Kcp ��Ϊ�������������������
    /// </summary>
    /// <param name="byteDatas"> Kcp �������ֽڣ������� Kcp ��Ϣͷ����Ϣ </param>
    /// <param name="length"> ��Ҫ���͵��ֽڳ��� </param>
    private void ReadySend(byte[] byteDatas, int length)
    {
        byte[] bytes = new byte[length];
        Array.Copy(byteDatas, bytes, length);
        this.socketSendEvent?.Invoke(bytes);
    }

    /// <summary>
    /// ��һ��Ƶ�ʵ��� ikcp_update������ kcp״̬�����Ҵ��뵱ǰʱ�ӣ����뵥λ��
    /// �� 10ms����һ�Σ����� ikcp_checkȷ���´ε��� update��ʱ�䲻��ÿ�ε���
    /// </summary>
    public void Update()
    {
        if (0 == mNextUpdateTime || mKcp.CurrentMS >= mNextUpdateTime)
        {
            mKcp.Update();
            // ��ȡ��һ�θ���ʱ��
            mNextUpdateTime = mKcp.Check();
        }

        CheckReceive();
    }

    /// <summary>
    /// UDP Socket ���յ������ݣ���Ҫֱ�Ӵ�������Ҫͨ�� mKcp.Input ���������ݴ��ݸ� Kcp
    /// ��Ϊ byteDatas �� kcp ��װ�����Ϣ����Ҫ kcp ��������ʹ��
    /// byteDatas �а����� Kcp ��װ����Ϣͷ��Kcp ����ͨ���������Ϣͷ����ȷ�ϵ��Ƚ��յ��ǵڼ�����Ϣ��
    /// ��û�ж�����ʱ��
    /// </summary>
    /// <param name="byteDatas"> ���յ������� </param>
    /// <param name="length"> ���յ�����Ч�ֽڸ��� </param>
    public void SocketReceive(byte[] byteDatas, int length)
    {
        bool ackNoDelay = true;
        mKcp.Input(byteDatas, 0, length, true, ackNoDelay);
    }

    /// <summary>
    /// ��� Kcp �Ƿ��н��յ�������
    /// </summary>
    private void CheckReceive()
    {
        for (; ; )
        {
            // �鿴�Ƿ�����Ҫ���յ��ֽڣ�������Ҫ���յ��ֽ�����
            int size = mKcp.PeekSize();
            if (size < 0)
                break;

            byte[] byteDatas = new byte[size];
            // �� Kcp �л�ȡ���յ�����
            int n = mKcp.Recv(byteDatas, 0, size);
            if (n > 0)
            {
                // �� Kcp �ж�ȡ�� ����˷��͵����ݣ������� Kcp ͷ����Ϣ��
                socketReceiveEvent?.Invoke(byteDatas);
            }
        }
    }

}


/*

�� KCP Э���У�conv��conversation ID����������ʶһ��ͨ�ŻỰ��Ψһ��ʶ������� ID �������ֲ�ͬ��ͨ��ͨ���ͻỰ��ȷ����ͬһ�������ӣ���UDP���ӣ��ϣ��������ֲ�ͬ�Ŀͻ��˻�Ự��

1. conv �����ã�
conv ���ڱ�ʶ�ͻ��˺ͷ�����֮��ĻỰ���� KCP Э���У�ÿ���Ự����һ���ͻ��˺ͷ�����֮������ӣ�������һ��Ψһ�� conv ֵ��
���ͻ������������������ʱ���ͻ��˺ͷ���������ʹ����ͬ�� conv ����ʶ����ͨ����·����� conv �����ݰ���ͷ�����д��ݣ���ȷ�������ܹ���ȷ�ش��͵�Ŀ��Ự��

2. conv ��ʹ�ó�����
�ͻ����������֮��ĻỰ��ÿ���ͻ����������֮������ӻ���һ��Ψһ�� conv�����磬�����������ͻ��� A �� B ���ӵ�ͬһ������������ô�ͻ��� A �ͷ�����֮��� conv ������ conv1���ͻ��� B �ͷ�����֮��� conv ������ conv2�������� conv ֵ�ǲ�ͬ�ġ�
ͬһ���ͻ������������������ӣ����ͬһ���ͻ��������������������ӣ���ÿ�����ӣ��ͻ����벻ͬ������֮������ӣ�����һ�������� conv��

3. ���� conv1 �� conv2 �����⣺
��ͬ�ͻ��˵� conv�������ᵽ�ͻ��� A �� B ʱ��ͨ����˵��A �� B �� conv �����ǲ�ͬ�ġ�Ҳ����˵��
�ͻ��� A �ͷ�����������ʹ�� conv1
�ͻ��� B �ͷ�����������ʹ�� conv2
Ϊʲô conv ����Ψһ��
conv ���������ֲ�ͬͨ�ŻỰ�ı�ʶ��������ͬһ����������˵��A �ͻ��˺� B �ͻ��˵ĻỰ��Ҫͨ����ͬ�� conv �����֣�������������޷���ȷ�ؽ����ݰ����͵���Ӧ�Ŀͻ��ˡ�
��� conv1 �� conv2 ��ͬ�����������޷������������ͻ��˵�ͨ�ţ����ܻᵼ�����ݻ�����ʧ��

4. �Ƿ����ʹ����ͬ�� conv��
����ͬһ�ͻ����������֮���ͨ�ţ�conv ���뱣��һ�£����ͻ��� A �ͷ�����֮���ͨ�ű���ʹ��ͬһ�� conv���ͻ��� B �������֮���ͨ��Ҳ��Ҫʹ��һ����ͬ�� conv��
���ڲ�ͬ�ͻ��ˣ��� A �� B������ͬһ������ʱ�����ǵ� conv ������ͬ����Ϊ�����ᵼ�·������޷����ֲ�ͬ�ͻ��˵����ݰ���

5. �ܽ᣺
ÿ���ͻ��˺ͷ������ĻỰ����Ψһ�� conv����ÿ���ͻ����������֮��� conv �ǲ�ͬ�ġ�
ͬһ�ͻ����벻ͬ������֮��ĻỰ��ÿ��������Ҫ��Ψһ�� conv������ͻ��� A �ͷ����� 1 ʹ�� conv1���ͻ��� A �ͷ����� 2 ʹ�� conv2��
��ͬ�ͻ���֮��� conv ���벻ͬ���� A �ͻ��˺� B �ͻ��˵� conv ��Ҫ���Զ���

*/




















