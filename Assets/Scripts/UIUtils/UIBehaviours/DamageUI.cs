using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageUI : MonoBehaviour
{
    private float m_timer = 0.0f;
    private float m_time = 1.0f;

    private Text m_text;
    // Start is called before the first frame update
    void Start()
    {
        m_text = GetComponent<Text>();
        Destroy(this.gameObject, m_time);
    }

    // Update is called once per frame
    void Update()
    {
        Scroll();
    }

    void Scroll()
    {
        this.transform.Translate(Vector3.up * 1.5f * Time.deltaTime);
        m_timer += Time.deltaTime;

        // m_text.fontSize--;
        m_text.color = new Color(1, 0, 0, 1 - m_timer);
    }
}
