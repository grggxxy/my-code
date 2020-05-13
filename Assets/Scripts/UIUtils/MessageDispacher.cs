using System.Collections.Generic;

delegate void MessageDispacherCallBack(object Param);

class MessageDispacher
{
    public static readonly MessageDispacher Instance = new MessageDispacher();

    private Dictionary<object, MessageDispacherCallBack> m_msgCallbackMap = new Dictionary<object, MessageDispacherCallBack>();

    public void Send(object key, object message)
    {
        if (m_msgCallbackMap.ContainsKey(key))
        {
            m_msgCallbackMap[key].Invoke(message);
        }
    }

    public void Register(object key, MessageDispacherCallBack callback)
    {
        if (m_msgCallbackMap.ContainsKey(key))
        {
            m_msgCallbackMap[key] += callback;
        }
        else
        {
            m_msgCallbackMap[key] = callback;
        }
    }

    public void Unregister(object key, MessageDispacherCallBack callback)
    {
        if (m_msgCallbackMap.ContainsKey(key))
        {
            m_msgCallbackMap[key] -= callback;
            if (m_msgCallbackMap[key] is null)
            {
                m_msgCallbackMap.Remove(key);
            }
        }
    }

    public void Clear()
    {
        m_msgCallbackMap.Clear();
    }
}