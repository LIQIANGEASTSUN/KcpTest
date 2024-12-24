using System.Text;
using UnityEngine;

public class KcpTest : MonoBehaviour
{
    private KcpEntity kcpEntity;

    // Start is called before the first frame update
    void Start()
    {
        uint conv = (uint)Random.Range(1, uint.MaxValue);
        kcpEntity = new KcpEntity(conv, SocketSend, Receive);
    }

    private uint mNextUpdateTime = 0;

    private int number = 0;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            string msg = $"我是消息:{number}";
            ++number;
            byte[] byteDatas = Encoding.UTF8.GetBytes(msg);

            Debug.LogError("开始发送消息:" + msg);
            Debug.LogError("Send Origin byteLength:" + byteDatas.Length);
            // 测试发送消息
            kcpEntity.Send(byteDatas);
        }

        kcpEntity.Update();
    }

    /// <summary>
    /// 真正向 UDP 服务器发送消息的方法
    /// </summary>
    /// <param name="byteDatas"> Kcp 处理后的字节 </param>
    private void SocketSend(byte[] byteDatas)
    {
        // 此处实现 Udp Socket 向服务器发送消息

        // 由于没有写 UDP Socket 下面方法是模拟 Socket 接收后的处理
        // 假设 UDP Socket 收到了消息 byteDatas
        kcpEntity.SocketReceive(byteDatas, byteDatas.Length);
    }

    /// <summary>
    /// 到这里才是拿到了服务器发送的数据
    /// </summary>
    /// <param name="byteDatas"></param>
    private void Receive(byte[] byteDatas)
    {
        string msg = Encoding.UTF8.GetString(byteDatas);
        Debug.LogError("Receive:" + msg + "     byteLength:" + byteDatas.Length);
    }

}
