using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class CountDown : MonoBehaviour
{
    private float time = 3;
    public TMP_Text timeText;

    private void OnEnable()
    {
        time = 3;
        timeText.text = time.ToString()+"�������޵�ʱ��";
        StartCoroutine(Count());
    }

    // Start is called before the first frame update
    void Start()
    {
        //timeText.text = time.ToString();
        //StartCoroutine(Count());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator Count()
    {
        while(time>0)
        {
            yield return new WaitForSeconds(1);
            time--;
            timeText.text = time.ToString() + "�������޵�ʱ��";

        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
