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
            string msg = $"������Ϣ:{number}";
            ++number;
            byte[] byteDatas = Encoding.UTF8.GetBytes(msg);

            Debug.LogError("��ʼ������Ϣ:" + msg);
            Debug.LogError("Send Origin byteLength:" + byteDatas.Length);
            // ���Է�����Ϣ
            kcpEntity.Send(byteDatas);
        }

        kcpEntity.Update();
    }

    /// <summary>
    /// ������ UDP ������������Ϣ�ķ���
    /// </summary>
    /// <param name="byteDatas"> Kcp �������ֽ� </param>
    private void SocketSend(byte[] byteDatas)
    {
        // �˴�ʵ�� Udp Socket �������������Ϣ

        // ����û��д UDP Socket ���淽����ģ�� Socket ���պ�Ĵ���
        // ���� UDP Socket �յ�����Ϣ byteDatas
        kcpEntity.SocketReceive(byteDatas, byteDatas.Length);
    }

    /// <summary>
    /// ����������õ��˷��������͵�����
    /// </summary>
    /// <param name="byteDatas"></param>
    private void Receive(byte[] byteDatas)
    {
        string msg = Encoding.UTF8.GetString(byteDatas);
        Debug.LogError("Receive:" + msg + "     byteLength:" + byteDatas.Length);
    }

}
